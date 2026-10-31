---
name: finnavis-workflow
description: >
  Turns a set of tasks from a feature epic into a gated multi-agent Workflow run for a solo,
  self-hosted .NET pet project (FinNavis and projects shaped like it). Reads
  docs/features/<slug>/tasks/_epic.md, tasks.json and the task files, builds stages from the
  dependency graph (independent tasks in parallel git worktrees, migrations and overlapping
  files_hint in series), checks the plan for problems that would stop a gate later and asks about
  each one, then writes .claude/workflows/scripts/<slug>-<tasks>.js from a fixed template, shows it
  for review and runs it only after approval. Every task gets an author, an independent reviewer
  that runs build and tests itself, and an integrator that commits one Conventional Commit per task
  and marks the task done in tracker.md and in its task file. Use this whenever the user wants to
  implement, run or orchestrate several tasks of an epic with agents — including phrasings like
  "реализуй задачи T09–T13", "запусти workflow для {slug}", "сделай задачи эпика", "прогони
  задачи агентами", "/finnavis-workflow <path to _epic.md> T09-T13" — even when they do not say
  the word workflow. Hard-refuses if _epic.md or tasks.json is missing, or a task id is not in
  tasks.json.
argument-hint: "<path to _epic.md> <task ids: T09-T13 | T09,T10,T12>"
---

# finnavis-workflow

Implement a chosen set of tasks of one epic with a gated Workflow run. The skill does the planning
and the questions itself. The template does the running. Nothing runs before the owner approves the
script.

Questions to the owner go through **AskUserQuestion**, in the language the owner wrote in
(`.claude/rules/language.md`). The script, its prompts and every commit are English.

## Inputs

`$ARGUMENTS` = `<path to _epic.md> <task ids>`.

- Task ids: a range `T09-T13`, a list `T09,T10,T12`, or both mixed. Expand a range against the ids
  in `tasks.json`.
- If either part is missing, ask for it with AskUserQuestion. Offer the tasks whose tracker status is
  `todo` and whose deps are all `done` as options.

## Hard refusals

Stop and say why, before writing anything:

- `_epic.md` does not exist at the given path.
- `tasks.json` or `tracker.md` is not next to it.
- A task id is not in `tasks.json`, or its task file is missing.

## Step 1 — Read

- `_epic.md`, `tasks.json`, `tracker.md`, and every selected task file in full.
- `CLAUDE.md` and `.claude/rules/*` are already in context. Do not paste them into the script:
  workflow agents get them too.
- `git status`, `git branch --show-current`, `git worktree list`.

## Step 2 — Build the stages

Use `deps` from `tasks.json`, limited to the selected tasks.

1. Topological levels: a task goes into the first stage after every selected task it depends on.
2. Split a level when two of its tasks cannot run side by side. Move the later task (by id) into the
   next stage:
   - `layer: migration` always runs alone. The EF Core model snapshot is one file.
   - Two tasks whose `files_hint` overlap (one path equals or contains the other) run in series. The
     epic's "Serialization lanes" section names these; trust it over your own guess.
3. A stage with one task runs in the main checkout. A stage with several tasks runs in parallel, one
   `git worktree add --detach` per task, created from the branch tip.

## Step 3 — Check the plan, ask about each finding

Each finding gets its own AskUserQuestion with a recommended option first. Record every answer:
it becomes `authorNotes`, `reviewNotes` or `testAllowance` in CONFIG, so the gate judges by the
owner's decision and does not stop on it again.

Check these. Each one stopped a real run before:

1. **Deps outside the run.** A selected task depends on a task that is not `done` in `tracker.md` and
   not in the run. Options: add it to the run, drop the dependent task, stop.
2. **Owner-only work.** A task whose DoD needs something only the owner has (a real bank statement,
   a real export, a product decision). Options: drop it from the run, stop. Never fake it.
3. **Branch.** Propose a branch name from `.claude/rules/git.md` (prefix by the task type, 3–4 word
   slug). Options: new branch from `master` (recommended when on `master`), the current work branch
   (recommended when it already holds earlier tasks of this epic). Never `master` itself.
4. **Docker.** Needed when any task touches `tests/*IntegrationTests*`, a migration or a repository.
   Run `docker info --format '{{.ServerVersion}}'` now. If it fails, stop and tell the owner.
5. **A port without its adapter.** A task adds an interface under `Domain/Abstractions` or
   `Application/Abstractions`, and its adapter or DI registration is in a task outside the run.
   `ContainerResolutionTests` resolves every port by reflection, so it will fail. Options: move the
   new ports into a skipped theory with a Skip message until the registration task (recommended; xUnit
   2 cannot skip one row, so it is a second theory), allow that exact failure as `testAllowance`,
   add the registration task to the run.
6. **An EF model change before its migration.** A configuration task runs in an earlier stage than
   the migration. Until the migration lands, every integration test fails with
   `PendingModelChangesWarning`, because `PostgresFixture` migrates on start. Options: allow exactly
   that failure in that task's `testAllowance` (recommended), put both in one stage.
