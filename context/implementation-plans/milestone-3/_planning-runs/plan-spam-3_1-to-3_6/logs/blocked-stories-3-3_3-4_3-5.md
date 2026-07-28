# Blocked: Stories 3.3, 3.4, 3.5 — planning gate upheld

- **Date:** 2026-07-28
- **Run:** `plan-spam-3_1-to-3_6`
- **Decision:** Owner, in chat at run start.

`context/milestones/milestone-3.md` (Notes) requires that Stories 3–5 not be planned until
wire contract v1 exists, and that Story 1's open decisions be resolved first. The user's
request named all six stories; when asked, the owner upheld the gate: plan Stories 1, 2,
and 6 only.

These rows stay `BLOCKED` — no `plan.md` exists for `profiles-auth`, `friend-codes`, or
`mailbox`. Requeue them deliberately (BLOCKED → TODO) once `docs/wire-contract-v1.md` is
written and owner-adjudicated. Three of Story 1's four open decisions were adjudicated at
run start (see `../questions.md`); only the endpoint authentication scheme remains open.
