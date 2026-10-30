# PROMPT.md · core-ledger · усі задачі, що лишились

Цей файл читають два раннери. `ralph.sh` подає його холодним стартом щоітерації. Команда `/goal`
тримає одну сесію і не дає вийти, доки умова не виконана. Кроки нижче однакові для обох.

## Контекст

Ти працюєш у проекті **FinNavis** — особистий фінансовий REST API, один власник, self-hosted, без
автентифікації.

Стек: .NET 10 / C# 14 (nullable on, `TreatWarningsAsErrors`), xUnit + AwesomeAssertions. Чотири
кільця: `Domain` → `Application` → `Infrastructure` → `Presentation`, залежності тільки всередину.

Цей зріз — **решта епіку core-ledger цілком**: ADR про коди помилок, CI, адаптери і запити
Infrastructure, композиційний корінь, усі use cases, усі HTTP-ендпоінти, три quality gate і звірка
згенерованого документа з контрактом. **Потрібен запущений Docker.** Домен, конфігурації EF,
міграція, compose-стек і фікстура Testcontainers уже є — T01, T02, T05–T12 закриті.

Прочитай перед роботою:

- `CLAUDE.md` у корені — конвенції, заборони, команди. Особливо розділи Database, Dependency
  injection, API і Tests.
- `ARCHITECTURE.md` — чотири кільця, правило портів, «Why Infrastructure has no reference to
  Domain».
- `.claude/rules/git.md`, `.claude/rules/language.md`, `.claude/rules/openapi.md`,
  `.claude/rules/migrations.md`.
- `docs/features/core-ledger/sad.md` — §5 (будівельні блоки), §6 (усі потоки), §8 (наскрізні
  рішення), §10 (quality gates), §11 (ризики), §12 (словник).
- `docs/features/core-ledger/PRD.md` — AC, на які посилається задача. Формулювання відмови береться
  звідти дослівно.
- `docs/features/core-ledger/data-model.md` — таблиці, індекси, 8 рішень.
- `docs/features/core-ledger/adr/` — ADR-0001 … ADR-0006. `docs/adr/` — рішення рівня проекту.
- `docs/features/core-ledger/contracts/openapi.yaml` — **контракт це джерело правди**, обробник
  слідує за ним, ніколи навпаки. `contracts/api-sync-report.md` — що вже вирішено і що парковано.
- `docs/features/core-ledger/tasks/tracker.md` — статуси.
- `docs/features/core-ledger/tasks/T<NN>-*.md` — файл задачі, яку взяв у роботу.

## Завдання

Закрити **рівно ці двадцять чотири задачі** епіку core-ledger і зупинитися.

| Задача | Файл | Залежить від | Коротко |
|---|---|---|---|
| T04 | `T04-adr-notfound-module-convention.md` | — | ADR: що називає модуль у `*.NotFound` |
| T03 | `T03-setup-ci-build-and-test.md` | T02 (done) | GitHub Actions: build + test |
| T13 | `T13-infra-repositories-and-unit-of-work.md` | T12, T10 (done) | 5 адаптерів + `UnitOfWork` + 3 перевірки імені |
| T14 | `T14-infra-queries-balances.md` | T12, T10 (done) | Баланс рахунку + 2 перевірки перед видаленням |
| T15 | `T15-infra-queries-history.md` | T12, T10 (done) | `UNION ALL` + keyset-курсор |
| T16 | `T16-infra-queries-breakdown.md` | T12, T10 (done) | Агрегат по періоду |
| T17 | `T17-wiring-dependency-injection.md` | T01, T13–T16 | `AddInfrastructure` + `AddApplication` + `Program.cs` |
| T18 | `T18-app-accounts-use-cases.md` | T13, T14 | 4 сценарії по рахунках |
| T19 | `T19-app-category-groups-use-cases.md` | T13 | 4 сценарії по групах |
| T20 | `T20-app-categories-use-cases.md` | T13, T14 | 5 сценаріїв по категоріях |
| T21 | `T21-app-transactions-use-cases.md` | T13 | 3 сценарії по транзакціях |
| T22 | `T22-app-transfers-use-cases.md` | T13 | 3 сценарії по переказах |
| T23 | `T23-app-reporting-use-cases.md` | T15, T16 | Історія + розріз |
| T24 | `T24-ports-error-envelope-and-registry.md` | T05 (done), T04 | Реєстр помилок + плоский конверт |
| T25 | `T25-ports-accounts-endpoints.md` | T18, T24, T17 | 4 ендпоінти |
| T26 | `T26-ports-category-groups-endpoints.md` | T19, T24, T17 | 4 ендпоінти |
| T27 | `T27-ports-categories-endpoints.md` | T20, T24, T17 | 5 ендпоінтів |
| T28 | `T28-ports-transactions-endpoints.md` | T21, T24, T17 | 3 ендпоінти |
| T29 | `T29-ports-transfers-endpoints.md` | T22, T24, T17 | 3 ендпоінти |
| T30 | `T30-ports-reporting-endpoints.md` | T23, T24, T17 | Історія + розріз |
| T31 | `T31-tests-qg1-balance-property.md` | T25, T28, T29 | QG-1: property-тест балансу |
| T32 | `T32-tests-qg2-latency-and-explain.md` | T30, T25 | QG-2: затримка + `EXPLAIN` |
| T33 | `T33-tests-qg3-breakdown-stability.md` | T30, T27, T26 | QG-3: стабільність звіту |
| T34 | `T34-docs-contract-diff-and-conventions.md` | T25–T30 | Згенерований документ проти контракту |

