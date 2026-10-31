export const meta = {
  name: 'csv-import-t03-t08',
  description: 'Implement csv-import T03-T08 in four gated stages: author, independent reviewer, integrator commit',
  whenToUse: 'Run once on feature/csv-import-domain to implement the Domain, Application ports, configurations and migration of csv-import',
  phases: [
    { title: 'Preflight', detail: 'Docker, branch, clean tree' },
    { title: 'Stage 1 · T03', detail: 'enums and MovementFingerprint' },
    { title: 'Stage 2 · T04–T06', detail: 'three tasks, each in its own worktree' },
    { title: 'Stage 3 · T07', detail: 'DbContext sets and configurations' },
    { title: 'Stage 4 · T08', detail: 'EF Core migration and diff against the staged spec' },
  ],
}

const REPO = 'F:\\Projects\\FinNavis'
const BRANCH = 'feature/csv-import-domain'
const TASKS_DIR = 'docs/features/csv-import/tasks'

const TASKS = {
  T03: {
    file: 'T03-domain-import-enums-and-fingerprint.md',
    files_hint: ['src/FinNavis.Domain/Enums/', 'src/FinNavis.Domain/ValueObjects/MovementFingerprint.cs', 'tests/FinNavis.Domain.UnitTests/ValueObjects/MovementFingerprintTests.cs'],
    layerTests: ['tests/FinNavis.Domain.UnitTests', 'tests/FinNavis.ArchitectureTests'],
  },
  T04: {
    file: 'T04-domain-mapping-profile.md',
    files_hint: ['src/FinNavis.Domain/Entities/MappingProfile.cs', 'src/FinNavis.Domain/Entities/MappingRule.cs', 'src/FinNavis.Domain/Abstractions/IMappingProfileRepository.cs', 'tests/FinNavis.Domain.UnitTests/Entities/MappingProfileTests.cs'],
    layerTests: ['tests/FinNavis.Domain.UnitTests', 'tests/FinNavis.ArchitectureTests'],
  },
  T05: {
    file: 'T05-domain-import-run.md',
    files_hint: ['src/FinNavis.Domain/Entities/ImportRun.cs', 'src/FinNavis.Domain/Entities/ImportTransactionLink.cs', 'src/FinNavis.Domain/Entities/ImportTransferLink.cs', 'src/FinNavis.Domain/Entities/ImportCreatedAccountLink.cs', 'src/FinNavis.Domain/Entities/ImportCreatedCategoryLink.cs', 'src/FinNavis.Domain/Entities/SetAsideRow.cs', 'src/FinNavis.Domain/Abstractions/IImportRunRepository.cs', 'tests/FinNavis.Domain.UnitTests/Entities/ImportRunTests.cs'],
    layerTests: ['tests/FinNavis.Domain.UnitTests', 'tests/FinNavis.ArchitectureTests'],
  },
  T06: {
    file: 'T06-app-reader-and-query-ports.md',
    files_hint: ['src/FinNavis.Application/Abstractions/IStatementReader.cs', 'src/FinNavis.Application/Abstractions/IImportQueries.cs', 'src/FinNavis.Application/Abstractions/Reads/'],
    layerTests: ['tests/FinNavis.Application.UnitTests', 'tests/FinNavis.ArchitectureTests'],
  },
  T07: {
    file: 'T07-infra-dbcontext-and-configurations.md',
    files_hint: ['src/FinNavis.Infrastructure/Persistence/FinNavisDbContext.cs', 'src/FinNavis.Infrastructure/Persistence/Configurations/'],
    layerTests: ['tests/FinNavis.Domain.UnitTests', 'tests/FinNavis.Application.UnitTests', 'tests/FinNavis.ArchitectureTests'],
  },
  T08: {
    file: 'T08-migration-csv-import-initial.md',
    files_hint: ['src/FinNavis.Infrastructure/Persistence/Migrations/', 'docs/features/csv-import/migrations/', 'tests/FinNavis.Infrastructure.IntegrationTests/Persistence/'],
    layerTests: ['tests/FinNavis.Infrastructure.IntegrationTests', 'tests/FinNavis.ArchitectureTests'],
  },
}

