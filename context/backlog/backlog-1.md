---
name: backlog-1
description: Front backlog file (story inventory / Milestone -1) for the FoodUs relay - initialization record plus items deliberately deferred by the 2026-07-27 dictation.
metadata:
  version: "3.0"
  agentic_rails_source_version: "3.0"
  owner: "Jarryd Adaens"
  repo: "FoodUs-Server"
---
# Backlog 1

> Story inventory / Milestone -1. The front (highest-priority) backlog file. Maximum **30 stories**; overflow spawns `backlog-2.md`.

---

## Story Index

| # | Story | Type | Priority | Complexity | Effort | Risk | Milestone | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | [Initialize project context](#story-1) | Docs | High | — | — | — | *unscheduled* | Complete |
| 2 | [N-person group server support](#story-2) | Feature | Low | — | — | — | *unscheduled* | Backlog |
| 3 | [Docker-based deployment](#story-3) | Refactor | Low | — | — | — | *unscheduled* | Backlog |

---

<a id="story-1"></a>

### Story: Initialize project context

**Type:** Docs

**Summary:** Stand up this repository's agentic-rails context from the
[2026-07-27 tier-0 design seed](../dictations-tier-0/2026-07-27_foodus-relay_tier-0-design.md):
preserve the dictation, synthesize `design.md` with its Milestones Index, create the
reserved Milestone 1–2 placeholders and the Milestone 3 story breakdown, and sweep starter
residue from maintained truth.

**Why / value:** Future agents build from the repository, not from memory or chat.

**Rough scope:** `README.md`, frontmatter of root and context docs, `design.md`,
`milestones/milestone-{1,2,3}.md`, this backlog, `wiki/secrets.md`, the preserved dictation.

**Scores (filled at planning):**

- Complexity: —
- Effort: —
- Risk: —

**Reclassification check:** Not applicable — completed at initialization, 2026-07-27.

---

<a id="story-2"></a>

### Story: N-person group server support

**Type:** Feature

**Summary:** Whatever server-side capability groups larger than two members would require
beyond the Milestone 3 surface. Explicitly deferred by the dictation alongside the app-side
N-person-group backlog items; the app's two-member group cap means Milestone 3 needs nothing
here.

**Why / value:** Only becomes relevant if the app ever lifts the two-member cap. Carries a
known conscious privacy decision (server exposure of third-party friendship data) that must be
made before any work starts.

**Rough scope:** Unknown until the app-side design exists; likely group membership state and
fan-out semantics.

**Scores (filled at planning):**

- Complexity: —
- Effort: —
- Risk: —

**Reclassification check:** If scoring reveals this is epic-sized, promote it to a
[Milestone](../milestones/).

---

<a id="story-3"></a>

### Story: Docker-based deployment

**Type:** Refactor

**Summary:** Replace the plain-systemd deployment with a containerised one. The dictation
chose systemd deliberately (fewer moving parts; a single small process doesn't need
containerisation) and noted Docker "may be adopted later if it earns its place".

**Why / value:** Speculative; only worth doing if the deployment story grows enough moving
parts that containerisation pays for itself.

**Rough scope:** Dockerfile, publish-script changes, droplet runtime changes.

**Scores (filled at planning):**

- Complexity: —
- Effort: —
- Risk: —

**Reclassification check:** If scoring reveals this is epic-sized, promote it to a
[Milestone](../milestones/).
