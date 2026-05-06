param(
    [string]$QtPath = "C:\Qt\Qt6.10\6.8.3\msvc2022_64"
)

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BuildDir = Join-Path $ProjectDir "build"

if (-not (Test-Path $QtPath)) {
    throw "Qt path not found: $QtPath"
}

cmake -S $ProjectDir -B $BuildDir -DCMAKE_PREFIX_PATH=$QtPath

$solution = Get-ChildItem $BuildDir -File -Include *.sln,*.slnx |
    Select-Object -First 1

if ($null -eq $solution) {
    throw "No Visual Studio solution was generated under: $BuildDir"
}

Invoke-Item $solution.FullName
