<!-- Scaffolds for finnavis-complete-sequence-diagrams. Each block is embedded INLINE in           -->
<!-- docs/features/<slug>/sad.md §6, appended after the last existing flow.                        -->
<!--                                                                                               -->
<!-- Participants are NOT placeholders. They are the containers §5 of this feature's sad.md         -->
<!-- declared, with the repo's fixed aliases (references/participants.md). Replace the <...> text   -->
<!-- in messages, notes and headings — never the participant names.                                 -->
<!--                                                                                                -->
<!-- Heading format, so the next audit can trace it:                                                -->
<!--   **Critical flow <N>: <what happens>** (US-<nn> — AC-<nn>, AC-<nn>)                           -->
<!-- <N> is the next free number. Numbers already committed never move.                             -->

---

## Write flow — anything that changes the ledger

<!-- Shows: the rules running in Domain, the unit of work committing, and what is NOT written on   -->
<!-- each refusal. Refusal wording comes from PRD §10 word for word.                                -->

**Critical flow <N>: <what happens>** (US-<nn> — AC-<nn>, AC-<nn>)

```mermaid
sequenceDiagram
    actor Owner
    participant Pres as Presentation
    participant App as Application
    participant Dom as Domain
    participant Inf as Infrastructure
    participant PG as PostgreSQL

    Note over Owner,PG: Precondition: <state required before this flow, from the PRD>
    Owner->>Pres: <what the owner asks for, in product words>
    Pres->>App: <UseCaseName> use case
    App->>Inf: <what the use case needs loaded>
    Inf->>PG: <read>
    PG-->>Inf: rows
    Inf-->>App: <entities, or not found>
    alt <refusal condition, named as the PRD names it>
        App-->>Pres: refuse and say <PRD §10 wording for this AC, verbatim>
        Pres-->>Owner: <what was NOT written — "nothing is created", "neither account changed">
    else <the valid path>
        App->>Dom: <build or change the entity, check the rules>
        Dom-->>App: <a valid result>
        App->>Inf: <add or update> and commit the unit of work
        Inf->>PG: <one insert / update / delete>
        PG-->>Inf: committed
        Inf-->>App: done
        App-->>Pres: <the result>
        Pres-->>Owner: <confirmation, in words the owner would use>
    end
    Note over Owner,PG: <the invariant this flow preserves, when there is one>
```

<!-- When the commit itself can fail and the failure matters — a multi-row write, a transfer —      -->
<!-- nest this inside the valid path instead of ending at "committed":                              -->
<!--                                                                                                -->
<!--     alt the commit fails                                                                       -->
<!--         PG-->>Inf: rolled back                                                                 -->
<!--         Inf-->>App: failure                                                                    -->
<!--         App-->>Pres: report the failure                                                        -->
<!--         Pres-->>Owner: <nothing changed — name both sides>                                     -->
<!--     else the commit succeeds                                                                   -->
<!--         ...                                                                                    -->
<!--     end                                                                                        -->

---

## Read flow — balances, history, a breakdown

<!-- NO Domain participant. The read path goes through a query port (ADR-0004), so no entity is     -->
<!-- loaded — and since the arrows cannot show that, a note says it.                                -->
<!-- Every read is an index hint: name the table and the column in a note, and repeat it in the     -->
<!-- Notes-flagged section for the data-model stage.                                                 -->

**Critical flow <N>: <what is read>** (US-<nn> — AC-<nn>, AC-<nn>)

```mermaid
sequenceDiagram
    actor Owner
    participant Pres as Presentation
    participant App as Application
    participant Inf as Infrastructure
    participant PG as PostgreSQL

    Note over Owner,PG: Precondition: <what is true before — e.g. no balance is stored anywhere>
    Owner->>Pres: <what the owner asks to see>
    Pres->>App: <UseCaseName> use case<, carrying the filters>
    alt <the owner named something the ledger does not have>
        App-->>Pres: refuse and say <PRD §10 wording, verbatim>
        Pres-->>Owner: <nothing is invented — say it>
    else <everything named exists>
        App->>Inf: ask <IQueryPortName> for <what>
        Note over App,Inf: no repository is used here, so no entity is loaded into memory
        Inf->>PG: <one query, described in product words>
        Note over Inf,PG: reads <table> by <column> (informs data-model indexes)
        Note over Inf,PG: <what is deliberately NOT read, when that is a guarantee>
        PG-->>Inf: <the rows>
        Inf-->>App: <the result>
        App->>App: <any shaping the use case does in memory>
        App-->>Pres: <the answer>
        Pres-->>Owner: <what the owner sees>
    end
    Note over Owner,PG: Postcondition: <what holds afterwards — reads change nothing>
```

---

## Refusal-only flow — the flow exists to show that something cannot happen

<!-- Use when the whole point is a rule that refuses: "turn a transaction into a transfer",         -->
<!-- "delete an account that has movements". Keep it short. The value is the refusal and the way   -->
<!-- out, not the setup.                                                                            -->

**Critical flow <N>: refuse to <what>** (US-<nn> — AC-<nn>)

```mermaid
sequenceDiagram
    actor Owner
    participant Pres as Presentation
    participant App as Application
    participant Dom as Domain
    participant Inf as Infrastructure
    participant PG as PostgreSQL

    Note over Owner,PG: Precondition: <the rule, stated as a fact about the model>
    Owner->>Pres: <what the owner tries to do>
    Pres->>App: <UseCaseName> use case
    App->>Inf: load the entry
    Inf->>PG: read one row
    PG-->>Inf: row or nothing
    Inf-->>App: <the entity, or not found>
    App->>Dom: apply the change
    Dom-->>App: rejected by the rule, because <why, in domain terms>
    App-->>Pres: refuse and say <PRD §10 wording, verbatim>
    Pres-->>Owner: <nothing changed, no unit of work committed, nothing created instead>
    Note over Owner,PG: <the way through for the owner — "remove it in flow 4, then record it in flow 2">
```
