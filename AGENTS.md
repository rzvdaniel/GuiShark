# GuiShark agent instructions

Follow [the architecture and extension boundaries](docs/architecture.md). Keep the HTML/CSS rendering core lean; higher-level components, desktop hosting and application-specific details belong in separate projects that embed GuiShark.

For building, compiling, validating, or running projects and demos, read and use the repository-local [guishark-build-run skill](.agents/skills/guishark-build-run/SKILL.md).

Follow [CONTRIBUTING.md](CONTRIBUTING.md) for the current analyzer policy and required checks. The SDK libraries are under `src/GuiShark` and `src/GuiShark.OpenGL`, demos under `src/demos`, and regression tests under `src/tests`.
