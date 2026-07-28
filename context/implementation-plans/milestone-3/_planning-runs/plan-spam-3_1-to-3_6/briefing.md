# Plan Spam Briefing — Milestone 3 (Multiplayer Relay), Run `plan-spam-3_1-to-3_6`

- **Milestone:** `context/implementation-plans/milestone-3/`
- **Run slug:** `plan-spam-3_1-to-3_6`
- **Date:** 2026-07-28
- **Requested scope:** all six stories in `context/milestones/milestone-3.md`

## Source documents read

| Document | Why it matters |
| --- | --- |
| `context/milestones/milestone-3.md` | The story source: six stories, interdependency order, constitutional constraints, paired-story slugs |
| `context/design.md` | Design tier: relay architecture, API evolution rules, testing policy, open questions |
| `context/laws.md` | Constitutional code-quality and security laws binding every plan |
| `context/agenticworkflow.md` | Tier system, plan artifact rules (one `plan.md` per story, sidecars opt-in) |
| `context/dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md` | Rationale source: locked stack/hosting decisions, open decisions 1–4, app-side obligations |

Workers may also read the peer client app repo at `D:\forked-projects\FoodYou` (read-across is free; edits happen at home).

## Template source

`skills/rails-plan-spam-workflow` tooling repo: `skills/rails-planning/references/plan-template.md` (unified default). No repository-local template exists; `context/implementation-plans/` was created by this run.

## Owner adjudications taken at run start (2026-07-28, in chat)

1. **Planning gate:** Milestone note "Stories 3–5 must not be planned until the contract exists" is upheld. This run plans **Stories 1, 2, 6 only**; Stories 3–5 are queued as `BLOCKED` until wire contract v1 is adjudicated.
2. **Friend-code minting authority:** **server-assigned** (uniqueness guaranteed; no wire collision protocol).
3. **Friend-code alphabet:** **Crockford-style** — uppercase letters + digits excluding 0/O and 1/I; case-insensitive on input, displayed uppercase; shape fixed at 4-4-4 dashed blocks.
4. **Spec location:** `docs/wire-contract-v1.md` **confirmed**.

Still open (carried into Story 1's plan): the **relay endpoint authentication scheme**. Canonical temporary assumption for any plan that must reference it: device-key request signing + replay protection + a defined re-key announcement trust rule, per the dictation's "likely shape".

## Planning queue (ordered)

| Order | Story | Slug | Disposition |
| --- | --- | --- | --- |
| 1 | Story 1: Wire contract v1 | `wire-contract-v1` | Plan first — its plan carries the adjudications above and the open auth decision |
| 2 | Story 2: Relay service skeleton | `relay-skeleton` | Plan after Story 1's plan exists (worker reads it as input) |
| 3 | Story 6: Hosting & first deployment | `hosting-deployment` | Plan in parallel with Story 2 |
| — | Story 3: Profiles & endpoint auth | `profiles-auth` | BLOCKED — gated on wire contract v1 |
| — | Story 4: Friend codes, resolution & blocks | `friend-codes` | BLOCKED — gated on wire contract v1 |
| — | Story 5: Mailbox & sweep | `mailbox` | BLOCKED — gated on wire contract v1 |

## Output folders

`context/implementation-plans/milestone-3/<slug>/plan.md` for each planned story, using the slugs above (already fixed by the milestone's Plan links).

## Constraints

- Plans only — no production code, no `docs/wire-contract-v1.md` authoring (that is Story 1's *execution*, not its plan).
- One primary artifact per story: `plan.md`. No sidecar files.
- Constitutional constraints from the milestone (no accounts, 30-day sweep, block indistinguishability, HTTPS-only, endpoint address never in repo) bind every plan.
- Run-level questions index: `_planning-runs/plan-spam-3_1-to-3_6/questions.md`.
