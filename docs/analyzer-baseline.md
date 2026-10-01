# Analyzer enforcement baseline

Recorded before configuration changes on 2026-10-01 at main commit `1157ef6`.

- No repository or parent AGENTS.md instructions found for GuiShark. Automaton configuration was inspected read-only.
- global.json requests .NET SDK 10.0.100 with latestFeature roll-forward; installed SDK used: 10.0.401, MSBuild 18.9.11, Windows x64.
- All six tracked C# projects target net10.0: GuiShark, GuiShark.OpenGL, GuiShark.Demo, GuiShark.Balloon, GuiShark.TextDemo, GuiShark.ControlsDemo. Gui.Shark.sln includes every project in Debug and Release.
- No tracked test projects or test suite. No existing CI workflow, EditorConfig, central package management, analyzer package reference, or metrics config.
- src/Directory.Build.props enables nullable and implicit usings and previously promoted all warnings to errors.
- Fresh Debug and Release builds of Gui.Shark.sln passed with zero warnings and zero errors. Commands: `dotnet build Gui.Shark.sln -c Debug --no-incremental -v minimal` and the equivalent Release command. Full local logs are in ignored artifacts/analyzer-baseline-*.log.
- Automaton uses SonarAnalyzer.CSharp 10.34.0.3385 with PrivateAssets=all in shared Directory.Build.targets and CodeMetricsConfig.txt as AdditionalFiles. Its metrics and selected severities match this request.
