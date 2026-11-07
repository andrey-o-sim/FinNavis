# finnavis-tdd-loop

Runs every `todo` task of a feature epic through `finnavis-tdd`, one task after another, in
dependency order. It replaces starting `/finnavis-tdd <slug> <id>` by hand for each task.

The rules live in [`SKILL.md`](SKILL.md). This file is a short map for a human reader.

## Usage

```
/finnavis-tdd-loop csv-import --dry-run              # show the plan, change nothing
/finnavis-tdd-loop csv-import                        # run; stop on the first failed task
/finnavis-tdd-loop csv-import --continue-on-error    # run; after a failed task, go on
```

## How it works

- **Plan.** `scripts/plan.py` reads `tasks.json`, `tracker.md` and the `Task:` trailer of HEAD.
  It picks the `todo` tasks of the layers `finnavis-tdd` takes (`domain`, `app`, `infra`,
  `wiring`) and orders them by `deps`. A task that an interrupted run left half-done goes first.
  Every skipped task is listed with its reason.
- **One runner per task.** The main session starts a task-runner agent for each task. The runner
  calls `finnavis-tdd`, which starts its own three phase agents: test-writer, implementer,
  refactorer. All the long output stays in the runner. The main session gets back five lines and
  the path of a detail file.
- **Check.** The main session does not trust the runner's answer. It checks that the tree is clean
  and that the task is `done` in `tracker.md`.
- **Failure.** Uncommitted work goes to `git stash`. The task's commits are rolled back with
  `git revert`, so the next task starts from a green baseline. Then the loop stops, or, with
  `--continue-on-error`, goes on with the tasks that do not depend on the failed one. A failure of
  the environment (red baseline, Docker down) always stops the loop.
- **Reports.** After every attempt the loop writes `tasks/reports/<id>.md`: the attempt history
  and the last attempt in full. At the end of the run a Haiku agent appends a `## Run <n>` section
  to `tasks/reports/final-report.md`: done, failed, needs attention, changed since an earlier run,
  not tried. Everything is committed. Nothing is pushed.

## Sequence

One run. The `loop` block repeats once per task. Inside it, `alt` shows the two outcomes of a
task: T10 passes, T12 fails at Gate 2. The notes show the five lines the runner hands back — the
only part of a task that reaches the main session.

```mermaid
sequenceDiagram
    autonumber
    actor Owner
    participant Main as Main session<br/>(finnavis-tdd-loop)
    participant Plan as plan.py
    participant Runner as task-runner agent
    participant TDD as finnavis-tdd
    participant Phases as Phase agents<br/>(RED / GREEN / REFACTOR)
    participant Git as git repo
    participant Reporter as final-reporter<br/>(Haiku)

    Owner->>Main: /finnavis-tdd-loop {slug}
    Main->>Git: pre-flight: clean tree, not master

    loop until the planner says NEXT: none
        Main->>Plan: plan {slug} --failed {ids failed in this run}
        Plan-->>Main: NEXT: {id} (skips tasks that depend on a failed one)
        Main->>Runner: run {id}, DETAIL_FILE
        Runner->>TDD: /finnavis-tdd {slug} {id}
        TDD->>Phases: RED, Gate 1, GREEN, Gate 2, REFACTOR, Gate 3

        alt every gate passes
            Phases->>Git: test( / feat( / refactor( commits
            TDD->>Git: docs: mark {id} done
            Runner-->>Main: result lines
            Note over Runner,Main: TASK_RESULT: done<br/>DETAIL_FILE: {temp}/T10.md
            Main->>Git: check tree and tracker
            Main->>Git: commit reports/{id}.md
        else a gate fails (example: T12 at Gate 2)
            Phases--xTDD: Gate 2 fails
            Runner-->>Main: result lines
            Note over Runner,Main: TASK_RESULT: failed<br/>FAILED_AT: Gate 2<br/>REASON: the implementer did not commit<br/>ENVIRONMENT: no<br/>DETAIL_FILE: {temp}/T12.md
            Main->>Git: stash uncommitted work
            Main->>Git: revert T12 commits (rollback commit)
            Main->>Git: commit reports/T12.md
            break ENVIRONMENT: yes, or no --continue-on-error
                Note over Main: stop the loop, go to the final report
            end
            Note over Main: --continue-on-error: T12 joins --failed,<br/>the next plan skips T13 and T15
        end
    end

    Main->>Reporter: write run section from reports/*.md
    Reporter->>Git: append Run n section to final-report.md
    Main->>Git: commit final-report.md
    Main-->>Owner: run summary
```

## Components

```mermaid
flowchart LR
    Owner([Owner])

    subgraph Skill["finnavis-tdd-loop"]
        Main["Main session<br/>SKILL.md"]
        Plan["scripts/plan.py"]
        Runner["task-runner agent<br/>agents/task-runner.md"]
        Reporter["final-reporter agent (Haiku)<br/>agents/final-reporter.md"]
    end

    subgraph TDDSkill["finnavis-tdd"]
        TDD["finnavis-tdd<br/>SKILL.md + gates"]
        Phases["test-writer / implementer / refactorer"]
    end

    subgraph Repo["docs/features/{slug}/tasks"]
        Tasks[("tasks.json")]
        Tracker[("tracker.md")]
        TaskReports[("reports/{id}.md")]
        Final[("reports/final-report.md")]
    end

    Detail[("DETAIL_FILE<br/>temp dir, outside the repo")]
    Git[("git history<br/>commits, trailers, stash")]

    Owner -->|"/finnavis-tdd-loop"| Main
    Main -->|runs| Plan
    Plan -->|reads| Tasks
    Plan -->|reads| Tracker
    Plan -->|reads HEAD trailer| Git
    Main -->|one per task| Runner
    Runner -->|Skill call| TDD
    TDD -->|starts| Phases
    Phases -->|commits| Git
    TDD -->|marks done| Tracker
    Runner -->|writes| Detail
    Main -->|cat, never reads| Detail
    Main -->|writes| TaskReports
    Main -->|stash, revert, commit| Git
    Main -->|end of run| Reporter
    Reporter -->|reads| TaskReports
    Reporter -->|appends| Final
```

## Files

| File                       | Role                                                            |
|----------------------------|-----------------------------------------------------------------|
| `SKILL.md`                 | The orchestrator: pre-flight, loop, failure handling, reports.  |
| `scripts/plan.py`          | Picks and orders the tasks. Prints `NEXT: <id>` or `NEXT: none`. |
| `agents/task-runner.md`    | Role of the agent that runs one task through `finnavis-tdd`.     |
| `agents/final-reporter.md` | Role of the Haiku agent that writes the run into `final-report.md`. |
