# Plan: Wire contract v1

## Metadata

- Task Type: `STORY`
- Status: `Draft`
- Owner: `Jarryd Adaens`
- Last Updated: `28 July 2026`

## Linked Context

- Milestone: [context/milestones/milestone-3.md](../../../milestones/milestone-3.md)
- Story: [Milestone 3, Story 1: Wire contract v1](../../../milestones/milestone-3.md#story-1) (slug `wire-contract-v1`, Type: Research)
- Backlog source: none — story was synthesized directly into Milestone 3 from the tier-0 dictation
- Dictation source: [2026-07-27 FoodUs relay tier-0 design seed](../../../dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md) (locked decisions, open decisions, app-side obligations)
- Related Plans: none yet — this is the first plan in this repository; Stories 3–5 plans are gated on this story's output
- External Tooling: `rails-planning` (this plan), `rails-grade-cer` (CER grading, deferred), `commit-log` (when committing)
- Peer repo (read-only): `D:\forked-projects\FoodYou` — app-repo `context/milestones/milestone-3.md` Stories 2–3 and 6–8 plus its "Relay Contract Conformance" section are the consumer surface this contract must satisfy

## CER

- Complexity: —
- Effort: —
- Risk: —
- Notes: Grading deferred to `rails-grade-cer`.

## Objective

Produce `docs/wire-contract-v1.md`: the owner-adjudicated wire contract v1 specification — endpoint list, envelope schema with mandatory version stamp, auth handshake, and error semantics — that becomes the single source of truth both the server and app codebases conform to. Record decision resolutions back into `context/design.md` and update the milestone's Story 1 status and plan link. This is a Research story: the deliverable is the specification document, not code.

## Scope

### In Scope

- Author `docs/wire-contract-v1.md` covering the full minimum API surface (see Current Understanding).
- Work up concrete candidate design(s) for the relay endpoint authentication scheme and get them owner-adjudicated before the spec's auth section is finalized.
- Settle the unknown-version envelope disposition question flagged by the app repo (ack-and-lose vs leave-queued) as part of the error-semantics section.
- Record the three already-adjudicated decisions (friend-code minting authority, friend-code alphabet, spec location) into the spec with their provenance.
- Update `context/design.md`: resolve/retire the settled entries in "Assumptions and Open Questions", confirm the spec path in "The Wire Contract" and the repository-structure tree, and align the "Endpoint authentication" bullet in Security and Privacy once adjudicated.
- Update `context/milestones/milestone-3.md`: Story 1 plan link, status transitions, and the "Open decisions" list annotated as resolved.

### Out Of Scope

- Any implementation code (server or app) — Stories 2–6 own that.
- Writing implementation plans for Stories 2–6.
- Editing anything in the peer app repository (`D:\forked-projects\FoodYou`) — read-across is free, edits happen at home; the owner carries contract consequences across.
- Choosing or documenting the owner's domain, endpoint address, or any private runtime value.
- Contract v2 or any speculative future surface beyond v1's minimum.

## Non-Goals

- No push transport (FCM or self-hosted) endpoints — deferred to the app repo's backlog.
- No N-person group support beyond what the two-member cap requires — backlog-1 Story 2.
- No accounts, login, sessions-in-the-account-sense, server-side backup, or web/companion client affordances anywhere in the contract.
- No server-readable diary semantics: the contract must never require the relay to parse envelope payloads.

## Current Understanding

- Likely files or directories:
  - `docs/wire-contract-v1.md` — the deliverable (path confirmed by owner, 2026-07-28); `docs/` does not exist yet and is created by this story.
  - `context/design.md` — "Assumptions and Open Questions" items 1–4, "The Wire Contract" section, "Security and Privacy" endpoint-auth bullet, and the Repository Structure tree all need resolution edits.
  - `context/milestones/milestone-3.md` — Story 1 row/section (plan link, status), Story 1 "Open decisions" list.
- Minimum API surface the spec must cover (from Milestone 3 Story 1):
  1. Register/update profile — GUID, username (cosmetic, collisions allowed), public key.
  2. Resolve friend code → `{ GUID, username, public key }` with block enforcement.
  3. Regenerate friend code (old copies die; existing friendships unaffected).
  4. Push sealed message (append envelope to recipient GUID's queue).
  5. Poll/drain mailbox for the authenticated owner GUID.
  6. Record blocks.
  7. Version/capability query (also doubles as liveness signal).
  Plus, cross-cutting: envelope schema with mandatory version stamp, the auth handshake, and error semantics — including "user not found" returned identically for blocked and nonexistent users.
- Consumer surface (app repo, read-only grounding): app Milestone 3 Story 2 (profile), Story 3 (crypto identity registers the public key; re-key announcements reuse the same seam), Story 6 (friend codes: register/regenerate), Story 7 (friends list: resolve + block), Story 8 (envelope schema, push/poll, version stamping). The app's "Relay Contract Conformance" section binds it to two-way field tolerance, refuse-loudly on unknown versions, capability-aware UI, and HTTPS-only via Ktor.
- Existing behaviors to preserve: none in code (no code exists); the constraints to preserve are constitutional — no accounts; 30-day sweep of undelivered messages; blocked = "user not found" indistinguishable from nonexistent; HTTPS mandatory with the relay never internet-facing; the owner's endpoint address never enters this repository.
- Interfaces, data contracts, or external dependencies: the spec itself is the interface. Evolution rules are fixed by design: additive-only within a major version, two-way unknown/absent field tolerance, parallel `/v1/`-style routes for breaking changes, and a version/capability endpoint clients adapt to at runtime.
- Known tests, build steps, or observability points: none applicable — this is a documentation story. `harness/` gates apply only if a matching documentation verifier exists (check `harness/README-HARNESS.md` at execution start).
- Assumptions and constraints:
  - Resolved decisions (provenance: Owner, 2026-07-28, run plan-spam-3_1-to-3_6):
    1. **Friend-code minting authority: server-assigned.** The relay generates codes, uniqueness guaranteed; no wire collision protocol is needed or specified.
    2. **Friend-code alphabet: Crockford-style** — uppercase letters + digits excluding 0/O and 1/I; case-insensitive on input, displayed uppercase; shape fixed at 4-4-4 dashed blocks.
    3. **Spec document location: `docs/wire-contract-v1.md`** confirmed.
  - Contract ownership: this repo owns the spec; the app conforms; contract changes are adjudicated and carried across by the owner — agents never negotiate directly.
  - The spec must be written so a stranger standing up their own relay can implement against it: public mechanism, private values.

## Questions / Unknowns

- Q: STORY 3.1 — What is the relay endpoint authentication scheme: how does a client prove GUID ownership without accounts, so a known GUID cannot be drained, overwritten, re-keyed, impersonated, or replayed?
  Impact: Gates the spec's auth-handshake section, the error semantics for auth failures, and the entire Story 3 (profiles-auth) implementation plan. It is the real security of the whole relay.
  Assumption: Device-key request signing (the app's crypto identity doubles as the device credential) + replay protection + a defined re-key announcement trust rule. Canonical temporary assumption for this run; Execution Step 3 works this up into concrete candidate(s) for owner adjudication before the auth section is finalized.
  Status: `OPEN`
  Answer: —

- Q: STORY 3.1 — Unknown-version envelope disposition: when a client refuses an envelope version it doesn't know, is the message acknowledged off the mailbox (accepting loss) or left queued (risking a repeated poll error)?
  Impact: Flagged by the app repo ("Open contract question", app milestone-3 Relay Contract Conformance); must be settled in this contract before app Story 8 is planned. Shapes the drain/acknowledge semantics of the mailbox endpoints.
  Assumption: Left queued until the 30-day sweep, with the drain protocol letting a client skip past unreadable envelopes without permanent poll failure — pending owner adjudication in Execution Step 4.
  Status: `OPEN`
  Answer: —

- Q: STORY 3.1 — Friend-code minting authority?
  Impact: Decides whether the spec needs a collision/registration protocol.
  Assumption: n/a — answered.
  Status: `ANSWERED`
  Answer: Server-assigned; relay generates codes, uniqueness guaranteed, no wire collision protocol. (Owner, 2026-07-28, run plan-spam-3_1-to-3_6.)

- Q: STORY 3.1 — Friend-code alphabet and case rules?
  Impact: Fixes the code format the spec publishes and both sides validate.
  Assumption: n/a — answered.
  Status: `ANSWERED`
  Answer: Crockford-style — uppercase letters + digits excluding 0/O and 1/I; case-insensitive on input, displayed uppercase; 4-4-4 dashed blocks. (Owner, 2026-07-28, run plan-spam-3_1-to-3_6.)

- Q: STORY 3.1 — Spec document location?
  Impact: Fixes the deliverable path and the design.md references to it.
  Assumption: n/a — answered.
  Status: `ANSWERED`
  Answer: `docs/wire-contract-v1.md` confirmed. (Owner, 2026-07-28, run plan-spam-3_1-to-3_6.)

## Execution Steps

1. Draft the spec skeleton at `docs/wire-contract-v1.md`.
   - Why: Fix the document's structure before filling sections, so owner review and app-repo read-across have stable anchors.
   - Edits: Create `docs/wire-contract-v1.md` with frontmatter (owner, repo, contract major version 1) and these sections:
     1. Purpose & Authority (this document is the single source of truth; both codebases conform)
     2. Constitutional Constraints (no accounts; 30-day sweep; blocked = "user not found" indistinguishable; HTTPS mandatory; relay never internet-facing; endpoint address never published)
     3. Transport & Base Path (HTTPS only; `/v1/` route prefix; no endpoint address anywhere)
     4. Versioning & Evolution Rules (additive-only within v1; two-way unknown/absent field tolerance; parallel major routes; refuse-loudly on unknown envelope versions)
     5. Authentication (handshake, request signing, replay protection, re-key trust rule — drafted from the canonical assumption, marked PENDING ADJUDICATION until Step 3 resolves)
     6. Data Types (GUID, username, public key encoding, friend-code format per the adjudicated Crockford-style 4-4-4 rules, timestamps)
     7. Envelope Schema (sealed ciphertext payload, mandatory version stamp, sender/recipient addressing — relay never parses payloads)
     8. Endpoints — one subsection each: register/update profile; resolve friend code (block-enforced); regenerate friend code (server-assigned minting); push sealed message; poll/drain mailbox; record block; version/capability query
     9. Error Semantics (uniform error shape; "user not found" identical for blocked and nonexistent; unknown-version refusal; auth-failure responses; unknown-version envelope disposition per Step 4)
     10. Retention (30-day sweep of undelivered envelopes)
     11. Change Log (v1.0 initial entry; additive amendments append here)
   - Dependencies: none — first step.

2. Fill every non-auth section to full specification precision, cross-checked against the app-repo consumer stories.
   - Why: Everything except authentication is already decided (dictation + owner adjudications); it can be specified completely now.
   - Edits: Complete sections 1–4 and 6–11 in `docs/wire-contract-v1.md`. For each endpoint, verify the request/response surface covers its app-repo consumer: Story 2/3 (register profile + public key, re-key seam), Story 6 (regenerate), Story 7 (resolve + block), Story 8 (envelope, push/poll), capability endpoint for all relay-backed UI.
   - Dependencies: after Step 1.

3. Work up the endpoint authentication decision and obtain owner adjudication.
   - Why: The one OPEN gating decision. The spec's auth section cannot be finalized — and Stories 3–5 cannot be planned — until the owner adjudicates.
   - Edits: Write a concise candidate analysis (2–3 concrete schemes, e.g. per-request detached signature over method+path+body+timestamp+nonce with a server-side replay window vs challenge–response nonce exchange; each with its replay-protection mechanism and its re-key announcement trust rule, such as new-key-signed-by-old-key). Present in chat for owner adjudication; record the ruling in this plan's Questions section (Status → ANSWERED, with provenance) and finalize spec section 5, removing the PENDING ADJUDICATION marker.
   - Dependencies: candidate analysis can proceed in parallel with Step 2; spec finalization requires the owner's ruling.

4. Adjudicate the unknown-version envelope disposition and finalize error semantics.
   - Why: App-repo-flagged open contract question that gates app Story 8 planning; it belongs to this contract.
   - Edits: Present the ack-and-lose vs leave-queued trade-off (with the plan's temporary assumption as the recommended option) for owner ruling; record the answer in this plan and in spec section 9.
   - Dependencies: after Step 1; can ride the same adjudication sitting as Step 3.

5. Record resolutions back into `context/design.md`.
   - Why: Design is the maintained tier; decisions must not stay stranded in the plan or spec alone.
   - Edits: In "Assumptions and Open Questions", mark items 1–4 resolved with their answers and provenance (item 5, the domain choice, remains open — it belongs to Story 6). Update "Security and Privacy" endpoint-auth bullet from "open decision" to the adjudicated scheme. In "The Wire Contract", replace "*Assumed location (to confirm...)*" with the confirmed `docs/wire-contract-v1.md` link. Update the Repository Structure tree (`docs/` no longer "planned: location to confirm").
   - Dependencies: after Steps 3–4 (all four decisions answered).

6. Update `context/milestones/milestone-3.md` for Story 1.
   - Why: Milestone doc must reflect reality; the Story Index and status drive downstream sequencing.
   - Edits: Story Index row 1 — Plan column links to this plan, Status per actual state (In Progress at execution start; Complete when the spec is owner-approved). Story 1 section — annotate each of the four open decisions as resolved with provenance; set Status. Update the milestone Status line's story count when Story 1 completes.
   - Dependencies: plan-link edit can happen immediately at execution start; completion edits after Step 7.

7. Owner review gate: final adjudication of the complete spec.
   - Why: Milestone Definition of Done requires "Wire contract v1 written and owner-adjudicated". The owner is the human relay who will carry the contract's consequences to the app repo.
   - Edits: Present the finished spec for owner sign-off; apply any requested amendments; on approval, mark Story 1 Complete (Step 6 completion edits) and output a commit log via the `commit-log` skill in chat (commit only if explicitly requested).
   - Dependencies: after Steps 2–6.

## Validation

### Automated Checks

- None applicable — documentation-only story, no code, no test harness for prose. If `harness/README-HARNESS.md` documents a matching docs/context verifier or gate at execution time, run it and record the result in the Execution Log; otherwise record here that no matching harness check exists.
- Markdown link check (manual or scripted): every relative link added to `design.md`, `milestone-3.md`, and the spec resolves.

### Manual Checks

1. Coverage sweep: every item of the minimum API surface (7 endpoints + envelope schema + auth handshake + error semantics) has a dedicated, unambiguous spec section.
2. Consumer cross-check against the app repo (read-only): each of app Stories 2–3 and 6–8, plus the capability-aware-UI and two-way-tolerance obligations in its Relay Contract Conformance section, can be implemented from the spec alone without guessing.
3. Constitutional audit of the spec: no accounts/login/sessions anywhere; 30-day sweep stated; blocked and nonexistent responses byte-identical in the error section; HTTPS-only stated; no endpoint address, domain, credential, or other private value appears anywhere in the document or diffs.
4. Evolution-rules compliance: the spec states the additive-only rule, two-way tolerance, mandatory version stamp with refuse-loudly semantics, and the `/v1/` parallel-route convention, consistent with design.md.
5. Adjudication trace: all four Story 1 decisions plus the envelope-disposition question show ANSWERED status with owner provenance in this plan, and matching resolutions in design.md and milestone-3.md.

### Acceptance Criteria

- `docs/wire-contract-v1.md` exists, is complete per the coverage sweep, and is owner-adjudicated (Step 7 sign-off).
- The endpoint authentication scheme is owner-adjudicated and fully specified — no PENDING ADJUDICATION markers remain.
- `context/design.md` and `context/milestones/milestone-3.md` reflect every resolution; no decision is stranded solely in this plan or the spec.
- Stories 2–5 are unblocked: their implementation plans can be written against the contract without reopening any Story 1 decision.
- No secret or private value entered the repository (guardrail: constitutional audit above).

## Risk Mitigation

- Risk: The auth scheme gets designed in isolation and doesn't fit the app's actual crypto identity (Android Keystore constraints, e.g. available signing algorithms).
  Mitigation: Step 3's candidate analysis reads the app repo's Story 3 (crypto identity) before proposing schemes; candidates state their algorithm/encoding expectations explicitly so the owner can flag app-side conflicts at adjudication.
- Risk: Spec finalized with the auth section still under the temporary assumption (adjudication skipped or forgotten).
  Mitigation: The PENDING ADJUDICATION marker in the spec plus Acceptance Criteria explicitly fail Step 7 sign-off while any marker remains.
- Risk: "User not found" indistinguishability quietly broken by side channels the spec forgets (timing, differing error bodies or status codes between blocked and nonexistent).
  Mitigation: Error-semantics section must specify byte-identical response shape and status for both cases and note the timing consideration for implementers; manual check 3 verifies.
- Risk: Contract drifts from what app consumer stories actually need, discovered only during app implementation.
  Mitigation: Manual check 2's story-by-story cross-check at planning quality; the owner — who adjudicates both repos — is the final catch at Step 7. Post-v1 gaps are handled by the additive-evolution rules, not v1 edits.
- Risk: Resolution edits to design.md / milestone-3.md are forgotten after the spec ships, stranding decisions.
  Mitigation: Steps 5–6 are explicit plan steps gated into the acceptance criteria; the Definition of Done in laws.md requires docs updated when scope changes.
- Risk: A private value (domain, endpoint address) leaks into examples in the spec.
  Mitigation: Spec examples use placeholder hosts only (e.g. `https://relay.example`); constitutional audit checks all diffs.

## Evidence / References

- Planning inputs read: `context/laws.md`, `context/design.md` (v3.0, incl. Milestones Index and "Assumptions and Open Questions"), `context/milestones/milestone-3.md` (v3.0, Story 1 + constraints + paired-story traceability), `context/dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md`, `context/agenticworkflow.md` (plan-artifact rules).
- Peer-repo grounding (read-only): `D:\forked-projects\FoodYou\context\milestones\milestone-3.md` — Relay Contract Conformance section (incl. the flagged unknown-version envelope disposition question), Stories 2–3 and 6–8 consumer scope and dependency notes.
- Owner adjudications recorded 2026-07-28 (run plan-spam-3_1-to-3_6): friend-code minting server-assigned; Crockford-style alphabet, 4-4-4 uppercase-display case-insensitive-input; spec path `docs/wire-contract-v1.md`.
- Known unverified claims: none — no runtime or build claims are made by this plan.
