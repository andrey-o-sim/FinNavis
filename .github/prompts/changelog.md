You build the changelog for one merge into `master`. You run headless inside GitHub Actions, so
nobody can answer a question during this run.

Read `.claude/skills/finnavis-changelog/SKILL.md` and follow it in merge-commit mode, as if the
user had typed `/finnavis-changelog <merge sha>`. The merge sha is given at the end of this prompt.
The skill is the only source of the rules: what is notable, which section an entry goes to, and
which version comes next.

## What is different in this run

- **Nobody can confirm anything.** Where the skill says to ask the user or to wait for a
  confirmation, do not write the file. Return `status: stopped` and give the reason in `report`.
  This covers an octopus merge, a release dated after this one, and a commit from another scope
  that may belong to the feature.
- **You are on `master`. Write `CHANGELOG.md` anyway.** The workflow moves the change to a
  Release-PR branch, and the owner reviews it there before it reaches `master`.
- **Do not commit, push or tag.** Do not change any file except `CHANGELOG.md`.
- **The report goes into the `report` field, not into the chat.** Write it in English, as short
  Markdown. Include items 2–5 of the skill's Step 8 (Report): the version and the entry that
  decided the bump, the entries, the out-list, and the judgement calls. Leave out item 6 (Next
  step): the workflow does that part.

## Final output

Return a JSON object that matches the requested schema:

- `status`:
  - `written` — you added a new release to `CHANGELOG.md`;
  - `nothing_notable` — after Step 3 (Decide what is notable) nothing was left, so the file is
    unchanged;
  - `stopped` — the skill told you to stop or to ask, so the file is unchanged.
- `version`: the new version as `X.Y.Z` when `status` is `written`. Otherwise an empty string.
- `report`: the report described above. For `nothing_notable` and `stopped`, say why.
