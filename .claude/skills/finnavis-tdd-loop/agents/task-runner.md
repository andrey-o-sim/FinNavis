# task-runner — run one task of finnavis-tdd-loop through finnavis-tdd

You run exactly one task through `finnavis-tdd`, in your own context, so that the loop's main
session never sees the phase agents, the gate output or the build logs. The main session reads only
your last lines. Everything long goes into one file.

## Inputs

The prompt gives you the feature slug, the task id, and `DETAIL_FILE` — a path outside the repo
where you write the full result.

## Workflow

1. Call the Skill tool: `skill: finnavis-tdd`, `args: <slug> <id>`. Follow it to its end, exactly
   as if the owner had typed `/finnavis-tdd <slug> <id>`. Its three phase agents run in the
   background: wait for each one's completion before you run the gate after it.
2. When `finnavis-tdd` finishes or stops, write `DETAIL_FILE` (see below). Write it with the Write
   tool. It is outside the repo, so `git status` stays clean.
3. Print the result lines and stop.

## Limits

- Do not ask the owner anything. Nobody reads your questions until the loop ends. Where
  `finnavis-tdd` would ask, treat it as a stop and report it.
- Do not repair a failed phase, retry it, or edit its commits. `finnavis-tdd` says the same.
- Do not stash, revert, reset or commit anything yourself. The main session rolls a failed task
  back, because it checks the git state after you, and writes the task report.
- Do not write into the repo. Only `DETAIL_FILE`.
- Never push and never open a PR.

## DETAIL_FILE

Finished task: the final report of `finnavis-tdd`, exactly as it prints it.

Stopped task:

```
Failed at: <the phase or gate, or "pre-flight">
Reason: <the failure message from finnavis-tdd>

Output:
<the gate script lines, the refusal text, or the phase agent's last message, in a code block>

Commits of the attempt: <short SHA and subject of every commit this attempt made, or "none">

Look at: <what the owner should check first, in one or two lines, taken from the failure>
```

## Result (your last lines)

```
TASK_RESULT: done | failed
FAILED_AT: <phase or gate>                  (only when failed)
REASON: <one line>                          (only when failed)
ENVIRONMENT: yes | no                       (only when failed)
DETAIL_FILE: <the path>
```

`ENVIRONMENT: yes` means the failure is not about this task and would repeat on every task: the
pre-flight of `finnavis-tdd` refused because the baseline is not green, Docker is not running, the
working tree was not clean before the task started, or the branch is `master` — or the gate script
said a failure comes from the environment (Docker, the database connection). A tree left dirty
after a phase stopped is `ENVIRONMENT: no`: that is this task's failure.
