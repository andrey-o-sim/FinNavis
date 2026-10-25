---
status: Living
updated_at: "2026-10-25"
---

# Domain Context — FinNavis

<!--
CONTEXT.md is the domain glossary — not a PRD and not a scratch pad. NO implementation
detail here (no datastore/broker/framework names, no API contracts) — only domain words
and the boundaries between them. Implementation choices live in the SAD and ADRs;
behaviour lives in PRD.md.

Terms get fixed inline, the moment they surface in an interview / PRD / review — never
batched «I'll consolidate later».
-->

## Glossary

<!-- One line per term: name · one-sentence canonical definition · one-sentence boundary
     (what it is NOT / the concept it gets confused with). Alphabetical once there are a few. -->
- Account — a place where the owner's money sits, such as cash, a card or a savings account; a transaction is recorded on exactly one account and a transfer touches two. NOT Category (a category says what the money was for, an account says where the money is).
- Balance — how much money sits on one account right now, equal to the sum of every transaction and transfer recorded on that account. NOT a period total (a period total covers only the movements inside a date range, while a balance is the running state of the account at a point in time).
- Breakdown — the totals for one chosen period reported by category and by category group at the same time, with spending and earning kept apart. NOT History (a history lists each movement one by one, while a breakdown reports sums and never names an individual movement).
- Category — a label the owner files a transaction under; each category belongs to either expenses or income, and every transaction names exactly one. NOT Category Group (a group holds categories and is never named by a transaction).
- Category Group — an optional container holding related categories for reporting, for example utilities over electricity, water and gas; a group belongs to one side of the ledger and holds only categories from that same side, and a category belongs to at most one group. NOT Category (a transaction always names a category, never a group) and NOT a container that spans both sides (expenses and income never mix, so a spending group and an earning group may share a name while staying separate things).
- Default group — the bucket a period breakdown reports every category that belongs to no Category Group under, so the group figures always add up to the period total. NOT a Category Group (it is produced by the breakdown rather than created by the owner, so it cannot be renamed, deleted, or picked when filing a transaction).
- Duplicate — a row in a loaded file that names a money movement already recorded in the ledger, so loading that file again must not create a second transaction. NOT two separate transactions that happen to share the same date, amount and text (both are real spending, and both belong in the ledger).
- Export — a file produced by a tracker the owner is leaving, covering all of their data in that tracker's own shape, so it names accounts and categories in the owner's own words and is loaded once. NOT a Statement (a statement comes from a bank, covers one account over a period, and carries only the bank's own categories).
- History — the owner's recorded movements listed newest first over a chosen period, optionally filtered, and read a page at a time. NOT Balance (a history lists the individual movements, while a balance is the single running figure they add up to) and NOT the fact that an account or a category has movements against it (that fact blocks deletion and is about existence, not about a list the owner reads).
- Import run — one load of one file taken as a whole: everything it recorded, everything it found already in the ledger, and everything it set aside, so the owner can read it back or undo it in one move. NOT a Mapping profile (a profile holds the rules for a shape of file and outlives every load; a run is one event with one file).
- Ledger — everything the owner has recorded about their money: every account, category, transaction and transfer taken together, so "in the ledger" means recorded rather than merely seen in a file. NOT Account (an account is one place inside the ledger, while the ledger is the whole record) and NOT a Statement or an Export (those are files from outside that may describe the same movements, and a movement is not in the ledger until it is loaded).
- Mapping profile — the rules remembered for one shape of file, saying what each category that shape carries means in the ledger, so the next file of the same shape needs no confirmation. NOT an Import run (a profile records no movement; a run records movements by following a profile).
- Movement — one recorded change of money on an account, either a transaction or a transfer, used when the difference between the two does not matter — as in a history page, a duplicate check or an account's balance. NOT Transaction (a transaction is one kind of movement and a transfer is the other, so a rule written about movements covers both).
- Owner — the single person whose money this installation tracks and the only actor in the product; every account, category and recorded movement belongs to them. NOT a user account (there is no sign-in and no second person, so records carry no owner field — the owner is a fact about the product, not stored data).
- Set-aside row — a row an Import run read and deliberately did not record, kept together with the reason it was not recorded, so the owner can see it and act on it later. NOT a Duplicate (a duplicate names a movement already in the ledger and needs nothing from the owner; a set-aside row is not in the ledger at all).
- Shape — the arrangement of columns a file carries: which columns there are, named how, in what order, fixed by whoever produced the file. NOT the file itself (one shape outlives any number of files, which is why a Mapping profile is remembered per shape rather than per file).
- Statement — a file produced by a bank, listing the money movements on one account over a period, in a shape the bank controls and the owner cannot change. NOT an Export from a tracker the owner uses (that covers all of the owner's data rather than one account, and already carries categories).
- Transaction — a single income or expense recorded on exactly one account, with an amount, a date, a category, and an optional free-text note. NOT Transfer (a transfer moves money between two of the owner's own accounts, so it touches two accounts, carries no category, and leaves the owner's total unchanged).
- Transfer — moves money between two different accounts of the owner: it decreases one and increases the other by the same amount, carries no category, and leaves the owner's total unchanged. NOT Transaction (a transaction touches one account, has a category, and changes the owner's total).
