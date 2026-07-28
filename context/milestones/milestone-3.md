---
name: milestone-3
description: Milestone 3 - Multiplayer Relay. The server side of the project-wide Multiplayer milestone - the wire contract, the store-and-forward relay, and its hosting, delivered in lockstep with the FoodUs app repository's Milestone 3.
metadata:
  version: "3.0"
  agentic_rails_source_version: "3.0"
  owner: "Jarryd Adaens"
  repo: "FoodUs-Server"
---
# Milestone 3: Multiplayer Relay

> Milestone. A coherent macro-feature or delivery outcome. (Tier numbers live only in `design.md` / `agenticworkflow.md`.)
>
> Related: [design.md (Milestones Index)](../design.md#milestones-index), [../backlog/](../backlog/)
>
> Source dictation: [2026-07-27 FoodUs relay tier-0 design seed](../dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md)
>
> **Lockstep counterpart:** the FoodUs app repository's Milestone 3 (Multiplayer). This
> milestone is the server half of one project-wide milestone. The app repo's Story 4 (relay
> architecture spike) resolves into this milestone's wire-contract story; its Story 5 (relay
> server build) *is* this milestone's delivery; its Stories 6 onward carry one-way dependency
> notes against the stories below. The dependency arrow only ever points app → server; the
> owner releases each block once the server story is deployed.

---

## Intent

Design, build, and deploy the FoodUs relay: the wire contract v1 specification, the ASP.NET
store-and-forward relay that implements it (profiles, endpoint auth, friend codes, blocks,
mailbox with 30-day sweep), and the hardened droplet it runs on — so the app's Milestone 3
social features have a live server to consume.

## Why it matters

- Every cross-device feature in the project's Multiplayer milestone flows through the relay;
  the app's Stories 5–14 are downstream of this repo's build.
- The relay proves multi-user can exist without sacrificing the constitutional privacy stance:
  no accounts, no cloud diary, no server-readable data.
- The wire contract created here becomes the permanent single source of truth both codebases
  conform to.

## Outcome / Definition of Done

Wire contract v1 written and owner-adjudicated. The relay deployed on the droplet behind
Caddy/HTTPS, serving registration, friend-code resolution with block enforcement, mailbox
push/drain, the version/capability endpoint, and the 30-day sweep. Secrets discipline intact
(no endpoint address or credentials in the repo).

Joint acceptance evidence is the **household proof** (app repo Milestone 3 Story 14): the real
two-phone end-to-end run — friend add via code, Full-trust group, cross-diary round trip,
Suggest flow, block flows, and the re-key drill — against this live relay. The server side of
that proof (sweep behavior, block indistinguishability, re-key handling observed live) closes
this milestone; it is validation, not a separate server story.

## Status

Not Started — 0/6 stories complete.

---

## Constitutional constraints (restated from the dictation, binding)

- No accounts, no login, no sessions in the account sense, no server-side backup, no
  server-readable diary data, no web/companion clients.
- Undelivered messages sweep after **30 days**.
- Blocked requesters receive "user not found" — indistinguishable from a nonexistent user.
- The server is a transmission vector between databases, nothing more.
- HTTPS is mandatory; the relay never faces the internet directly.
- The owner's endpoint address is private and never enters this repository.

---

## Story Index

