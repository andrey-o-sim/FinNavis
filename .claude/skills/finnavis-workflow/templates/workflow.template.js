export const meta = {
  name: '__NAME__',
  description: '__DESCRIPTION__',
  phases: [
    { title: 'Preflight', detail: 'Docker, branch, clean tree' },
    // One entry per CONFIG.stages item, titled exactly as stageTitle() builds it:
    // { title: 'Stage 1 · T09', detail: '...' },
    // { title: 'Stage 2 · T10, T11', detail: 'parallel, one worktree per task' },
  ],
}

/* CONFIG:START — the only block finnavis-workflow fills in. Everything after CONFIG:END is the
   fixed gate logic. Change that in the skill template, never in one generated script. */
const CONFIG = {
  repo: 'F:\\Projects\\FinNavis',
  repoPosix: 'F:/Projects/FinNavis',
  branch: '__BRANCH__',
  slug: '__SLUG__',
  tasksDir: 'docs/features/__SLUG__/tasks',
  needsDocker: true,
  // One entry per task in the run.
  //   file           task file name inside tasksDir
  //   files_hint     copied from tasks.json; the only paths the author may touch
  //   layerTests     test projects the author runs; the reviewer always runs ./build.sh test
  //   commitSubject  suggested Conventional Commit subject, lowercase imperative, <= 72 chars
  //   authorNotes    extra instructions for the author: owner decisions from the plan check
  //   reviewNotes    owner decisions the reviewer must judge by, e.g. "CLAUDE.md wins over DoD line X"
  //   testAllowance  owner-approved test failures for this task's gate; '' means fully green
  tasks: {
    // T09: {
    //   file: 'T09-infra-repositories.md',
    //   files_hint: ['src/...'],
    //   layerTests: ['tests/FinNavis.Infrastructure.IntegrationTests', 'tests/FinNavis.ArchitectureTests'],
    //   commitSubject: 'add the import repositories',
    //   authorNotes: '',
    //   reviewNotes: '',
    //   testAllowance: '',
    // },
  },
  // Built from tasks.json deps. A stage with more than one task runs in parallel worktrees.
  stages: [
    // ['T09'],
    // ['T10', 'T11'],
  ],
}
/* CONFIG:END */

const TASK_IDS = CONFIG.stages.flat()
const WT_ROOT = `${CONFIG.repoPosix}/.claude/worktrees`
const wtPath = id => `${WT_ROOT}/${CONFIG.slug}-${id}`
const stageTitle = (index, ids) => `Stage ${index + 1} · ${ids.join(', ')}`
const hintList = id => CONFIG.tasks[id].files_hint.map(p => `  - ${p}`).join('\n')

const SAFETY = `
Safety (hard rules):
- Never open, print, copy or stage src/FinNavis.Presentation/Properties/launchSettings.json, .env,
  *.pem or *.key. Never put a connection string, password or key into a shell command.
- Never run ./build.sh db-update or "dotnet ef database update" against any database. Prove a
  migration applies and reverts with an integration test on the Testcontainers PostgresFixture.
- Never run git reset, git checkout, git switch, git stash or git push. Never use --no-verify.
- If a tool call is denied or a hook blocks a command, do not retry it another way. Stop and report
  the denial in your result.
- Touch no task outside this run: ${TASK_IDS.join(', ')}.`

const DOD_ITEM = {
  type: 'object',
  properties: { item: { type: 'string' }, met: { type: 'boolean' }, evidence: { type: 'string' } },
  required: ['item', 'met', 'evidence'],
}
const BUILD = {
  type: 'object',
  properties: { ok: { type: 'boolean' }, warnings: { type: 'integer' }, summary: { type: 'string' } },
  required: ['ok', 'warnings', 'summary'],
}
const TESTS = {
  type: 'object',
  properties: { ok: { type: 'boolean' }, summary: { type: 'string', description: 'per project: passed/failed/skipped, and each allowed failure' } },
  required: ['ok', 'summary'],
}

