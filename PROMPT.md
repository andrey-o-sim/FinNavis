# PROMPT.md · Ralph loop (core-ledger · T05–T11)

## Контекст

Ти працюєш у проекті **FinNavis** — особистий фінансовий REST API, один власник, self-hosted, без
автентифікації.

Стек: .NET 10 / C# 14 (nullable on, `TreatWarningsAsErrors`), xUnit + AwesomeAssertions. Чотири
кільця: `Domain` → `Application` → `Infrastructure` → `Presentation`, залежності тільки всередину.

Цей зріз — **кільце Domain цілком, порти Application і схема EF Core**: примітиви, п'ять сутностей
з їхніми портами, `IUnitOfWork` з `ILedgerQueries`, `FinNavisDbContext` з п'ятьма конфігураціями.
Ні міграції, ні реальної БД, ні HTTP. Docker не потрібен.

Прочитай перед роботою:

- `CLAUDE.md` у корені — конвенції, заборони, команди.
- `.claude/rules/git.md`, `.claude/rules/language.md`, `.claude/rules/migrations.md`.
- `ARCHITECTURE.md` — чотири кільця і правило портів.
- `docs/features/core-ledger/sad.md` §5, §6 і §8, `docs/features/core-ledger/PRD.md` (AC, на які
  посилається задача), `docs/features/core-ledger/data-model.md` (таблиці, індекси, 8 рішень),
  `docs/features/core-ledger/adr/` (ADR-0001, ADR-0003, ADR-0004, ADR-0006).
- `docs/features/core-ledger/tasks/tracker.md` — статуси.
- `docs/features/core-ledger/tasks/T<NN>-*.md` — файл задачі, яку взяв у роботу.

## Завдання

Реалізувати **рівно сім задач** епіку core-ledger і зупинитися. Більше нічого не брати.

| Задача | Файл | Залежить від | Коротко |
|---|---|---|---|
| T05 | `T05-domain-primitives.md` | — | `Money`, `DateRange`, `CategoryKind`, `IClock`, доменна помилка |
| T06 | `T06-domain-account.md` | T05 | `Account` + `IAccountRepository` |
| T07 | `T07-domain-category-and-group.md` | T05 | `Category` + `CategoryGroup` + два порти |
| T08 | `T08-domain-transaction.md` | T05 | `Transaction` + `ITransactionRepository` |
| T09 | `T09-domain-transfer.md` | T05 | `Transfer` + `ITransferRepository` |
| T10 | `T10-app-abstractions.md` | T05 | `IUnitOfWork` + `ILedgerQueries` + read-рядки |
| T11 | `T11-infra-dbcontext-and-configurations.md` | T06, T07, T08, T09 | `FinNavisDbContext` + 5 конфігурацій |

Усі файли — у `docs/features/core-ledger/tasks/`. Порядок жорсткий: згори вниз, T05 → T11. Він
задовольняє всі залежності з таблиці, тому іншого порядку не шукай.

Одна ітерація — це одна задача. Кроки однієї ітерації:

1. Обери задачу в такому порядку:
   - у корені є файл `BLOCKED` → нічого не роби і вийди;
   - усі сім задач мають статус `done` → переходь до кроку 9;
   - якась із T05–T11 має статус `in_progress` → продовжуй її: попередня ітерація її не закінчила;
   - інакше візьми **першу згори** задачу з таблиці, у якої статус `todo`.

   Задачі поза цими сімома не бери, навіть якщо вони `todo` і не заблоковані. Якщо задача, яку ти
   взяв, залежить від задачі з таблиці, у якої статус не `done` — це помилка вибору, повернись до
   початку кроку.
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
   **вийди**, навіть якщо в таблиці ще є `todo`: наступну задачу візьме наступна ітерація з чистим
   контекстом. Виняток — це була остання із семи задач: тоді спершу виконай крок 9.
8. Задача не закрилась за 3 спроби (`attempts` більше за 3) → постав `blocked` у `tracker.md` і в
   `status:` у frontmatter, допиши в `tracker.md` причину одним рядком: що саме зараз не проходить
   (build, test чи пункт DoD). Створи файл `BLOCKED` у корені і вийди. Нічого не комміть — рішення
   далі за власником.
9. Усі сім задач `done` → створи файл `DONE` у корені і вийди. **T12 не починай.**

## DoD

DoD кожної задачі — це чекліст `## Definition of Done` у її `T<NN>-*.md`, дослівно. Задача зроблена,
коли **кожен** пункт того чекліста виконано. Не додавай своїх критеріїв і не прибирай чужих.

Плюс жорсткі правила епіку, які діють у цьому зрізі (`_epic.md` §Risks / Hard rules):

- Гроші — `decimal`, ніколи `double` чи `float`. У схемі — `NUMERIC(19,4)`.
- Enum зберігається рядком, ніколи не int. У конфігурації — `.HasConversion<string>()`.
- Залежності вказують тільки всередину. **Domain не посилається ні на що** — ні
  `ProjectReference`, ні `PackageReference`. Application бачить тільки Domain і не знає про EF Core,
  HTTP чи PostgreSQL. `tests/FinNavis.ArchitectureTests` це перевіряє.
- БД — тупе сховище: жодного `CHECK` на бізнес-правило, жодного тригера, жодного бізнес-`DEFAULT`,
  жодного каскаду на FK (`.claude/rules/migrations.md`). Інваріант відмовляє сутність, а не схема.
- Жодної реальної банківської виписки, реального експорту, реального connection string чи ключа в
  репозиторії. Фікстури синтетичні.

## Не роби

- Не бери задачі поза T05–T11.
- Не міняй тести, щоб вони зеленіли; падає тест → фіксь код.
- Жодних `--dangerously-skip-permissions`; максимум `acceptEdits`.
- Не змінюй `CLAUDE.md`, `.claude/rules/*`, ADR, `data-model.md` чи контракти без окремої задачі,
  яка цього просить.
- Не створюй міграцію: ні `./build.sh migrate`, ні `dotnet ef migrations add`, ні
  `./build.sh db-update` — це T12. У цьому зрізі конфігурації тільки компілюються.
- Не піднімай БД і не пиши інтеграційні тести — fixture на Testcontainers це T02, його ще немає.
- Не пиши репозиторії-адаптери і запити читання — це T13–T16. У цьому зрізі порти тільки
  оголошуються.
- Не реєструй нічого в DI-контейнері — це T17.
- Не додавай полю `Account` балансу і не роби перевірку унікальності імені в сутності — це T14 і
  T18. Унікальність імені категорії за стороною — теж не в сутності, це T19 і T20.
- Не створюй сутність чи рядок для групи `Default` — вона рахується у звіті і ніколи не зберігається.
- Не бери дві задачі за одну ітерацію і не бундли рефакторинг у commit задачі.
- Не роби `git push` і не відкривай PR — це рішення власника.
