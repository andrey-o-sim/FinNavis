---
id: T05
title: "Add the domain primitives: Money, DateRange, CategoryKind, IClock and the domain error"
layer: "domain"
deps: []
acs: []
files_hint: ["src/FinNavis.Domain/ValueObjects/", "src/FinNavis.Domain/Enums/", "src/FinNavis.Domain/Abstractions/IClock.cs", "src/FinNavis.Domain/Errors/", "tests/FinNavis.Domain.UnitTests/ValueObjects/", "tests/FinNavis.Domain.UnitTests/SmokeTests.cs"]
owner: "Andrey Simonenko"
estimate: "S"
status: "done"
attempts: 1
---

# T05 — Add the domain primitives

## Why

[sad.md §5](../sad.md) lists `ValueObjects/`, `Enums/` and `Abstractions/` as part of the Domain
ring. Every entity task depends on these, so they go first and unlock four parallel branches.
[sad.md §8](../sad.md) names the sentinel domain error as the thing Presentation maps to the flat
envelope.

## What

- `ValueObjects/Money` — wraps `decimal`, single currency, no currency code stored
  (`CLAUDE.md`, PRD §3). Never `double` or `float`.
- `ValueObjects/DateRange` — a closed range of calendar days, used by the history filter and the
  breakdown period. Built on `DateOnly`, matching
  [`data-model.md` decision 1](../data-model.md): the date an entry is filed under is a calendar
  day with no time and no timezone.
- `Enums/CategoryKind` — `Expense | Income`. Stored as a string, never as an int (`CLAUDE.md`).
- `Abstractions/IClock` — keeps date rules testable without the machine clock (sad.md §8).
- `Errors/` — the sentinel domain error type carrying a stable code and the refusal message. The
  code strings themselves live in Presentation's registry
  ([T24](./T24-ports-error-envelope-and-registry.md)); Domain carries the error, not the HTTP
  spelling.
- Delete `tests/FinNavis.Domain.UnitTests/SmokeTests.cs`.

## Definition of Done

- [ ] unit tests cover `Money` equality and arithmetic, and `DateRange` boundary days (both ends
      inclusive)
- [ ] `Domain` still references nothing — no `ProjectReference`, no `PackageReference`
      (`CLAUDE.md`); `tests/FinNavis.ArchitectureTests` stays green
- [ ] `SmokeTests.cs` is deleted from `FinNavis.Domain.UnitTests`
- [ ] `./build.sh test` is green, warnings are errors

## Notes

- `DateTimeOffset` stays the type for audit timestamps (`created_at`). `occurred_on` is a
  **different type on purpose** — sad.md §8 keeps the two Time rows apart, and every §10 period
  scenario rests on it.
