# `--reconcile`

Run when `tasks/` exists and something upstream moved — a PRD amendment, a new acceptance
criterion, a tightened data model, a contract regenerated with `--reconcile`, a new ADR.

The point is narrow: **change only what actually differs, and never lose work already done.**

## What is preserved, always

These survive a reconcile untouched. Overwriting any of them is the failure this mode exists to
prevent.

| Preserved | Why |
|---|---|
| `status` of every task in `tracker.md` and in each task's frontmatter | the implement stage wrote them; they are the only record of what is built |
| Task **ids** | they are referenced from `deps`, from `_epic.md`, from commit messages and possibly from a branch name |
| A task whose `status` is not `todo` | its content described work that is done or in flight. Changing it rewrites history |
| Hand edits to Notes and Why | the user wrote them for a reason the upstream documents do not carry |
| The id of a dropped task | the number is retired, not reused and not renumbered. `T07` missing is information |

## What is re-derived

Everything else, from the upstream documents as they stand now:

- the task set — new tasks appended with the next free ids;
- `acs`, `dod`, `files_hint`, `deps`, `estimate` of tasks still at `todo`;
- the DAG, the parallel branches and the lanes;
- `_epic.md` scope, hard rules and parked items;
- the coverage line.

## Protocol

1. **Read the existing breakdown first** — `tasks.json`, `tracker.md`, every task's frontmatter.
   Build the current model before reading anything upstream, so the diff has two sides.
2. **Re-derive from scratch in memory.** Do not patch the old model; produce the new one the way a
   first run would, then compare.
3. **Classify every difference** into one of five buckets:

   | Bucket | Action |
   |---|---|
   | New task | append with the next free id, `status: todo` |
   | Changed task, `status: todo` | rewrite it, and name what changed in the report |
   | Changed task, `status` beyond `todo` | **do not rewrite.** Report it as a finding — the built thing and the design now disagree |
   | Dropped task, `status: todo` | remove the file, retire the id, say why |
   | Dropped task, `status` beyond `todo` | **do not remove.** Report it — work was done that the design no longer asks for |

4. **Re-check coverage.** A new acceptance criterion with no task is the most common reason to run
   this mode, and the one worth stating first.
5. **Report before writing.** One table: bucket, task, what moved upstream. Then ask whether to
   apply — the same two-option checkpoint a first run uses for the task list.
6. **Write, then re-run the self-check** in the skill's Definition of Done.

## Findings that stop the run

- A task beyond `todo` that the new design contradicts. The user has to decide whether to revert
  the code or amend the design. Neither is this skill's call.
- A dependency cycle introduced by a new task.
- A PRD §5 criterion that lost its only task because that task was dropped.

Report these and stop. A reconcile that half-applies is worse than one that refuses, because the
next run has no clean side to diff against.

## What this mode is not

- **Not a status update.** The implement stage owns `tracker.md` statuses. This mode reads them and
  puts them back unchanged.
- **Not a rewrite.** If the feature changed so much that most tasks move, say so and offer a clean
  run after the user has deleted `tasks/` on purpose.
- **Not a way to edit one task.** Open the file and edit it. Then run `--reconcile` if you want the
  DAG and `tasks.json` brought back in line.