// Extra instructions for a single task, on top of the shared author rules.
const TASK_NOTES = {
  T07: `
- Do NOT generate a migration. T08 does that. Do not touch Persistence/Migrations/.
- Add only new configuration files and new DbSet entries. Do not change any core-ledger configuration.
- Known side effect: until T08 adds the migration, every integration test fails at fixture start-up
  with EF Core's PendingModelChangesWarning, because PostgresFixture calls MigrateAsync(). That is
  expected at this stage. Do not try to work around it (no ConfigureWarnings, no fixture edit).
  Run only the layer test projects listed below.`,
  T08: `
- Generate the migration with: bash ./build.sh migrate AddCsvImport
  The runner only checks that launchSettings.json exists. It does not read it, and the design-time
  factory needs no live server. Do NOT set ConnectionStrings__FinNavisDb.
- Produce the SQL offline with: dotnet ef migrations script CoreLedgerInitial AddCsvImport
  --project src/FinNavis.Infrastructure --startup-project src/FinNavis.Presentation
  and compare it, table by table, against the eight staged .up.sql files in
  docs/features/csv-import/migrations/. Compare the schema, not the literal text (IF NOT EXISTS and
  the timestamps differ by design).
- Never run ./build.sh db-update and never run "dotnet ef database update" against any database.
  Prove "applies and reverts" with an integration test in
  tests/FinNavis.Infrastructure.IntegrationTests/Persistence/ that uses the existing PostgresFixture
  (Testcontainers): migrate to the latest migration, assert the eight tables exist, then migrate back
  to the CoreLedgerInitial migration through IMigrator and assert the eight tables are gone and the
  five ledger tables are still there. Leave the database migrated to latest at the end, because the
  fixture is shared.
- ContainerResolutionTests.AddInfrastructureAndAddApplication_ForPendingPort_ResolveAnAdapter is
  skipped on purpose until T13 registers the import adapters. Do not unskip it. Every other test
  must be green.
- If the diff shows the configuration (T07) is wrong, you may NOT edit Configurations/. Report it in
  not_done with the exact difference. If the staged spec is wrong, fix the spec file and say why in
  the report.
- Do not edit the generated migration to make the diff pass.`,
}

const AUTHOR_SCHEMA = {
  type: 'object',
  properties: {
    task: { type: 'string' },
    worktree_path: { type: 'string', description: 'output of git rev-parse --show-toplevel' },
    head_sha: { type: 'string', description: 'output of git rev-parse HEAD (you did not commit, so this is the base)' },
    changed_files: { type: 'array', items: { type: 'string' }, description: 'git status --porcelain paths, added and modified' },
    build: {
      type: 'object',
      properties: { ok: { type: 'boolean' }, warnings: { type: 'integer' }, summary: { type: 'string' } },
      required: ['ok', 'warnings', 'summary'],
    },
    tests: {
      type: 'object',
      properties: { ok: { type: 'boolean' }, summary: { type: 'string', description: 'per project: passed/failed/skipped' } },
      required: ['ok', 'summary'],
    },
    dod: {
      type: 'array',
      items: {
        type: 'object',
        properties: { item: { type: 'string' }, met: { type: 'boolean' }, evidence: { type: 'string' } },
        required: ['item', 'met', 'evidence'],
      },
    },
    not_done: { type: 'array', items: { type: 'string' } },
  },
  required: ['task', 'worktree_path', 'head_sha', 'changed_files', 'build', 'tests', 'dod', 'not_done'],
}