const AUTHOR_SCHEMA = {
  type: 'object',
  properties: {
    task: { type: 'string' },
    worktree_path: { type: 'string', description: 'git rev-parse --show-toplevel where you worked' },
    head_sha: { type: 'string', description: 'git rev-parse HEAD where you worked' },
    changed_files: { type: 'array', items: { type: 'string' }, description: 'paths from git status --porcelain' },
    build: BUILD,
    tests: TESTS,
    dod: { type: 'array', items: DOD_ITEM },
    not_done: { type: 'array', items: { type: 'string' } },
    notes_for_later_tasks: { type: 'array', items: { type: 'string' } },
  },
  required: ['task', 'worktree_path', 'head_sha', 'changed_files', 'build', 'tests', 'dod', 'not_done', 'notes_for_later_tasks'],
}

const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    task: { type: 'string' },
    passed: { type: 'boolean' },
    reasons: { type: 'array', items: { type: 'string' }, description: 'why it failed; empty when passed' },
    owner_decision_needed: { type: 'array', items: { type: 'string' }, description: 'conflicts only the owner can settle, e.g. a DoD line against a CLAUDE.md rule' },
    files_outside_hint: { type: 'array', items: { type: 'string' } },
    dod: { type: 'array', items: DOD_ITEM },
    convention_issues: { type: 'array', items: { type: 'string' } },
    build: BUILD,
    tests: TESTS,
  },
  required: ['task', 'passed', 'reasons', 'owner_decision_needed', 'files_outside_hint', 'dod', 'convention_issues', 'build', 'tests'],
}

const SETUP_SCHEMA = {
  type: 'object',
  properties: { ok: { type: 'boolean' }, stop_reason: { type: 'string' }, created: { type: 'array', items: { type: 'string' } } },
  required: ['ok', 'stop_reason', 'created'],
}

const INTEGRATE_SCHEMA = {
  type: 'object',
  properties: {
    ok: { type: 'boolean' },
    stop_reason: { type: 'string', description: 'empty when ok' },
    conflicts: { type: 'array', items: { type: 'string' } },
    commits: {
      type: 'array',
      items: {
        type: 'object',
        properties: { task: { type: 'string' }, sha: { type: 'string' }, subject: { type: 'string' }, files: { type: 'array', items: { type: 'string' } } },
        required: ['task', 'sha', 'subject', 'files'],
      },
    },
    head_sha: { type: 'string', description: 'git rev-parse HEAD after the last commit' },
    build_ok: { type: 'boolean' },
    tests_summary: { type: 'string' },
    worktrees_removed: { type: 'array', items: { type: 'string' } },
    worktrees_left: { type: 'array', items: { type: 'string' } },
  },
  required: ['ok', 'stop_reason', 'conflicts', 'commits', 'head_sha', 'build_ok', 'tests_summary', 'worktrees_removed', 'worktrees_left'],
}

const PREFLIGHT_SCHEMA = {
  type: 'object',
  properties: {
    ok: { type: 'boolean' },
    stop_reason: { type: 'string' },
    docker_server_version: { type: 'string' },
    branch: { type: 'string' },
    head_sha: { type: 'string' },
  },
  required: ['ok', 'stop_reason', 'docker_server_version', 'branch', 'head_sha'],
}

function testRule(ids) {
  const allowances = ids.map(id => CONFIG.tasks[id].testAllowance).filter(Boolean)
  const command = `"bash ./build.sh test"${CONFIG.needsDocker ? ' (all .NET tests; Docker is available)' : ''}.`
  if (!allowances.length) {
    return `${command} It must be fully green. A test that was already skipped before this run may stay
skipped; a test this change newly fails or newly skips fails the gate.`
  }
  return `${command} The owner approved these exceptions, and nothing else:
${allowances.map(a => `- ${a}`).join('\n')}
Open the failure output and confirm every failure matches one exception exactly. Any other failure,
or the same test failing for another reason, fails the gate. Name each allowed failure in tests.summary.`
}

