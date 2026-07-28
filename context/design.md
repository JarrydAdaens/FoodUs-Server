---
name: design
description: Design specification for the FoodUs relay - a deliberately dumb, self-hosted store-and-forward server ferrying sealed E2E-encrypted envelopes between FoodUs household phones. Includes the embedded Milestones Index.
metadata:
  version: "3.0"
  agentic_rails_source_version: "3.0"
  owner: "Jarryd Adaens"
  repo: "FoodUs-Server"
---
# FoodUs Relay - Design Specification

## Purpose of This File

This file is the Design tier: the maintained design specification covering the whole
deliverable and how it breaks into its largest pieces. It synthesizes the
[2026-07-27 tier-0 dictation](dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md) into
stable project direction. The **Milestones Index** lives as a subsection of this file (see
below); the actual Milestone documents are separate files under `milestones/`.

## Context Hierarchy

This is one of only two files in the framework allowed to state the numbered tier table (the other is [agenticworkflow.md](agenticworkflow.md)). Elsewhere, refer to tiers by name only.

| Tier | Document | Purpose |
| --- | --- | --- |
| 0 - Dictation | [dictations-tier-0/](dictations-tier-0/) | Raw dictation and supplemental design changes, before structure is imposed |
| 1 - Design | `design.md` | The whole deliverable, its largest pieces, and the Milestones Index subsection |
| 2 - Milestone | [milestones/](milestones/) | One coherent macro-feature or delivery outcome; contains the story list needed to deliver it |
| 3 - Story | Inside milestone docs, optionally staged first in [backlog/](backlog/) | Discrete work units (features, bugs, refactors) |
| 4 - Implementation Plan | `implementation-plans/<milestone-slug>/<story-slug>/plan.md` | The normalized how-to for one story, with scoring and mitigation |
| 5 - Phase *(optional)* | `implementation-plans/<milestone-slug>/<story-slug>/` | A safe slice of an over-large story |
| Support | [laws.md](laws.md) | Constitutional code quality and security laws — loaded first by all agents |
| Support | [agenticworkflow.md](agenticworkflow.md) | Workflow for AI agent collaboration |
| Support | [agent-thinking.md](agent-thinking.md) | Optional temporary scratchpad for long tasks |
| Support | [wiki/home.md](wiki/home.md) | Operational reference notes and cheat sheets |

The backlog (`backlog/`) is not a numbered tier. It is a staging pool — informally "Milestone -1" — of unscheduled stories before they are pulled into a milestone document. See the Milestones Index below and [agenticworkflow.md](agenticworkflow.md) for how milestone planning draws from it.

---

## Milestones Index

> This index is the table of contents for the project's milestones. It lives inside Design because a standalone milestones table is the same tier as Design. Each entry links down to a separate Milestone document under `milestones/`, which directly contains that milestone's story list.
>
> **Milestone numbering is project-wide**, shared with the FoodUs app repository. Milestones 1
> and 2 have no server-side scope; they exist here only as reserved numbering placeholders so
> both repositories always mean the same thing by "Milestone N".

| Milestone | Document | Status | Why it matters | What it unlocks |
| --- | --- | --- | --- | --- |
| Milestone 1: Initialization *(app repo only)* | [milestones/milestone-1.md](milestones/milestone-1.md) | Not Applicable | Project-wide numbering alignment with the FoodUs app repo | — |
| Milestone 2: Customisation *(app repo only)* | [milestones/milestone-2.md](milestones/milestone-2.md) | Not Applicable | Project-wide numbering alignment with the FoodUs app repo | — |
| Milestone 3: Multiplayer Relay | [milestones/milestone-3.md](milestones/milestone-3.md) | In Progress | The relay is the single controlled break in the app's island architecture; every cross-device feature flows through it | Cross-diary logging between the two household phones; friends, groups, and messaging in the app's Milestone 3 |

Keep this index in sync as milestones are added, completed, reordered, or reclassified. When a backlog story scores as epic-sized, promote it into this index as a new milestone.

---

## Executive Summary

The FoodUs relay is the "post office": a deliberately dumb, self-hosted, store-and-forward
server that ferries sealed end-to-end-encrypted envelopes between household phones running the
FoodUs Android app (a fork of Food You). It is the single controlled break in the app's island
architecture, built for one household (the owner and his wife) but published so anyone can run
their own instance.