const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    task: { type: 'string' },
    passed: { type: 'boolean' },
    reasons: { type: 'array', items: { type: 'string' }, description: 'why it failed; empty when passed' },
    files_outside_hint: { type: 'array', items: { type: 'string' } },
    dod: {
      type: 'array',
      items: {
        type: 'object',
        properties: { item: { type: 'string' }, met: { type: 'boolean' }, evidence: { type: 'string' } },
        required: ['item', 'met', 'evidence'],
      },
    },
    convention_issues: { type: 'array', items: { type: 'string' }, description: 'CLAUDE.md rule breaks: comments, ///, sealed, collection expressions, enum as string, etc.' },
    build: {
      type: 'object',
      properties: { ok: { type: 'boolean' }, warnings: { type: 'integer' }, summary: { type: 'string' } },
      required: ['ok', 'warnings', 'summary'],
    },
    tests: {
      type: 'object',
      properties: { ok: { type: 'boolean' }, summary: { type: 'string' } },
      required: ['ok', 'summary'],
    },
  },
  required: ['task', 'passed', 'reasons', 'files_outside_hint', 'dod', 'convention_issues', 'build', 'tests'],
}

const INTEGRATE_SCHEMA = {
  type: 'object',
  properties: {
    ok: { type: 'boolean' },
    stop_reason: { type: 'string', description: 'empty when ok' },
    conflicts: { type: 'array', items: { type: 'string' } },
    commit_sha: { type: 'string' },
    commit_subject: { type: 'string' },
    committed_files: { type: 'array', items: { type: 'string' } },
    build_ok: { type: 'boolean' },
    tests_summary: { type: 'string' },
    worktrees_removed: { type: 'array', items: { type: 'string' } },
  },
  required: ['ok', 'stop_reason', 'conflicts', 'commit_sha', 'commit_subject', 'committed_files', 'build_ok', 'tests_summary', 'worktrees_removed'],
}

const PREFLIGHT_SCHEMA = {
  type: 'object',
  properties: {
    ok: { type: 'boolean' },
    stop_reason: { type: 'string' },
    docker_server_version: { type: 'string' },
    branch: { type: 'string' },
    head_sha: { type: 'string' },
    master_sha: { type: 'string' },
  },
  required: ['ok', 'stop_reason', 'docker_server_version', 'branch', 'head_sha', 'master_sha'],
}

const SAFETY = `
Safety (hard rules):
- Never open, print, copy or stage src/FinNavis.Presentation/Properties/launchSettings.json, .env,
  *.pem or *.key. Never put a connection string, password or key into a shell command.
- Do not touch T01, T02, T09 or later, and do not change any core-ledger code outside your files_hint.`

function hintList(id) {
  return TASKS[id].files_hint.map(p => `  - ${p}`).join('\n')
}

function authorPrompt(id, baseSha, worktreePath) {
  const t = TASKS[id]
  const where = worktreePath
    ? `You work in your own git worktree: ${worktreePath}. cd there first and work only inside it.
Never edit anything in ${REPO} itself, and never run git reset, checkout or switch anywhere.
Before the first edit, "git rev-parse HEAD" in the worktree must print ${baseSha}. If it does not,
change nothing and return with not_done = ["worktree base is wrong"].`
    : `You run in the main checkout ${REPO} on branch ${BRANCH}. Check "git branch --show-current" is
${BRANCH} before the first edit. If it is not, change nothing and return with not_done = ["wrong branch"].`

  return `Implement task ${id} of the csv-import epic in the FinNavis repo.

${where}

Read first:
- ${TASKS_DIR}/${t.file} — the task itself. Its "Definition of Done" checklist is your DoD.
- Whatever it links that you need: docs/features/csv-import/data-model.md, sad.md, adr/*, and the
  staged SQL under docs/features/csv-import/migrations/. CLAUDE.md and .claude/rules/* are already in
  your context; follow them.
- Existing code next to your files, so new code reads like it (naming, style, test style).

Scope — you may create or change ONLY these paths:
${hintList(id)}
Nothing else. If the DoD cannot be met without touching another file, do not touch it — put the
reason in not_done.

Do NOT commit, do NOT stage, do NOT edit tracker.md or any task file.
${TASK_NOTES[id] || ''}
${SAFETY}

Definition of Done for you:
1. Every checkbox in the task file's Definition of Done.
2. "bash ./build.sh build" passes with 0 warnings (TreatWarningsAsErrors).
3. Your layer's tests are green: ${t.layerTests.map(p => `dotnet test ${p}`).join(' ; ')}

Return the structured report. In dod, list every checkbox of the task file with met true/false and
short evidence (test name, file, grep result). changed_files is the output of "git status --porcelain".`
}