function authorPrompt(id, baseSha, worktreePath) {
  const t = CONFIG.tasks[id]
  const where = worktreePath
    ? `You work in your own git worktree: ${worktreePath}. cd there first and work only inside it.
Never edit anything in ${CONFIG.repo} itself. Before the first edit, "git rev-parse HEAD" in the
worktree must print ${baseSha}. If it does not, change nothing and return with
not_done = ["worktree base is wrong"].`
    : `You work in the main checkout ${CONFIG.repo}. Before the first edit, "git branch --show-current"
must print ${CONFIG.branch}. If it does not, change nothing and return with not_done = ["wrong branch"].`

  return `Implement task ${id} of the ${CONFIG.slug} epic in the FinNavis repo.

${where}

Read first:
- ${CONFIG.tasksDir}/${t.file} — the task. Its "Definition of Done" checklist is your DoD.
- What it links that you need: docs/features/${CONFIG.slug}/PRD.md, sad.md, data-model.md, adr/*,
  contracts/openapi.yaml, migrations/. CLAUDE.md and .claude/rules/* are in your context: follow them.
- The existing code next to your files, so the new code reads like it.

Scope — you may create or change ONLY these paths:
${hintList(id)}
Nothing else. If the DoD cannot be met without touching another file, do not touch it: say why in
not_done. If a DoD line contradicts CLAUDE.md or .claude/rules, follow CLAUDE.md and say so in
not_done, so the owner can decide.

Do NOT commit, do NOT stage, do NOT edit tracker.md or any task file.
${t.authorNotes ? `\nOwner decisions for this task:\n${t.authorNotes}\n` : ''}${SAFETY}

Your Definition of Done:
1. Every checkbox in the task file's Definition of Done.
2. "bash ./build.sh build" passes with 0 warnings (TreatWarningsAsErrors).
3. Your layer's tests: ${t.layerTests.map(p => `dotnet test ${p}`).join(' ; ')}
   ${t.testAllowance ? `Owner-approved exception: ${t.testAllowance}` : 'They must be green.'}

Return the structured report. In dod, list every checkbox of the task file with met true/false and
short evidence (test name, file, grep result). Put anything a later task must know (design choices,
a type a later adapter needs) in notes_for_later_tasks.`
}

function reviewPrompt(id, author, path) {
  const t = CONFIG.tasks[id]
  return `You are the independent reviewer for task ${id} of the ${CONFIG.slug} epic in the FinNavis repo.
You did not write this code. Look for reasons it should NOT pass. Fail a DoD item when its evidence
is missing.

Work in: ${path}. cd there first. Do not edit any file, do not commit, do not stage.
The change is uncommitted: read it with "git status --porcelain", "git diff" and the untracked files.

Inputs:
- Task file: ${CONFIG.tasksDir}/${t.file} — its Definition of Done checklist.
- Allowed paths (files_hint):
${hintList(id)}
- The author's report. Verify it, do not trust it: ${JSON.stringify(author)}

Check:
1. Every changed or new file is inside files_hint. List any other file in files_outside_hint.
   A change to tracker.md or to any task file fails the gate.
2. Every DoD checkbox of the task file: verify it yourself in the code and the tests, with evidence.
3. CLAUDE.md on the new code: no /// comments; no comment that restates the code or names a
   document, ADR, AC or task; collection expressions; sealed by default; one public type per file;
   enums stored as strings; money as decimal; DateTimeOffset; test names Method_Scenario_ExpectedResult;
   assertions with .Should(). Domain and Application csproj: no new PackageReference or ProjectReference.
   Every new error code must exist in docs/features/${CONFIG.slug}/contracts/openapi.yaml.
4. Run it yourself: "bash ./build.sh build" (0 warnings), then ${testRule([id])}

passed = true only if: no file outside the hint, every DoD item met, the build has 0 warnings, the
tests follow the rule above, and no CLAUDE.md "never/must" rule is broken. Minor style remarks go to
convention_issues and do not fail the gate. When a DoD line and CLAUDE.md contradict each other and
no owner decision below settles it, fail the gate and describe the conflict in owner_decision_needed.
${t.reviewNotes ? `\nOwner decisions — judge by these:\n${t.reviewNotes}\n` : ''}${SAFETY}`
}

function setupPrompt(ids, baseSha) {
  return `Create one git worktree per task for parallel work, from commit ${baseSha} of ${CONFIG.branch}.
Work in ${CONFIG.repo}. Do not edit any file.
1. If any of these paths already exists, STOP (ok=false) and create nothing:
${ids.map(id => `   ${wtPath(id)}`).join('\n')}
2. Run, one per task:
${ids.map(id => `   git worktree add --detach ${wtPath(id)} ${baseSha}`).join('\n')}
3. In each new worktree, "git rev-parse HEAD" must print ${baseSha} and "git status --porcelain"
   must be empty. Otherwise ok=false.
${SAFETY}`
}

