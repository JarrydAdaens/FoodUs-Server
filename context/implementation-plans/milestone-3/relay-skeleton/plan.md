# Plan: Relay service skeleton

## Metadata

- Task Type: `STORY`
- Status: `Draft`
- Owner: `Jarryd Adaens`
- Last Updated: `28 July 2026`

## Linked Context

- Milestone: [context/milestones/milestone-3.md](../../../milestones/milestone-3.md)
- Story: [Milestone 3, Story 2: Relay service skeleton](../../../milestones/milestone-3.md#story-2) (slug `relay-skeleton`, Type: Feature)
- Backlog source: none — story was synthesized directly into Milestone 3 from the tier-0 dictation
- Dictation source: [2026-07-27 FoodUs relay tier-0 design seed](../../../dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md) (stack decisions: ASP.NET minimal API, SQLite, localhost behind Caddy, config template + `.gitignore` secret blocking)
- Related Plans: [wire-contract-v1/plan.md](../wire-contract-v1/plan.md) — Story 1; this story executes **after** it, and this plan's capability-endpoint details are contingent on its adjudicated spec
- External Tooling: `rails-planning` (this plan), `rails-grade-cer` (CER grading, deferred), `commit-log` (when committing)
- Peer repo (read-only): `D:\forked-projects\FoodYou` — app Milestone 3 Story 15 (relay URL setting) performs its connection check against this story's version/capability endpoint, and every relay-backed app feature gates on it (capability-aware UI obligation)

## CER

- Complexity: —
- Effort: —
- Risk: —
- Notes: Grading deferred to `rails-grade-cer`.

## Objective

Stand up the ASP.NET (C#) minimal API relay chassis: the solution under `source/` with a test project under `tests/`, SQLite storage wiring (connection handling plus a minimal startup migration mechanism), Kestrel bound to localhost only, the committed config template with blank values, `.gitignore` secret-blocking, and the version/capability endpoint — the runtime signal the app's graceful-degradation model depends on, doubling as the liveness check.

> **Execution gate:** this plan must not begin execution until Story 1's wire contract (`docs/wire-contract-v1.md`) is written and **owner-adjudicated**. The contract fixes the capability endpoint's exact route and response schema, the `/v1/` base-path convention, and the data types any early schema touches. Contract-contingent details below are marked as such and carried in Questions / Unknowns.

## Scope

### In Scope

- New solution and project scaffolding: `source/FoodUsRelay.sln`, the minimal API web project `source/FoodUsRelay/`, and the test project `tests/FoodUsRelay.Tests/`.
- Kestrel configured to listen on localhost only (e.g. `http://127.0.0.1:5000`), matching the design chain phone → Caddy → localhost relay.
- SQLite wiring: `Microsoft.Data.Sqlite` connection handling, database path taken from configuration, and a minimal idempotent schema-migration mechanism run at startup (numbered SQL scripts + a schema-version record).
- Configuration surface: committed defaults safe for local development, a committed production config **template with blank values**, environment-variable override as the droplet's real-value mechanism, and `.gitignore` rules that block real secret files.
- The version/capability endpoint per the contract, returning the relay's contract version and supported capability set, serving as the liveness signal.
- One smoke-level integration test proving the app boots and the capability endpoint answers — the seam later stories' real tests plug into.
- Update `context/design.md`'s Repository Structure tree and `context/milestones/milestone-3.md` Story 2 row/section as the solution takes shape.

### Out Of Scope

- Any domain endpoint or domain table: profiles, endpoint auth, friend codes, blocks, mailbox, sweep — Stories 3–5 own those and their schema migrations.
- Caddy, TLS, systemd, droplet provisioning, publish script — Story 6 (`hosting-deployment`).
- Editing the wire contract or anything in the peer app repository.
- Any real endpoint address, domain, credential, or private runtime value.

## Non-Goals

- No Docker, no container tooling (deferred to backlog; plain systemd is Story 6's model).
- No ORM / EF Core: the data model is tiny and the dictation's stack decision is SQLite + minimal machinery; raw `Microsoft.Data.Sqlite` with parameterized SQL keeps the dependency surface at the Innovation Boundary.
- No speculative abstractions: no repository-pattern layers, no plugin seams, no configuration knobs beyond what this story needs (laws.md Scope vs. Speculation).
- No authentication middleware — the auth scheme is Story 1's adjudication and Story 3's implementation; the capability endpoint is deliberately unauthenticated (it must serve capability discovery before a client is known).

## Current Understanding

- Repository state: **greenfield** — no production code exists. `source/`, `tests/`, and `scripts/` are "planned" entries in design.md's Repository Structure tree; this story creates the first two.
- Likely files or directories (all new):
  - `source/FoodUsRelay.sln` — the solution.
  - `source/FoodUsRelay/FoodUsRelay.csproj` — the ASP.NET minimal API web project (single project; the relay is small enough that class-library splits would be speculative).
  - `source/FoodUsRelay/Program.cs` — minimal API host: Kestrel localhost binding, configuration load, startup migration, endpoint mapping.
  - `source/FoodUsRelay/Data/` — SQLite connection factory and the schema migrator (each primary type in its own file per the global rules).
  - `source/FoodUsRelay/Data/Migrations/` — numbered idempotent `.sql` scripts; this story ships only the baseline (schema-version bookkeeping).
  - `source/FoodUsRelay/Endpoints/` — endpoint mapping units; this story ships only the capability endpoint.
  - `source/FoodUsRelay/appsettings.json` — committed, local-development-safe defaults (localhost URL, relative dev database path). Never a secret.
  - `source/FoodUsRelay/appsettings.Production.template.json` — the committed config template with **blank values** (database path, any future signing material path); copied on the droplet to an uncommitted real file or expressed as environment variables.
  - `.gitignore` (repo root or `source/`-level additions) — standard dotnet ignores plus explicit blocks for `appsettings.Production.json`, `*.secrets.json`, `.env`, and local SQLite files (`*.db`, `*.db-wal`, `*.db-shm`).
  - `tests/FoodUsRelay.Tests/FoodUsRelay.Tests.csproj` — xUnit test project referencing the web project (WebApplicationFactory-style boot test).
- Contract surface this story consumes (from Story 1's planned spec skeleton — contingent until adjudicated): `/v1/` route prefix; a version/capability query endpoint; additive-evolution rules (the capability response is exactly the mechanism that lets new capabilities appear without breaking old clients).
- Consumer surface (app repo, read-only grounding): app Story 15's relay-URL connection check calls the capability endpoint and degrades gracefully when unreachable; all relay-backed app UI gates on the capability list. The skeleton must therefore report an honest, initially near-empty capability set — capabilities appear as Stories 3–5 deploy ("server leads, app follows").
- Existing behaviors to preserve: constitutional constraints only — relay never faces the internet directly (localhost binding is this story's share of that), no secrets/endpoint addresses/credentials in the repo ever, no accounts.
- Known tests, build steps, or observability points: none exist yet; this story creates the first (`dotnet build`, `dotnet test`, run + curl smoke). `harness/` has no installed modules (index table empty as of 28 July 2026) — no matching gates to run; re-check `harness/README-HARNESS.md` at execution start.
- Assumptions and constraints:
  - Testing policy (design.md): the skeleton is transport glue and hosting configuration — validated by smoke/deployment checks, not mocks. The first real unit-test seams (signature verification, block indistinguishability, version refusal, sweep boundaries) land in Stories 3–5; this story only ensures the test project exists and boots the app.
  - Dependency budget: `Microsoft.Data.Sqlite` plus the standard ASP.NET/xUnit test stack. Nothing else without justification.
  - The capability endpoint is intentionally the one contract-facing piece in this story; everything else is inward-facing chassis.

## Questions / Unknowns

- Q: STORY 3.2 — What are the exact route and response schema of the version/capability endpoint (and its precise relationship to the `/v1/` prefix — versioned route, unversioned root, or both)?
  Impact: This is the story's only wire-facing deliverable and the app's capability-aware UI consumes it verbatim; guessing here would fork the contract. Also determines whether the liveness check Story 6 and app Story 15 use is version-stable across future major routes.
  Assumption: Per Story 1's plan skeleton — a version/capability query endpoint under the `/v1/` prefix returning contract version plus a capability list; final shape taken from the adjudicated `docs/wire-contract-v1.md`, not from this assumption.
  Status: `OPEN`
  Answer: — (gated on Story 1 completion)

- Q: STORY 3.2 — Which .NET version does the relay target?
  Impact: Fixes the SDK on the dev machine and the runtime footprint on the droplet (design budgets 1 GB for Ubuntu + dotnet + Caddy); baked into the csproj and later Story 6's publish/systemd setup, so it should be settled once, here.
  Assumption: The current LTS release (.NET 10) — set-and-forget appliance semantics favor LTS support windows, matching the Ubuntu 24.04 LTS hosting choice.
  Status: `OPEN`
  Answer: —

- Q: STORY 3.2 — Exact config template file name and shape: is `appsettings.Production.template.json` (committed, blank values) + uncommitted `appsettings.Production.json` / environment variables on the droplet the owner's preferred convention?
  Impact: Design.md explicitly defers "exact file name fixed during Milestone 3 implementation" to this story; the name is what setup instructions (Story 6) and the `.gitignore` blocking rules key on, so it should not churn.
  Assumption: `appsettings.Production.template.json` as proposed above, leaning on ASP.NET's standard environment/`appsettings.{Environment}.json`/environment-variable override chain rather than inventing a custom config loader.
  Status: `OPEN`
  Answer: —

- Q: STORY 3.2 — How much SQLite schema counts as "schema foundations" in this story: machinery only, or domain tables up front?
  Impact: Milestone rough scope says "SQLite schema foundations"; over-reading it would have this story define profiles/codes/blocks/mailbox tables before their owning stories (and before the contract's data types are final), coupling the skeleton to decisions it doesn't own.
  Assumption: Machinery only — connection handling, the migration runner, and a baseline migration establishing schema-version bookkeeping. Domain tables ship as numbered migrations inside Stories 3–5, each beside the code that uses them.
  Status: `OPEN`
  Answer: —

## Execution Steps

> All steps are gated on Story 1's contract being owner-adjudicated (see Objective). Step 5 is where the contract dependency actually bites; Steps 1–4 could technically start earlier, but the story executes as a unit after the gate to avoid building against a moving spec.

1. Scaffold the solution and projects.
   - Why: Creates the chassis every later story bolts onto, at the paths design.md plans (`source/`, `tests/`).
   - Edits: `dotnet new` the solution `source/FoodUsRelay.sln`, minimal API web project `source/FoodUsRelay/`, and xUnit project `tests/FoodUsRelay.Tests/` (referencing the web project); strip template noise (no HTTPS redirection — TLS is Caddy's job; no OpenAPI/Swagger unless the owner wants it; no weather-forecast sample).
   - Dependencies: .NET version question answered (or its LTS assumption accepted).

2. Bind Kestrel to localhost only.
   - Why: Constitutional — the relay never faces the internet directly; Caddy (Story 6) proxies to localhost.
   - Edits: `Urls`/Kestrel endpoint configuration in `appsettings.json` pinned to `http://127.0.0.1:5000` (port from config, host loopback); no HTTPS endpoint in the relay itself.
   - Dependencies: after Step 1.

3. Wire SQLite storage foundations.
   - Why: Every capability story (3–5) needs a working database seam on day one; the skeleton owns the machinery, not the domain schema.
   - Edits: Add `Microsoft.Data.Sqlite`; `Data/` connection factory reading the database path from configuration; `Data/` schema migrator that applies numbered idempotent `.sql` scripts from `Data/Migrations/` at startup and records the applied version; baseline migration `001` creating the schema-version bookkeeping only (per the Questions assumption). Parameterized SQL discipline stated at the seam (laws.md Data Access).
   - Dependencies: after Step 1; schema-foundations question resolved or assumption accepted.

4. Establish the configuration surface and secret-blocking.
   - Why: "Public mechanism, private values" — the repo ships a template; real values live on the droplet as environment variables or an uncommitted file.
   - Edits: `appsettings.json` with local-dev-safe defaults (no secrets by construction); committed `appsettings.Production.template.json` with blank values and a header comment explaining the copy-and-fill convention; `.gitignore` additions blocking `appsettings.Production.json`, `*.secrets.json`, `.env`, and local `*.db*` files; confirm ASP.NET's environment-variable override chain covers every templated key (no custom loader).
   - Dependencies: after Step 1; config-naming question resolved or assumption accepted.

5. Implement the version/capability endpoint per the adjudicated contract.
   - Why: The one wire-facing deliverable — the runtime capability signal the app's graceful-degradation model depends on, and the liveness signal (design.md Observability).
   - Edits: `Endpoints/` capability endpoint mapped at the contract's exact route under `/v1/`, returning the contract's exact response schema (contract version + honest capability list — initially reporting only what actually exists, i.e. essentially nothing beyond the capability query itself). Unauthenticated by design. Response construction follows the additive-evolution rules so future capabilities append without breaking old clients.
   - Dependencies: **hard-gated on `docs/wire-contract-v1.md` being owner-adjudicated**; after Steps 1–2.

6. Add the smoke-level boot test and run the manual smoke pass.
   - Why: Testing policy says skeleton glue gets smoke validation, not mocks; this also plants the seam Stories 3–5's real unit tests grow from.
   - Edits: One integration test in `tests/FoodUsRelay.Tests/` (WebApplicationFactory-style) asserting the app boots, migrations apply against a throwaway database, and the capability endpoint returns the contract-shaped response. Manual: `dotnet run`, curl `http://127.0.0.1:5000/...` succeeds, and an external-interface request refused/unreachable (validation section).
   - Dependencies: after Steps 3 and 5.

7. Update maintained context and produce the commit log.
   - Why: laws.md Definition of Done — docs updated when behavior/scope changes; the milestone index drives downstream sequencing.
   - Edits: `context/design.md` Repository Structure tree (`source/`, `tests/` no longer "planned"; record actual layout) and, if the config-template question's answer differs from design's wording, the Configuration section; `context/milestones/milestone-3.md` Story 2 row (plan link, status) and section status; output a commit log via the `commit-log` skill in chat (commit only if explicitly requested; push never).
   - Dependencies: after Steps 1–6.

## Validation

### Automated Checks

- `dotnet build source/FoodUsRelay.sln` — clean build, no warnings introduced.
- `dotnet test tests/FoodUsRelay.Tests` — the boot/capability smoke test passes.
- Harness: no installed modules exist as of planning (empty index in `harness/README-HARNESS.md`); re-check at execution start and record in the Execution Log either the gate results or that no matching module exists.

### Manual Checks

1. Smoke run: `dotnet run` from `source/FoodUsRelay/`; curl the capability endpoint on `127.0.0.1` and receive the contract-shaped response (doubles as the liveness-signal proof).
2. Localhost-only proof: confirm the listener is bound to loopback only (e.g. `netstat`/`Get-NetTCPConnection` shows `127.0.0.1:5000`, not `0.0.0.0`), and a request against the machine's LAN address fails.
3. Secrets audit: `git status`/diff shows no real config file, database file, endpoint address, domain, or credential staged; the template contains only blank values; `.gitignore` blocks each secret pattern (verify with `git check-ignore` on a dummy `appsettings.Production.json`).
4. Contract conformance: capability endpoint route and response fields match `docs/wire-contract-v1.md` exactly — no invented fields, no missing mandatory ones.
5. Simplicity review: no ORM, no speculative layers, every project/file traces to this story's scope (laws.md §1.2 / Definition of Done).

### Acceptance Criteria

- `source/FoodUsRelay.sln` builds and runs: a localhost-only minimal API that opens/creates its SQLite database via configuration and applies its baseline migration at startup.
- The version/capability endpoint answers per the adjudicated contract and honestly reports the (initially minimal) capability set.
- The committed config template with blank values exists; real-value paths are environment variables or `.gitignore`-blocked files; the secrets audit passes.
- The smoke test passes in `tests/FoodUsRelay.Tests`; Stories 3–5 have a working solution, database seam, and test seam to build on without restructuring.
- `context/design.md` and `context/milestones/milestone-3.md` reflect the new structure and story status.

## Risk Mitigation

- Risk: Skeleton built before the contract is final, forcing rework of the capability endpoint (or worse, shipping a shape the app then conforms to instead of the spec).
  Mitigation: Hard execution gate on Story 1 adjudication (Objective note; Step 5 dependency); manual check 4 verifies conformance against the spec document, not this plan's assumption.
- Risk: A real secret, endpoint address, or local database file slips into the initial commit — the highest-consequence failure in a repo whose endpoint address is itself a secret.
  Mitigation: Template-with-blank-values convention, `.gitignore` blocks written **before** any real-value file could exist (Step 4 ordering), and manual check 3's `git check-ignore` audit as the gate before any commit.
- Risk: Localhost binding silently widened later (config override on the droplet exposing Kestrel directly).
  Mitigation: Loopback host pinned in committed config with a comment stating the constitutional rule; manual check 2 proves it now; Story 6's deployment checks re-verify on the droplet (noted here so the concern is not stranded).
- Risk: Over-scaffolding — class-library splits, repository patterns, EF Core, or config abstractions that a two-phone relay never needs.
  Mitigation: Non-Goals pin the dependency budget; manual check 5 is an explicit simplicity review against laws.md §1.2 and the Innovation Boundary.
- Risk: The homegrown migration runner grows into unowned complexity or breaks idempotency as Stories 3–5 add scripts.
  Mitigation: Keep it to sequential numbered scripts + one version record (the boring, predictable shape); the smoke test applies migrations against a throwaway database on every run, so a broken script fails fast in Story 3's first migration, not on the droplet.
- Risk: Capability list over-reports (claims capabilities Stories 3–5 haven't shipped), breaking the app's graceful-degradation trust.
  Mitigation: "Honest capability set" is an explicit acceptance criterion; each later story appends its capability in its own plan, mirroring "server leads, app follows".

## Evidence / References

- Planning inputs read: `context/laws.md` (v1.3), `context/design.md` (v3.0 — architecture chain, repository structure, configuration/secrets policy, API evolution rules, testing policy, observability), `context/milestones/milestone-3.md` (v3.0 — Story 2, constitutional constraints, interdependency order, paired-story traceability), `context/implementation-plans/milestone-3/wire-contract-v1/plan.md` (spec skeleton: `/v1/` prefix, capability endpoint section, adjudicated decisions), `context/dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md` (stack, secrets discipline, deployment ordering), `harness/README-HARNESS.md` (v3.2 — module index empty).
- Peer-repo grounding (read-only, shallow): `D:\forked-projects\FoodYou\context\milestones\milestone-3.md` — capability-aware UI obligation and Story 15's capability-endpoint connection check confirm the consumer surface and the "honest capability set" requirement.
- Known unverified claims: none — no runtime or build claims are made by this plan; the `dotnet new` template contents assumed in Step 1 are verified at execution time.
