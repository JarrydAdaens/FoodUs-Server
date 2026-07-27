---
name: readme
description: FoodUs Relay - a deliberately dumb, self-hosted, store-and-forward server that ferries sealed end-to-end-encrypted envelopes between FoodUs household phones.
metadata:
  version: "3.1"
  agentic_rails_source_version: "3.1"
  owner: "Jarryd Adaens"
  repo: "FoodUs-Server"
---
# FoodUs Relay

The server side of FoodUs: a deliberately dumb, self-hosted, store-and-forward relay ("the
post office") that ferries sealed end-to-end-encrypted envelopes between household phones
running the [FoodUs](https://github.com/maksimowiczm/FoodYou) Android app fork.

## Overview

The FoodUs app is an island: each phone's local database is the sole source of truth. This
relay is the single controlled break in that island. It holds only GUIDs, usernames, public
keys, friend codes, block relationships, and per-GUID queues of sealed ciphertext envelopes.
It never sees plaintext diary data — a breach yields ciphertext, usernames, and GUIDs, nothing
else.

Planned stack: ASP.NET (C#) minimal APIs, SQLite storage, Caddy in front for automatic HTTPS,
deployed as a plain systemd service on a small Ubuntu VPS.

**Status:** context initialized; no server code yet. Work begins at
[Milestone 3](context/milestones/milestone-3.md) (milestone numbering is project-wide, shared
with the FoodUs app repository — Milestones 1 and 2 have no server-side scope).

This repository ships everything needed for someone to stand up *their own* relay: source,
config template, and setup instructions. The owner's own relay endpoint address is private and
is never published here.

## Getting Started

Build, deploy, and configuration instructions will land alongside the first server code
(Milestone 3). The wire contract specification — the single source of truth the app conforms
to — will be a maintained document in this repository, created by Milestone 3's first story.

## Project Context

This repository was created from `agentic_rails_context_starter`, the context-template side of
the Agentic Rails system.

This README is the single entry point for the repository. Use the links below as a convenient
nexus into the broader context layers — not every file, just the doorways into each part of the
system.

### Start here

- [AGENTS.md](AGENTS.md) - mandatory agent startup workflow and working rules
- [AGENTIC_RAILS_README.MD](AGENTIC_RAILS_README.MD) - durable overview of the Agentic Rails system
- [context/laws.md](context/laws.md) - constitutional code-quality and security laws (loaded first)
- [context/agenticworkflow.md](context/agenticworkflow.md) - the context tier system workflow standard
- [context/design.md](context/design.md) - Design specification and Milestones Index

### Context layers

- [context/dictations-tier-0/](context/dictations-tier-0/) - Dictation, raw and unstructured
- [context/design.md](context/design.md) - Design (includes the Milestones Index)
- [context/milestones/](context/milestones/) - Milestone documents, each directly containing its story list
- [context/backlog/](context/backlog/) - Backlog / story inventory / Milestone -1
- [context/implementation-plans/](context/implementation-plans/) - Implementation Plans and optional Phases
- [context/wiki/home.md](context/wiki/home.md) - operational reference notes and cheat sheets

### Harness

- [harness/README-HARNESS.md](harness/README-HARNESS.md) - the project's agentic machinery: verifiers, gates, guardrail seams, sensors, and actuators, one self-contained module per artifact

## Repository Layout

```text
FoodUs-Server/
|-- AGENTS.md
|-- AGENTIC_RAILS_README.MD
|-- CLAUDE.md
|-- README.md
|-- context/
|   |-- laws.md
|   |-- agenticworkflow.md
|   |-- design.md
|   |-- agent-thinking.md
|   |-- dictations-tier-0/
|   |-- milestones/
|   |-- backlog/
|   |-- implementation-plans/
|   `-- wiki/
`-- harness/
    `-- README-HARNESS.md
```

Server source, tests, and the deployment script will be added when Milestone 3 implementation
begins.

## Notes

- The peer repository is the FoodUs Android app (fork of maksimowiczm/FoodYou). This repo owns
  the wire contract; the app conforms to it. See
  [context/design.md](context/design.md) for the two-repository working model.
- Keep `AGENTIC_RAILS_README.MD` so the framework context is not lost as this README evolves.
