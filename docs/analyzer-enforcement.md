# Analyzer enforcement validation

This records the initial enforcement baseline. [Stricter checks](strict-checks.md) subsequently add compiler enforcement, regex timeouts, solution coverage verification and permanent regression tests; the current warning count is 87.

Validated on 2026-10-01 with .NET SDK 10.0.401 / MSBuild 18.9.11, Windows x64. See [the pre-change baseline](analyzer-baseline.md).

## Configuration and coverage

Root `Directory.Build.targets` imports SonarAnalyzer.CSharp **10.34.0.3385**, pinned with `PrivateAssets="all"`, and `CodeMetricsConfig.txt` as `AdditionalFiles`. It explicitly enables `RunAnalyzers`, `RunAnalyzersDuringBuild` and `EnableNETAnalyzers`. MSBuild evaluation confirmed one package reference and one metrics input per project. The package loaded and executed successfully with the selected SDK; no analyzer loading failures occurred. There is no central package management or competing package reference.

`Gui.Shark.sln` already includes every tracked C# project in both configurations:

- `src/GuiShark/GuiShark.csproj`
- `src/GuiShark.OpenGL/GuiShark.OpenGL.csproj`
- `src/demos/GuiShark.Demo/GuiShark.Demo.csproj`
- `src/demos/GuiShark.Balloon/GuiShark.Balloon.csproj`
- `src/demos/GuiShark.TextDemo/GuiShark.TextDemo.csproj`
- `src/demos/GuiShark.ControlsDemo/GuiShark.ControlsDemo.csproj`

There are no tracked test projects. Root configuration also applies to future C# test projects. No solution exclusions were introduced. `.editorconfig` declares the requested error severities and CA1501 warning. The previous blanket `TreatWarningsAsErrors` setting is now false so unrelated analyzer findings remain warnings; configured error diagnostics still fail compilation. Metrics thresholds are exactly 15 / 60 / 30, with S3776's default threshold unchanged.

No CI existed. `.github/workflows/build.yml` adds one Windows job with ordinary fresh Debug and Release solution builds, without a separate analysis service.

## Fixes

Resolved 23 enforced diagnostic occurrences (CA1502 and S3776). CSS property dispatch now uses a property registry with shared value validation. Text control attribute loading has its own responsibility. HTML conversion separates element metadata and select validation. Default styles separate inheritance, overlay defaults and control defaults while retaining their precedence.

Text editing separates clipboard commands, navigation and mutation; input dispatch separates modal/popup scope and activation. Layout separates child placement and text viewport scrolling. OpenGL painting separates content and chrome while preserving paint order, clipping and GL state ownership. Demo launch parsing separates window options from text rendering options; statistics refresh and gallery option validation have cohesive functions. Public contracts and resource ownership remain unchanged. No security check was removed or weakened.

## Validation

- Cleaned and rebuilt the complete solution with `dotnet build Gui.Shark.sln -c Debug --no-incremental -v minimal` and its Release equivalent: **both passed, 0 errors / 94 warnings each**.
- Temporary violations in the production SDK and a temporary `IsTestProject=true` project caused ordinary builds to fail on **CA1502**, **CA1506(Method)**, **S3776**, and **S1764**. These demonstrated the 15, 30 and standard 15 thresholds and shared test coverage. All deliberate violations and temporary project sources were removed.
- Temporary behavioral smoke checks passed for editing, undo/redo, word selection, clipboard handling, password masking/reveal and copy/cut restrictions, multiline layout/caret scrolling, modal focus scope and dropdown navigation. No permanent test suite was added.
- Native OpenGL 3.3 captures passed and were visually inspected for the balloon menu, Text Lab comparison with rendering options, the journal's selection/scrolling and password dialog. Driver: Intel UHD Graphics 630, 31.0.101.2141.
- `git diff --check` passed.

Local logs and captures are retained in ignored `artifacts/`. Linux/macOS runtime behavior and the GitHub-hosted workflow were not executed locally; no cross-platform runtime verification is claimed.

## Other warnings

The following are the 94 unique warning occurrences per complete build, excluding the repeated build-summary lines. None was suppressed or promoted to an error.

| Rule | Count | Finding |
| --- | ---: | --- |
| S927 | 43 | OpenTK override parameter names differ from the base declaration |
| S2681 | 8 | Compact statement formatting obscures conditional boundaries |
| S3358 | 7 | Nested ternary expressions |
| S6444 | 7 | Existing parser regular expressions lack execution timeouts |
| S1244 | 6 | Exact floating-point comparisons |
| S3267 | 6 | Loops could use LINQ |
| S3265 | 3 | FreeType native load flag bitmasks use an enum without `Flags` |
| S8969 | 3 | Redundant null-forgiving operators |
| S1121 | 2 | Assignments inside expressions |
| S127 | 2 | Launch argument parsers advance the loop index when consuming values |
| S6618 | 2 | Interpolated formatting could use `string.Create` |
| S1118 | 1 | Constructor visibility recommendation |
| S1481 | 1 | Unused text backend local |
| S2245 | 1 | Seeded procedural terrain uses `Random`; no credential/security use |
| S4136 | 1 | Renderer overloads are not adjacent |
| S6640 | 1 | Existing unsafe FreeType native interop |

Regex timeouts merit a separate behavior-conscious follow-up for HTML/CSS parsing. The native interop and deterministic terrain warnings need context rather than mechanical replacement.