function integratePrompt(index, items) {
  const ids = items.map(x => x.id)
  const fromWorktrees = items.length > 1
  const sources = items.map(x => `- ${x.id}: ${fromWorktrees ? `worktree ${wtPath(x.id)}, ` : ''}files ${JSON.stringify(x.author.changed_files)}`).join('\n')
  const bringIn = fromWorktrees
    ? `Bring the changes of the ${ids.length} worktrees into ${CONFIG.branch} in ${CONFIG.repo}. They are
UNCOMMITTED. For each worktree, in this order (${ids.join(', ')}):
  a. In the worktree: "git add -A" (this stages inside the worktree only), then
     "git diff --cached --binary > <a temp dir outside the repo>/<id>.patch".
Then in ${CONFIG.repo}:
  b. Run "git apply --check" on EVERY patch first. If any check fails, or two patches touch the same
     file: STOP. Apply nothing and resolve nothing. Return ok=false with the conflict.
  c. Apply every patch with "git apply".
Sources:
${sources}`
    : `The change is already uncommitted in ${CONFIG.repo}:
${sources}`
  const commitSteps = ids.map(id => {
    const t = CONFIG.tasks[id]
    return `   ${id}:
     - ${CONFIG.tasksDir}/tracker.md: in the ${id} row only, set the Status column to "done".
     - ${CONFIG.tasksDir}/${t.file}: in the YAML frontmatter, set status: "todo" to status: "done".
     - "git diff" must show exactly one changed row in tracker.md and one changed line in the task
       file. Otherwise STOP (ok=false).
     - Stage by explicit path: the ${id} files listed above, tracker.md and the task file. Never
       "git add -A" or "git add ." in ${CONFIG.repo}.
     - Commit. Suggested subject: "feat(${CONFIG.slug}): ${t.commitSubject}". Use type test, docs or
       refactor instead of feat when the change is only that.`
  }).join('\n')

  return `You are the integrator for ${stageTitle(index, ids)} of the ${CONFIG.slug} epic in the FinNavis repo.
The independent reviewer passed every task of this stage.

1. In ${CONFIG.repo}, "git branch --show-current" must print ${CONFIG.branch}. If not, STOP (ok=false).
2. ${bringIn}
3. In ${CONFIG.repo} run "bash ./build.sh build" (0 warnings) and ${testRule(ids)}
   If either fails: STOP, commit nothing, return ok=false with the failure.
4. Make one commit per task, in this order. Before each commit run "git status" and confirm that no
   launchSettings.json, .env, *.pem, *.key and nothing under .claude/workflows/ or .claude/worktrees/
   is staged.
${commitSteps}
   Every commit follows .claude/rules/git.md: Conventional Commits, scope ${CONFIG.slug}, lowercase
   imperative subject of 72 characters or fewer, a body wrapped at 100 characters that says why, and
   one "See docs/.../adr/<file>.md" line for each ADR the task carries out. No attribution trailer.
   If a hook blocks a commit: STOP, do not bypass it, leave the files staged, return ok=false with
   the hook output.
5. ${fromWorktrees ? 'After the last commit, remove each worktree with "git worktree remove --force <path>". If a removal is denied, leave it and list it in worktrees_left. If you stopped earlier, leave every worktree in place.' : 'Nothing to clean up.'}
6. Return head_sha = "git rev-parse HEAD".
Do NOT push. Do NOT open a PR.${SAFETY}`
}

const stages = []
const stopWith = (stage, reason) => ({ status: 'stopped', stopped_at: stage, reason, stages, output_tokens_spent: budget.spent() })

// ---------- Preflight ----------
phase('Preflight')
const pre = await agent(
  `Preflight for a workflow run in ${CONFIG.repo}. Do not edit any tracked file.
1. ${CONFIG.needsDocker ? `Run "docker info --format '{{.ServerVersion}}'". If it fails, ok=false: the integration tests need Docker.` : 'Docker is not needed. Return an empty docker_server_version.'}
2. Branch: if ${CONFIG.branch} does not exist, create it from master ("git switch -c ${CONFIG.branch} master").
   If it exists and is not checked out, switch to it. Then "git merge-base --is-ancestor master HEAD"
   must succeed. Otherwise ok=false.
3. "git status --porcelain" must show nothing except untracked paths under .claude/workflows/ or
   .claude/worktrees/. Otherwise ok=false.
Return the structured result.`,
  { label: 'preflight', phase: 'Preflight', schema: PREFLIGHT_SCHEMA, effort: 'low' },
)
if (!pre || !pre.ok) return stopWith('Preflight', pre ? pre.stop_reason : 'preflight agent died')
log(`branch ${pre.branch} at ${pre.head_sha.slice(0, 7)}${CONFIG.needsDocker ? `, Docker ${pre.docker_server_version}` : ''}`)

