# Repository maintenance tools

## Weekly commit squash

`weekly_squash.py` replaces all commits made in the current ISO calendar week
(Monday through the time it runs) with one commit. It always prints the
proposed commit message first and does nothing unless `--apply` is specified.
The message lists commit subjects from newest to oldest, with whitespace
flattened to a single line.

First, inspect the planned rewrite on the current `development` branch:

```powershell
py tools\weekly_squash.py --branch development
```

If you want to test a different group, pass `--base-ref` with the commit that
must remain at the bottom of the rewritten history.

To preview a clean, linear weekly history for the complete reachable history:

```powershell
py tools\weekly_squash.py --branch development --all
```

`--all` has no retained base: it replaces the entire branch history with one
commit for each ISO week. Every weekly message includes that week's commit
subjects, newest first. The final commit preserves the current files exactly.
It cannot be combined with `--base-ref`.

After reviewing the dry run, make a recoverable remote backup and rewrite:

```powershell
git branch backup/development-before-weekly-squash
git push origin backup/development-before-weekly-squash
py tools\weekly_squash.py --branch development --apply --push
```

For the complete-history version, use:

```powershell
py tools\weekly_squash.py --branch development --all --apply --push
```

`--push` uses `--force-with-lease`, so it refuses to overwrite a remote branch
that has advanced since the last fetch. Run `git fetch origin` just before the
final command.

To register the 12:00 PM Saturday Windows task:

```powershell
powershell -ExecutionPolicy Bypass -File tools\install_weekly_squash_task.ps1
```

The scheduled task invokes `py tools\weekly_squash.py --branch development --apply --push` from the
repository. Ensure the machine is on, the `development` checkout is clean, and
GitHub permits force pushes to `development`.
