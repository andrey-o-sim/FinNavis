---
name: finnavis-tdd-loop
description: >
  Runs every todo task of a FinNavis feature epic through finnavis-tdd, one task after another, in
  dependency order. Reads docs/features/<slug>/tasks/tasks.json and tracker.md through a bundled
  planner script, which picks the todo tasks of a layer finnavis-tdd takes (domain, app, infra,
  wiring), orders them by their deps, puts first a task that an interrupted run left half-done, and
  lists every task it skips with the reason. Calls finnavis-tdd once per task. When a task fails,
  it rolls the task's commits back with git revert and records why; by default it then stops, and
  with --continue-on-error it goes on with the tasks that do not depend on the failed one. Writes
  one report per task to tasks/reports/<id>.md and, at the end of the run, a Haiku agent appends
  the run's summary — done, failed, needs attention, changed since an earlier run — to
  tasks/reports/final-report.md. Everything is committed. Use this whenever the user wants to
  implement all, the rest, or many tasks of an epic test-first in this repo — including phrasings
  like "/finnavis-tdd-loop csv-import", "прогони TDD по всем задачам csv-import", "сделай все todo
  задачи эпика через TDD", "запусти tdd для каждой задачи", "прогони без остановок", "TDD loop для
  {slug}", "что осталось сделать по TDD в {slug}" (that one with --dry-run) — even when they do not
  say the word loop. For one task, use finnavis-tdd. For docs, migration or ports tasks, use
  finnavis-workflow.
argument-hint: "<slug> [--continue-on-error] [--dry-run]"
allowed-tools: Bash, Read, Write, Edit, Skill, Agent
---

# finnavis-tdd-loop — run finnavis-tdd over a whole epic

`finnavis-tdd` runs one task. An epic has ten or twenty, and starting each one by hand is what this
skill removes. It adds no rule of its own about how a task is built. Every phase, gate, refusal and
commit of a task still comes from `finnavis-tdd`, so there is one place to change them. What the
loop adds is around the tasks: the order, what happens after a failure, and the reports.

The loop runs in the main context and calls `finnavis-tdd` through the Skill tool. It cannot hand
each task to a sub-agent: `finnavis-tdd` starts its own three agents, and a sub-agent cannot start
agents.

Talk to the owner in the language they wrote in (`.claude/rules/language.md`). Commits and every
file stay English. The fixed messages in this file are English to match the repo: say them in the
owner's language. The planner output is data: show it as it is, in a code block, and write your
own lines around it in the owner's language.

## Inputs

`$ARGUMENTS` = `<slug> [--continue-on-error] [--dry-run]`, for example `csv-import`.

- `<slug>` — a folder under `docs/features/`. If it is missing, list the folders that have a
  `tasks/tasks.json` and ask. This is the only question the loop ever asks.
- `--continue-on-error` — after a task fails and is rolled back, go on with the next task instead
  of stopping. The owner uses it to leave a long run unattended. The gates are not softened: a
  failed task is still failed, rolled back and reported.
- `--dry-run` — print the plan and stop. Change nothing, write no report.

`--continue-on-error` cannot stop Claude Code from asking for a permission. That prompt comes from
the harness, not from this skill. If the owner wants a run with no prompts at all, point them to
auto mode or an allowlist (`/fewer-permission-prompts`).

`--review-tests` is not supported: it stops after RED by design, so a loop would stop on every
task. If the owner asks for it, tell them to run `/finnavis-tdd <slug> <id> --review-tests` per
task instead.

## Fixed values

```
TASKS_DIR   = docs/features/<slug>/tasks
REPORTS_DIR = docs/features/<slug>/tasks/reports
PLAN        = .claude/skills/finnavis-tdd-loop/scripts/plan.py
REPORTER    = .claude/skills/finnavis-tdd-loop/agents/final-reporter.md
RUN_STARTED   = date '+%Y-%m-%d %H:%M', taken once in pre-flight
RUN_START_SHA = git rev-parse HEAD, taken once in pre-flight
FAILED        = the ids that failed in this run
TRIED         = the ids this run called finnavis-tdd for, in order
```

A long run can be compacted, and a list kept only in memory is lost with it. So `FAILED` and
`TRIED` are read from git, not remembered. Every attempt ends with a report commit, and every
failed attempt with a rollback commit:

