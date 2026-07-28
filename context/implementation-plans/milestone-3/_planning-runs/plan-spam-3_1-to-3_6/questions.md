# Plan Spam Questions — Milestone 3 (Multiplayer Relay), Run `plan-spam-3_1-to-3_6`

Milestone: `context/implementation-plans/milestone-3/`
Run: `context/implementation-plans/milestone-3/_planning-runs/plan-spam-3_1-to-3_6/`
Date: `2026-07-28`
Source: `context/milestones/milestone-3.md`, `context/design.md`, `context/dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md`

Per-plan questions live inside each plan's `## Questions / Unknowns` section under
`context/implementation-plans/milestone-3/`. This file indexes them and carries
cross-run Boss questions.

## Per-Plan Questions Index

| Story | Plan Folder | Questions | Status |
| --- | --- | --- | --- |
| STORY 3.1 | `context/implementation-plans/milestone-3/wire-contract-v1/` | [plan.md#questions--unknowns](../../wire-contract-v1/plan.md#questions--unknowns) | OPEN |
| STORY 3.2 | `context/implementation-plans/milestone-3/relay-skeleton/` | [plan.md#questions--unknowns](../../relay-skeleton/plan.md#questions--unknowns) | OPEN |
| STORY 3.3 | `context/implementation-plans/milestone-3/profiles-auth/` | *not planned — BLOCKED* | BLOCKED |
| STORY 3.4 | `context/implementation-plans/milestone-3/friend-codes/` | *not planned — BLOCKED* | BLOCKED |
| STORY 3.5 | `context/implementation-plans/milestone-3/mailbox/` | *not planned — BLOCKED* | BLOCKED |
| STORY 3.6 | `context/implementation-plans/milestone-3/hosting-deployment/` | [plan.md#questions--unknowns](../../hosting-deployment/plan.md#questions--unknowns) | OPEN |

## Boss Questions

Cross-cutting questions that apply to multiple plans, affect sequencing, or must be answered before workers proceed.

- Q: Should Stories 3–5 be planned in this run despite the milestone's "must not be planned until the contract exists" note?
  Affects: `STORY 3.3, STORY 3.4, STORY 3.5`
  Assumption: —
  Status: ANSWERED
  Answer: Owner (2026-07-28, in chat): gate upheld — plan Stories 1, 2, 6 only; 3–5 stay BLOCKED until wire contract v1 is adjudicated.

- Q: Friend-code minting authority — server-assigned vs client-generated + registered?
  Affects: `STORY 3.1, STORY 3.4`
  Assumption: —
  Status: ANSWERED
  Answer: Owner (2026-07-28, in chat): server-assigned; uniqueness guaranteed by the relay, no wire collision protocol.

- Q: Friend-code alphabet — charset and case rules for the fixed 4-4-4 dashed shape?
  Affects: `STORY 3.1, STORY 3.4`
  Assumption: —
  Status: ANSWERED
  Answer: Owner (2026-07-28, in chat): Crockford-style — uppercase letters + digits excluding 0/O and 1/I; case-insensitive on input, displayed uppercase.

- Q: Wire contract spec document location?
  Affects: `STORY 3.1, STORY 3.2`
  Assumption: —
  Status: ANSWERED
  Answer: Owner (2026-07-28, in chat): `docs/wire-contract-v1.md` confirmed.

- Q: Unknown-version envelope disposition — when a client refuses an envelope version it doesn't know, is the message acknowledged off the mailbox (accepting loss) or left queued? (Surfaced by the STORY 3.1 worker from the app repo's "Relay Contract Conformance" open contract question.)
  Affects: `STORY 3.1, STORY 3.5` (and app-repo Story 8)
  Assumption: Left queued until the 30-day sweep, with drain semantics letting a client skip past unreadable envelopes without permanent poll failure. To be adjudicated by the owner during Story 1 execution.
  Status: OPEN
  Answer: —

- Q: Relay endpoint authentication scheme — exact mechanism for proof of GUID ownership without accounts?
  Affects: `STORY 3.1, STORY 3.3` (and transitively 3.4, 3.5)
  Assumption: Device-key request signing + replay protection + a defined re-key announcement trust rule (the dictation's "likely shape"). To be settled during Story 1 execution with owner adjudication.
  Status: OPEN
  Answer: —
