# Ask style — questions for a senior owner working alone

Every `AskUserQuestion` in this skill follows these rules. They are not the generic ones: the generic
style glosses every technical term for a junior reading the repo for the first time. Here the reader is
the person who wrote the repo.

## The two rules that matter

1. **Explain the trade-off, not the vocabulary.** Do not gloss `index`, `migration`, `invariant`,
   `mutex`, `DI`. The reader knows them, and glossing them reads as condescension. Do explain what the
   choice costs: what has to be rebuilt later, what data needs a backfill, how many evenings the other
   option adds.
2. **An option label is the next mechanical step, not a name.** "Прийняти як є" tells the user what
   happens. "Option A" does not.

## Language

Mirror the language the user wrote in. They opened the conversation in some language; every question,
label and description follows it. Do not switch, and do not ask which language they prefer. The artifact
is still written in English — only the conversation mirrors them.

Keep identifiers and domain terms in their original form: `CONTEXT.md`, `AC-04`, RICE, `Default group`.
They are names, not jargon to translate.

## Shape

- **`question`** — three short blocks, each one or two sentences:
  - **what we are deciding**, with a concrete example from this feature;
  - **why it matters** — what it costs if it is wrong, and whether it is reversible;
  - a nudge to read the options.
- **Name the section, never just number it.** `§5 (Acceptance criteria)`, `§8 (Open questions)`,
  `§10 (Refusal messages)`. A bare `§8` forces the reader to remember a numbering they have no
  reason to hold in their head — and in a question they cannot look it up, because the document is
  not on screen yet. Name it on every mention inside a question, not only the first. The numbering:
  §1 Context · §2 Goals · §3 Non-goals · §4 User stories · §5 Acceptance criteria · §6 NFR ·
  §6.1 Security / privacy · §7 Metrics / KPIs · §8 Open questions · §9 Data impact ·
  §10 Refusal messages.
- **`header`** — ≤12 characters, where a bare number is fine because the question body spells it
  out: `AC-04`, `NFR`, `Размер`, `Глоссарий`.
- **each option**:
  - `label` — 1-5 words, the action form. Put "(Recommended)" on the first option when you recommend one.
  - `description` — 2-4 sentences carrying three things: what I will do next concretely (which section
    changes, which row is added), what it costs or buys, and the hidden catch if there is one
    ("in six months this needs a backfill over every row you have entered by then").

## The canonical action set

```
- label: "Принять как есть"
  description: "Оставляю формулировку дословно и иду к следующему пункту."
- label: "Исправить"
  description: "Ты даёшь новую формулировку, я перегенерирую пункт под неё и спрошу ещё раз. Один
    круг — второй ответ финальный."
- label: "В открытые вопросы (§8 Open questions)"
  description: "Убираю пункт из секции и добавляю строку в §8 (Open questions) с owner и due —
    спрошу их следующим шагом. Без обоих пункт становится Drop."
- label: "Выбросить"
  description: "Убираю пункт. Если он обязательный (тип покрытия или последний AC у истории) —
    переформулирую варианты и спрошу ещё раз."
- label: "Добавить ещё AC"          # §5 (Acceptance criteria) only
  description: "Ты диктуешь критерий своими словами, я оформляю его в Given/When/Then, проставляю тип
    покрытия и задаю по нему один уточняющий вопрос."
```

## Worked rewrite

Too dry — unanswerable without re-reading the draft:

```
Question: "AC-09: approve?"
Options: "Approve" / "Edit" / "Drop"
```

Right — the decision, its cost, and what each answer does:

```
Question: "AC-09 запрещает удалять счёт, на котором есть записанные движения. Альтернатива —
  разрешить удаление и оставить движения висеть без счёта. Решение почти необратимое: если
  разрешить сейчас, позже понадобится чистка данных и правило 'что показывать вместо счёта'
  в каждом отчёте. Посмотри варианты."
Options:
- label: "Принять запрет (Recommended)"
  description: "§5 (Acceptance criteria) оставляет AC-09 как есть, §10 (Refusal messages) получает
    строку 'На счёте есть записанные движения, поэтому удалить его нельзя'. Цена: чтобы убрать
    счёт, сначала надо разобраться с его историей. Выигрыш: баланс и отчёты никогда не ссылаются
    в пустоту."
- label: "Разрешить удаление"
  description: "Переписываю AC-09: счёт удаляется, движения остаются. Тогда нужен второй AC про то,
    что отчёт показывает вместо удалённого счёта, а §9 (Data impact) запишет, что у движения
    появляется состояние 'счёт больше не существует'. Скрытая цена: любой отчёт за прошлый период
    придётся учить работать с дырой."
- label: "В открытые вопросы (§8 Open questions)"
  description: "Убираю AC-09 из §5 (Acceptance criteria), добавляю строку в §8 (Open questions) с
    owner и due. Но тип 'domain invariant' потеряет свой единственный критерий — придётся
    сгенерировать замену, иначе пол покрытия не закроется."
```

The second version costs three more lines and removes a round trip. That is the trade.

## Forbidden

- Terse labels with no action (`Approve`, `Yes`, `Option B`).
- One-line descriptions on a decision that is hard to reverse.
- Glossing terms the owner uses daily.
- Hiding the catch in a follow-up question ("if you pick this, I'll ask about X later").
- More than 4 options, or more than 4 questions in one call.
- Asking a question whose answer is already in `idea-brief.md` §13 (Recommendation) or in
  `CONTEXT.md`.
- A bare `§8` anywhere in a question or an option. Name the section every time.
