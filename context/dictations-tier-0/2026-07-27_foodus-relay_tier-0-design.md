---
name: foodus-relay-tier-0-design
description: Tier 0 seed document for the FoodUs relay server - a new, separate repository. Captures the durable decisions from the 2026-07-27 infrastructure discussion session. To be synthesized into the relay repo's design.md when its rails are stood up.
metadata:
  version: "1.0"
  source_session: "2026-07-27 relay infrastructure discussion"
  owner: "Jarryd Adaens"
  repo: "foodus-relay (new, separate repository)"
  related: "FoodUs (fork of maksimowiczm/FoodYou) - Milestone 3: Multiplayer"
---

# FoodUs Relay - Tier 0 Design Seed

> Tier 0 (Dictation). Raw durable decisions from a working session, before full structure is
> imposed. This document seeds the **foodus-relay** repository, which gets its own complete
> agentic-rails setup (design, milestones, stories, laws) as a peer of the FoodUs app repo.
>
> Source of authority for feature scope: [FoodUs milestone-3.md](../FoodUs/milestones/milestone-3.md)
> (Stories 4, 5, and the durable relay decisions promoted into the app repo's design.md).
> This document owns the server-side infrastructure and organisational decisions settled
> 2026-07-27.

---

## What This Is

The FoodUs relay is the "post office": a deliberately dumb, self-hosted, store-and-forward
server that ferries sealed end-to-end-encrypted envelopes between household phones. It is the
single controlled break in the FoodUs app's island architecture.

It holds only: GUIDs, usernames, public keys, friend codes, block relationships, and per-GUID
queues of sealed ciphertext envelopes. It never sees plaintext diary data. A breach yields
ciphertext, usernames, and GUIDs — nothing else.

Constitutional constraints inherited from the app (restated, binding here):

- No accounts, no login, no sessions in the account sense, no server-side backup, no
  server-readable diary data, no web/companion clients.
- Undelivered messages sweep after **30 days**.
- Blocked requesters receive "user not found" — indistinguishable from a nonexistent user.
- The server is a transmission vector between databases, nothing more. Each phone's Room
  database remains the sole source of truth.

---

## Decisions Locked 2026-07-27

### Stack

- **ASP.NET (C#), minimal API style.** Chosen for owner fluency and long-term maintainability;
  this project is result-driven, not a learning exercise. The relay is small enough that all
  mainstream stacks perform identically — the deciding factor is building the security-critical
  component (signature verification, endpoint auth) in the stack the owner knows cold.
- **SQLite** for storage. The data model is tiny; a managed database is explicitly rejected.
- **dotnet's built-in cryptography libraries** for signature verification and any server-side
  crypto duties.
- Ktor/Kotlin server was considered (language symmetry with the app) and rejected: a mixed
  polyglot stack is normal professional practice, and a well-built ASP.NET relay beats a
  learned-on-the-fly Ktor one on every axis that matters, including portfolio value.
- Licensing note: GPL does not reach across the network boundary. The relay is a separate work
  that communicates with the app over the wire; it may carry its own licence.

### Hosting

- **DigitalOcean droplet, Sydney region (SYD1).**
- **Ubuntu 24.04 LTS** (security patches to 2029; set-and-forget appliance).
- Plan: cheapest tier acceptable ($4 USD/mo, 512 MB); **$6 USD/mo 1 GB plan advised** for
  headroom under Ubuntu + dotnet runtime + Caddy. If staying on 512 MB, add a swap file.
- **SSH key authentication only.** No password auth — a password-auth droplet on a public IP is
  bot-hammered within minutes.
- Public IPv4 on (phones need it), IPv6 on (free, harmless).
- DigitalOcean backups: **off** (relay data is transient ciphertext under a 30-day sweep;
  nothing on the box is precious — the precious data lives on the phones).
- Managed database: **off** ($15/mo for what SQLite does free).
- Monitoring agent: **on** (free memory/CPU graphs; useful on a small box).
- Peer-to-peer / same-network sync was considered and rejected: it loses the away-from-home
  logging case (the marquee scenario) and is harder than store-and-forward (NAT, discovery,
  both-awake requirements). Serverless was rejected as the wrong abstraction for a learner
  crossing into web hosting; a plain always-on box is the easiest thing to reason about.

### Front Door and Transport

- **HTTPS is mandatory.** Plain HTTP is disqualifying, even for sealed envelopes.
- **Caddy** as reverse proxy: it terminates TLS with automatic Let's Encrypt certificate
  issuance and renewal (near-zero config, no manual renewal chores).
- Chain: phone → domain over HTTPS → Caddy on the droplet → ASP.NET relay listening on
  localhost (e.g. port 5000). The relay never faces the internet directly.
- A cheap owned domain points at the droplet IP (owner already holds domains; to be sorted).
  Phones are configured with the domain, not the raw IP, so an IP change never touches the
  phones.

### Deployment Model

- **Publish, don't build on the server.** `dotnet publish` produces a self-contained bundle
  locally; the bundle is what ships.
- **Plain systemd service, not Docker.** Fewer moving parts, teaches what a Linux service is,
  and a single small process doesn't need containerisation. Docker may be adopted later if it
  earns its place.
- **Committable publish script.** One local command: publish → secure copy (scp over SSH) →
  restart service. The script is public; it contains steps, never credentials.
- **Secrets discipline (public mechanism, private values):**
  - SSH key: lives on the owner's local machine, outside the repo, referenced by identity.
  - Runtime secrets (database path, signing material, etc.): live on the droplet as environment
    variables or an uncommitted config file. The repo carries a **config template** with blank
    values.
  - `.gitignore` explicitly blocks real secret files as the safety net.
- **The relay's endpoint address is private.** It is never published in the repo, docs, or
  anywhere public. Endpoint auth is the real security; obscurity is a free extra layer that
  denies strangers a target for saturation or probing. The repo ships everything needed for
  someone to stand up *their own* relay (source, config template, setup instructions) — this
  instance is the owner's alone.

### Repository and Contract Ownership

- **Separate repository (`foodus-relay`)**, not a monorepo with the app. The app repo carries a
  delicate upstream-merge relationship and GPL obligations the relay doesn't share; keeping
  them apart preserves the fork's mergeability principle and keeps licensing unambiguous.
- **The server repo owns the wire contract** as a written specification document: envelope
  shape, endpoints, auth scheme. Both codebases are implementations of that one document. The
  app is a client that conforms.
- **No git submodules — explicitly off the table.** Cross-language schema sharing yields no
  code reuse (Kotlin and C# each hand-write their models anyway), and submodule pointer-bumping
  friction outweighs the payoff. Discipline replaces machinery.
- **Every packet carries a version stamp** (e.g. envelope v1). A receiver that sees an unknown
  version refuses loudly instead of silently mangling. The version field is the enforcement the
  submodule pretends to be.
- When the contract changes, both sides change in the same sitting.

### API Versioning and Evolution

- **Additive by default.** Within a major version, only ever *add* — never rename, remove, or
  repurpose. The moment a rename/removal is needed, that is the signal for a new major version,
  not a v1 edit.
- **Tolerance on both sides of the fence:**
  - Receivers tolerate **extra** fields they don't recognise (new server, old client).
  - Receivers tolerate **absent** fields (old server, new client) — absent means "not
    provided", never a crash.
- **Major versions as parallel routes** (`/v1/`, `/v2/`, ...) only for genuinely breaking
  changes: stand up v2 alongside v1, let clients drift over, decommission v1 after a cutover
  window (a month or two). Household support policy: "did you update? You had three months."
- **Version/capability endpoint.** The relay answers "what version are you / do you support X"
  so clients can adapt at runtime rather than assume.

### Deployment Ordering Rule

- **Server leads, app follows. Always deploy the server first.** New server capability sits
  dormant and harmless until the app consumes it; the reverse (app UI targeting endpoints that
  don't exist) breaks visibly. The app degrades gracefully — it hides or greys features whose
  capability the relay doesn't yet report.

### Agent and Planning Organisation

- The relay repo gets its **own complete agentic-rails structure**: design.md, milestones,
  stories, laws — a peer of the app repo, not a subordinate.
- **Two rooted agents.** Each agent stays rooted in its own repo under its own laws and
  context. Agents may **read across** the repo boundary freely (contract spec, the other
  side's implementation) but **edits happen at home** — changes to a repo are made by the agent
  rooted in it. Running an agent one level up over both repos is a deliberate exception for
  rare, genuinely simultaneous cross-boundary changes, never the default.
- **The owner is the human relay between the two agents.** Contract changes are proposed via
  reports, adjudicated by the owner, and carried across by the owner. The agents never
  negotiate the contract directly. The owner also enforces deployment sequencing.
- **Paired stories for cross-cutting features.** A feature like "friends" splits into:
  - a **server parent story** (expose the capability) that knows nothing of the app, and
  - an **app child story** (consume and display) carrying a one-way, versioned
    **dependency note** — e.g. *"blocked by foodus-relay: friends endpoints, contract v1,
    deployed"*. That note is the seam between the two planning worlds.
  - Paired stories share a **common slug** (e.g. `...-friends` on both sides) so the
    counterpart is findable by name with no tooling.
  - The dependency arrow only ever points app → server, mirroring the code. The owner releases
    the block once the server story is deployed.

---

## The Existing Android Client (FoodUs App)

The relay does not exist in isolation. The FoodUs Android app (Kotlin Multiplatform / Compose,
fork of Food You) is the sole intended client, and it must become aware of the relay. This
section records what the *app side* owes this design, so it can be carried into the app repo's
Milestone 3 stories as requirements.

### What the app already commits to (per Milestone 3 / app design.md)

- **Poll-on-wake only.** No background polling, no FCM. A brief "getting messages" step on app
  wake drains the mailbox, decrypts, routes (Full trust → insert + notify; Suggest → queue),
  and populates the Notification Center.
- **E2E crypto identity** (Milestone 3 Story 3): key pair alongside the profile GUID, private
  key in the Android Keystore, public key registered with the relay.
- **Friend-code resolution, block semantics, and the message envelope pipeline**
  (Stories 6–8) all consume the relay contract this repo owns.

### New obligations on the app arising from this session

1. **Configurable relay URL setting.** A new settings surface where the relay endpoint is
   entered by the user — same spirit and likely same neighbourhood as the existing
   user-entered AI endpoint configuration from Milestone 2. Both household phones are pointed
   at the owner's private endpoint by typing it in; the address ships nowhere in code or repo.
   This also makes the published app usable by strangers running their own relay.
2. **Contract conformance, not contract ownership.** The app implements the wire spec that
   lives in the foodus-relay repo. App-side data classes for envelopes are hand-written to
   match the spec. When the spec changes, the app changes in the same sitting.
3. **Envelope version handling.** The app stamps the envelope version on every packet it sends
   and refuses loudly (surfacing a Notification Center event, never silently dropping —
   consistent with the never-dropped invariant's spirit) on receiving a version it doesn't
   know.
4. **Two-way tolerance.** App-side deserialisation ignores unknown fields and treats absent
   fields as "not provided". This is what lets the relay evolve additively without breaking
   phones that haven't updated.
5. **Capability-aware UI.** Before exposing a relay-backed feature, the app checks the relay's
   version/capability endpoint and hides or greys features the connected relay doesn't
   support. A phone that updates before the server deploys simply waits gracefully.
6. **HTTPS only.** The app talks to the relay exclusively over HTTPS via its existing Ktor
   client stack.
7. **Dependency notes in app stories.** Every Milestone 3 app story that consumes a relay
   capability carries the one-way versioned dependency note naming its server parent story,
   using the shared slug convention. The app repo's agent reads the relay repo's contract spec
   directly (read-across is free) but never edits the relay repo.

### Sequencing reality for Milestone 3

The app's Milestone 3 stories 5–14 are downstream of this repo's build. Concretely: the relay's
architecture spike (app-repo Story 4) resolves into *this* repo's first milestone; the relay
build (app-repo Story 5) *is* this repo's delivery; app Stories 6 onward carry dependency notes
against it. The owner sequences: relay deployed → app story unblocked.

---

## Open Decisions (carried, not resolved here)

Inherited from Milestone 3 Story 4; must be settled before the relay's first implementation
plan is written:

1. **Relay endpoint authentication.** "No accounts" still requires proof of GUID ownership so a
   known GUID cannot be drained, overwritten, re-keyed, impersonated, or replayed. Likely
   shape: requests signed with the device key pair (crypto identity doubles as device
   credential) plus replay protection and a defined trust rule for re-key announcements.
2. **Friend-code minting authority:** server-assigned (uniqueness guaranteed) vs
   client-generated + registered (collision handling). *(Owner input wanted.)*
3. **Friend-code alphabet:** exact charset (exclude 0/O and 1/I ambiguity?), case rules. Shape
   is fixed (4-4-4 blocks, dashes, letters+numbers); charset is not.
4. Exact wire contract document (endpoints, envelope schema, auth handshake) — the first
   deliverable of this repo's rails.

Resolved this session (formerly open in Story 4): **stack and hosting** — ASP.NET on a
DigitalOcean droplet, as recorded above.

---

## Non-Goals

- No web or companion clients, ever, for this relay.
- No accounts, login, or server-side backup.
- No push transport (FCM or self-hosted) — deferred to the app repo's backlog; adopt only if
  poll-on-wake latency ever grates.
- No N-person groups server support beyond what the two-member group cap requires — deferred
  with the app-side backlog items.
- No public service operation. The source is published; the owner's instance is private.

---

## Synthesis Instructions

When the foodus-relay repo's rails are stood up:

- Promote the durable decisions above into the relay repo's `design.md`.
- The wire contract spec becomes a first-class maintained document in this repo — the single
  source of truth both agents read.
- Open decisions 1–4 become explicit decision points in the relay's first milestone, resolved
  (with owner input where marked) before implementation planning.
- The "Existing Android Client" section's obligations are carried back into the FoodUs app
  repo's Milestone 3 stories as requirements and dependency notes; the app repo's design.md
  "Multiplayer Exception" section gains a pointer to this repo as contract owner.

---

## Integration Notes (added at synthesis, 2026-07-27)

> Added by the initializing agent when this dictation was preserved into the repository.
> Everything above this section is the raw intake, verbatim from
> `E:\Downloads\2026-07-27_foodus-relay_tier-0-design.md`.

- Captured from: owner-provided tier-0 seed document (2026-07-27 relay infrastructure
  discussion session), delivered at repository initialization.
- Owner directive at initialization (chat, 2026-07-27): milestone numbering is **project-based**
  and shared with the FoodUs app repo. This server repo's Milestones 1 and 2 are intentionally
  empty/unused (delivered entirely in the app repo); server work begins at **Milestone 3**,
  executed in lockstep with the app repo's Milestone 3 (Multiplayer). This supersedes the
  dictation's phrasing "this repo's first milestone" — that milestone is numbered 3, not 1.
- Repository naming: the dictation's working name `foodus-relay` landed as this repository,
  `FoodUs-Server`. The peer app repository lives locally at `D:\forked-projects\FoodYou`
  (project name FoodUs, fork of maksimowiczm/FoodYou). The raw intake's relative link
  `../FoodUs/milestones/milestone-3.md` was written from the source document's original
  location and does not resolve from inside this repository; the document it means is the app
  repo's `context/milestones/milestone-3.md`. Preserved as-is per raw-intake fidelity.
- Update `../design.md` (and its Milestones Index): done — durable decisions promoted;
  Milestones Index rows for Milestones 1–3.
- Update `../milestones/`: done — `milestone-1.md` and `milestone-2.md` as not-applicable
  stubs; `milestone-3.md` carries the server-side Multiplayer Relay stories.
- Update `../backlog/`: done — initialization story plus deferred items named by this
  dictation (N-person group support, Docker adoption).
- Update `../implementation-plans/`: deliberately not created at initialization; plans are
  written close to execution, after the wire-contract story's open decisions are resolved.
