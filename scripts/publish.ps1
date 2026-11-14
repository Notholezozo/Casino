<#
.SYNOPSIS
    Produit une version prête à tourner sur la machine : l'API .NET et le front Angular compilé, dans ./publish.
.EXAMPLE
    pwsh scripts/publish.ps1
    dotnet publish/Casino.Api.dll --urls http://0.0.0.0:5080
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root 'publish'

Write-Host '== Front Angular' -ForegroundColor Cyan
npm --prefix (Join-Path $root 'frontend') ci
if ($LASTEXITCODE -ne 0) { throw 'npm ci a échoué.' }
npm --prefix (Join-Path $root 'frontend') run build
if ($LASTEXITCODE -ne 0) { throw 'La compilation du front a échoué.' }

Write-Host '== API .NET' -ForegroundColor Cyan
dotnet publish (Join-Path $root 'backend/src/Casino.Api') -c Release -o $output
if ($LASTEXITCODE -ne 0) { throw 'La publication de l''API a échoué.' }

# Le build Angular place les fichiers à servir dans dist/<projet>/browser.
$index = Get-ChildItem (Join-Path $root 'frontend/dist') -Recurse -Filter index.html | Select-Object -First 1
if (-not $index) { throw 'index.html introuvable dans frontend/dist.' }

$wwwroot = Join-Path $output 'wwwroot'
New-Item -ItemType Directory -Force $wwwroot | Out-Null
Copy-Item (Join-Path $index.DirectoryName '*') $wwwroot -Recurse -Force

Write-Host "`nPrêt dans $output" -ForegroundColor Green
Write-Host 'Lancer :  dotnet publish/Casino.Api.dll --urls http://0.0.0.0:5080'
