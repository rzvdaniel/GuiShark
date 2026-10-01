# Contributing

Use the .NET 10 SDK selected by `global.json`. Ordinary builds run the SDK analyzers and SonarAnalyzer.CSharp 10.34.0.3385 for every C# project, including future test projects. Add new projects to `Gui.Shark.sln` so local validation and CI cover them.

The root `.editorconfig` makes CA1502 (cyclomatic complexity), CA1506 (referenced-type coupling), S3776 (cognitive complexity), S1764, S2757, S4830, S5445, S4502, S2930 and S2190 build errors. CA1501 remains a warning. `CodeMetricsConfig.txt` sets CA1502 to 15 and CA1506 to 60 referenced types per type / 30 per method; this does not count constructor parameters. S3776 keeps its standard threshold of 15. Fix findings without increasing thresholds, suppressing diagnostics or excluding projects. Other diagnostics remain warnings instead of being promoted globally.

Run the same complete, nonincremental builds as CI from the repository root:

```powershell
dotnet build Gui.Shark.sln -c Debug --no-incremental -v minimal
dotnet build Gui.Shark.sln -c Release --no-incremental -v minimal
git diff --check
```

Exercise the affected demos on a desktop with OpenGL 3.3. There is currently no tracked automated test suite. The recorded baseline, validation and remaining warnings are in [docs/analyzer-baseline.md](docs/analyzer-baseline.md) and [docs/analyzer-enforcement.md](docs/analyzer-enforcement.md).