```bash
# TRIED, oldest first
git log --reverse --format=%s RUN_START_SHA..HEAD \
  | sed -n 's/^docs(<slug>): record the \(T[0-9]*\) tdd run$/\1/p'
# FAILED
git log --format=%s RUN_START_SHA..HEAD \
  | sed -n 's/^chore(<slug>): roll back the failed \(T[0-9]*\) attempt$/\1/p'
```

Print `RUN_STARTED` and `RUN_START_SHA` in the first progress line, so they stay in the
conversation even after a compaction.

## The planner

```
python "$PLAN" <slug> [--dry-run] [--failed <FAILED, comma-separated>]
```

It reads `tasks.json`, `tracker.md` and the `Task:` trailer of HEAD. It prints how many tasks are
already done, the tasks to run in order, the tasks that failed in this run, the tasks skipped for
their layer, the tasks skipped because a dependency will not be done, and the tasks left alone
because their status is not `todo`. Its last line is one of:

- `NEXT: <id>` — the task to run now.
- `NEXT: none` — nothing left that `finnavis-tdd` can take.
- `ERROR: <text>` — stop and show the text. For example, HEAD is a stopped run of another feature.

Pass `--failed` whenever `FAILED` is not empty. A failed task stays `todo` in `tracker.md`, so
without the flag the planner would offer it again, and would run the tasks that depend on it.

Pass `--dry-run` to the planner only when the owner passed it. Then a stopped run of another
feature at HEAD prints a `Warning:` line above the plan instead of `ERROR:`. A real run must stop
there, because its first task would start on top of that feature's half-done commits.

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
   - `NEXT: none` without `--dry-run` → say there is nothing to run, and stop. No report: the run
     tried nothing.
4. With `--dry-run`, stop here. End with exactly one line:
   - the planner printed `Warning:` → `Dry run, nothing changed. A real run would stop: <the
     warning text>.`
   - else `NEXT: <id>` → `Dry run, nothing changed. The loop would run <n> tasks, starting with
     <id>.`
   - else `NEXT: none` → `Dry run, nothing changed. The loop has nothing to run.`
5. Take `RUN_STARTED` and `RUN_START_SHA`. Print:
   `Run started <RUN_STARTED> at <short RUN_START_SHA>.`

`finnavis-tdd` runs its own pre-flight for each task too — Docker, dependencies, the green
baseline. The loop does not repeat those checks.

## The loop

Repeat until the planner prints `NEXT: none`:

1. Run the planner with `--failed`. For the first task, reuse the pre-flight output. Take `<id>`
   from `NEXT: <id>`. On `ERROR:`, stop the loop and go to "End of the run".
2. Print one line: `[<n>/<total>] <id> — <title>`. `<n>` is the number of tasks in `TRIED` plus
   one. `<total>` is `<n> - 1` plus the length of the planner's current run list, so it drops when
   a failure makes the planner skip the tasks that depend on it.
3. Take `TASK_START`, the commit the task starts from. Usually that is HEAD. When the planner
   marked the task as resuming an interrupted run, the task's earlier commits are already on top,
   so walk back past them:

   ```bash
   TASK_START=$(git log --format='%H %(trailers:key=Task,valueonly,separator=%x2C)' \
     | awk -v t="<slug>/<id>" '$2 != t { print $1; exit }')
   ```

   A rollback needs this commit. Without it, the loop could not tell the task's own commits from
   the ones before it.
4. Call the Skill tool: `skill: finnavis-tdd`, `args: <slug> <id>`. Follow it to its end, exactly
   as if the owner had typed `/finnavis-tdd <slug> <id>`. When it says "stop", the task has
   stopped, not the loop: come back here for step 5. Do not print its final report in the chat;
   it goes into the task report.
5. Check that the task really finished. Both must hold:
   - `git status --porcelain` is empty, and
   - the `<id>` row in `tracker.md` says `done`.
6. Finished → write the task report with `Status: done` ("The task report") and commit it.
   Print `[<n>/<total>] <id> done — <short SHA of the tracker commit>`.

   Not finished — a refusal, a failed gate, Docker not running, or a check in step 5 — → follow
   "When a task fails". Print `[<n>/<total>] <id> failed — <one-line reason>`.

The planner runs again before every task because it reads `tracker.md` fresh. If a task did not
reach `done`, a plan kept in memory would be wrong, and the planner is cheap.

## When a task fails

Run these steps in order, with or without `--continue-on-error`.

1. **Keep uncommitted work.** If `git status --porcelain` is not empty, an agent stopped in the
   middle of its phase. Do not delete that work; move it aside:

   ```bash
   git stash push --include-untracked -m "finnavis-tdd-loop <slug>/<id> failed attempt <RUN_STARTED>"
   ```

   The stash name goes into the task report, so the owner can `git stash list` and look at it.
