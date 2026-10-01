<#
.SYNOPSIS
    Sets the version in src/Jev.Sdk/Jev.Sdk.csproj, packs the project,
    and pushes Jev.Sdk to NuGet.

.DESCRIPTION
    PowerShell version of publish-nuget.sh, for Windows without bash.
    The API key is read from NUGET_API_KEY, or from the .env file next to
    this script if the variable is not set.

.EXAMPLE
    .\publish-nuget.ps1 1.1.0 -DryRun

.EXAMPLE
    $env:NUGET_API_KEY = "your-key"
    .\publish-nuget.ps1 1.1.0
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version,

    # Set the version and pack, but do not push
    [switch]$DryRun,

    # NuGet feed to push to (default: nuget.org)
    [string]$Source = "https://api.nuget.org/v3/index.json"
)

$ErrorActionPreference = "Stop"

$Root   = $PSScriptRoot
$Csproj = Join-Path $Root "src/Jev.Sdk/Jev.Sdk.csproj"
$Out    = Join-Path $Root "artifacts"

function Die([string]$Message) {
    Write-Host "error: $Message" -ForegroundColor Red
    exit 1
}

if ($Version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$') {
    Die "'$Version' is not a version like 1.0.0 or 1.0.0-beta.1"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Die "dotnet is not on PATH"
}

# Load NUGET_API_KEY from .env if it is not already set in the environment.
$EnvFile = Join-Path $Root ".env"
if (-not $env:NUGET_API_KEY -and (Test-Path $EnvFile)) {
    foreach ($line in Get-Content $EnvFile) {
        if ($line -match '^\s*NUGET_API_KEY\s*=\s*(.*?)\s*$') {
            $env:NUGET_API_KEY = $Matches[1].Trim('"', "'")
            Write-Host "Using NUGET_API_KEY from .env"
        }
    }
}

if (-not $DryRun -and -not $env:NUGET_API_KEY) {
    Die "NUGET_API_KEY is not set. Add it to .env or set `$env:NUGET_API_KEY"
}

$text = [System.IO.File]::ReadAllText($Csproj)
$match = [regex]::Match($text, '<Version>(.*?)</Version>')
if (-not $match.Success) {
    Die "no <Version> in $Csproj"
}
$Current = $match.Groups[1].Value

$dirty = git -C $Root status --porcelain
if ($dirty) {
    $head = git -C $Root rev-parse --short HEAD
    Write-Host "warning: the working tree has uncommitted changes; the package will point at commit $head" -ForegroundColor Yellow
}

Write-Host "Version $Current -> $Version"
$text = $text.Replace("<Version>$Current</Version>", "<Version>$Version</Version>")
[System.IO.File]::WriteAllText($Csproj, $text, (New-Object System.Text.UTF8Encoding $true))

Write-Host "Packing"
if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
dotnet pack $Csproj -c Release -o $Out --nologo -v quiet -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { Die "dotnet pack failed" }

$Pkg = Get-ChildItem -Path $Out -Filter *.nupkg | Select-Object -First 1
if (-not $Pkg) { Die "no .nupkg found in $Out" }
Write-Host "  $($Pkg.Name)"

if ($DryRun) {
    Write-Host "Dry run: not pushing. Package is in $Out"
    exit 0
}

Write-Host "Pushing to $Source"
dotnet nuget push $Pkg.FullName --api-key $env:NUGET_API_KEY --source $Source --skip-duplicate
if ($LASTEXITCODE -ne 0) { Die "dotnet nuget push failed" }

Write-Host "Published $Version. Commit Jev.Sdk.csproj and tag the release:"
Write-Host "  git commit -am 'Release $Version'; git tag v$Version"
