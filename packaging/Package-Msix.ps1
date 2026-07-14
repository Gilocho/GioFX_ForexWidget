# Package-Msix.ps1 (rama store-release) — Empaqueta ForexWidget como MSIX SIN firmar para Microsoft Store.
#
# Requiere: Windows SDK (makeappx.exe) — verificado presente en 10.0.22621.
# Uso:
#   .\Package-Msix.ps1                       # empaqueta sin firmar (unico modo en esta rama)
#   .\Package-Msix.ps1 -Version 1.0.1.0      # version distinta para una nueva sumision a Store
#
# NOTA: esta rama NO firma el paquete. Microsoft firma automaticamente durante la
# certificacion de Store con la identidad real (Publisher CN=A300F14C-...). El flujo
# de sideload con certificado autofirmado (CN=GioFX) vive solo en la rama main.

param(
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = "Stop"

$sdkBin = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64"
$makeappx = Join-Path $sdkBin "makeappx.exe"
if (-not (Test-Path $makeappx)) { throw "makeappx.exe no encontrado en $sdkBin — instalar Windows SDK" }

$root = Split-Path $PSScriptRoot -Parent
$layout = Join-Path $PSScriptRoot "layout"
$dist = Join-Path $PSScriptRoot "dist"
$msixPath = Join-Path $dist "ForexWidget_${Version}_x64.msix"

# 1. Publish self-contained (no requiere .NET runtime en la maquina destino)
Write-Host "[1/3] dotnet publish (self-contained win-x64)..." -ForegroundColor Cyan
Remove-Item $layout -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish (Join-Path $root "ForexWidget.App\ForexWidget.App.csproj") `
    -c Release -r win-x64 --self-contained true -o $layout
if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo" }

# 2. Manifest + imagenes al layout
Write-Host "[2/3] Copiando manifest e imagenes..." -ForegroundColor Cyan
$manifest = Get-Content (Join-Path $PSScriptRoot "AppxManifest.xml") -Raw
$manifest = $manifest -replace 'Version="\d+\.\d+\.\d+\.\d+"', "Version=`"$Version`""
Set-Content (Join-Path $layout "AppxManifest.xml") $manifest -Encoding UTF8
Copy-Item (Join-Path $PSScriptRoot "Images") (Join-Path $layout "Images") -Recurse -Force

# 3. makeappx pack (sin firmar)
Write-Host "[3/3] makeappx pack..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Remove-Item $msixPath -Force -ErrorAction SilentlyContinue
& $makeappx pack /o /d $layout /p $msixPath
if ($LASTEXITCODE -ne 0) { throw "makeappx fallo" }

Write-Host ""
Write-Host "[3/3] Paquete generado para Microsoft Store (SIN firmar):" -ForegroundColor Green
Write-Host $msixPath
Write-Host ""
Write-Host "Este paquete se sube directamente a Partner Center." -ForegroundColor Yellow
Write-Host "Microsoft lo firma automaticamente durante la certificacion." -ForegroundColor Yellow