It holds only: GUIDs, usernames, public keys, friend codes, block relationships, and per-GUID
queues of sealed ciphertext envelopes. It never sees plaintext diary data. A breach yields
ciphertext, usernames, and GUIDs — nothing else.

### Core Principles

- **Dumb by design.** The relay is a transmission vector between phone databases, nothing
  more. Each phone's local database remains the sole source of truth. No server-side logic
  touches diary content.
- **Constitutional privacy (inherited from the app, binding here):** no accounts, no login,
  no sessions in the account sense, no server-side backup, no server-readable diary data, no
  web/companion clients. Undelivered messages sweep after **30 days**. Blocked requesters
  receive "user not found" — indistinguishable from a nonexistent user.
- **Public mechanism, private values.** The source, config template, and setup instructions
  are public; the owner's endpoint address, credentials, and runtime secrets are not. See
  [wiki/secrets.md](wiki/secrets.md).
- **Contract ownership.** This repo owns the wire contract as a written specification; both
  codebases are implementations of that one document. The app is a client that conforms.
- **Additive API evolution.** Within a major version, only ever add. Breaking changes mean a
  new parallel major route, never a v1 edit.
- **Server leads, app follows.** New server capability deploys first and sits dormant until
  the app consumes it.
- **Result-driven stack choice.** Built in the stack the owner knows cold (ASP.NET/C#), not a
  learning-exercise stack, because the security-critical parts deserve fluency.

---

## System Architecture

### How the Pieces Fit Together

```text
phone (FoodUs app, Ktor client)
   │  HTTPS (domain, never raw IP)
   ▼
Caddy (reverse proxy on the droplet; automatic Let's Encrypt TLS)
   │  localhost only (e.g. port 5000)
   ▼
ASP.NET minimal API relay (C#, dotnet built-in crypto for signature verification)
   │
   ▼
SQLite (single small database on the droplet)
```

- The relay never faces the internet directly; Caddy terminates TLS and proxies to localhost.
- Plain HTTP is disqualifying, even for sealed envelopes. HTTPS is mandatory end to end.
- Phones are configured with the domain (typed into a user-facing relay-URL setting in the
  app), so an IP change never touches the phones.
- The data model is tiny: profiles (GUID, username, public key), friend codes, block
  relationships, and per-GUID envelope queues. A managed database is explicitly rejected.

**External services and dependencies:**

- `DigitalOcean droplet (SYD1, Ubuntu 24.04 LTS)` - the always-on host; monitoring agent on,
  backups off (relay data is transient ciphertext), managed database off.
- `Caddy` - reverse proxy and automatic TLS via Let's Encrypt.
- `An owner-held domain` - points at the droplet IP; the specific domain is private.

**Rejected alternatives (recorded so they stay rejected):** Ktor/Kotlin server (owner fluency
wins), peer-to-peer / same-network sync (loses the away-from-home case; NAT/discovery pain),
serverless (wrong abstraction), Docker (plain systemd first; Docker only if it earns its
place), git submodules for contract sharing (discipline replaces machinery).

### The Two-Repository Working Model

- This repository (`FoodUs-Server`) and the FoodUs app repository are peers, each with its own
  complete agentic-rails structure. **Two rooted agents:** each agent stays rooted in its own
  repo under its own laws. Read-across the repo boundary is free; **edits happen at home**.
- **The owner is the human relay between the two agents.** Contract changes are proposed via
  reports, adjudicated and carried across by the owner. Agents never negotiate the contract
  directly. The owner also enforces deployment sequencing.
- **Paired stories** for cross-cutting features: a server parent story (expose the capability,
  knows nothing of the app) and an app child story (consume and display) carrying a one-way,
  versioned dependency note — e.g. *"blocked by FoodUs-Server: friends endpoints, contract v1,
  deployed"*. Paired stories share a common slug so the counterpart is findable by name. The
  dependency arrow only ever points app → server.
- Licensing: GPL does not reach across the network boundary. The relay is a separate work and
  may carry its own licence.

### The Wire Contract

The wire contract — envelope shape, endpoints, auth scheme, version stamps — is a first-class
maintained document in this repository, the single source of truth both agents read. It is the
first deliverable of Milestone 3 (story `wire-contract-v1`).

- *Location (confirmed, story `wire-contract-v1` complete):*
  [../docs/wire-contract-v1.md](../docs/wire-contract-v1.md).
- Every packet carries a version stamp (e.g. envelope v1). A receiver that sees an unknown
  version refuses loudly instead of silently mangling. A client that meets an unknown envelope
  version on drain leaves it queued and skips past it, so polling never permanently fails.
- When the contract changes, both sides change in the same sitting.

### API Versioning and Evolution

- **Additive by default** within a major version: never rename, remove, or repurpose. A needed
  rename/removal is the signal for a new major version.
- **Tolerance on both sides:** receivers tolerate extra unknown fields (new server, old
  client) and absent fields (old server, new client) — absent means "not provided", never a
  crash.
- **Major versions as parallel routes** (`/v1/`, `/v2/`, ...) only for genuinely breaking
  changes, with a cutover window of a month or two before decommissioning the old route.
- **Version/capability endpoint:** the relay answers "what version are you / do you support X"
  so clients adapt at runtime. The app hides or greys features the connected relay doesn't
  report.

### Repository Structure

```text
FoodUs-Server/
|-- context/                  (this context tier system)
|-- harness/                  (verifiers, gates, guardrail seams)
|-- docs/                     (wire contract spec: wire-contract-v1.md)
|-- source/                   (planned: ASP.NET relay solution)
|-- tests/                    (planned)
|-- scripts/                  (planned: committable publish script)
`-- README.md
```

Update this tree as the server solution takes shape in Milestone 3.

---

## Processing Pipelines

### Message flow (steady state)

1. Sender's phone encrypts an entry with the recipient's stored public key and pushes the
   sealed, version-stamped envelope to the relay over HTTPS.
2. The relay appends the envelope to the recipient GUID's queue. It cannot read it.
3. The recipient's phone, on app wake (poll-on-wake only — no background polling, no FCM),
   drains its mailbox, decrypts locally, and routes.
4. Envelopes undelivered after **30 days** are swept.

### Deployment pipeline

1. `dotnet publish` produces a self-contained bundle locally — **publish, don't build on the
   server**.
2. One committable publish script: publish → secure copy over SSH → restart the systemd
   service. The script is public; it contains steps, never credentials.
3. Server deploys **before** any app release that consumes the new capability.

---

## Configuration

### Primary Configuration

The repo carries a config template with blank values (exact file name fixed during Milestone 3
implementation). Real runtime values (database path, signing material, etc.) live on the
droplet as environment variables or an uncommitted config file.

### Secrets and Credentials

See [wiki/secrets.md](wiki/secrets.md): public mechanism, private values. The relay's endpoint
address is itself a secret and is never published anywhere in this repository.

---

## Security and Privacy

- **Data at rest:** only ciphertext envelopes, GUIDs, usernames, public keys, friend codes,
  and block relationships. No plaintext diary data ever reaches the server.
- **Transport:** HTTPS only, terminated by Caddy; relay listens on localhost only.
- **Host hardening:** SSH key authentication only, no password auth.
- **Endpoint authentication (settled — see the [wire contract](../docs/wire-contract-v1.md)
  §5):** "no accounts" still requires proof of GUID ownership so a known GUID cannot be drained,
  overwritten, re-keyed, impersonated, or replayed. **Detached request signing:** every
  authenticated request carries a signature over method, path, body hash, timestamp, and nonce,
  made with the device private key whose public half is registered on the profile (the app's
  crypto identity doubles as the device credential). Replay protection is a timestamp freshness
  window plus a nonce cache covering that window. A re-key announcement is trusted only when
  the new public key is signed by the old key. Algorithms are ECDSA P-256 over SHA-256 with
  base64url encodings — chosen so Android Keystore and dotnet built-in crypto both implement
  them with stock primitives.
- **Block semantics:** blocked requesters receive "user not found", indistinguishable from a
  nonexistent user.
- **Sweep:** undelivered messages are deleted after 30 days.
- **No public service operation:** the source is published; the owner's instance is private.

---

## Observability

### Logging

Application logging approach to be defined during Milestone 3 implementation. Logs must never
contain envelope contents (they are ciphertext anyway) or anything that deanonymizes users
beyond what the database already holds.

### Health Checks

The DigitalOcean monitoring agent (free memory/CPU graphs) is enabled on the droplet. The
version/capability endpoint doubles as a liveness signal.

---

## Testing Policy

Follow the workspace unit-testing rules: tests serve confidence, not coverage. For this repo
specifically, the security-critical logic — signature verification, endpoint auth, replay
protection, block enforcement ("user not found" indistinguishability), version-stamp refusal,
and the 30-day sweep — is stable business logic and gets unit tests. Transport glue and
hosting configuration are validated by deployment smoke checks and the household proof
(app-repo Milestone 3 Story 14) rather than mocks.

---

## Performance

Not a concern by design. The relay serves a two-phone household; the cheapest droplet tier
handles it. The advised plan is the $6 USD/mo 1 GB droplet for headroom under Ubuntu + dotnet
runtime + Caddy (or 512 MB plus a swap file). Revisit only if the relay is ever asked to serve
more than a handful of users.

---

## Assumptions and Open Questions

Recorded here so they are not stranded in the dictation; each is also staged on the owning
story in [milestones/milestone-3.md](milestones/milestone-3.md).

1. **Relay endpoint authentication scheme** — **Resolved:** detached request signing over
   method + path + body hash + timestamp + nonce, with a timestamp-freshness replay window plus
   nonce cache, and re-key announcements trusted only when the new public key is signed by the
   old key. ECDSA P-256 / SHA-256, base64url. *(Owner, 2026-07-28, run `rails-boss-execute`;
   specified in [wire-contract-v1.md](../docs/wire-contract-v1.md) §5.)*
2. **Friend-code minting authority** — **Resolved:** server-assigned; the relay generates codes
   and guarantees uniqueness, so no wire collision protocol exists. *(Owner, 2026-07-28, run
   `plan-spam-3_1-to-3_6`.)*
3. **Friend-code alphabet** — **Resolved:** Crockford-style 32-symbol alphabet — uppercase
   letters and digits excluding 0/O and 1/I; case-insensitive on input, displayed uppercase;
   shape fixed at 4-4-4 dashed blocks. *(Owner, 2026-07-28, run `plan-spam-3_1-to-3_6`.)*
4. **Wire contract document** — **Resolved:** written and living at
   [docs/wire-contract-v1.md](../docs/wire-contract-v1.md), covering endpoints, envelope schema,
   auth handshake, and error semantics. Path confirmed by the owner, 2026-07-28 (run
   `plan-spam-3_1-to-3_6`). The app-flagged unknown-version envelope disposition was settled in
   the same document: leave queued until the 30-day sweep, with selective acknowledgement so
   polling never permanently fails *(Owner, 2026-07-28, run `rails-boss-execute`)*.
5. **Which domain** points at the droplet — **open**; owner holds several; to be sorted in
   Milestone 3 Story 6 (the value stays private either way).

---

## Context Maintenance

Use Dictation to revise this design when the project vision changes. Do not leave important decisions stranded in raw notes, chats, or addenda. Promote durable decisions into this file, the Milestones Index, milestone docs, stories, or implementation plans as appropriate.

When an older design statement is superseded, update it directly and preserve only the rationale needed for future agents to understand the decision.

---

## Navigation

### Specification Hierarchy

- [dictations-tier-0/README-DICTATIONS-TIER-0.md](dictations-tier-0/README-DICTATIONS-TIER-0.md) - Dictation, raw and unstructured
- [design.md](design.md) - Design overview and Milestones Index
- [milestones/](milestones/) - Milestone documents, each directly containing its story list
- [backlog/](backlog/) - Story inventory / Milestone -1, the unscheduled staging pool
- `implementation-plans/*/` - Implementation Plans, optional Phases, and execution records

### Reference Documents

- [agenticworkflow.md](agenticworkflow.md) - AI collaboration workflow
- [agent-thinking.md](agent-thinking.md) - optional temporary agent scratchpad

### Wiki

- [wiki/home.md](wiki/home.md) - wiki navigation hub