function reviewPrompt(id, author, path, fullTestNote) {
  const t = TASKS[id]
  return `You are the independent reviewer for task ${id} of the csv-import epic in the FinNavis repo.
You did not write this code. Your job is to find reasons it should NOT pass. Default to failing a
DoD item when the evidence is missing.

Work in: ${path}  (cd there first; do not edit any file, do not commit, do not stage).
The change is uncommitted: inspect it with "git status --porcelain", "git diff" and by reading
untracked files.

Inputs:
- Task file: ${TASKS_DIR}/${t.file} — its Definition of Done checklist.
- Allowed paths (files_hint):
${hintList(id)}
- The author's own report (do not trust it, verify it): ${JSON.stringify(author)}

Check:
1. Every changed or new file is inside files_hint. List any file outside it in files_outside_hint.
   tracker.md or any task file changed = fail.
2. Every DoD checkbox of the task file — verify it yourself in the code and tests, with evidence.
3. CLAUDE.md conventions on the new code: no /// comments, no comment that restates code or names a
   document/ADR/AC (except where the task file explicitly asks for a comment), collection
   expressions, sealed by default, one public type per file, enums stored as strings, money decimal,
   DateTimeOffset, test names Method_Scenario_ExpectedResult, AwesomeAssertions .Should().
   Domain and Application csproj: no new PackageReference or ProjectReference.
4. Run it yourself: "bash ./build.sh build" (0 warnings required), then ${fullTestNote}

passed = true only if: no file outside the hint, every DoD item met, build ok with 0 warnings, tests
ok as defined above, and no convention issue that breaks a CLAUDE.md "never/must" rule. Minor style
remarks go to convention_issues but do not fail the gate.${SAFETY}`
}

const FULL_TESTS = `"bash ./build.sh test" (all .NET tests, Docker is available). It must be fully green.`
// The core-ledger test below finds every port by reflection and needs an adapter for each one.
// The adapters for the new import ports arrive in T09-T12 and their registration in T13, so until
// then this one test is red by plan, not by a defect.
const KNOWN_RED = `ONE known failure is allowed, and nothing else:
ContainerResolutionTests.AddInfrastructureAndAddApplication_ForEveryDeclaredPort_ResolveAnAdapter,
only for the ports IMappingProfileRepository, IImportRunRepository, IStatementReader and
IImportQueries, and only with the error "No service for type ... has been registered".
Open the failure output and confirm each failing case matches exactly this. Any other failing test,
any other port, or any other error fails the gate. Report the known failures in tests.summary.`
const GATE_TESTS = `"bash ./build.sh test" (all .NET tests, Docker is available). ${KNOWN_RED}`
// Since commit fa5edfc the four import ports sit in their own skipped theory, so from T07 on
// no failure is known any more: one skipped test is expected, and nothing may fail.
const SKIPPED_PORTS = `One skipped test is expected:
ContainerResolutionTests.AddInfrastructureAndAddApplication_ForPendingPort_ResolveAnAdapter
("unskip when repositories are added"). Do not unskip it. No test may fail.`
const STRICT_TESTS = `"bash ./build.sh test" (all .NET tests, Docker is available). ${SKIPPED_PORTS}`
const T07_TESTS = `"bash ./build.sh test". Unit and architecture tests must be fully green. Integration
tests are allowed to fail ONLY because of EF Core PendingModelChangesWarning at fixture start-up
(the migration arrives in T08). Open the failure output and confirm that is the only cause; any other
integration failure fails the gate. ${SKIPPED_PORTS.replace('No test may fail.', '')}`

