---
name: finnavis-tdd
description: >
  Runs one task of a FinNavis feature epic through a full Red-Green-Refactor cycle with three
  isolated agents — finnavis-tdd-test-writer, finnavis-tdd-implementer, finnavis-tdd-refactorer —
  and checks a hard gate after each phase: RED = the build is green, every new test fails and every
  old test still passes; GREEN = all tests pass, tests/ is untouched and no NotImplementedException
  stub is left; REFACTOR = still green, tests/ still untouched. Reads the task from
  docs/features/<slug>/tasks/ (tasks.json, the task file, its Definition of Done and the PRD
  acceptance criteria it names), commits one Conventional Commit per phase, then marks the task done
  in tracker.md. Use this whenever the user wants to implement a single domain, app, infra or wiring
  task test-first in this repo — including phrasings like "/finnavis-tdd csv-import T14", "TDD для
  T14", "сделай T14 через TDD", "прогони red-green-refactor на T09", "test-first для задачи",
  "напиши сначала тесты, потом код" — even when they do not say the word TDD. Prefer it over a
  generic TDD skill in this repo. Refuses docs, migration, ports and tests tasks, a dirty tree, the
  master branch, and a task whose dependencies are not done.
argument-hint: "<slug> <task id> [--review-tests]"
allowed-tools: Bash, Read, Edit, Agent
---

# finnavis-tdd — the Red-Green-Refactor orchestrator

This skill is the conductor. It writes no test and no production code itself. It starts three
agents from `.claude/agents/`, one per phase, each in its own context. Between the phases it runs
the gates in Bash. When a gate fails, it stops and tells the owner exactly what failed.

Why three agents and not three steps in one context: a skill runs inside the main context. The
agent that wrote the tests would then also write the code, and it would "remember" what it meant
instead of reading what the tests say. A separate agent only sees the committed tests, so the
tests are the real spec.

Talk to the owner in the language they wrote in (`.claude/rules/language.md`). Commits and every
file stay English.

## Inputs

`$ARGUMENTS` = `<slug> <task id> [--review-tests]`, for example `csv-import T14`.

- `<slug>` — a folder under `docs/features/`.
- `<task id>` — an id in `docs/features/<slug>/tasks/tasks.json`.
- `--review-tests` — stop after Gate 1, so the owner can read the tests before any code is
  written.

If the slug or the id is missing, ask for it. Offer the `todo` tasks of an allowed layer whose
dependencies are all `done` in `tracker.md`.

## Fixed values

```
TASKS_DIR = docs/features/<slug>/tasks
GATE      = .claude/skills/finnavis-tdd/scripts/failed_tests.py
TRAILER   = Task: <slug>/<id>
```

Test projects of a gate, by the task's `layer`:

| layer           | test projects                                                | Docker |
|-----------------|--------------------------------------------------------------|--------|
| `domain`        | `FinNavis.Domain.UnitTests`, `FinNavis.ArchitectureTests`    | no     |
| `app`           | `FinNavis.Application.UnitTests`, `FinNavis.ArchitectureTests` | no   |
| `infra`, `wiring` | `FinNavis.Infrastructure.IntegrationTests`, `FinNavis.ArchitectureTests` | yes |

Refused layers, with the reason to give:

- `docs` — nothing to test. Do it by hand or with finnavis-workflow.
- `migration` — EF Core generates the code, so "implement to make tests pass" does not fit. Use
  finnavis-workflow.
- `ports` — there is no HTTP test harness yet (no `WebApplicationFactory`, no Presentation test
  project), so the RED agent has nowhere to put a failing endpoint test. Use finnavis-workflow.
- `tests` — a quality-gate task is tests only. There is no production code to drive.

## Running a gate

Every gate uses the same run. The results go to a temporary folder outside the repo, so
`git status` stays clean.

```bash
RUN=$(mktemp -d)
dotnet build FinNavis.slnx -nologo && echo BUILD_OK
for p in <test projects>; do
  dotnet test "tests/$p" --no-build --nologo --logger "trx;LogFileName=$p.trx" --results-directory "$RUN"
done
python "$GATE" --trx-dir "$RUN" --expect <red|green> [--base <SHA>] [--allow-green <names>]
```

- No `BUILD_OK` → the gate fails. Warnings are errors here, so a warning is a failure too.
- `dotnet test` exits non-zero when a test fails. That is expected in RED. Judge by the script,
  not by the exit code of `dotnet test`.
- The script prints `PASS` or `FAIL` on its last line, plus one line per problem.

## Pre-flight

Run all checks before any phase. On the first failure, stop with the message shown.

