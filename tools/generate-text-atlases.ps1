param([Parameter(Mandatory = $true)][string]$Generator)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$fontFolder = Join-Path $taskRoot 'src/demos/GuiShark.Demo/Assets/fonts'
$atlasFolder = Join-Path $taskRoot 'src/demos/GuiShark.TextDemo/Assets/atlas'
New-Item -ItemType Directory -Force $atlasFolder | Out-Null
foreach ($weight in @('Regular', 'Bold')) {
    $font = Join-Path $fontFolder "Lato-$weight.ttf"
    $png = Join-Path $atlasFolder "Lato-$weight.png"
    $json = Join-Path $atlasFolder "Lato-$weight.json"
    & $Generator -font $font -chars '[0x20,0x17f], [0x2010,0x2026]' -type msdf -format png -size 48 -pxrange 4 -yorigin bottom -nokerning -imageout $png -json $json
    if ($LASTEXITCODE -ne 0) { throw "MSDF generation failed for $weight." }
}