| # | Story | Type | Complexity | Effort | Risk | Plan | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | [Wire contract v1](#story-1) | Research | — | — | — | [plan.md](../implementation-plans/milestone-3/wire-contract-v1/plan.md) | Not Started |
| 2 | [Relay service skeleton](#story-2) | Feature | — | — | — | [plan.md](../implementation-plans/milestone-3/relay-skeleton/plan.md) | Not Started |
| 3 | [Profiles & endpoint auth](#story-3) | Feature | — | — | — | Not yet planned — blocked pending Story 1 | Not Started |
| 4 | [Friend codes, resolution & blocks](#story-4) | Feature | — | — | — | Not yet planned — blocked pending Story 1 | Not Started |
| 5 | [Mailbox & sweep](#story-5) | Feature | — | — | — | Not yet planned — blocked pending Story 1 | Not Started |
| 6 | [Hosting & first deployment](#story-6) | Feature | — | — | — | [plan.md](../implementation-plans/milestone-3/hosting-deployment/plan.md) | Not Started |

### Paired-story traceability (server ↔ app)

Shared slugs make counterparts findable by name; the app stories listed are consumers carrying
the one-way dependency note. Where one server story serves two app stories (a deliberate
one-to-many deviation from strict slug pairing — e.g. `friend-codes` serving app stories 6
and 7), each app story's dependency note must name this server story's slug explicitly.

| Server story (this repo) | Slug | App-repo Milestone 3 consumer(s) |
| --- | --- | --- |
| 1 Wire contract v1 | `wire-contract-v1` | Story 4 (relay architecture spike — resolves here) |
| 2 Relay service skeleton | `relay-skeleton` | Story 5 (relay server build umbrella); capability endpoint consumed by all relay-backed UI |
| 3 Profiles & endpoint auth | `profiles-auth` | Stories 2–3 (profile + crypto identity register the public key) |
| 4 Friend codes, resolution & blocks | `friend-codes` | Stories 6 (friend codes) and 7 (friends list: resolve + block calls) |
| 5 Mailbox & sweep | `mailbox` | Story 8 (message envelope & E2E pipeline) |
| 6 Hosting & first deployment | `hosting-deployment` | All relay-backed stories; Story 14 (household proof) runs against this deployment |

---

## Stories

<a id="story-1"></a>

### Story 1: Wire contract v1

**Type:** Research

**Summary:**
Write the wire contract v1 specification — the first deliverable of this repository and the
single source of truth both agents read: endpoint list, envelope schema with mandatory version
stamp, auth handshake, and error semantics (including "user not found" for blocked and
nonexistent alike). Minimum API surface per the dictation: register/update profile, resolve
friend code → { GUID, username, public key } with block enforcement, regenerate friend code,
push sealed message, poll/drain mailbox, record blocks, version/capability query.

**Open decisions (resolve in this story; owner input where marked):**

1. **Relay endpoint authentication.** Proof of GUID ownership without accounts — likely
   device-key request signing plus replay protection and a trust rule for re-key
   announcements.
2. **Friend-code minting authority:** server-assigned vs client-generated + registered.
   *(Owner input wanted.)*
3. **Friend-code alphabet:** charset and case rules; shape fixed at 4-4-4 dashed blocks.
   *(Owner input wanted.)*
4. **Spec document location.** Assumed `docs/wire-contract-v1.md`; confirm with the owner.

**Why / value:**
Everything else in this milestone — and every app-repo consumer story — conforms to this
document. Its open decisions must be settled before any implementation plan is written.

**Rough scope:**
One maintained specification document plus resolutions recorded back into
[design.md](../design.md). No code.

**CER:**

- Complexity: —
- Effort: —
- Risk: —

**Plan:** [plan.md](../implementation-plans/milestone-3/wire-contract-v1/plan.md)

**Status:** Not Started

---

<a id="story-2"></a>

### Story 2: Relay service skeleton

**Type:** Feature

**Summary:**
Stand up the ASP.NET (C#) minimal API solution: project structure, SQLite storage wiring,
localhost-only listening, the committed config template with blank values, `.gitignore`
secret-blocking, and the **version/capability endpoint** so clients can adapt at runtime from
day one.

**Why / value:**
The chassis every capability story bolts onto, and the runtime capability signal the app's
graceful-degradation model depends on.

**Rough scope:**
New server codebase under `source/` (structure per repo conventions), SQLite schema
foundations, configuration surface, capability endpoint per the contract.

**CER:**

- Complexity: —
- Effort: —
- Risk: —

**Plan:** [plan.md](../implementation-plans/milestone-3/relay-skeleton/plan.md)

**Status:** Not Started

---

<a id="story-3"></a>

### Story 3: Profiles & endpoint auth

**Type:** Feature

**Summary:**
Register/update profile — GUID, username (cosmetic, collisions allowed), public key — and
implement the endpoint authentication scheme settled in Story 1: signature verification with
dotnet's built-in cryptography, replay protection, and the re-key announcement trust rule so a
known GUID cannot be drained, overwritten, re-keyed, impersonated, or replayed.

**Why / value:**
The security-critical core. Endpoint auth is the real security of the whole relay.

**Rough scope:**
Profile storage and endpoints, request-signature verification middleware/filters, replay
protection, re-key handling. Unit tests per the design testing policy.

**CER:**

- Complexity: —
- Effort: —
- Risk: —

**Plan:** Not yet planned — blocked pending Story 1 (wire contract v1).

**Status:** Not Started

---

<a id="story-4"></a>

### Story 4: Friend codes, resolution & blocks

**Type:** Feature

**Summary:**
Friend-code lifecycle per the Story 1 decisions: mint/register, regenerate (old copies die;
existing friendships unaffected), and resolve code → { GUID, username, public key }. Record
block relationships and enforce them at resolution: a blocked requester gets "user not found",
indistinguishable from a nonexistent user, even with a fresh code.

**Why / value:**
The out-of-band connection handle the app's social graph is built from, and the block
mechanism that keeps specific people out permanently.

**Rough scope:**
Friend-code and block storage, mint/regenerate/resolve/block endpoints, indistinguishability
tests.

**CER:**

- Complexity: —
- Effort: —
- Risk: —

**Plan:** Not yet planned — blocked pending Story 1 (wire contract v1).

**Status:** Not Started

---

<a id="story-5"></a>

### Story 5: Mailbox & sweep

**Type:** Feature

**Summary:**
Per-GUID queues of sealed ciphertext envelopes: authenticated push, poll/drain for the owner
GUID, envelope version-stamp validation (unknown versions refused loudly, never silently
mangled), and the **30-day sweep** of undelivered messages.

**Why / value:**
The store-and-forward heart of the relay — the pipeline every cross-device action rides on.

**Rough scope:**
Envelope queue storage, push and drain endpoints, version-stamp refusal, scheduled sweep job.
Unit tests for sweep boundaries and version refusal.

**CER:**

- Complexity: —
- Effort: —
- Risk: —

**Plan:** Not yet planned — blocked pending Story 1 (wire contract v1).

**Status:** Not Started

---

<a id="story-6"></a>

### Story 6: Hosting & first deployment

**Type:** Feature

**Summary:**
Provision and harden the DigitalOcean droplet (SYD1, Ubuntu 24.04 LTS, SSH-key-only auth,
monitoring agent on, backups off, 1 GB plan or 512 MB + swap), install Caddy with automatic
HTTPS in front of the localhost-bound relay, point an owner-held domain at it, run the relay
as a plain systemd service, and write the committable publish script (local
`dotnet publish` → scp → service restart; steps, never credentials). Runtime secrets land on
the droplet only.

**Why / value:**
Turns the codebase into the live private endpoint the household phones talk to. **Server
leads, app follows** — this deployment must exist before any app story's dependency note is
released.

**Rough scope:**
Droplet provisioning, Caddy config, systemd unit, `scripts/` publish script, setup
instructions generic enough for a stranger to stand up their own relay.

**CER:**

- Complexity: —
- Effort: —
- Risk: —

**Plan:** [plan.md](../implementation-plans/milestone-3/hosting-deployment/plan.md)

**Status:** Not Started

---

## Interdependency Order

1. Story 1 (wire contract) strictly first — its open decisions gate every implementation
   plan, including endpoint auth before Story 3.
2. Story 2 (skeleton) after 1; Stories 3 → 4 → 5 build on it in order (auth before anything
   authenticated; codes/blocks before the mailbox they gate is exercised end to end).
3. Story 6 can be prepared in parallel after 2 (a skeleton can deploy early), but the
   milestone's deployment gate is the full capability set live.
4. The household proof (app repo Story 14) runs last, against the Story 6 deployment, and
   closes the milestone jointly.

---

## Backlog Sources

- This milestone was synthesized directly from the 2026-07-27 tier-0 dictation, not pulled
  from the backlog. Deferred items were pushed *to* the backlog instead — see below.

---

## Deferred / Follow-up Work

Parked deliberately by the dictation; staged as backlog stories, **not** Milestone 3 scope:

- **N-person group server support** beyond the two-member cap.
  [backlog-1 Story 2](../backlog/backlog-1.md#story-2)
- **Docker-based deployment** — plain systemd first; adopt Docker only if it earns its place.
  [backlog-1 Story 3](../backlog/backlog-1.md#story-3)
- **Push transport** (FCM or self-hosted) is a non-goal here and is deferred to the *app*
  repo's backlog; it would land on this repo only if the app ever adopts it.

---

## Notes

- Keep this Story Index in sync with the [Milestones Index](../design.md#milestones-index) in Design.
- Story 1's open decisions must be carried into its implementation plan as explicit decision
  points and resolved (with the owner where marked) before the story is planned in detail;
  Stories 3–5 must not be planned until the contract exists.
- Contract changes after v1 follow the design's additive-evolution rules and are adjudicated
  by the owner between the two repo agents.