function integratePrompt(stage, ids, items, commitHint) {
  const sources = items.map(x => `- ${x.id}: worktree ${x.author.worktree_path}, files: ${JSON.stringify(x.author.changed_files)}`).join('\n')
  const fromWorktrees = items.some(x => x.worktree)
  const mergeSteps = fromWorktrees
    ? `Merge the three worktrees into ${BRANCH} in the main checkout ${REPO}. Their changes are
UNCOMMITTED. For each worktree, in this order (${ids.join(', ')}):
  a. cd into the worktree, "git add -A" there (staging inside the worktree only), then
     "git diff --cached --binary > <temp dir outside the repo>/<id>.patch".
Then in ${REPO}:
  b. Run "git apply --check" for ALL three patches first. If any check fails, or two patches touch
     the same file: STOP. Apply nothing. Do not resolve anything. Return ok=false with the conflict.
  c. Apply the three patches with "git apply".
Sources:
${sources}`
    : `The change is already uncommitted in the main checkout ${REPO}:
${sources}`

  return `You are the integrator for ${stage} of the csv-import epic in the FinNavis repo.
The independent reviewer already passed every task of this stage.

1. In ${REPO} check "git branch --show-current" is ${BRANCH}. If not, STOP (ok=false).
2. ${mergeSteps}
3. In ${REPO} run "bash ./build.sh build" (0 warnings) and ${stage.includes('T07') ? T07_TESTS : stage.includes('T03') ? FULL_TESTS : stage.includes('T08') ? STRICT_TESTS : GATE_TESTS}
   If either fails: STOP, commit nothing, return ok=false with the failure.
4. Mark the tasks of this stage as done, in two places, and change nothing else in either file:
   a. ${TASKS_DIR}/tracker.md: in the row of each of ${ids.join(', ')}, set the Status column to "done".
   b. The task file of each of ${ids.join(', ')} (${ids.map(id => `${TASKS_DIR}/${TASKS[id].file}`).join(', ')}):
      in the YAML frontmatter, set status: "todo" to status: "done".
   Then check with "git diff" that tracker.md changed exactly ${ids.length} row(s) and each task file
   changed exactly one line. If not, STOP (ok=false) and commit nothing.
5. Stage ONLY the changed files of this stage plus tracker.md and the task files from step 4, by
   explicit path (never "git add -A"
   or "git add ." in ${REPO}). Run "git status" and confirm no launchSettings.json, .env, *.pem,
   *.key and nothing under .claude/workflows/ is staged.
6. Commit with a Conventional Commit (.claude/rules/git.md): type feat, scope csv-import, imperative
   lowercase subject <= 72 chars, body wrapped at 100 that says why and names the ADRs by path that
   the change carries out. Suggested subject: "${commitHint}". No attribution trailer.
7. ${fromWorktrees ? 'After a successful commit, remove the three worktrees with "git worktree remove --force <path>" (they are detached, so there is no branch to delete). If a removal is denied, leave it and list it as not removed. If you stopped earlier, leave them all in place.' : 'Nothing to clean up.'}
Do NOT push. Do NOT open a PR.${SAFETY}`
}

const stages = []
const spent = () => budget.spent()

function stopWith(stage, reason, extra) {
  return { status: 'stopped', stopped_at: stage, reason, stages, output_tokens_spent: spent(), ...(extra || {}) }
}

