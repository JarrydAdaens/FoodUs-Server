---
name: readme
description: Minimal project README template for repositories created from the Agentic Rails context starter.
metadata:
  version: "3.1"
  agentic_rails_source_version: "3.1"
  owner: "Your Name"
  repo: "your-repo"
---
# Project Name

Short description of the project.

## Overview

Replace this section with the project-specific purpose, audience, and current status.

## Getting Started

Add setup, build, test, and run instructions once the project stack is known.

## Project Context

This repository was created from `agentic_rails_context_starter`, the context-template side of the Agentic Rails system.

This README is the single entry point for the repository. Use the links below as a convenient nexus into the broader context layers — not every file, just the doorways into each part of the system.

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
project-root/
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
    |-- README-HARNESS.md
    `-- <module-name>/          (one folder per verifier, gate, guardrail seam, sensor, or actuator)
```

## Notes

- Replace this README with project-specific information as the project matures.
- Keep `AGENTIC_RAILS_README.MD` in derived projects so the framework context is not lost when this README changes.
