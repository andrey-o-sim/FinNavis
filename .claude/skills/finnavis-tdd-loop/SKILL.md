---
name: finnavis-tdd-loop
description: >
  Runs every todo task of a FinNavis feature epic through finnavis-tdd, one task after another, in
  dependency order. Reads docs/features/<slug>/tasks/tasks.json and tracker.md through a bundled
  planner script, which picks the todo tasks of a layer finnavis-tdd takes (domain, app, infra,
  wiring), orders them by their deps, puts first a task that a stopped run left half-done, and
  lists every task it skips with the reason — a docs, migration, ports or tests layer, or a
  dependency that will not be done. Then it calls finnavis-tdd once per task, checks after each one
  that the task is done and the tree is clean, and stops the whole loop on the first failure. Ends
  with one summary of commits, skipped tasks and follow-ups. Use this whenever the user wants to
  implement all, the rest, or many tasks of an epic test-first in this repo — including phrasings
  like "/finnavis-tdd-loop csv-import", "прогони TDD по всем задачам csv-import", "сделай все todo
  задачи эпика через TDD", "запусти tdd для каждой задачи", "TDD loop для {slug}", "что осталось
  сделать по TDD в {slug}" (that one with --dry-run) — even when they do not say the word loop.
  For one task, use finnavis-tdd. For docs, migration or ports tasks, use finnavis-workflow.
argument-hint: "<slug> [--dry-run]"
allowed-tools: Bash, Read, Skill
---

# finnavis-tdd-loop — run finnavis-tdd over a whole epic

`finnavis-tdd` runs one task. An epic has ten or twenty, and starting each one by hand is the only
thing this skill removes. It adds no rule of its own about how a task is built. Every phase, gate,
refusal and commit still comes from `finnavis-tdd`, so there is one place to change them.

The loop runs in the main context and calls `finnavis-tdd` through the Skill tool. It cannot hand
each task to a sub-agent: `finnavis-tdd` starts its own three agents, and a sub-agent cannot start
agents.

Talk to the owner in the language they wrote in (`.claude/rules/language.md`). Commits and every
file stay English. The fixed messages in this file are English to match the repo: say them in the
owner's language. The planner output is data: show it as it is, in a code block, and write your
own lines around it in the owner's language.

## Inputs

`$ARGUMENTS` = `<slug> [--dry-run]`, for example `csv-import`.

- `<slug>` — a folder under `docs/features/`. If it is missing, list the folders that have a
  `tasks/tasks.json` and ask.
- `--dry-run` — print the plan and stop. Change nothing.

`--review-tests` is not supported: it stops after RED by design, so a loop would stop on every
task. If the owner asks for it, tell them to run `/finnavis-tdd <slug> <id> --review-tests` per
task instead.

## The planner

```
PLAN = .claude/skills/finnavis-tdd-loop/scripts/plan.py
python "$PLAN" <slug> [--dry-run]
```

Pass `--dry-run` to the planner only when the owner passed it. Then a stopped run of another
feature at HEAD prints a `Warning:` line above the plan instead of `ERROR:`. A real run must stop
there, because its first task would start on top of that feature's half-done commits.

It reads `tasks.json`, `tracker.md` and the `Task:` trailer of HEAD. It prints how many tasks are
already done, then four lists: the
tasks to run in order, the tasks skipped for their layer, the tasks skipped because a dependency
will not be done, and the tasks left alone because their status is not `todo`. Its last line is
one of:

- `NEXT: <id>` — the task to run now.
- `NEXT: none` — nothing left that `finnavis-tdd` can take.
- `ERROR: <text>` — stop and show the text. For example, HEAD is a stopped run of another feature.

Trust the planner's order. Do not reorder by task number, and do not add a task it skipped. The
order comes from `deps`, and a task whose dependency is a docs task cannot be built before that
docs task is done by hand.

## Pre-flight

Run these once, before the first task. On the first failure, stop with the message shown.
With `--dry-run`, checks 1 and 2 do not stop anything: show their message as a warning above the
plan, because a dry run changes nothing and the owner may want the plan while still working.

1. `git status --porcelain` is empty.
   Else: `Working tree is not clean. Commit or stash the changes, then run /finnavis-tdd-loop again.`
2. `git branch --show-current` is not `master`.
   Else: `TDD commits never go to master (.claude/rules/git.md). Create a work branch first.`
3. Run the planner. Show the owner its full output as the plan.
   - `ERROR:` → stop with the text.
   - `NEXT: none` without `--dry-run` → say there is nothing to run, and stop.
4. With `--dry-run`, stop here. End with exactly one line:
   - the planner printed `Warning:` → `Dry run, nothing changed. A real run would stop: <the
     warning text>.`
   - else `NEXT: <id>` → `Dry run, nothing changed. The loop would run <n> tasks, starting with
     <id>.`
   - else `NEXT: none` → `Dry run, nothing changed. The loop has nothing to run.`

`finnavis-tdd` runs its own pre-flight for each task too — Docker, dependencies, the green
baseline. The loop does not repeat those checks.