1. `git status --porcelain` is empty.
   Else: `Working tree is not clean. Commit or stash the changes, then run /finnavis-tdd again.`
2. `git branch --show-current` is not `master`.
   Else: `TDD commits never go to master (.claude/rules/git.md). Create a work branch first.`
3. The task is in `$TASKS_DIR/tasks.json` and its file `$TASKS_DIR/<id>-*.md` exists.
4. The task's `layer` is allowed (table above). Else: the refusal reason from the list above.
5. In `tracker.md`, the task is not `done`, and every id in its `deps` is `done`.
   Else: name each dependency that is not done, with its status.
6. Docker, when the layer needs it: `docker info --format '{{.ServerVersion}}'` succeeds.
   Else: `Docker is not running. Integration tests need it (Testcontainers).`
7. **Resume check.** Read `git log -1 --format='%s%n%(trailers:key=Task,valueonly)'`. If the
   trailer is `<slug>/<id>`, an earlier run of this task stopped after a phase:
   - subject starts with `test(` → `RED_SHA` = HEAD, `BASE_SHA` = `HEAD~1`. Go to Gate 1, then
     Phase 2. Do not stop for `--review-tests` again: a re-run after a review means "continue".
   - subject starts with `feat(` → `GREEN_SHA` = HEAD, find `RED_SHA` with
     `git log --grep='Task: <slug>/<id>' --format=%H`. Go to Gate 2, then Phase 3.
   - subject starts with `refactor(` → go to Gate 3, then "Mark the task done".
8. Not resuming: `BASE_SHA` = `git rev-parse HEAD`. Run a gate with `--expect green`.
   It must pass. Else: `The baseline is not green, so Gate 1 cannot tell a new failure from an old
   one. Fix these first:` + the failed lines.

## Phase 1 — RED

```
Agent(
  subagent_type="finnavis-tdd-test-writer",
  description="RED: failing tests for <slug> <id>",
  prompt="Feature: <slug>. Task: <id>. Task file: <path>. Layer: <layer>.
Gate test projects: <projects>. Commit trailer: Task: <slug>/<id>.
Write the failing tests for this task and the stubs they need to compile, then commit and stop."
)
```

From its last message, take `RED phase commit: <SHA>` as `RED_SHA`, the `DOD COVERAGE` block and
every `ALREADY_GREEN:` line.

### Gate 1 — the RED state is real

1. `git rev-parse HEAD` equals `RED_SHA`, and `git status --porcelain` is empty.
2. The subject starts with `test(<slug>):` and the trailer is `<slug>/<id>`.
3. `git diff --name-only BASE_SHA RED_SHA` lists only paths under `tests/` and `src/`.
4. Every `src/` file in that diff only adds stubs: run `git diff BASE_SHA RED_SHA -- src/` and read
   it. A method body that does more than `throw new NotImplementedException();` is implementation
   written in the RED phase. Fail the gate and name the file.
5. Run a gate: `--expect red --base BASE_SHA --allow-green <names from ALREADY_GREEN>`. It must
   print `PASS`. The script checks that the build is green, at least one test fails, every new test
   fails, no old test fails, and no failure comes from the environment.

On failure: `Gate 1 failed: <reason>. RED commit: <RED_SHA>.` + the script lines. Then stop.

If `--review-tests` is set, stop here:

```
RED is committed: <RED_SHA>. Read the tests with: git show <RED_SHA>
DoD coverage: <the DOD COVERAGE block>
To continue: /finnavis-tdd <slug> <id>   (the run resumes at Phase 2)
To discard:  git reset --hard <BASE_SHA>   (run it yourself; this repo denies it to Claude)
```

## Phase 2 — GREEN

```
Agent(
  subagent_type="finnavis-tdd-implementer",
  description="GREEN: implement <slug> <id>",
  prompt="Feature: <slug>. Task: <id>. Task file: <path>. Layer: <layer>. RED commit: <RED_SHA>.
Gate test projects: <projects>. Commit trailer: Task: <slug>/<id>.
Make every test green without touching tests/, then commit and stop."
)
```

Take `GREEN phase commit: <SHA>` as `GREEN_SHA`, and every `FOLLOW_UP:` line.

### Gate 2 — GREEN, and the tests were not touched

1. `git rev-parse HEAD` equals `GREEN_SHA`, and `git status --porcelain` is empty.
2. The subject starts with `feat(<slug>):` and the trailer is `<slug>/<id>`.
3. `git diff --name-only RED_SHA GREEN_SHA -- tests/` is empty.
   Else: `Gate 2 failed: the implementer changed tests/. The tests are the spec. Files: <files>.`
