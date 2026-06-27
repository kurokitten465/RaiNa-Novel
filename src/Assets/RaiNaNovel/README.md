# RaiNa Novel

> A timeline-based visual novel framework for Unity — built to make players feel like they're watching anime, not reading a script.

---

> ⚠️ **Experimental** — This project is in active early development. Architecture and APIs will change without notice. Not recommended for production use yet.

---

## What is RaiNa Novel?

Most visual novel frameworks think in sequences.

```
show character → play dialogue → hide character → repeat
```

**RaiNa Novel thinks in time.**

Characters exist in a scene. Things happen to them simultaneously. Music swells before the emotional line lands. A reaction fires 0.3 seconds after the previous cue — not on a tap. The result is a scene that breathes, not a script that executes.

RaiNa Novel is a Unity framework that gives scene authors the tools to compose that kind of experience — visually, without writing code.

---

## Core Philosophy

**Cinematic over sequential.**
Every design decision is made in service of one goal — scenes that feel like anime, authored like video editing.

**Timeline as the foundation.**
Commands live on a timeline with real timestamps. Things can overlap. Timing is intentional. The playhead can seek.

**Authors, not programmers.**
A writer or director should be able to build a full branching scene using the editor alone. Code is for extending the system, not for authoring stories.

---

## How It Works

RaiNa Novel is built on three concepts working together:

### Commands
Every action in a scene is a discrete **command** — show a character, play music, display dialogue, trigger a choice. Commands are data. They carry parameters. They have a position in time.

### Timeline
Commands live on a **multi-track timeline**. Tracks can overlap. A character can fade in while music starts while dialogue begins — all authored visually, all executing in sync. Under the hood, the timeline drives a **Unity PlayableGraph** — the same API that powers Unity's own Timeline system.

### Editor Tooling
Three tools compose the authoring environment:

| Tool | Purpose |
|---|---|
| **ScriptEditor** | Primary tool. Add, remove, group, and organize commands. |
| **TimelineEditor** | Sub-tool. Adjust timing and preview the scene visually. |
| **Inspector** | Sub-tool. Configure each command's parameters with a custom UI per command type. |

---

## Key Features

- **Timeline-based sequencing** — author overlapping, time-aware scenes like editing video
- **Command-driven architecture** — every scene action is a composable, inspectable data object
- **PlayableGraph runtime** — built on Unity's Playable API for native seek, scrub, and time control
- **Branching support** — choices branch to independent timeline assets
- **Extensible command system** — add new command types without modifying framework source
- **Per-command custom inspectors** — each command type exposes its own tailored editor UI
- **Partial graph rebuild** — live editing without tearing down the runtime graph
- **Interface-driven services** — swap rendering, audio, input, and save backends freely

---

## Built On

- **Unity** (PlayableGraph / Playable API)
- **ScriptableObjects** for all timeline data
- **Custom EditorWindow** tooling for the authoring environment

---

## Status

| Area | Status |
|---|---|
| Core architecture | 🔬 Experimental |
| Runtime player | 🔬 Experimental |
| Editor tooling | 🔬 Experimental |
| Command system | 🔬 Experimental |
| Documentation | 🚧 In progress |
| Public API | 🚧 Not stable |

This framework is an ongoing experiment. The goal is to validate whether a timeline-first, command-driven approach can genuinely make visual novel scenes feel cinematic inside Unity — and to build toward a tool that a full production team could rely on.

Nothing here is final. Everything is subject to change.

---

## Inspiration

The cinematic feel this framework chases was inspired by **"Stella Sora"** that make characters feel alive on screen — not sprites swapping states on cue, but scenes with weight, timing, and presence.

The production-scale validation of this architecture approach comes from the **"Uguiss"** internal toolset used by QualiArts on *Gakuen iDOLM@STER*, presented at CEDEC 2025 — a real-world example of a command-driven, timeline-based, PlayableGraph-powered scene system shipped at scale.

---

## License

[MIT](LICENSE)

---

*RaiNa Novel is an experimental project. Crack it.*