2. **Roll back the task's commits.** List them with `git log --format='%h %s' TASK_START..HEAD`.
   If there are any:

   ```bash
   git revert --no-commit TASK_START..HEAD
   git commit -m "chore(<slug>): roll back the failed <id> attempt" \
     -m "<the one-line reason>" \
     -m "The attempt stays in history: <the short SHAs, oldest first>."
   ```

   Then `git diff --stat TASK_START HEAD` must be empty: the tree is back where the task started.

   If the revert hits a conflict, run `git revert --abort` and stop at once, even with
   `--continue-on-error`. Commit nothing more: no task report and no final report. The task's
   commits are still at HEAD, and a report commit on top would hide them from the resume check
   (see below). Tell the owner in the chat what failed, which commits are at HEAD, and the stash
   name if there is one. This should be rare: the loop reverts commits it has just made.

   Why roll back, and why with `revert`:
   - The next task starts with a green baseline gate. A RED commit at HEAD makes that gate fail,
     so every later task would fail too.
   - The task report is committed after this. A report commit at HEAD would hide the task's RED or
     GREEN commit from `finnavis-tdd`'s resume check, and the next run would start the task again
     on top of red tests. After a rollback there is nothing to resume, so the report is safe to
     commit, and the next run starts the task clean.
   - `git reset --hard` is denied to Claude in this repo, and it would lose the attempt. `revert`
     keeps the attempt in history, and the squash merge removes it from the PR.

   The rollback commit carries no `Task:` trailer on purpose. With one, the planner would treat it
   as an interrupted run of that task.
3. **Write the task report** with `Status: failed` and commit it.
4. **Decide whether to go on.**
   - The failure is not about this task, so it would repeat on every task → stop the loop, even
     with `--continue-on-error`. That is, `finnavis-tdd` refused in its **pre-flight** because:
     the baseline is not green, Docker is not running, the working tree was not clean before the
     task started, or the branch is `master`. Also stop when its gate script says a failure comes
     from the environment (Docker, the database connection). A tree that is dirty **after** a
     phase stopped is not on this list: that is this task's failure, handled in step 1.
   - Without `--continue-on-error` → stop the loop.
   - With `--continue-on-error` → add `<id>` to `FAILED` and go back to step 1 of the loop. The
     planner then skips every task that depends on `<id>`.

   When the loop stops, go to "End of the run".

## The task report

One file per task: `REPORTS_DIR/<id>.md`. It is the record of every attempt at that task, so the
owner can open one file and see what happened. Write it after every attempt, done or failed.

If the file exists from an earlier run, keep its `## History` lines and add the new one at the
end. Replace the rest. Use this layout:

```
# <id> — <title>

Status: done | failed
Last run: <RUN_STARTED>

## History

- <RUN_STARTED> — done
- <RUN_STARTED> — failed at <phase or gate>: <one-line reason>. Stash: <name>   (when there is one)

## Last attempt

<for done: the final report of finnavis-tdd, exactly as it would have printed it. When the task
resumed an interrupted run, the earlier phases' output (the DoD coverage block, ALREADY_GREEN
lines) is gone with the interrupted context: write "resumed — output of the earlier phases was
not available" in its place, never a guess>

<for failed:>
Failed at: <the phase or gate, or "pre-flight">
Reason: <the failure message from finnavis-tdd, or the failed check from step 5>

Output:
<the gate script lines or the refusal text, in a code block>

Commits of the attempt: <short SHA and subject, one per line, or "none">
Rolled back by: <short SHA of the rollback commit, or "nothing to roll back">
Stash: <the stash name, or "none">

Look at: <what the owner should check first, in one or two lines, taken from the failure>
```

The stash sits on top of the commit that was rolled back, so `git stash apply` on the new HEAD
makes no sense. When there is a stash, add to `Look at:` the command that restores it with its
own base: `git stash branch <a branch name> stash@{<n>}`. The stash name also goes on the History
line, so it is still there after the next run replaces `## Last attempt`.

Commit it by path:

```bash
git add REPORTS_DIR/<id>.md
git commit -m "docs(<slug>): record the <id> tdd run"
```

The report commit has no `Task:` trailer either, for the same reason as the rollback commit.

## End of the run

Do this when the run tried at least one task: the planner printed `NEXT: none`, or the loop
stopped. Skip it for `--dry-run` and for a run that tried nothing.

