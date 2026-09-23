---
name: chillsharp-documentation
description: Create or update ChillSharp repository documentation under doc with a mirrored Italian translation and reciprocal language switchers.
---

# ChillSharp Documentation

Use this skill for every Markdown change below `doc/`, including new guides and translated documentation.

## Localization contract

- The English source lives at `doc/<relative-path>.md`.
- Create and maintain its Italian translation at `doc/it/<relative-path>.md`; preserve the same relative directory and filename.
- Each document must start with its title and then a reciprocal language switcher within the opening section:
  - English: `Versione italiana: [Italiano](<relative link to doc/it/...>)`
  - Italian: `English version: [English](<relative link to doc/...>)`
- Translate prose, headings, image alt text, and link text for the Italian version. Keep code, commands, identifiers, file paths, URLs, and literal API payload fields unchanged unless their surrounding explanatory prose requires a translation.
- When an English document changes, apply the equivalent semantic change to its Italian counterpart in the same task. Do not leave a placeholder, an English duplicate, or a stale translation.

## Workflow

1. For a new document, create the English document and its Italian counterpart together.
2. Keep internal links valid from both mirrored locations; use relative Markdown links.
3. Run `python .agents/skills/chillsharp-documentation/scripts/check_localized_docs.py doc` before handing off documentation changes. Resolve every reported issue.

The checker verifies structure and switchers. Review the translation itself for meaning, terminology, and code fidelity.
