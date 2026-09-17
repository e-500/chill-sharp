#!/usr/bin/env python3
"""Squash this week's commits into one safely reviewable commit.

The command is deliberately a dry run unless --apply is supplied. With no
explicit base it selects the final commit before the current ISO week began.
"""

from __future__ import annotations

import argparse
from collections import defaultdict
import datetime as dt
import subprocess
import sys
from dataclasses import dataclass


class SquashError(RuntimeError):
    pass


def git(*args: str, check: bool = True, input_text: str | None = None) -> str:
    result = subprocess.run(
        ["git", *args], text=True, input=input_text, capture_output=True, check=False
    )
    if check and result.returncode:
        raise SquashError(result.stderr.strip() or "git " + " ".join(args) + " failed")
    return result.stdout.strip()


def one_line(text: str) -> str:
    return " ".join(text.split())


def weekly_subject(today: dt.date) -> str:
    iso_year, week, _ = today.isocalendar()
    return f"Cumulative commit ({iso_year}) WEEK #{week}"


@dataclass(frozen=True)
class Commit:
    sha: str
    subject: str
    committed_on: dt.date | None = None


def commits_after(base: str) -> list[Commit]:
    # Git log's default newest-first order matches the message requested by the
    # caller: each later entry is an older squashed commit.
    output = git("log", "--format=%H%x1f%s", f"{base}..HEAD")
    if not output:
        return []
    return [
        Commit(sha, one_line(subject))
        for line in output.splitlines()
        for sha, subject in [line.split("\x1f", 1)]
    ]


def all_commits() -> list[Commit]:
    """Return every reachable commit, newest first, including its ISO date."""
    output = git("log", "--date-order", "--format=%H%x1f%cI%x1f%s", "HEAD")
    if not output:
        return []
    return [
        Commit(sha, one_line(subject), dt.date.fromisoformat(committed_at[:10]))
        for line in output.splitlines()
        for sha, committed_at, subject in [line.split("\x1f", 2)]
    ]


def weekly_groups(commits: list[Commit]) -> list[tuple[tuple[int, int], list[Commit]]]:
    """Group a newest-first history into chronological ISO-week buckets."""
    groups: dict[tuple[int, int], list[Commit]] = defaultdict(list)
    for commit in commits:
        if commit.committed_on is None:
            raise SquashError("Full-history commits must include a commit date.")
        iso_year, week, _ = commit.committed_on.isocalendar()
        groups[(iso_year, week)].append(commit)
    return sorted(groups.items())


def weekly_message(week: tuple[int, int], commits: list[Commit]) -> str:
    year, number = week
    return f"Cumulative commit ({year}) WEEK #{number}\n\n" + "\n".join(
        f"- {commit.subject}" for commit in commits
    )


def validate_repository(require_clean_tree: bool) -> None:
    if git("rev-parse", "--is-inside-work-tree") != "true":
        raise SquashError("Run this command inside a Git working tree.")
    if require_clean_tree and git("status", "--porcelain"):
        raise SquashError("Working tree is not clean; commit or stash changes first.")
    if git("symbolic-ref", "--quiet", "--short", "HEAD", check=False) == "":
        raise SquashError("HEAD is detached; switch to the branch to be squashed first.")


def find_base(explicit_base: str | None, today: dt.date) -> str:
    if explicit_base:
        return git("rev-parse", "--verify", explicit_base)

    monday = today - dt.timedelta(days=today.weekday())
    base = git(
        "rev-list", "-1", f"--before={monday.isoformat()}T00:00:00", "HEAD"
    )
    if not base:
        raise SquashError(
            "No commit exists before this ISO week. Use --base-ref <commit> to "
            "choose a retained boundary explicitly."
        )
    return base


def commit_message(today: dt.date, commits: list[Commit]) -> str:
    return weekly_subject(today) + "\n\n" + "\n".join(
        f"- {commit.subject}" for commit in commits
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--base-ref", help="Last commit to retain; overrides the automatic weekly boundary."
    )
    parser.add_argument(
        "--all",
        action="store_true",
        help="Rewrite all reachable history as one cumulative commit per ISO week.",
    )
    parser.add_argument("--apply", action="store_true", help="Perform the local rewrite.")
    parser.add_argument(
        "--push",
        action="store_true",
        help="Push after applying, using git push --force-with-lease.",
    )
    parser.add_argument(
        "--date",
        type=dt.date.fromisoformat,
        help="ISO date used in the message (useful for testing).",
    )
    parser.add_argument(
        "--branch", help="Require the currently checked-out branch to have this name.")
    args = parser.parse_args()
    if args.push and not args.apply:
        parser.error("--push requires --apply")
    if args.all and args.base_ref:
        parser.error("--all cannot be combined with --base-ref")

    try:
        validate_repository(args.apply)
        branch = git("branch", "--show-current")
        if args.branch and branch != args.branch:
            raise SquashError(f"Expected branch {args.branch!r}, but HEAD is on {branch!r}.")
        today = args.date or dt.date.today()
        base = None if args.all else find_base(args.base_ref, today)
        commits = all_commits() if args.all else commits_after(base)
        if not commits:
            raise SquashError("There are no commits to squash after the selected base.")

        print(f"Branch: {branch}")
        print("Retained base: none (full-history rewrite)" if args.all else f"Retained base: {base}")
        print(f"Commits to squash: {len(commits)}")
        groups = weekly_groups(commits) if args.all else []
        if args.all:
            print(f"Weekly commits to create: {len(groups)}")
            print("\nProposed weekly commit messages:\n")
            for week, group in groups:
                print(weekly_message(week, group))
                print()
        else:
            message = commit_message(today, commits)
            print("\nProposed commit message:\n")
            print(message)

        if not args.apply:
            print("\nDry run only. Re-run with --apply after reviewing this output.")
            return 0

        if args.all:
            # commit-tree creates a linear history. Each weekly tree is a snapshot
            # of that week's latest commit; the final one always uses HEAD's tree,
            # preserving the checked-out files exactly.
            old_head = git("rev-parse", "HEAD")
            parent: str | None = None
            for index, (week, group) in enumerate(groups):
                latest = max(group, key=lambda commit: commit.committed_on)
                tree = "HEAD^{tree}" if index == len(groups) - 1 else f"{latest.sha}^{{tree}}"
                command = ["commit-tree", tree]
                if parent:
                    command.extend(["-p", parent])
                parent = git(*command, "-F", "-", input_text=weekly_message(week, group) + "\n")
            git("update-ref", f"refs/heads/{branch}", parent, old_head)
        else:
            # --soft preserves the exact combined index and working-tree state.
            git("reset", "--soft", base)
            git("commit", "-F", "-", input_text=message + "\n")
        print("\nLocal history was rewritten successfully.")

        if args.push:
            git("push", "--force-with-lease", "origin", branch)
            print(f"Updated origin/{branch} with --force-with-lease.")
        else:
            print("Run `git push --force-with-lease origin " + branch + "` after review.")
        return 0
    except SquashError as error:
        print(f"error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