Усі файли — у `docs/features/core-ledger/tasks/`. **Порядок жорсткий: згори вниз.** Він
задовольняє всі залежності з колонки, тому іншого порядку не шукай і задачі не переставляй.

Порядок також розводить задачі, які правлять один файл: T17 і T24 обидві чіпають `Program.cs`,
T03 і T32 обидві чіпають `.github/workflows/ci.yml`.

T04 стоїть першою не випадково. Вона вирішує, що називає модуль у коді `*.NotFound`, а T24 зашиває
всі 27 рядків у реєстр. Вирішити після T24 означає переписати і реєстр, і кожен тест ендпоінта.

Одна ітерація — це одна задача. Кроки однієї ітерації:

1. Обери задачу в такому порядку:
   - у корені є файл `BLOCKED` → нічого не роби і вийди;
   - усі двадцять чотири задачі мають статус `done` → переходь до кроку 9;
   - якась із них має статус `in_progress` → продовжуй її: попередня ітерація її не закінчила;
   - інакше візьми **першу згори** задачу з таблиці, у якої статус `todo`.
2. Постав їй `in_progress` у `tracker.md` і в `status:` у frontmatter її `T<NN>-*.md`. У тому ж
   frontmatter збільш `attempts:` на 1; якщо поля немає — додай `attempts: 1`. Роби це до роботи над
   задачею: так перервана ітерація теж рахується. Якщо після збільшення `attempts` більше за 3 —
   задачу не роби, переходь до кроку 8.
3. Прочитай її `T<NN>-*.md` повністю — `## Why`, `## What`, `## Definition of Done`, `## Notes` — і
   документи, на які він посилається.
4. Зроби задачу. Дотримуйся конвенцій `CLAUDE.md`.
5. Перевірка: `./build.sh build` і `./build.sh test` мають бути зеленими. Warnings тут це помилки.
6. Один atomic commit на задачу, Conventional Commits за `.claude/rules/git.md`. Гілка вже створена,
   у `master` не комітити, `git push` не робити.
7. Постав `done` у `tracker.md` і у frontmatter задачі. Це входить у той самий commit. Після commit
   **вийди**, навіть якщо в таблиці ще є `todo`: наступну задачу візьме наступна ітерація з чистішим
   контекстом. Виняток — це була остання задача: тоді спершу виконай крок 9.
8. Задача не закрилась за 3 спроби (`attempts` більше за 3) → постав `blocked` у `tracker.md` і в
   `status:` у frontmatter, допиши в `tracker.md` причину одним рядком: що саме зараз не проходить
   (build, test, Docker чи пункт DoD). Створи файл `BLOCKED` у корені і вийди. Нічого не комміть —
   рішення далі за власником.
9. Усі двадцять чотири задачі `done` → створи файл `DONE` у корені і вийди.

## DoD

DoD кожної задачі — це чекліст `## Definition of Done` у її `T<NN>-*.md`, дослівно. Задача зроблена,
коли **кожен** пункт того чекліста виконано. Не додавай своїх критеріїв і не прибирай чужих.

Плюс жорсткі правила епіку (`_epic.md` §Risks / Hard rules, `CLAUDE.md`):

- **Схема — тупе сховище.** Жодного `CHECK` на бізнес-правило, тригера, збереженої процедури,
  бізнес-`DEFAULT` чи каскаду на FK. Інваріант відмовляє сутність, а не схема.
- **Група `Default` — не рядок, не сутність і не id.** Її виробляє розріз для категорій без групи
  (sad.md §12). Хто створив реальний рядок `Default`, той неправильно прочитав AC-29.
- **Кожна відповідь про помилку — плоский конверт `{code, message, details?}`.** Стандартний
  `application/problem+json` від ASP.NET Core треба вимкнути, інакше API відповідає двома різними
  формами залежно від того, як глибоко запит пройшов конвеєр.
- **Код помилки — `Module.ErrorName` у PascalCase.** Один код у контракті = одна константа в
  реєстрі, що повертає той самий рядок. Ключі JSON лишаються snake_case, включно з усім усередині
  `details`.
- **Гроші — `decimal` у коді і `string` на дроті.** Число в JSON більшість клієнтів читає як IEEE
  double і втрачає четвертий знак `NUMERIC(19,4)`.