// ---------- Preflight ----------
phase('Preflight')
const pre = await agent(
  `Preflight for a run in ${REPO}. Do not edit any tracked file.
1. Run "docker info --format '{{.ServerVersion}}'". If it fails, ok=false: Docker is needed by T08
   (Testcontainers).
2. Branch: if ${BRANCH} does not exist, create it from master ("git switch -c ${BRANCH} master").
   If it exists, switch to it. Then it must satisfy: "git merge-base --is-ancestor master HEAD"
   succeeds, and "git diff --name-only master...HEAD" lists only files under .claude/workflows/
   (the commit that added this workflow is allowed; any other work on the branch is not).
   Otherwise ok=false.
3. "git status --porcelain" must be empty (untracked files under .claude/workflows/ are allowed).
   Otherwise ok=false.
Return the structured result.`,
  { label: 'preflight', phase: 'Preflight', schema: PREFLIGHT_SCHEMA, effort: 'low' },
)
if (!pre || !pre.ok) return stopWith('Preflight', pre ? pre.stop_reason : 'preflight agent died')
log(`Docker ${pre.docker_server_version}, branch ${pre.branch} at ${pre.head_sha.slice(0, 7)}`)

// ---------- One sequential stage in the main checkout ----------
async function runMainStage(stageName, id, commitHint, testsForReview) {
  phase(stageName)
  const author = await agent(authorPrompt(id, null, false), { label: `author:${id}`, phase: stageName, schema: AUTHOR_SCHEMA })
  if (!author) return { ok: false, reason: `${id} author agent died` }
  const review = await agent(reviewPrompt(id, author, REPO, testsForReview), { label: `review:${id}`, phase: stageName, schema: REVIEW_SCHEMA })
  const record = { stage: stageName, tasks: [{ id, author, review }] }
  stages.push(record)
  if (!review) return { ok: false, reason: `${id} reviewer agent died` }
  if (!review.passed) return { ok: false, reason: `${id} failed review: ${review.reasons.join(' | ')}` }
  const integ = await agent(
    integratePrompt(stageName, [id], [{ id, author, worktree: false }], commitHint),
    { label: `integrate:${id}`, phase: stageName, schema: INTEGRATE_SCHEMA },
  )
  record.integration = integ
  record.output_tokens_after = spent()
  if (!integ || !integ.ok) return { ok: false, reason: integ ? integ.stop_reason : 'integrator died' }
  log(`${stageName} committed ${integ.commit_sha.slice(0, 7)} — ${integ.commit_subject}`)
  return { ok: true, sha: integ.commit_sha }
}

// ---------- Stage 1 · T03 ----------
const s1 = await runMainStage('Stage 1 · T03', 'T03', 'add the import enums and the movement fingerprint', FULL_TESTS)
if (!s1.ok) return stopWith('Stage 1 · T03', s1.reason)