1. Run the planner once more, with `--failed`. The output from before the last task is stale: it
   still lists that task as next. A task still in the run list was not reached because the loop
   stopped; tell the reporter so.
2. Start the final reporter on Haiku. A fresh agent reads only the files, so the summary shows
   what the reports say, not what the main context remembers, and Haiku costs little:

   ```
   Agent(
     subagent_type="general-purpose",
     model="haiku",
     description="Final report for <slug> tdd loop",
     prompt="Read .claude/skills/finnavis-tdd-loop/agents/final-reporter.md in full. It is your role for this run; follow it exactly.
   Use only Read, Write, Edit, Glob and Grep. Do not run commands. Do not start other agents.
   Feature: <slug>. REPORTS_DIR: <REPORTS_DIR>. RUN_STARTED: <RUN_STARTED>.
   Ended: <finished | stopped at <id>: <reason>>. Continue on error: <yes | no>.
   Tried, in order: <TRIED>.
   Tasks still in the planner's run list were not reached: the loop stopped before them.
   Last planner output:
   <the full output of the planner run from step 1>"
   )
   ```

3. Check that `git status --porcelain` shows only `REPORTS_DIR/final-report.md`. If the agent
   touched anything else, stop and show the owner the paths; do not commit.
4. Commit it: `git add REPORTS_DIR/final-report.md` and
   `git commit -m "docs(<slug>): record tdd loop run <n>"`, with `<n>` from the agent's last line.
5. In the chat, show the new `## Run <n>` section of `final-report.md`, then:

   ```
   Reports: <REPORTS_DIR>/
   Before the PR: run ./build.sh test. The gates ran only the test projects of each task's layer.
   ```

   When `FAILED` is not empty, add: `To try the failed tasks again after you fix the cause:
   /finnavis-tdd-loop <slug>`.

Never push and never open a PR. Both need the owner's approval (`.claude/rules/git.md`).

## Anti-patterns

- **Do not do any phase work yourself.** The loop only calls `finnavis-tdd`. If a task looks easy,
  it still goes through all three agents and all three gates.
- **Do not repair a failed task.** Roll it back, report it, and stop or go on. Do not retry it in
  the same run, and do not edit its commits. `finnavis-tdd` says the same about a failed phase.
- **Do not leave a failed task's commits at HEAD.** Even when the loop stops, roll back first.
  Otherwise the next task's baseline fails, and the report commit hides the stopped run. The one
  case where they stay is a revert conflict, and then nothing else is committed.
- **Do not run a skipped task.** A docs, migration, ports or tests task is outside what
  `finnavis-tdd` can do. The final report names it.
- **Do not edit `tracker.md` yourself.** `finnavis-tdd` marks a task done. A failed task stays
  `todo`, so the next run tries it again.
- **Do not write `final-report.md` yourself.** The Haiku agent writes it from the files.
- **Do not ask between tasks.** The owner started a loop so that they would not have to start each
  task.

## Examples

```
User: /finnavis-tdd-loop csv-import --dry-run
You:  pre-flight → planner → show the plan: run T10, T11, T12, T13, T15, T18, T19; skip T01,
      T02, T20–T27 for their layer; skip T14, T16, T17 (wait for T01).
      End: "The loop would run 7 tasks, starting with T10." Stop. No report.

User: /finnavis-tdd-loop csv-import
You:  [1/7] T10 → done → T10.md committed → [2/7] T11 → … → [7/7] T19 → NEXT: none
      → Haiku appends "## Run 1" to final-report.md → commit → show the section.

User: /finnavis-tdd-loop csv-import          (Gate 2 fails on T12)
You:  … [3/7] T12 → finnavis-tdd stops at Gate 2 → revert T12's RED commit → T12.md
      (failed) committed → stop → final-report.md "Run 1: stopped at T12" → show it.

User: /finnavis-tdd-loop csv-import --continue-on-error      (Gate 2 fails on T12)
You:  … [3/7] T12 failed → revert → T12.md committed → FAILED=T12 → planner skips T13 and T15
      (depend on T12) → T18 → T19 → NEXT: none → "Run 1: finished, 1 failed".

User: /finnavis-tdd-loop csv-import          (next day, after the owner fixed the cause)
You:  T12 is todo again → it runs → done → T12.md History has two lines → final-report.md
      "Run 2", with "T12 — failed in run 1, done now".

User: /finnavis-tdd-loop core-ledger
You:  plan says NEXT: none — only ports, tests and docs tasks are left. Show the plan. Stop.
      No report.
```
