# Multilingual input and Skia simplification validation

Validated on 2026-10-01 with .NET SDK 10.0.401, Windows x64 and Intel UHD Graphics 630 (OpenGL 3.3, driver 31.0.101.2141).

- Solution coverage: all seven tracked C# projects build in Debug and Release.
- Fresh nonincremental Debug and Release solution builds: passed, zero errors, 64 analyzer warnings each (previous baseline 87). No analyzer settings, thresholds or warning policy changed.
- Debug and Release tests: 36 passed in each configuration; no failures or skips. Fifteen new composition cases cover preedit/commit/undo, cancellation, focus and pointer changes, read-only/disabled fields, wrapped underlines, modal Escape, password masking and UTF-16 offset validation.
- `git diff --check`: passed.
- Native Release OpenGL captures: multilingual gallery with underlined preedit, journal selection/scrolling, password dialog, Lantern Valley menu, Arabic shaping comparison and CJK comparison at 1.5x simulated density with shadows. Captures were visually inspected. Source-folder gallery startup was also checked before the final host refinements.
- Framework-dependent Controls Gallery publishes for Linux x64, macOS x64 and macOS ARM64: passed. Each contains the expected Skia, HarfBuzz and SDL native libraries. Fresh publish directories contain no retired FreeTypeSharp or MSDF runtime artifacts.

## Remaining warnings

| Diagnostic | Count per build |
| --- | ---: |
| S1118 | 1 |
| S1121 | 2 |
| S1244 | 4 |
| S127 | 2 |
| S2245 | 1 |
| S2681 | 8 |
| S3267 | 6 |
| S3358 | 6 |
| S4136 | 1 |
| S6618 | 2 |
| S6640 | 3 |
| S8969 | 3 |
| S927 | 25 |

S6640 now concerns the three demo SDL binding adapters. Their unsafe pointers are required by the native API; extended editing and clipboard allocations are freed by their owner. Other remaining warnings fall within the repository's existing reviewed warning policy. Full logs and native captures are in the ignored `artifacts` directory.

The permission profile briefly switched to a network-restricted sandbox during validation and blocked NuGet restore with NU1301. Those attempts were not counted as successful builds; validation was rerun with full access restored.

## Limits requiring manual verification

Linux/macOS package contents were inspected, but their native applications were not executed. Real OS IME composition, candidate-window placement and physical HiDPI behavior were not automated. The capture flag exercises the portable SDK composition API, not an installed input method. Full bidirectional layout, RTL editing, shaped-cluster caret mapping, font fallback and color emoji remain unimplemented; see [the host contract and limits](multilingual-input.md).

## Windows interaction follow-up

Native gallery mouse checks confirmed preview, conversion, commit, cancellation and cancellation on focus change. The user confirmed physical Ctrl+Z and Tab behavior. See [Windows IME checks](windows-ime-checks.md) for the evidence and remaining native IME checklist. The event log now separates records with visible slashes instead of unsupported newline glyphs.

