param([string] $Solution = 'Gui.Shark.sln')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$solutionPath = Join-Path $repoRoot $Solution
$solutionText = Get-Content -LiteralPath $solutionPath -Raw
$solutionDirectory = Split-Path $solutionPath -Parent
$tracked = @(git -C $repoRoot ls-files -- '*.csproj')
if ($LASTEXITCODE -ne 0) { throw 'Unable to list tracked C# projects.' }
if ($tracked.Count -eq 0) { throw 'No tracked C# projects found.' }

$projects = @{}
foreach ($match in [regex]::Matches($solutionText, 'Project\("[^"\r\n]+"\) = "[^"\r\n]+", "([^"\r\n]+\.csproj)", "(\{[^}]+\})"')) {
    $projectPath = [IO.Path]::GetFullPath((Join-Path $solutionDirectory $match.Groups[1].Value.Replace('\', '/')))
    $relativePath = [IO.Path]::GetRelativePath($repoRoot, $projectPath).Replace('\', '/')
    if ($projects.ContainsKey($relativePath)) { throw "Duplicate solution project: $relativePath" }
    $projects[$relativePath] = $match.Groups[2].Value
}

foreach ($project in $tracked) {
    $project = $project.Replace('\', '/')
    if (!$projects.ContainsKey($project)) { throw "Tracked C# project missing from solution: $project" }
    $guid = [regex]::Escape($projects[$project])
    foreach ($configuration in 'Debug', 'Release') {
        if ($solutionText -notmatch "(?m)^\s*$guid\.$configuration\|Any CPU\.Build\.0\s*=\s*$configuration\|Any CPU\s*$") {
            throw "Project excluded from $configuration build: $project"
        }
    }
}
Write-Host "Solution covers all $($tracked.Count) tracked C# projects in Debug and Release."
