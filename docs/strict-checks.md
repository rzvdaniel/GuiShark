# Stricter build checks

The first analyzer pass recorded 94 warnings with six C# projects and no permanent tests. This follow-up keeps every complexity/coupling threshold unchanged and adds four checks:

- **Regex timeouts:** S6444 is now an error. All seven parser regex calls use a shared, explicit one-second timeout. Patterns and options are unchanged. Exhausting the timeout propagates `RegexMatchTimeoutException`, instead of permitting indefinite matching; matching remains subject to the existing parser validation.
- **Compiler diagnostics:** shared properties moved to root `Directory.Build.props` so test projects and future projects inherit the same policy. `TreatWarningsAsErrors=true` makes all emitted compiler warnings, including nullable warnings, fatal. A short list of reviewed analyzer warning IDs in `WarningsNotAsErrors` keeps those findings visible as warnings. No enforced error IDs or compiler warning IDs are exempted. New diagnostic types fail by default.
- **Solution coverage:** `eng/Verify-SolutionCoverage.ps1` rejects tracked C# projects missing from the solution or excluded from its Debug/Release Any CPU builds. CI runs it before the ordinary complete builds.
- **Regression tests:** `src/tests/GuiShark.Tests` is the seventh solution project. Its 21 xUnit cases cover history/selection, word navigation/double-click, Unicode graphemes, readonly/maxlength behavior, password masking/reveal/clipboard restrictions, multiline caret scrolling, modal cancellation/focus restoration, borrowed metrics ownership and view disposal, and valid/invalid parsing. The suite uses public SDK APIs and needs no OpenGL context. CI runs it in Debug and Release with the .NET 10 Microsoft Testing Platform runner.

## Validation

Validated locally on Windows x64 with SDK 10.0.401 / MSBuild 18.9.11:

- Fresh complete Debug and Release builds: 0 errors, 87 warnings each.
- Debug and Release regression runs: all 21 passed, none skipped.
- Deliberate production and actual test-project probes failed on CS0168, CS8602, CA1502 and S3776. A production regex probe also failed on S6444. Probes were removed before final validation.
- Deliberate coverage probes rejected both an omitted test project and its missing Release build entry. The real solution covers all seven tracked C# projects.
- MSBuild evaluation confirms the test project inherits the compiler policy, analyzer package and metrics input.
- `git diff --check` passed.

Sonar's pinned [S6444 metadata](https://github.com/SonarSource/sonar-dotnet/blob/10.34.0.3385/analyzers/rspec/cs/S6444.json) defines it as `Main` scope. It does not report a deliberately untimed regex in an xUnit assembly. This is an analyzer scope limitation, not a repository exclusion; tests still receive the shared analyzer reference and error configuration, and their compiler and complexity enforcement was verified. No tests were mislabeled as production to work around that behavior.

The 87 remaining warning occurrences are the initial [warning inventory](analyzer-enforcement.md#other-warnings) minus the seven fixed S6444 findings. No test warnings were introduced. No suppressions or complexity threshold increases were added. Native Linux/macOS runtime execution was not verified locally; the regression suite itself uses portable managed APIs.
