---
name: wiki-secrets
description: Secrets discipline for the FoodUs relay - public mechanism, private values. What lives where, and what must never enter this repository.
metadata:
  version: "0.2"
  agentic_rails_source_version: "0.2"
  owner: "Jarryd Adaens"
  repo: "FoodUs-Server"
---

# Secrets and Sensitive Configuration

The relay's rule is **public mechanism, private values** (locked in the
[2026-07-27 tier-0 dictation](../dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md)).
The repository ships everything needed for a stranger to stand up their own relay; nothing in
it identifies or unlocks the owner's instance.

## Baseline Rules

- Never commit secrets to source control. `.gitignore` explicitly blocks real secret files as
  the safety net; the discipline is not to create them inside the repo in the first place.
- **The relay's endpoint address (domain and IP) is itself a secret.** It is never published in
  this repo, its docs, commit messages, or anywhere public. Endpoint auth is the real security;
  obscurity is a free extra layer.
- The repo carries a **config template with blank values**; real values never leave the droplet
  or the owner's machine.

## What Lives Where

| Secret | Location | Notes |
| --- | --- | --- |
| SSH private key | Owner's local machine, outside the repo | Referenced by identity in the publish script; SSH key auth only, no passwords |
| Runtime secrets (database path, signing material, etc.) | On the droplet, as environment variables or an uncommitted config file | Repo ships only the blank-valued template |
| Relay endpoint address (domain / droplet IP) | Owner's head, phones' relay-URL setting, DNS | Typed into each phone by hand; ships nowhere in code or repo |
| Deployment credentials | None committed | The publish script contains steps, never credentials |
| Deploy target (host, SSH user, key path) | `scripts/publish.local.psd1` on the owner's machine, git-ignored | Read by `scripts/publish.ps1`; every value can also be passed as a parameter instead |

## Non-Secrets (by design)

The relay stores no plaintext user data. Its database holds only GUIDs, usernames, public
keys, friend codes, block relationships, and sealed ciphertext envelopes — a breach yields
nothing readable. There are no accounts, so there are no passwords or session tokens to
protect.

## To Document When Implementation Lands

1. The exact config template file name and its blank keys.
2. **Droplet bootstrap — settled (Milestone 3 Story 6).** Runtime configuration reaches the
   service through ASP.NET's standard override chain: environment variables beat
   `appsettings.Production.json` beside the executable, which beats the committed
   `appsettings.json` defaults. Two keys carry real values —
   `Kestrel__Endpoints__Http__Url` (must stay loopback) and `Relay__DatabasePath`. The
   preferred placement is `/etc/foodus-relay/foodus-relay.env`, root-owned and mode 640,
   loaded by the systemd unit's `EnvironmentFile=` and untouched by deploys; the alternative
   is a filled copy of `appsettings.Production.template.json` in the install directory.
   Neither file is ever committed. Full walkthrough: [docs/self-hosting.md](../../docs/self-hosting.md).
   *(Remaining once the droplet exists: nothing repo-side — the values themselves stay on the
   host.)*
3. Rotation story for any server-side signing material, once endpoint auth is designed
   (Milestone 3, wire-contract story).
