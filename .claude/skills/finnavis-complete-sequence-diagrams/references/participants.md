# Participants — §5 is the source of truth

Used in Phase 0 to build the list, in Phase 3 whenever a block is drafted, and in Phase 6 as a
backstop.

## The rule in one sentence

**A participant in §6 is a container §5 declared.** Not a name from a template, not a name from a
generic vocabulary, not a name that seemed obvious while drawing.

A diagram is the one place in the document where an invented name looks authoritative. Prose can
hedge; an arrow between two boxes cannot. So the list is closed before any drawing starts.

## The default vocabulary of this repo

These are what §5 declares today. Reuse the exact names and aliases, because §3, §5 and §6 are read
as one story and three spellings of `Infrastructure` break it.

| Alias | Name | Is | Appears in |
|---|---|---|---|
| `Owner` | `Owner` (actor) | The single person the product serves. From `docs/GLOSSARY.md` | §3, §5, §6 |
| `Pres` | `Presentation` | Minimal API endpoints, contracts, composition root | §5, §6 |
| `App` | `Application` | Use cases, `IUnitOfWork`, outside-world ports | §5, §6 |
| `Dom` | `Domain` | Entities, value objects, rules, repository ports | §5, §6 |
| `Inf` | `Infrastructure` | EF Core adapters, `DbContext`, configurations, migrations | §5, §6 |
| `PG` | `PostgreSQL <version>` | The database, a separate process on the same host | §3, §5, §6 |
| `Web` | `Web client` | The React PWA — **only** when `web-frontend` is in `target_surfaces` | §3, §5, §6 |

Declare them in this order, top to bottom, in every block. A reader who has seen one diagram can
then read the next one without re-learning the layout.

Drop the ones a flow does not use. A read flow has no `Dom` line; drawing an unused participant adds
a column of empty space and suggests an interaction that never happens.

## Arrows obey the dependency rule

`CLAUDE.md` fixes the rings: Presentation → Infrastructure → Application → Domain, and Infrastructure
→ Domain. `tests/FinNavis.ArchitectureTests` fails the build when code breaks it, which means a
diagram that breaks it is simply wrong, not stylistically different.

In practice, in a §6 block:

- `Owner ->> Pres` — the request arrives.
- `Pres ->> App` — the endpoint calls the use-case class directly. There is no driving port, so there
  is never a box between these two.
- `App ->> Dom` — the use case builds or changes an entity and lets the rules run.
- `App ->> Inf` — the use case calls a port. `Inf` is the adapter answering it, which is why the
  arrow points outward here while the *dependency* still points inward. Say what is being asked for
  (`load the named account`), not which class answers.
- `Inf ->> PG` — the only participant that ever talks to the database.
- `Pres ->> Inf` never appears in §6. Presentation touches Infrastructure once, at startup, to
  register adapters. That belongs in §5, not in a runtime flow.

## When a flow needs a participant §5 never declared

This is the interesting case, and it is **not** a drawing problem.

If a flow genuinely needs an object store, a background worker, a queue, a mail sender or a third
party, then the architecture of this feature has a container that stage 03 never declared. That is a
`finnavis-architecture-design` gap, and it may well deserve an ADR — where the file lives while it is
being processed is exactly the kind of decision that clears the blast-radius gate.

So: **stop and raise it with ⚠️.** Name three things:

1. the flow that needs it,
2. what the missing participant does in that flow,
3. that §5 of this SAD does not declare it.

Then offer, in one question:

- *Stop here, fix §5 first* (recommended) — the user runs `finnavis-architecture-design` for that
  section, and this pass resumes afterwards.
- *Draw it and flag it* — the block is drawn with the name the user gives it, and a line lands in
  **Notes flagged → deliberate deviations** saying §5 does not carry this container yet. Use this
  when the user wants the flow captured now and knows the SAD is behind.

What never happens: drawing it quietly. A reader six months from now cannot tell an agreed component
from an improvised one, and the diagram is the most trusted thing in the document.

## When §5 *does* declare more

If `target_surfaces` includes `web-frontend`, `Web client` is a real participant: `Owner ->> Web`,
`Web ->> Pres`, and the response travels back the same way, so the owner-visible outcome is shown
rather than implied.

If a future §5 declares an object store, a worker or a queue, use **§5's own name and description**
for it — not `<external-system>`, not `<message-bus>`. Then read
[async-flows.md](async-flows.md), which is what those participants switch on.
