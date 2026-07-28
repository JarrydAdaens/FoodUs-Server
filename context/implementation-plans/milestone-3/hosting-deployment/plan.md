# Plan: Hosting & first deployment

## Metadata

- Task Type: `STORY`
- Status: `In Progress`
- Owner: `Jarryd Adaens`
- Last Updated: `28 July 2026`

## Linked Context

- Milestone: [context/milestones/milestone-3.md](../../../milestones/milestone-3.md)
- Story: [Milestone 3, Story 6: Hosting & first deployment](../../../milestones/milestone-3.md#story-6) (slug `hosting-deployment`, Type: Feature)
- Backlog source: none — story was synthesized directly into Milestone 3 from the tier-0 dictation
- Dictation source: [2026-07-27 FoodUs relay tier-0 design seed](../../../dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md) (Hosting, Front Door and Transport, Deployment Model sections — decisions locked 2026-07-27)
- Related Plans: [wire-contract-v1 plan](../wire-contract-v1/plan.md) (context only — hosting barely touches contract internals); Story 2 `relay-skeleton` plan (not yet written) produces the deployable service this plan ships
- External Tooling: `rails-planning` (this plan), `rails-grade-cer` (CER grading, deferred), `commit-log` (when committing)
- Secrets policy: [context/wiki/secrets.md](../../../wiki/secrets.md) — public mechanism, private values; binding on every artifact this story commits

## CER

- Complexity: —
- Effort: —
- Risk: —
- Notes: Grading deferred to `rails-grade-cer`.

## Objective

Stand up the relay's live private endpoint: a provisioned and hardened DigitalOcean droplet (SYD1, Ubuntu 24.04 LTS) running the relay as a localhost-bound plain systemd service behind Caddy with automatic Let's Encrypt HTTPS, reachable via an owner-held domain, deployed by a committable publish script (local `dotnet publish` → scp → service restart). The repo gains the public mechanism — publish script, Caddyfile and systemd unit templates with placeholder values, and setup instructions a stranger can follow to stand up their own relay — while every private value (domain, IP, credentials, runtime secrets) lands on the droplet or the owner's machine only. **Server leads, app follows:** this deployment must exist before any app-repo dependency note is released.

## Scope

### In Scope

- Committed deployment artifacts (public mechanism, placeholder values only):
  - `scripts/publish.ps1` — the committable publish script: `dotnet publish` self-contained bundle locally → scp over SSH → restart the systemd service. Steps, never credentials; host and SSH identity supplied by parameters or local untracked config.
  - `deploy/Caddyfile.template` — Caddy reverse-proxy config with a placeholder host (e.g. `relay.example.com`) proxying to the localhost relay port.
  - `deploy/foodus-relay.service.template` — systemd unit template: dedicated service user, working directory, environment/secrets sourcing, restart-on-failure.
  - `docs/self-hosting.md` — setup instructions generic enough for a stranger: droplet provisioning, hardening, Caddy install, DNS, unit install, first deploy, smoke check. Placeholder values throughout.
- Droplet provisioning and hardening (owner-executed, values never committed): SYD1, Ubuntu 24.04 LTS, SSH-key-only auth, monitoring agent on, backups off, managed database off, IPv4 + IPv6 on; firewall allowing only SSH/HTTP/HTTPS; dedicated non-root service user; swap file if the 512 MB plan is chosen.
- Caddy installation and real-value configuration on the droplet; automatic Let's Encrypt issuance and renewal.
- DNS: pointing the chosen owner-held domain at the droplet IP (owner action; value private).
- Installing the systemd unit with real values and runtime secrets on the droplet only.
- First deployment of the Story 2 relay build via the publish script, then smoke checks (capability endpoint over HTTPS as liveness).
- Context maintenance: `design.md` Repository Structure tree and open-question 5 resolution note (domain chosen, value private), `wiki/secrets.md` "To Document When Implementation Lands" items 2 (droplet bootstrap) as far as this story settles them, `milestone-3.md` Story 6 plan link and status.

### Out Of Scope

- Any relay application code — Stories 2–5 own the service, its config template, and `.gitignore` secret-blocking (Story 2 scope).
- Publishing, hinting at, or committing the owner's domain, droplet IP, or any credential anywhere in the repo, docs, or commit messages.
- Docker-based deployment — explicitly rejected for now; [backlog-1 Story 3](../../../backlog/backlog-1.md#story-3).
- CI/CD automation, staging environments, blue/green deploys — one household, one box, one script.
- Releasing app-repo dependency notes — the owner does that once deployment is live.
- The household proof (app repo Story 14) — it runs against this deployment but is milestone validation, not this story.

## Non-Goals

- No public service operation: the source and instructions are published; the owner's instance stays private.
- No server-side backup of relay data (DigitalOcean backups deliberately off — relay data is transient ciphertext under the 30-day sweep).
- No push transport, no N-person scaling, no performance work (two-phone household; cheapest tier suffices by design).
- No monitoring beyond the free DigitalOcean agent and the capability endpoint as liveness.

## Current Understanding

- Likely files or directories:
  - `scripts/` — planned in design.md's Repository Structure for the publish script; does not exist yet, created here.
  - `deploy/` — proposed new directory for the Caddyfile and systemd unit templates; not in the design tree yet, so the tree must be updated when it lands (alternative: fold templates into `docs/self-hosting.md` as fenced blocks — discovery step: decide at execution start, templates-as-files preferred for copy-paste fidelity).
  - `docs/self-hosting.md` — setup instructions beside the wire contract spec.
  - `context/design.md` — Repository Structure tree update; "Assumptions and Open Questions" item 5 (domain) resolution note.
  - `context/wiki/secrets.md` — droplet bootstrap documentation hook.
  - `context/milestones/milestone-3.md` — Story 6 plan link and status.
- Likely subsystems: no code changes. The deployable artifact is Story 2's ASP.NET minimal API solution under `source/`; this plan consumes its published output and its committed config template.
- Existing behaviors to preserve: constitutional — HTTPS mandatory end to end; the relay never faces the internet directly (listens on localhost only, e.g. port 5000, behind Caddy); the endpoint address never enters this repository; runtime secrets live on the droplet only as environment variables or an uncommitted config file.
- Interfaces, data contracts, or external dependencies:
  - Architecture chain (design.md): phone → HTTPS (domain, never raw IP) → Caddy (TLS termination, Let's Encrypt) → localhost ASP.NET relay → SQLite on the droplet.
  - External services: DigitalOcean droplet (SYD1, Ubuntu 24.04 LTS, monitoring on, backups off), Caddy, an owner-held domain, Let's Encrypt.
  - The version/capability endpoint (Story 2 deliverable) doubles as the liveness signal for smoke checks.
- Known tests, build steps, or observability points: `dotnet publish` self-contained (no dotnet runtime install needed on the droplet); deployment smoke checks per design.md's testing policy — hosting configuration is validated by smoke checks and the household proof, not unit tests; DigitalOcean monitoring agent for memory/CPU graphs.
- Assumptions and constraints:
  - **Sequencing gate:** the repo has no production code yet. Execution of this plan requires at least Story 2 (relay-skeleton) built — a skeleton can deploy early (interdependency order allows preparing Story 6 in parallel after Story 2), but the milestone's deployment gate is the **full capability set live** (Stories 3–5 deployed).
  - Publish script is PowerShell (`publish.ps1`) because the owner's local machine is Windows with OpenSSH available; steps are portable enough that `self-hosting.md` describes the equivalent shell commands for non-Windows readers.
  - Self-contained publish means the droplet never needs the dotnet SDK or runtime installed — "publish, don't build on the server".
  - Caddy installed from its official apt repository (standard Ubuntu path); systemd manages both Caddy and the relay.
  - The relay's localhost port (e.g. 5000) is fixed by Story 2's configuration surface; templates reference whatever Story 2 settles on.
  - Hardening baseline beyond the dictation's SSH-key-only mandate (firewall allowing 22/80/443 only, dedicated non-root service user, unattended security updates) is treated as uncontroversial good practice, not new scope.

## Questions / Unknowns

- Q: STORY 3.6 — Which owner-held domain points at the droplet? (Owner holds several; design.md open question 5.)
  Impact: Gates DNS pointing, the real Caddyfile on the droplet, Let's Encrypt issuance, and the value typed into both phones' relay-URL setting. The value stays private regardless — it is recorded in DNS, on the droplet, and in the owner's head, never in this repo; the repo-side resolution is only a note that the question is settled.
  Assumption: Owner selects a domain (or subdomain of one) before the DNS step; all committed artifacts use the placeholder `relay.example.com` and are unaffected by the choice.
  Status: `OPEN` *(owner input)*
  Answer: —

- Q: STORY 3.6 — Droplet plan: the advised $6 USD/mo 1 GB, or $4 USD/mo 512 MB plus a swap file?
  Impact: Decides provisioning parameters and whether the hardening sequence includes swap-file creation; also whether `docs/self-hosting.md` presents swap as required or optional. Dictation advises 1 GB for headroom under Ubuntu + dotnet + Caddy.
  Assumption: The advised $6/mo 1 GB plan; `self-hosting.md` documents the 512 MB + swap path as the budget alternative either way.
  Status: `ANSWERED`
  Answer: Assumption accepted for the **document only** — `docs/self-hosting.md` advises 1 GB and documents 512 MB + swap (with the swap-file commands) as the budget alternative. The actual plan purchased remains the owner's choice at provisioning time; no committed artifact depends on it. *(Boss run rails-boss-execute, 2026-07-28, plan assumptions accepted.)*

- Q: STORY 3.6 — Do the Caddyfile and systemd unit templates live as files under a new `deploy/` directory, or as fenced blocks inside `docs/self-hosting.md`?
  Impact: Repository Structure tree in design.md; copy-paste fidelity for a stranger standing up their own relay.
  Assumption: Standalone template files under `deploy/` (scp-able as-is, diffable, referenced from the doc); design tree updated when they land. Low-stakes — resolvable at execution start without owner input unless the owner cares.
  Status: `ANSWERED`
  Answer: Standalone files under `deploy/` — `Caddyfile.template` and `foodus-relay.service.template`, both linked from `docs/self-hosting.md`. `design.md`'s Repository Structure tree gained `deploy/` and un-planned `scripts/`. *(Boss run rails-boss-execute, 2026-07-28, plan assumptions accepted.)*

## Execution Steps

1. Author the committed deployment artifacts (repo work; can start as soon as Story 2's config surface and localhost port are fixed).
   - Why: The public mechanism must exist and be reviewable before anything touches a real server; a stranger's setup path and the owner's setup path are the same documents.
   - Edits: Create `scripts/publish.ps1` (parameters/local-config for host + SSH identity; `dotnet publish` self-contained → scp bundle → `systemctl restart` over SSH; no credential, host value, or key path baked in), `deploy/Caddyfile.template` (placeholder host, reverse_proxy to the Story 2 localhost port), `deploy/foodus-relay.service.template` (service user, env/secrets sourcing per the Story 2 config template, `Restart=on-failure`), and `docs/self-hosting.md` (full stranger-ready walkthrough of Steps 2–7 with placeholder values).
   - Dependencies: Story 2 built (port + config template known). Template-location question resolved (assumption: `deploy/`).

2. Provision the droplet (owner console/CLI action; no repo changes).
   - Why: The always-on host everything else installs onto.
   - Edits: None in repo. DigitalOcean: SYD1, Ubuntu 24.04 LTS, plan per the open question (assumption: 1 GB $6/mo), SSH key added at creation (key-only from first boot), monitoring agent on, backups off, managed database off, IPv4 + IPv6 on.
   - Dependencies: Plan-size question answered (or assumption accepted).

3. Harden the droplet.
   - Why: A public-IP box is bot-hammered within minutes; SSH-key-only and a minimal firewall are the dictation-mandated and baseline defenses.
   - Edits: On droplet only — verify password auth disabled in sshd config; enable firewall allowing only SSH (22), HTTP (80, for ACME) and HTTPS (443); create the dedicated non-root service user the systemd unit runs as; enable unattended security updates; create swap file if on the 512 MB plan.
   - Dependencies: After Step 2.

4. Install Caddy and stage the real front-door config.
   - Why: Caddy terminates TLS with automatic Let's Encrypt issuance/renewal — the mandatory-HTTPS front door.
   - Edits: On droplet only — install Caddy from its official apt repo; copy `Caddyfile.template` and fill the real domain and relay port; do not start issuance until DNS resolves (Step 5), since Let's Encrypt needs the domain pointing at the box.
   - Dependencies: After Step 3; real values require the domain question answered.

5. Point the owner-held domain at the droplet.
   - Why: Phones are configured with the domain, never the raw IP, so an IP change never touches the phones; Let's Encrypt validation requires it.
   - Edits: None in repo. Owner sets A (and AAAA) records for the chosen domain to the droplet IP; verify propagation; then start/reload Caddy and confirm certificate issuance.
   - Dependencies: Domain question answered (owner); after Step 4.

6. Install the relay as a systemd service and run the first deployment.
   - Why: Plain systemd (not Docker) is the locked deployment model; the publish script is the single deployment path from day one — first deploy proves it, not a hand-carried exception.
   - Edits: On droplet only — place the filled systemd unit from the template; create the uncommitted runtime config/env values per Story 2's config template and `wiki/secrets.md`; enable the service. Locally: run `scripts/publish.ps1` end to end (publish → scp → restart) to ship the current Story 2+ build.
   - Dependencies: After Steps 1, 3; Caddy path (Steps 4–5) needed only for the HTTPS smoke check, so unit install may proceed in parallel.

7. Smoke-check the deployment.
   - Why: Design testing policy — hosting is validated by deployment smoke checks; the capability endpoint doubles as liveness.
   - Edits: None. Checks recorded in this plan's Execution Log: capability endpoint responds over `https://<domain>/...` from an external network (phone on mobile data is the honest check); certificate valid and auto-issued; plain-HTTP request is redirected or refused, never served; the relay port is unreachable on the droplet's public IP (localhost binding + firewall proven); service survives a droplet reboot (`systemctl` enabled units come back); publish script re-run is idempotent (deploy twice, still healthy); DigitalOcean monitoring graphs reporting.
   - Dependencies: After Steps 5–6.

8. Secrets audit of everything committed.
   - Why: Constitutional — the endpoint address is itself a secret; laws.md §2 forbids committed credentials; this is the story's hard guardrail.
   - Edits: None if clean. Sweep every committed file and staged diff from this story (`scripts/`, `deploy/`, `docs/self-hosting.md`, context edits, commit messages) for the real domain, droplet IP, SSH usernames/key paths, or any credential; confirm only placeholder values appear. Remediate before any commit if found.
   - Dependencies: After Step 1 (first pass) and again before final commit (after Steps 6–7, in case execution notes leaked values).

9. Context maintenance.
   - Why: Decisions must not strand in this plan; design and milestone docs must reflect reality (laws.md Definition of Done).
   - Edits: `context/design.md` — Repository Structure tree gains `deploy/` and un-plans `scripts/`; "Assumptions and Open Questions" item 5 marked resolved as "domain selected and pointed; value private, recorded nowhere in this repo". `context/wiki/secrets.md` — fill "To Document When Implementation Lands" item 2 (droplet bootstrap: env/config placement) as settled here. `context/milestones/milestone-3.md` — Story 6 plan link, status transitions, milestone story count on completion. Note: the milestone's deployment *gate* (full capability set live) may trail this story's mechanics; record the deployed capability level explicitly in the Execution Log.
   - Dependencies: Plan-link edit at execution start; completion edits after Steps 7–8.

## Validation

### Automated Checks

- None applicable as unit tests — per design.md's testing policy, hosting configuration is validated by deployment smoke checks, not mocks. If `harness/README-HARNESS.md` documents a matching gate (docs verifier, secrets scanner) at execution time, run it and record the result; otherwise record that no matching harness check exists.
- Scriptable secrets sweep (Step 8): grep the committed tree and diffs for the real domain string, droplet IP, and credential patterns — must return nothing.

### Manual Checks

1. Capability endpoint over HTTPS from an external network (mobile data) returns a healthy response with a valid Let's Encrypt certificate — the liveness proof.
2. Plain HTTP is not served; the relay's localhost port is unreachable from the public internet (direct IP:port probe fails).
3. `scripts/publish.ps1` run from the owner's machine performs publish → scp → restart end to end, twice, with the service healthy after each run.
4. Droplet reboot: Caddy and the relay both return without manual intervention.
5. Stranger test (desk-check): `docs/self-hosting.md` read top to bottom contains every step and no owner-specific value — a reader with their own droplet and domain could follow it without asking anything.
6. Secrets audit: no domain, IP, credential, key path, or username of the owner's instance in any committed file, template, doc, context edit, or commit message.

### Acceptance Criteria

- The relay build is live behind Caddy/HTTPS at the private domain, running as an enabled systemd service, deployed exclusively via the committed publish script.
- Repo contains the complete public mechanism: publish script, Caddyfile template, systemd unit template, and stranger-ready setup instructions — all placeholder-valued.
- All smoke checks above pass and are recorded in the Execution Log.
- Secrets discipline intact: audit clean; runtime secrets exist on the droplet only.
- design.md, wiki/secrets.md, and milestone-3.md reflect the outcome; the deployed capability level (skeleton vs full set) is recorded, and the milestone gate is only claimed satisfied when Stories 3–5 are live.

## Risk Mitigation

- Risk: The owner's domain or droplet IP leaks into a committed file, doc example, or commit message.
  Mitigation: Placeholder-only rule for every committed artifact (`relay.example.com`); Step 8's audit runs twice (post-authoring and pre-commit); commit-log drafting reviewed against the same rule.
- Risk: The relay accidentally binds a public interface (or the firewall exposes its port), bypassing Caddy.
  Mitigation: Localhost binding is a Story 2 deliverable; smoke check 2 probes the public IP:port directly and must fail; firewall allows only 22/80/443.
- Risk: Let's Encrypt issuance fails (DNS not propagated, port 80 blocked) leaving the endpoint dark.
  Mitigation: Step ordering — DNS verified before Caddy issuance; firewall explicitly allows 80 for ACME; Caddy logs checked at Step 5.
- Risk: First deploy is done by hand "just this once", leaving the publish script unproven.
  Mitigation: Step 6 mandates the script as the only deployment path from the first deploy; smoke check 3 requires two successful runs.
- Risk: Deploying only the skeleton is mistaken for milestone completion.
  Mitigation: The gate distinction (story mechanics vs milestone's full-capability-set-live gate) is stated in Metadata-adjacent assumptions, Step 9, and the acceptance criteria; the Execution Log must record the deployed capability level.
- Risk: 512 MB plan (if chosen) thrashes under Ubuntu + relay + Caddy.
  Mitigation: Open question defaults to the advised 1 GB plan; swap file mandated if 512 MB is chosen; monitoring agent graphs watched after first deploy.
- Risk: `docs/self-hosting.md` drifts toward owner-specific steps and stops being stranger-usable.
  Mitigation: Manual check 5's desk-check is an explicit acceptance criterion; the doc and the owner's own setup are the same instructions by construction.
- Risk: This plan executes before Story 2 exists, with nothing to deploy.
  Mitigation: Sequencing gate stated in Current Understanding; Step 1 explicitly depends on Story 2's port and config surface; Status stays `Draft`/`Blocked` until Story 2 is built.

## Evidence / References

- Planning inputs read: `context/laws.md` (v1.3), `context/design.md` (v3.0 — architecture chain, deployment pipeline, configuration/secrets policy, observability, testing policy, open question 5), `context/milestones/milestone-3.md` (v3.0 — Story 6, constitutional constraints, interdependency order item 3), `context/dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md` (Hosting / Front Door / Deployment Model decisions locked 2026-07-27), `context/wiki/secrets.md` (v0.2), `context/implementation-plans/milestone-3/wire-contract-v1/plan.md` (Story 1 plan, context only).
- Locked hosting decisions carried verbatim from the dictation: DigitalOcean SYD1, Ubuntu 24.04 LTS, SSH-key-only, monitoring on, backups off, 1 GB advised (or 512 MB + swap), Caddy with automatic Let's Encrypt, plain systemd not Docker, publish-don't-build-on-server, committable steps-never-credentials publish script, endpoint address private.
- Known unverified claims: none — no runtime, provisioning, or build actions were performed at planning time; all droplet/DNS/Caddy behavior claims are deferred to the Step 7 smoke checks.

---

## Execution Log

**Run:** `rails-boss-execute`, 2026-07-28. **Slice executed:** repository side only — Steps 1,
8, and 9. **Deployed capability level: none.** Nothing has been provisioned or deployed; the
milestone's deployment gate (full capability set live) is untouched.

| Step | Outcome |
| --- | --- |
| 1. Author committed deployment artifacts | Done. `scripts/publish.ps1` (parameters plus a git-ignored `scripts/publish.local.psd1` for host / SSH user / identity file; `dotnet publish` self-contained `linux-x64` → `tar` → `scp` → stop, unpack, chown, start over `ssh`; no host, credential, or key path baked in). `deploy/Caddyfile.template` (placeholder `relay.example.com`, `reverse_proxy 127.0.0.1:5000` — the port verified against Story 2's `appsettings.json`). `deploy/foodus-relay.service.template` (`Type=simple` because Story 2's host has no systemd integration, dedicated `foodus-relay` system user, `WorkingDirectory=/opt/foodus-relay`, `ASPNETCORE_ENVIRONMENT=Production` plus optional `EnvironmentFile=-/etc/foodus-relay/foodus-relay.env` carrying `Kestrel__Endpoints__Http__Url` and `Relay__DatabasePath`, `Restart=on-failure`, baseline sandboxing). `docs/self-hosting.md` (stranger-ready walkthrough of Steps 2–7). `.gitignore` gained the `scripts/publish.local.psd1` rule, and a new `.gitattributes` pins `deploy/**` to LF so a Windows checkout cannot copy CRLF into a systemd unit or Caddyfile. |
| 2. Provision the droplet | **Not run — owner-executed.** |
| 3. Harden the droplet | **Not run — owner-executed.** Documented in `docs/self-hosting.md` §2. |
| 4. Install Caddy | **Not run — owner-executed.** Documented in §3. |
| 5. Point the domain | **Not run — owner-executed.** Documented in §4. |
| 6. Install the unit and first deploy | **Not run — owner-executed.** Documented in §5–6. |
| 7. Smoke-check | **Not run — owner-executed.** The six checks are listed in §7 for the owner to run and record here. |
| 8. Secrets audit | Done, clean. Swept every authored file and the working diff: the only IPv4 literal is the documented `127.0.0.1` loopback; the only host name is the placeholder `relay.example.com`; the only other domains are the official Caddy apt repository (`dl.cloudsmith.io`) and the .NET download link. No credential, key path, or owner username appears anywhere — account names (`foodus-relay`, `deploy`) are mechanism defaults published for any reader. |
| 9. Context maintenance | Done for what this slice settles. `design.md` Repository Structure gained `deploy/` and un-planned `scripts/`; open question 5 (domain) deliberately left **open**. `wiki/secrets.md` item 2 (droplet bootstrap) filled with the settled env/config placement, and the deploy-target row added to "What Lives Where". `milestone-3.md` Story 6 status → In Progress with the owner-pending note; milestone completion count unchanged at 2/6. |

**Harness:** `harness/` currently holds only `README-HARNESS.md` and the module template — no
module matches this story, so no harness gate was run.

**Verification performed**

- Regression build: `dotnet build source/FoodUsRelay.sln` — succeeded, 0 warnings, 0 errors
  (the informational `NETSDK1057` preview-SDK notice remains, as recorded in the Story 2 plan).
- Regression tests: `dotnet test source/FoodUsRelay.sln` — 3 passed, 0 failed.
- PowerShell syntax: `[System.Management.Automation.Language.Parser]::ParseFile` on
  `scripts/publish.ps1` — 0 parse errors. The script was deliberately **not executed**; it
  would attempt to reach a host that does not exist yet.
- Secrets sweep (Step 8) — clean, as above.
- Stranger desk-check: `docs/self-hosting.md` read end to end covers provisioning, hardening
  (firewall 22/80/443, non-root service user, unattended upgrades, optional deploy account with
  narrow sudo), Caddy from the official apt repo, DNS, runtime config placement, unit install,
  first deploy, the non-Windows equivalent commands, and all six smoke checks — with no
  owner-specific value.
- Link check: every relative link added (`../deploy/*.template`, `../context/wiki/secrets.md`,
  `wire-contract-v1.md`, `../../docs/self-hosting.md`) resolves to an existing file.

## Completion Review

**Repository slice: complete. Story: not complete.**

What the repo now carries is the whole public mechanism — a publish script, both server-side
templates, and setup instructions a stranger can follow to stand up their own relay — with
placeholder values throughout. What it does not carry, and by design never will, is a live
endpoint: Steps 2–7 are physical actions on a real server and remain the owner's to execute.

Acceptance criteria status:

- Public mechanism committed and placeholder-valued — **met**.
- Secrets discipline intact, audit clean — **met**.
- `design.md`, `wiki/secrets.md`, and `milestone-3.md` reflect the outcome, with the deployed
  capability level recorded as none — **met**.
- Relay live behind Caddy/HTTPS as an enabled systemd service, deployed via the script — **not
  met, owner-pending**.
- Smoke checks passing and recorded — **not met, owner-pending**.

Two things stay open and must not be read as settled. The domain question (design.md open
question 5) is untouched: no committed artifact depends on it, but DNS, the real Caddyfile, and
the value typed into both phones do. And the droplet plan choice is fixed only in the
documentation's advice, not in a purchase.

The honest risk carried forward is the one the plan already named: the publish script has never
been run end to end. Its syntax parses and its steps are the documented ones, but "publish →
copy → restart" is unproven until it runs against a real host, and the sudoers rule in
`docs/self-hosting.md` §2 is the most likely first thing to need adjusting. Step 6's mandate
stands — the first deploy must be the script, not a hand-copy.

**Next action (owner):** execute Steps 2–7, then record the smoke-check results in this
Execution Log and flip the story to Complete.