## The loop

Repeat until the planner prints `NEXT: none`:

1. Run the planner. Take `<id>` from `NEXT: <id>`. On `ERROR:`, stop the loop.
2. Print one line: `[<n>/<total>] <id> — <title>`. `<total>` is the length of the run list from
   the first plan.
3. Call the Skill tool: `skill: finnavis-tdd`, `args: <slug> <id>`. Follow it to its end, exactly
   as if the owner had typed `/finnavis-tdd <slug> <id>`.
4. Do not print its full final report now. Keep from it: the commit SHAs and subjects, the
   `DoD coverage` block, and every `ALREADY_GREEN:` and `FOLLOW_UP:` line. They go into the
   summary.
5. Check that the task really finished:
   - `git status --porcelain` is empty, and
   - the `<id>` row in `tracker.md` says `done`.

   Both must hold. If `finnavis-tdd` stopped — a refusal, a failed gate, Docker not running — or
   either check fails, stop the whole loop and go to "Stopped summary".
6. Print one line: `[<n>/<total>] <id> done — <short SHA of the tracker commit>`.

Why the loop stops on the first failure instead of moving on: a failed gate leaves the RED or GREEN
commit of that task at HEAD. The next task would start on top of half-finished code, and its
baseline gate would not be green anyway. The owner decides what to do; the next
`/finnavis-tdd-loop <slug>` resumes that task first, because the planner sees its trailer on HEAD.

Why the planner runs again before every task, not only once: it reads `tracker.md` fresh. If a
task did not reach `done`, the plan in memory would be wrong, and the planner is cheap.

## Summary

### Finished summary

When the planner prints `NEXT: none`:

```
TDD loop complete for <slug>: <n> tasks done.

Done:
  <id>  <title>
        RED       <sha>  <subject>
        GREEN     <sha>  <subject>
        REFACTOR  <sha>  <subject>      (or: no change)
        TRACKER   <sha>  docs(<slug>): mark <id> done
  ...

DoD coverage:
  <id>: <the DOD COVERAGE block>
  ...

Not run by this loop:
  <the three skipped lists from the last planner output, with their reasons>

Follow-ups for the owner:
  <id>: <every ALREADY_GREEN and FOLLOW_UP line>      (or: none)

Before the PR: run ./build.sh test. The gates ran only the test projects of each task's layer.
```

### Stopped summary

When the loop stopped on a task:

```
TDD loop stopped at <id> — <title>.

Reason: <the failure message from finnavis-tdd, or the failed check from step 5>

Done before the stop (<n>):
  <same block per task as in the finished summary>

Not started: <the ids still in the run list>

To continue after you fix the cause: /finnavis-tdd-loop <slug>
  (the planner puts <id> first if its RED or GREEN commit is still at HEAD)
```

Never push and never open a PR. Both need the owner's approval (`.claude/rules/git.md`).

## Anti-patterns

- **Do not do any phase work yourself.** The loop only calls `finnavis-tdd`. If a task looks easy,
  it still goes through all three agents and all three gates.
- **Do not repair a failed task and go on.** `finnavis-tdd` already says not to repair a failed
  phase. The loop stops; it does not retry, reset, or skip ahead.
- **Do not run a skipped task.** A docs, migration, ports or tests task is outside what
  `finnavis-tdd` can do. Name it in the summary and leave it to the owner or finnavis-workflow.
- **Do not edit `tracker.md` yourself.** `finnavis-tdd` marks the task done. The loop only reads.
- **Do not ask between tasks.** The owner started a loop to avoid starting each task. Ask only when
  the slug is missing.

## Examples

```
User: /finnavis-tdd-loop csv-import --dry-run
You:  pre-flight → planner → show the plan: run T10 (marked as resuming a stopped run), T11,
      T12, T13, T15, T18, T19; skip T01, T02, T20–T27 for their layer; skip T14, T16, T17
      (wait for T01). End: "The loop would run 7 tasks, starting with T10." Stop.

User: /finnavis-tdd-loop core-ledger --dry-run      (HEAD is a stopped run of csv-import/T10)
You:  the planner prints a Warning line, then the plan. End: "A real run would stop: HEAD is a
      stopped run of csv-import/T10 …". Stop.

User: /finnavis-tdd-loop csv-import
You:  pre-flight → plan → [1/7] T10 via finnavis-tdd → check → [2/7] T11 → … → [7/7] T19
      → planner says NEXT: none → finished summary.

User: /finnavis-tdd-loop csv-import          (Gate 2 fails on T12)
You:  … [3/7] T12 → finnavis-tdd stops at Gate 2 → stopped summary: T10, T11 done;
      T13, T15, T18, T19 not started.

User: /finnavis-tdd-loop core-ledger
You:  plan says NEXT: none — only ports, tests and docs tasks are left. Show the plan. Stop.
      (If HEAD is a stopped run of csv-import, the planner prints ERROR instead: stop with it.
      Only --dry-run turns that ERROR into a Warning.)
```
