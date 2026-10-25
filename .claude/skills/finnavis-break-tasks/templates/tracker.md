# Tracker — <slug>

> Status of every task in the epic. The implement stage updates `status` as it commits each task.
> States: `todo` · `in_progress` · `blocked` · `review` · `done`.
>
> `--reconcile` preserves this column. Nothing else may rewrite it.

| # | Task | Layer | Estimate | Blocked by | Status |
|---|---|---|---|---|---|
| T01 | <title> | <layer> | S | — | todo |
| T02 | <title> | <layer> | M | — | todo |
| T03 | <title> | <layer> | M | T01, T02 | todo |

**Total:** <N> tasks, ~<P> person-days.
**Owner:** Andrey Simonenko — one owner, so the column is not repeated per row.

<!-- instruction: when the person-day total disagrees with ../.size, say so in one line here and
add a parked item in _epic.md. 34 tasks at ~17 person-days is an L wearing an M. -->