// ---------- Stage 2 · T04–T06 ----------
const S2 = 'Stage 2 · T04–T06'
phase(S2)
const s2ids = ['T04', 'T05', 'T06']
const WT_ROOT = 'F:/Projects/FinNavis/.claude/worktrees'
const wtPath = id => `${WT_ROOT}/csv-${id}`
const setup = await agent(
  `Create three git worktrees for parallel work, from the Stage 1 commit of ${BRANCH}.
Work in ${REPO}. Do not edit any file. Do not run git reset, checkout or switch.
1. For each of ${s2ids.map(wtPath).join(', ')}: if the path already exists, STOP (ok=false) and
   create nothing.
2. Run, one per task:
${s2ids.map(id => `   git worktree add --detach ${wtPath(id)} ${s1.sha}`).join('\n')}
3. In each new worktree, "git rev-parse HEAD" must print ${s1.sha} and "git status --porcelain" must
   be empty. Otherwise ok=false.
Return the structured result.`,
  {
    label: 'setup:worktrees',
    phase: S2,
    effort: 'low',
    schema: {
      type: 'object',
      properties: { ok: { type: 'boolean' }, stop_reason: { type: 'string' }, created: { type: 'array', items: { type: 'string' } } },
      required: ['ok', 'stop_reason', 'created'],
    },
  },
)
if (!setup || !setup.ok) return stopWith(S2, `worktree setup failed: ${setup ? setup.stop_reason : 'setup agent died'}`)
const s2 = await pipeline(
  s2ids,
  id => agent(authorPrompt(id, s1.sha, wtPath(id)), { label: `author:${id}`, phase: S2, schema: AUTHOR_SCHEMA }),
  (author, id) => author
    ? agent(reviewPrompt(id, author, author.worktree_path, GATE_TESTS), { label: `review:${id}`, phase: S2, schema: REVIEW_SCHEMA })
        .then(review => ({ id, author, review, worktree: true }))
    : { id, author: null, review: null, worktree: true },
)
const s2record = { stage: S2, tasks: s2.map((x, i) => x || { id: s2ids[i], author: null, review: null }) }
stages.push(s2record)
const s2failed = s2record.tasks.filter(x => !x.author || !x.review || !x.review.passed)
if (s2failed.length) {
  const reason = s2failed.map(x => `${x.id}: ${!x.author ? 'author died' : !x.review ? 'reviewer died' : x.review.reasons.join(' | ')}`).join(' ; ')
  return stopWith(S2, `review failed — ${reason}. Worktrees are left in place for inspection.`)
}
// Stage 2 was committed by hand (c0bfc7a) after the pre-commit hook blocked the integrator, so a
// resumed run passes args.stage2Commit and skips the integrator instead of applying the patches twice.
const integ2 = args && args.stage2Commit
  ? { ok: true, stop_reason: '', conflicts: [], commit_sha: args.stage2Commit, commit_subject: 'committed by hand', committed_files: [], build_ok: true, tests_summary: 'see the stage 2 reviews', worktrees_removed: [] }
  : await agent(
  integratePrompt(S2, s2ids, s2record.tasks, 'add the import aggregates and the reader and query ports'),
  { label: 'integrate:T04–T06', phase: S2, schema: INTEGRATE_SCHEMA },
)
s2record.integration = integ2
s2record.output_tokens_after = spent()
if (!integ2 || !integ2.ok) return stopWith(S2, integ2 ? `integration stopped: ${integ2.stop_reason} ${integ2.conflicts.join(', ')}` : 'integrator died')
log(`${S2} committed ${integ2.commit_sha.slice(0, 7)} — ${integ2.commit_subject}`)

// ---------- Stage 3 · T07 ----------
const s3 = await runMainStage('Stage 3 · T07', 'T07', 'map the import tables in the DbContext', T07_TESTS)
if (!s3.ok) return stopWith('Stage 3 · T07', s3.reason)

// ---------- Stage 4 · T08 ----------
const s4 = await runMainStage('Stage 4 · T08', 'T08', 'add the AddCsvImport migration', STRICT_TESTS)
if (!s4.ok) return stopWith('Stage 4 · T08', s4.reason)

// ---------- Summary ----------
return {
  status: 'completed',
  per_task: stages.flatMap(s => s.tasks.map(t => ({
    stage: s.stage,
    id: t.id,
    changed_files: t.author ? t.author.changed_files : [],
    author_not_done: t.author ? t.author.not_done : ['author died'],
    review_passed: t.review ? t.review.passed : false,
    review_reasons: t.review ? t.review.reasons : [],
    convention_issues: t.review ? t.review.convention_issues : [],
    build: t.review ? t.review.build : null,
    tests: t.review ? t.review.tests : null,
  }))),
  commits: stages.map(s => s.integration && { stage: s.stage, sha: s.integration.commit_sha, subject: s.integration.commit_subject }),
  output_tokens_per_stage: stages.map(s => ({ stage: s.stage, cumulative: s.output_tokens_after })),
  output_tokens_spent: spent(),
}
