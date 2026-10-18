<!-- Written at the END of §6 in docs/features/<slug>/sad.md, after the last flow.                -->
<!-- If the section already exists, MERGE into it — never add a second one.                        -->
<!-- All four buckets appear every time. A bucket with nothing in it gets one honest line, not a   -->
<!-- deletion: an absent bucket reads as forgotten, an empty one reads as checked.                 -->

### Notes flagged while completing §6

Flagged for the next stages. Nothing here is decided — no ADR is written from this list.

- **Index hints for the data-model stage.** <For each read path and each write the flows show: which
  table, by which column, and what the query asks for. Say when a check only asks whether a row
  exists rather than reading it, because that may not need the same index as an aggregate. Example:
  "movements by account (flow 5's delete check and flow 7's balances), transactions by category
  (flow 6's delete check), and movements by date together with account (flow 8's history page). The
  two delete checks only ask whether a row exists.">

- **Sync or async.** <One line, even when it is boring. Default: "No flow in this feature is
  asynchronous. Every one is request and response, so none carries an idempotency step, a retry note
  or a dead-letter branch." Stated here so a reader does not read the absence as an omission. If §5
  declares an async participant, say instead which flows are async and what they rely on.>

- **Left for the API-contract stage.** <Anything a flow had to assume because no artefact says it: a
  page size, an ordering, a limit, a filter interaction. Name the flow and what is missing, and say
  whether the PRD has an open question covering it. Example: "Flow 8 passes a page because §10 sets a
  per-page latency target, yet neither the PRD nor §10 says what a page holds, and PRD §8 has no open
  question covering it.">

- **Deliberate deviations.** <Where this pass departed from a rule, and why — so a later reader sees
  a choice rather than a slip. A participant drawn that §5 does not declare, a grouping that merged
  two user stories, a refusal reworded because PRD §10 had no row for it. "None this pass." is a
  valid entry.>