4. `git diff --name-only RED_SHA GREEN_SHA` lists only paths under `src/`.
5. `git grep -n NotImplementedException GREEN_SHA -- <src files changed in BASE_SHA..GREEN_SHA>`
   is empty. A stub that is left means a DoD line has no test that reaches it.
6. Run a gate with `--expect green`. It must print `PASS`.

On failure: `Gate 2 failed: <reason>. GREEN commit: <GREEN_SHA>.` Then stop.

## Phase 3 — REFACTOR

```
Agent(
  subagent_type="finnavis-tdd-refactorer",
  description="REFACTOR: clean up <slug> <id>",
  prompt="Feature: <slug>. Task: <id>. Task file: <path>. Layer: <layer>.
Changed by this task: BASE_SHA=<BASE_SHA>..GREEN_SHA=<GREEN_SHA>.
Gate test projects: <projects>. Commit trailer: Task: <slug>/<id>.
Improve the structure of the code this task changed, if it needs it. Commit only if you changed something."
)
```

The last message is either `REFACTOR phase commit: <SHA>` (`REFACTOR_SHA`) or
`REFACTOR phase: no change` with a one-line reason.

### Gate 3 — still green, behaviour unchanged

With no change: `git rev-parse HEAD` equals `GREEN_SHA` and `git status --porcelain` is empty.
Nothing else to check.

With a commit:

1. HEAD equals `REFACTOR_SHA`, the tree is clean, the subject starts with `refactor(<slug>):` and
   the trailer is `<slug>/<id>`.
2. `git diff --name-only GREEN_SHA REFACTOR_SHA` lists only paths under `src/`. Nothing under
   `tests/`.
3. Run a gate with `--expect green`. It must print `PASS`.

On failure: `Gate 3 failed: <reason>. REFACTOR commit: <REFACTOR_SHA>.` Then stop.

## Mark the task done

Do this yourself; it is two one-line edits.

1. `tracker.md`: in the `<id>` row only, set the Status column to `done`.
2. The task file: in the YAML front matter, set `status: "todo"` (or whatever it is) to
   `status: "done"`. Leave the DoD checkboxes as they are — the other tasks keep them unchecked too.
3. `git diff --stat` shows exactly these two files, one line each.
4. Stage them by path and commit:
   `git commit -m "docs(<slug>): mark <id> done" -m "Task: <slug>/<id>"`.

## Final report

```
TDD cycle complete for <slug> <id> — <task title>.

Commits:
  RED       <RED_SHA>       <subject>
  GREEN     <GREEN_SHA>     <subject>
  REFACTOR  <REFACTOR_SHA>  <subject>        (or: no change — <reason>)
  TRACKER   <SHA>           docs(<slug>): mark <id> done

Gates:
  ✓ RED       build green, <n> new tests red, old tests green
  ✓ GREEN     <n> tests green, tests/ untouched, no stub left
  ✓ REFACTOR  green, tests/ untouched

DoD coverage:
  <the DOD COVERAGE block from the test-writer>

Follow-ups for the owner:
  <every ALREADY_GREEN and FOLLOW_UP line, or "none">

Before the PR: run ./build.sh test. The gates ran only <projects>.
```

Do not push and do not open a PR. Both need the owner's approval (`.claude/rules/git.md`).

## Anti-patterns

- **Do not run a phase inline.** Each phase is one Agent call. If you write a test or a line of
  code in the main context, the isolation is gone and the gates prove nothing.
- **Do not skip a gate, and do not soften one.** Gate 2's "tests/ untouched" check is the whole
  point: without it, the implementer can "fix" a test instead of the code.
- **Do not repair a failed phase.** A phase that cannot pass its gate says something about the task
  or the tests. Stop, show the owner the failure, and let them decide. Do not retry with another
  agent, and do not edit the commit.
- **Do not squash the phase commits.** Three commits are how the owner sees that RED came first.
  The PR is squash-merged later anyway.
- **Do not reset on failure.** `git reset --hard` is denied to Claude in this repo. Give the owner
  the command and the SHA instead.

## Examples

```
User: /finnavis-tdd csv-import T14
You:  pre-flight → Phase 1 → Gate 1 → Phase 2 → Gate 2 → Phase 3 → Gate 3 → mark done → report

User: /finnavis-tdd csv-import T14 --review-tests
You:  same, but stop after Gate 1 with the review message.
User: /finnavis-tdd csv-import T14
You:  pre-flight sees the RED commit of T14 → Gate 1 again → Phase 2 → … → report

User: /finnavis-tdd csv-import T21
You:  refuse: T21 is a ports task; there is no HTTP test harness yet.
```
