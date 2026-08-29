#!/usr/bin/env python3
"""Validate the mirrored English/Italian Markdown documentation layout."""

from __future__ import annotations

import sys
import re
from pathlib import Path


def has_switcher(path: Path, label: str, target: Path) -> bool:
    lines = path.read_text(encoding="utf-8").splitlines()
    pattern = re.compile(rf"\[{re.escape(label)}\]\(([^)]+)\)")
    for line in lines[:12]:
        match = pattern.search(line)
        if match and (path.parent / match.group(1)).resolve() == target.resolve():
            return True
    return False


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: check_localized_docs.py <doc-root>", file=sys.stderr)
        return 2

    doc_root = Path(sys.argv[1]).resolve()
    italian_root = doc_root / "it"
    if not doc_root.is_dir():
        print(f"Documentation root does not exist: {doc_root}", file=sys.stderr)
        return 2

    issues: list[str] = []
    english_documents = sorted(
        path for path in doc_root.rglob("*.md") if italian_root not in path.parents
    )

    for english_path in english_documents:
        relative_path = english_path.relative_to(doc_root)
        italian_path = italian_root / relative_path
        if not italian_path.is_file():
            issues.append(f"Missing Italian translation: it/{relative_path.as_posix()}")
            continue

        if not has_switcher(english_path, "Italiano", italian_path):
            issues.append(f"Missing or invalid English switcher: {relative_path.as_posix()}")

        if not has_switcher(italian_path, "English", english_path):
            issues.append(f"Missing or invalid Italian switcher: it/{relative_path.as_posix()}")

    if italian_root.is_dir():
        for italian_path in sorted(italian_root.rglob("*.md")):
            relative_path = italian_path.relative_to(italian_root)
            if not (doc_root / relative_path).is_file():
                issues.append(f"Italian document has no English source: it/{relative_path.as_posix()}")

    if issues:
        print("Documentation localization check failed:")
        for issue in issues:
            print(f"- {issue}")
        return 1

    print(f"Documentation localization check passed for {len(english_documents)} document(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
