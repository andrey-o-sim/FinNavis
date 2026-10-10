# Socratic bank — single-owner project

Used in Phase 2. Pick 3-5, deliver in batches of 2-3.

The audience is one person: the owner, who is also the only user, and a senior engineer. That
changes what is worth asking. Questions about segments, personas and market sizing have no
answer here. Questions about **what the owner does today instead**, and **what this unblocks**,
carry the priority signal that Reach normally carries.

The questions below are written in English. Ask them in the language the user wrote in —
translate the meaning, not the words. Keep the shape: one idea per question, and a concrete
answer rather than an opinion.

---

## 1. Pain today — what actually hurts

Goal: get a fact with a number in it, not a feeling. Feeds §2 and the Impact part of §11.

- "What do you do today instead of this feature? Walk me through the last time, step by step."
- "How many times in the last month did you do that, and how long did each one take?"
- "When did this last actually annoy you? What happened exactly?"
- "If nothing changes for another six months, what gets worse? Or does nothing get worse?"

**Watch for:** "it is inconvenient" with no example. Push once: "inconvenient — how many
minutes, how often?" A pain nobody can date is usually a want, and §11 prices it accordingly.

## 2. Why the current tool is not enough

Goal: the gap list. On a personal project the tool being replaced **is** the requirements
document. Feeds §2 and §6.

- "What do you use today? Name the tool."
- "List everything it is missing — all of it, even the parts unrelated to this feature."
- "How many items on that list does this feature close? Are the rest separate features, or do
  you not actually need them?"
- "What does the current tool do well, that this has to match?"

**Watch for:** a gap list that is actually one gap retold four times. And the reverse — in
`core-ledger` the user named eight gaps, the feature closed two, and saying so plainly was the
most useful line in §2.

## 3. Success criteria — what "it worked" means

Goal: one metric with a baseline and a target. Feeds §7 outcome metrics and §13.

- "A month after this ships, how will you know it worked? What will you actually do to check?"
- "Name one number that has to change. What is it today?"
- "What becomes possible that is impossible right now?"
- "What result would make you say 'built it, but it was not worth it'?"

**Watch for:** "it will be more convenient". Not measurable, so §7 produces three approaches
that cannot be compared. Push for a count, a duration, or a binary "impossible → possible" —
the last one is a perfectly good metric and often the honest one on a new feature.

## 4. Scope and evenings — what gets cut

Goal: §5 Out of scope, and a realistic Effort for §11.

- "What nearby thing are we deliberately NOT doing in this feature?"
- "Realistically, how many evenings a week can you give this over the next month?"
- "If it turns out to be half that, what is the first thing you drop?"
- "Does anything here have to be ready by a date, or is there no deadline at all?"

**Watch for:** no out-of-scope list at all. A feature with no boundary grows until the owner
stops working on it. Phase 8 will name this as the top abandonment vector, so better to cut it
here.

## 5. Learning goal — why you, why by hand

Goal: feeds the Interest feasibility box and §17. A work process would never ask this; on a pet
project it is the strongest predictor of whether the thing gets finished.

- "Do you want to build this, or do you want it to exist? If it is the second — something
  already does this, why not use that?"
- "What do you want to learn on this feature? Or is it pure routine for the result?"
- "Which part here is the interesting one, and which part is the boring obligation?"

**Watch for:** "I just want it to exist" plus an existing tool that already does it. That is a
legitimate answer and it should go into §13 as a reason to pick the smallest approach — or not
to build it at all. Writing that down is a success for this skill, not a failure.

## 6. What this unlocks — the priority signal

Goal: §16, and the Impact part of §11. With Reach pinned at 1, this is what actually ranks
features against each other.

- "Which future features cannot start until this exists? Name them."
- "If this slips three months, what else stalls?"
- "Is there a decision here that would be expensive to undo later — something that would have
  to be backfilled across every record already entered?"
- "Does this feature give you relief right away, or does it only open the road for others?"

**Watch for:** the last question is the one that saved `core-ledger` from being mispriced. A
pure enabler that gives no immediate relief is the classic abandonment shape: the owner does
all the work and keeps using the old tool. If the answer is "it only opens the road", say so in
§2 and let §11 price it honestly.

---

## Delivery rules

- **2-3 questions per `AskUserQuestion` call.** More and the answers get shallow.
- **One idea per question.** "How many times and how long?" is two; ask it as one only when
  both parts are the same measurement.
- **Give options where a scale helps** (how many evenings, how often), free text where the
  wording matters (what hurts, what it unlocks). The verbatim wording of a pain is worth more
  than a bucketed answer.
- **Never answer on the user's behalf.** If a checkpoint cannot fire, stop and say so.
