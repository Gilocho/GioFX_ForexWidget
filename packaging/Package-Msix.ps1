# Package-Msix.ps1 — Empaqueta ForexWidget como MSIX firmado para sideload.
#
# Requiere: Windows SDK (makeappx.exe, signtool.exe) — verificado presente en 10.0.22621.
# Uso:
#   .\Package-Msix.ps1                       # empaqueta sin firmar
#   .\Package-Msix.ps1 -Sign                 # empaqueta y firma (crea el cert dev si no existe)
#   .\Package-Msix.ps1 -Version 1.0.1.0      # versión distinta (actualizar también el .appinstaller)
#
# NOTA: el Publisher del manifest y el Subject del certificado deben coincidir.
# Editar $PublisherCN aquí y en AppxManifest.xml / ForexWidget.appinstaller antes de distribuir.

param(
    [string]$Version = "1.0.0.0",
    [switch]$Sign
)

$ErrorActionPreference = "Stop"
$PublisherCN = "CN=GioFX"

$sdkBin = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64"
$makeappx = Join-Path $sdkBin "makeappx.exe"
$signtool = Join-Path $sdkBin "signtool.exe"
if (-not (Test-Path $makeappx)) { throw "makeappx.exe no encontrado en $sdkBin — instalar Windows SDK" }

$root = Split-Path $PSScriptRoot -Parent
$layout = Join-Path $PSScriptRoot "layout"
$dist = Join-Path $PSScriptRoot "dist"
$msixPath = Join-Path $dist "ForexWidget_${Version}_x64.msix"

# 1. Publish self-contained (no requiere .NET runtime en la máquina destino)
Write-Host "[1/4] dotnet publish (self-contained win-x64)..." -ForegroundColor Cyan
Remove-Item $layout -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish (Join-Path $root "ForexWidget.App\ForexWidget.App.csproj") `
    -c Release -r win-x64 --self-contained true -o $layout
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló" }

# 2. Manifest + imágenes al layout
Write-Host "[2/4] Copiando manifest e imágenes..." -ForegroundColor Cyan
$manifest = Get-Content (Join-Path $PSScriptRoot "AppxManifest.xml") -Raw
# Solo la Version de <Identity>. El lookbehind (?<!\w) evita que el patron toque
# MinVersion="10.0.17763.0" (sin el, el regex la reescribia a 1.0.0.0 al empaquetar).
# MaxVersionTested no coincide ("VersionTested").
$manifest = $manifest -replace '(?<!\w)Version="\d+\.\d+\.\d+\.\d+"', "Version=`"$Version`""
Set-Content (Join-Path $layout "AppxManifest.xml") $manifest -Encoding UTF8
Copy-Item (Join-Path $PSScriptRoot "Images") (Join-Path $layout "Images") -Recurse -Force

# 3. makeappx pack
Write-Host "[3/4] makeappx pack..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Remove-Item $msixPath -Force -ErrorAction SilentlyContinue
& $makeappx pack /o /d $layout /p $msixPath
if ($LASTEXITCODE -ne 0) { throw "makeappx falló" }
Write-Host "MSIX generado: $msixPath" -ForegroundColor Green

# 4. Firma (opcional)
if ($Sign) {
    Write-Host "[4/4] Firmando..." -ForegroundColor Cyan
    $cert = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object { $_.Subject -eq $PublisherCN -and $_.FriendlyName -eq "ForexWidget Dev Cert" } |
        Select-Object -First 1
    if ($null -eq $cert) {
        Write-Host "Creando certificado autofirmado de desarrollo ($PublisherCN)..." -ForegroundColor Yellow
        $cert = New-SelfSignedCertificate -Type Custom -Subject $PublisherCN `
            -KeyUsage DigitalSignature -FriendlyName "ForexWidget Dev Cert" `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    }
    & $signtool sign /fd SHA256 /sha1 $cert.Thumbprint $msixPath
    if ($LASTEXITCODE -ne 0) { throw "signtool falló" }
    Write-Host "Firmado con $($cert.Subject) ($($cert.Thumbprint))" -ForegroundColor Green
    Write-Host @"

RECORDATORIO: un cert autofirmado muestra 'editor no reconocido' al instalar.
Para probar en otra máquina, exportar el .cer e instalarlo en
'Trusted People' (LocalMachine) de esa máquina primero:
  Export-Certificate -Cert Cert:\CurrentUser\My\$($cert.Thumbprint) -FilePath ForexWidget.cer
"@ -ForegroundColor Yellow
}
else {
    Write-Host "[4/4] Sin firmar (usa -Sign para firmar). Un MSIX sin firma NO se puede instalar." -ForegroundColor Yellow
}

