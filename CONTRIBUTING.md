# Contributing

Use the .NET 10 SDK selected by `global.json`. Ordinary builds run the SDK analyzers and SonarAnalyzer.CSharp 10.34.0.3385 for every C# project, including future test projects. Add new projects to `Gui.Shark.sln` so local validation and CI cover them.

The root `.editorconfig` makes CA1502 (cyclomatic complexity), CA1506 (referenced-type coupling), S3776 (cognitive complexity), S1764, S2757, S4830, S5445, S4502, S2930, S2190 and S6444 (regex timeouts) build errors. Every parser regex has an explicit one-second timeout. CA1501 remains a warning. `CodeMetricsConfig.txt` sets CA1502 to 15 and CA1506 to 60 referenced types per type / 30 per method; this does not count constructor parameters. S3776 keeps its standard threshold of 15. Fix findings without increasing thresholds, suppressing diagnostics or excluding projects.

Root `Directory.Build.props` makes compiler warnings, including nullable diagnostics, errors. Its short `WarningsNotAsErrors` list preserves the reviewed analyzer warnings documented below; it does not hide diagnostics. Never add enforced error IDs or compiler IDs to that list. New diagnostic types fail by default and require a fix or an explicit policy review.

Run the same complete, nonincremental builds as CI from the repository root:

```powershell
pwsh -File eng/Verify-SolutionCoverage.ps1
dotnet build Gui.Shark.sln -c Debug --no-incremental -v minimal
dotnet build Gui.Shark.sln -c Release --no-incremental -v minimal
dotnet test --solution Gui.Shark.sln -c Debug --no-build --no-restore
dotnet test --solution Gui.Shark.sln -c Release --no-build --no-restore
git diff --check
```

The coverage check compares tracked C# projects with solution entries and Debug/Release build inclusion. Stage new project files before running it. Regression tests in `src/tests/GuiShark.Tests` use xUnit and the .NET 10 Microsoft Testing Platform runner selected by `global.json`; they need no window or GL context. Shaping regression cases load the Skia/HarfBuzz native libraries restored with the test project. See [xUnit's runner documentation](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform).

Exercise graphics changes in the affected demos on a desktop with OpenGL 3.3. The recorded baseline, initial enforcement and stricter validation are in [docs/analyzer-baseline.md](docs/analyzer-baseline.md), [docs/analyzer-enforcement.md](docs/analyzer-enforcement.md) and [docs/strict-checks.md](docs/strict-checks.md).