7. **A DoD line against `CLAUDE.md`.** For example a comment that must name an ADR, a `///` comment,
   an int enum column. Options: `CLAUDE.md` wins and the DoD line counts as met by the rule-abiding
   form (recommended), the DoD wins. Write the answer into the task's `reviewNotes` and `authorNotes`.
8. **"`./build.sh test` is green" that the task cannot reach.** Any DoD line that needs files outside
   its own `files_hint`. Ask what counts as done.
9. **Local secrets.** A task needs `./build.sh migrate`: `launchSettings.json` must exist. Check that
   it exists with `test -f`, never read it. If it is missing, tell the owner to create it from the
   example file.

If nothing is found, say so in one line and go on.

## Step 4 — Write the script

1. Copy `templates/workflow.template.js` to
   `.claude/workflows/scripts/<slug>-<first id>-<last id>.js` (for a list: `<slug>-T09-T10-T12.js`).
2. Fill **only** `meta` and the block between `CONFIG:START` and `CONFIG:END`:
   - `meta.name` = the file name without `.js`. `meta.description` = one line.
   - `meta.phases`: `Preflight`, then one entry per stage, titled exactly `Stage <n> · <ids joined by ", ">`.
   - `CONFIG.tasks[id]`: `file`, `files_hint` (copied from `tasks.json`), `layerTests`,
     `commitSubject`, and the notes from Step 3.
   - `layerTests` by layer: `domain` → Domain.UnitTests + ArchitectureTests; `app` →
     Application.UnitTests + ArchitectureTests; `infra`, `migration`, `wiring` →
     Infrastructure.IntegrationTests + ArchitectureTests; `ports` → all four; `docs` → none, and its
     reviewer only checks the DoD.
   - `needsDocker` from Step 3.
   - Task-specific instructions go into `authorNotes`. For a `migration` task always add: generate with
     `./build.sh migrate <Name>`, diff `dotnet ef migrations script` against the staged spec under
     `docs/features/<slug>/migrations/`, fix the spec or report the configuration, never edit the
     generated migration, prove apply and revert with an integration test through `IMigrator`.
3. Never change the code below `CONFIG:END` in a generated script. If the logic is wrong, fix the
   template in this skill and regenerate.
4. Check the syntax:
   ```bash
   sed 's/^export const meta/const meta/' <script> | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{new (Object.getPrototypeOf(async function(){}).constructor)(s);console.log('SYNTAX_OK')})"
   ```

## Step 5 — Review gate

Show the owner, in short bullets:

- the script path;
- the stage table: stage, tasks, parallel or not, test projects;
- every owner decision written into CONFIG;
- the agent count (1 preflight + per stage: authors + reviewers + 1 integrator, + 1 setup when
  parallel).

Then ask with AskUserQuestion: **Approve and run** / **I will edit the script first** / **Cancel**.
Do not run anything before "Approve and run". After an edit by the owner, read the script again and
re-check the syntax before you ask again.

## Step 6 — Run

1. Commit the script on the run branch: `chore(workflows): add the <slug> <ids> workflow`. Stage it by
   path only. The preflight allows untracked files under `.claude/workflows/`, but a committed script
   keeps the run reproducible.
2. `Workflow({ scriptPath })`. Tell the owner the run id. Do not poll; wait for the notification.

## Step 7 — When the run stops

The result has `stopped_at` and `reason`. Read the journal if the reason is unclear.

- Report what passed, what stopped and why, and the current state: branch head, staged files,
  worktrees left.
- If the reason is an owner decision (`owner_decision_needed`, a DoD conflict, a known failure), ask
  it with AskUserQuestion. Write the answer into that task's `reviewNotes`, `authorNotes` or
  `testAllowance`. Prompts of tasks that already passed stay unchanged, so they replay from the cache.
- Commit the script change (`chore(workflows): …`) and resume:
  `Workflow({ scriptPath, resumeFromRunId })`.
- If a stage was committed by hand after a stop, resume with
  `args: { committedStages: { "<stage index>": "<sha>" } }`, so its integrator does not apply the
  change twice.
- **Never work around a denial.** A permission denial or a blocking hook is reported to the owner
  with the options. No retry with another flag, no `--no-verify`, no `git reset` to "fix" a worktree.

## Step 8 — When the run completes

Report:

- per task: commit, reviewer verdict, DoD `met/total`, build warnings, test summary;
- `convention_issues` and `notes_for_later_tasks` — the second list is what the next run's tasks need;
- tokens: the sum of `subagent_tokens` from every run notification of this epic run, plus the
  script's `output_tokens_spent`; say that the main session is not included;
- worktrees left, temp folders left;
- follow-ups the run found (a contract change that needs `/finnavis-api-forge <slug> --reconcile`,
  a task-file wording problem).

Then ask before `git push`, and again before `gh pr create` (`.claude/rules/git.md`).