- **Контракт — джерело правди.** Документ на `/openapi/v1.json` генерується з коду і **звіряється з**
  контрактом, ніколи навпаки.
- **Жоден record відповіді не ділиться між операціями**, навіть коли дві виглядають однаково. Одна
  папка `Presentation/Endpoints/<Resource>/<Operation>/` володіє своїми request і response.
- **Читання не завантажує сутності в пам'ять.** Агрегація виконується в PostgreSQL (ADR-0004).
- **Розріз ніколи не читає `transfers`.** AC-20 виконано структурно (ADR-0001).
- **Enum зберігається рядком, ніколи не int.**
- **Залежності вказують тільки всередину.** `Infrastructure.csproj` містить тільки `Application`.
  Application не знає про EF Core, HTTP чи PostgreSQL. `tests/FinNavis.ArchitectureTests` це
  перевіряє.
- **`CancellationToken` проходить через кожен async-виклик.**
- **Юніт-тести не чіпають БД і мережу.** Інтеграційні йдуть через фікстуру Testcontainers з T02.
- **Формулювання відмови береться з AC у `PRD.md` дослівно**, не вигадується.
- **Жодної реальної банківської виписки, реального експорту, реального connection string чи ключа.**
  Фікстури синтетичні.

## Документи: що міняти можна, а що ні

За замовчуванням `CLAUDE.md`, `.claude/rules/*`, ADR, `data-model.md`, `sad.md`, `PRD.md` і
контракти **не чіпаються**. Три задачі — виняток, і тільки в межах свого DoD:

- **T04** пише новий ADR у `docs/adr/`, застосовує рішення до контрактів **обох** фіч —
  `core-ledger` і `csv-import` — переганяє обидва `api-sync-report.md` через `--reconcile`, і
  оновлює `CLAUDE.md` §API та `.claude/rules/openapi.md` §7, якщо правило змінює те, що там
  написано. ADR мусить назвати відкинутий варіант і його чесну ціну.
- **T34** звіряє згенерований `/openapi/v1.json` з контрактом і прибирає записи зі списку
  "Not set up yet" у `CLAUDE.md`.
- **T32** дописує у `.github/workflows/ci.yml`, який створює T03.

Рішення, уже зафіксовані в `api-sync-report.md`, не переглядаються: розмір сторінки (1…200, за
замовчуванням 50), непрозорий курсор, фільтр за категорією повністю виключає перекази, порожня
група в розрізі не показується, окремої операції "список рахунків" немає.

## Не роби

- Не бери задачі поза цими двадцятьма чотирма і не міняй їхній порядок.
- Не міняй тести, щоб вони зеленіли; падає тест → фіксь код. Не викидай і не позначай `Skip` тест,
  який заважає.
- Жодних `--dangerously-skip-permissions`; максимум `acceptEdits`.
- Не редагуй застосовану міграцію. Потрібна зміна схеми — нова міграція, і спершу перечитай
  `data-model.md`: майже завжди помиляється запит, а не схема.
- Не створюй сутність чи рядок для групи `Default`.
- Не додавай `Features/` всередину Application: бізнес-області лежать у корені проекту.
- Не додавай інтерфейс на кожен use case — вхідних портів у нас немає, Presentation викликає класи
  use case напряму.
- Не вводь `spectral lint` у CI: його парковано на api-forge gate і він не входить у цей епік.
- Не комміть `launchSettings.json`, `.env`, `*.pem`, `*.key`, `cost.log`, `DONE`, `BLOCKED`.
- Не бери дві задачі за одну ітерацію і не бундли рефакторинг у commit задачі.
- Не роби `git push` і не відкривай PR — це рішення власника.

## Відоме про оточення

- Remote — GitHub (`git@github.com:andrey-o-sim/FinNavis.git`), тому CI в T03 це GitHub Actions у
  `.github/workflows/ci.yml`. Папки `.github` ще немає. На раннері потрібні .NET 10 SDK і Docker,
  інакше інтеграційні тести мовчки не запустяться — а DoD T03 вимагає, щоб вони саме запускались.
- `./build.sh run` у DoD T17 піднімає API проти compose-стека з T01. Для нього потрібен
  git-ignored `.env` у корені; перелік змінних — у шапці `docker-compose.yml`.
- Design-time фабрика `FinNavisDbContextFactory` читає рядок підключення зі змінної оточення
  `ConnectionStrings__FinNavisDb`. Чи пробрасує `dotnet ef` змінні з `launchSettings.json` — не
  перевірено. Якщо `./build.sh db-update` впаде на хості `connection-string-is-unset.invalid`,
  задай змінну явно; це не привід міняти фабрику.
- Папка `src/FinNavis.Infrastructure/Persistence/Migrations` позначена в `.editorconfig` як
  згенерований код. Нові файли поза нею підпорядковуються звичайним правилам стилю.
- `./build.sh` не має задачі `check`. Стиль тримає `TreatWarningsAsErrors` разом з
  `EnforceCodeStyleInBuild`, тож попередження в збірці — це помилка.