// ---------- Stages ----------
// A stage committed by hand after a stop is passed back as args.committedStages = { "<index>": "<sha>" },
// so a resumed run skips its integrator instead of applying the same change twice.
const committedByHand = (args && args.committedStages) || {}
let baseSha = pre.head_sha

for (const [index, ids] of CONFIG.stages.entries()) {
  const title = stageTitle(index, ids)
  phase(title)
  const parallelStage = ids.length > 1

  if (parallelStage) {
    const setup = await agent(setupPrompt(ids, baseSha), { label: `setup:${ids.join('+')}`, phase: title, schema: SETUP_SCHEMA, effort: 'low' })
    if (!setup || !setup.ok) return stopWith(title, `worktree setup failed: ${setup ? setup.stop_reason : 'setup agent died'}`)
  }

  const results = await pipeline(
    ids,
    id => agent(authorPrompt(id, baseSha, parallelStage ? wtPath(id) : null), { label: `author:${id}`, phase: title, schema: AUTHOR_SCHEMA }),
    (author, id) => author
      ? agent(reviewPrompt(id, author, parallelStage ? wtPath(id) : CONFIG.repo), { label: `review:${id}`, phase: title, schema: REVIEW_SCHEMA })
          .then(review => ({ id, author, review }))
      : { id, author: null, review: null },
  )
  const record = { stage: title, tasks: results.map((x, i) => x || { id: ids[i], author: null, review: null }) }
  stages.push(record)

  const failed = record.tasks.filter(x => !x.author || !x.review || !x.review.passed)
  if (failed.length) {
    const reason = failed.map(x => `${x.id}: ${!x.author ? 'author agent died' : !x.review ? 'reviewer agent died' : [...x.review.reasons, ...x.review.owner_decision_needed].join(' | ')}`).join(' ; ')
    return stopWith(title, `review failed — ${reason}${parallelStage ? ' Worktrees are left in place.' : ''}`)
  }

  const handSha = committedByHand[String(index)]
  const integ = handSha
    ? { ok: true, stop_reason: '', conflicts: [], commits: [{ task: ids.join(', '), sha: handSha, subject: 'committed by hand', files: [] }], head_sha: handSha, build_ok: true, tests_summary: 'see the reviews', worktrees_removed: [], worktrees_left: [] }
    : await agent(integratePrompt(index, record.tasks), { label: `integrate:${ids.join('+')}`, phase: title, schema: INTEGRATE_SCHEMA })
  record.integration = integ
  record.output_tokens_after = budget.spent()
  if (!integ || !integ.ok) return stopWith(title, integ ? `integration stopped: ${integ.stop_reason} ${integ.conflicts.join(', ')}` : 'integrator agent died')
  integ.commits.forEach(c => log(`${c.task}: ${c.sha.slice(0, 7)} ${c.subject}`))
  baseSha = integ.head_sha
}

// ---------- Summary ----------
return {
  status: 'completed',
  per_task: stages.flatMap(s => s.tasks.map(t => ({
    stage: s.stage,
    id: t.id,
    changed_files: t.author ? t.author.changed_files : [],
    author_not_done: t.author ? t.author.not_done : ['author agent died'],
    notes_for_later_tasks: t.author ? t.author.notes_for_later_tasks : [],
    review_passed: t.review ? t.review.passed : false,
    dod_met: t.review ? `${t.review.dod.filter(d => d.met).length}/${t.review.dod.length}` : '0/0',
    convention_issues: t.review ? t.review.convention_issues : [],
    build: t.review ? t.review.build : null,
    tests: t.review ? t.review.tests : null,
  }))),
  commits: stages.flatMap(s => (s.integration ? s.integration.commits : [])),
  worktrees_left: stages.flatMap(s => (s.integration ? s.integration.worktrees_left : [])),
  output_tokens_per_stage: stages.map(s => ({ stage: s.stage, cumulative: s.output_tokens_after })),
  output_tokens_spent: budget.spent(),
}
