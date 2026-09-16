<#
    Builds the plugin and stages the Thunderstore package.

    ServerSync is merged into the plugin by the MergeServerSync target in BetterVeins.csproj,
    so this script no longer runs ILMerge itself. It builds, copies the merged DLL into
    Thunderstore\ and repacks Thunderstore.zip.

    Usage:  .\Package.ps1              # Release (what you upload)
            .\Package.ps1 -Configuration Debug
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root  = $PSScriptRoot
$name  = "BetterVeins"
$stage = Join-Path $root "Thunderstore"

Write-Host "Building $name ($Configuration)..."
& dotnet msbuild (Join-Path $root "$name.csproj") -t:Rebuild -p:Configuration=$Configuration -v:m -nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$dll = Join-Path $root "bin\$Configuration\$name.dll"
if (!(Test-Path $dll)) { throw "Build output not found: $dll" }
Copy-Item $dll $stage -Force
Write-Host "Staged $name.dll -> Thunderstore\"

$files = @("$name.dll", "manifest.json", "icon.png", "README.md", "CHANGELOG.md") |
    ForEach-Object { Join-Path $stage $_ }
foreach ($f in $files) { if (!(Test-Path $f)) { throw "Missing package file: $f" } }

$zip = Join-Path $stage "Thunderstore.zip"

# Windows Defender briefly locks a freshly written DLL while it scans it. Compress-Archive
# reports that as a non-terminating error and still leaves a partial zip behind, so build the
# archive, verify every file landed in it, and retry a few times if one was locked out.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$wanted = @($files | ForEach-Object { Split-Path $_ -Leaf })

for ($attempt = 1; ; $attempt++) {
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $files -DestinationPath $zip -ErrorAction SilentlyContinue

    $entries = @()
    if (Test-Path $zip) {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
        $entries = @($archive.Entries | ForEach-Object { $_.Name })
        $archive.Dispose()
    }

    $missing = @($wanted | Where-Object { $entries -notcontains $_ })
    if ($missing.Count -eq 0) { break }
    if ($attempt -ge 5) { throw "Thunderstore.zip is incomplete after $attempt attempts, missing: $($missing -join ', ')" }
    Write-Host "  locked ($($missing -join ', ')), retrying in 2s..."
    Start-Sleep -Seconds 2
}

Write-Host "Wrote $zip ($($entries.Count) files)"
