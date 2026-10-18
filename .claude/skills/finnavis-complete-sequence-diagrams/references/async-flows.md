# Async flows — conditional, read only when the gate fires

**Do not read this file by default.** It describes machinery this product does not have today, and
carrying it into a synchronous flow produces a diagram of a system nobody built.

## The gate

Read this file only when **§5 of this feature's `sad.md` declares a participant that makes a flow
asynchronous**. In practice that is one of:

- an object store or file store the service writes to and reads back later,
- a background worker or hosted service that continues after the response is sent,
- a queue, a broker or an event stream,
- a scheduler that starts a flow with no owner behind it,
- a third-party system that calls back.

Everything else is synchronous. The test is not "does this take a long time" — it is **does the flow
continue after the owner's request has been answered**.

If no such participant is in §5, close this file and write the sync/async line in Phase 5:

> No flow in this feature is asynchronous, so none carries an idempotency step, a retry note or a
> dead-letter branch.

That line matters even though it is boring. Without it, a reader cannot tell whether the absence was
decided or forgotten.

## If a flow needs one and §5 does not declare it

Then the gate has **not** fired. This is the ⚠️ case in
[participants.md](participants.md#when-a-flow-needs-a-participant-5-never-declared): the architecture
has a container stage 03 never named, and deciding to add it — where an uploaded file lives, whether
an import runs in the background — is a `finnavis-architecture-design` decision that probably clears
the blast-radius gate and deserves an ADR.

Stop and raise it. Do not draw a broker into existence to make a flow work.

## Shape of an async flow, once §5 declares the participants

Use §5's own names for the new participants. The three rules below are what make an async flow
honest; each exists because the alternative silently loses the owner's data.

**1. Idempotency first.** The first thing the handler does, before any work:

```
Worker->>Worker: check whether this <unit of work> was already processed, and stop if it was
```

Anything that can be delivered twice will be. In a ledger, processing twice means money recorded
twice, and the owner finds it months later.

**2. Retry, stated as a note, not as a loop.** A `loop` in a diagram suggests the reader can see how
many times. They cannot, and the number is a configuration value:

```
Note over Worker,Store: retried on failure, with a growing delay between attempts
```

If the PRD or §10 fixes the number, use it. If it does not, do not invent one — put "how many
retries" into **Notes flagged → left for the API-contract stage**.

**3. What happens when it never succeeds.** The branch everyone forgets:

```
alt it still fails after the last attempt
    Worker->>PG: record the <unit of work> as failed, with the reason
    Note over Worker,PG: the owner can see it failed and why, so nothing disappears silently
end
```

In a single-owner self-hosted product there is no dead-letter queue to route to and nobody on call
to drain it. The honest equivalent is a **visible failed state the owner can read**, which means a
row and a way to see it — and that implies a read flow, which implies an acceptance criterion. If the
PRD has none, that is a real gap: flag it, do not invent the recovery story.

## What stays the same

Everything in [flow-rules.md](flow-rules.md) still applies. Refusals still quote PRD §10, writes
still show the unit of work, reads still go through the query port, messages still carry no HTTP
verbs. Async changes who the participants are and adds three obligations; it does not relax the
rest.
