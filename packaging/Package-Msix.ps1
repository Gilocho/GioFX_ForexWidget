# Package-Msix.ps1 — Empaqueta ForexWidget como MSIX firmado para sideload.
#
# Requiere: Windows SDK (makeappx.exe, signtool.exe) — verificado presente en 10.0.22621.
# Uso:
#   .\Package-Msix.ps1                        # empaqueta sin firmar
#   .\Package-Msix.ps1 -Sign                  # empaqueta y firma (crea el cert dev si no existe)
#   .\Package-Msix.ps1 -Sign -Version 1.0.1.0 # versión nueva para publicar
#
# -Version estampa AppxManifest.xml y ForexWidget.appinstaller en una sola operación, así que
# no pueden quedar desincronizados: si el .appinstaller anuncia una versión distinta a la del
# paquete, la detección de actualizaciones se rompe sin ningún error visible.
# Los dos archivos del repo son PLANTILLAS (se quedan en 1.0.0.0); los estampados salen a dist/
# con los nombres EXACTOS que esperan las URLs "latest release" del .appinstaller.
#
# NOTA: el Publisher del manifest y el Subject del certificado deben coincidir.
# Editar $PublisherCN aquí y en AppxManifest.xml / ForexWidget.appinstaller antes de distribuir.

param(
    [string]$Version = "1.0.0.0",
    [switch]$Sign
)

$ErrorActionPreference = "Stop"
$PublisherCN = "CN=GioFX"

# Estampa los atributos Version="x.x.x.x" de un XML (Identity del manifest; raíz y MainPackage
# del .appinstaller). El lookbehind (?<!\w) evita que el patrón toque MinVersion="10.0.17763.0"
# (sin él, el regex la reescribía a $Version al empaquetar). MaxVersionTested no coincide
# ("VersionTested"). Un solo sitio con el regex = las dos plantillas se estampan igual.
function Update-XmlVersion {
    param([string]$Xml, [string]$NewVersion)
    return $Xml -replace '(?<!\w)Version="\d+\.\d+\.\d+\.\d+"', "Version=`"$NewVersion`""
}

$sdkBin = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64"
$makeappx = Join-Path $sdkBin "makeappx.exe"
$signtool = Join-Path $sdkBin "signtool.exe"
if (-not (Test-Path $makeappx)) { throw "makeappx.exe no encontrado en $sdkBin — instalar Windows SDK" }

$root = Split-Path $PSScriptRoot -Parent
$layout = Join-Path $PSScriptRoot "layout"
$dist = Join-Path $PSScriptRoot "dist"
# Nombres EXACTOS: las URLs latest/download del .appinstaller apuntan a estos assets.
$msixPath = Join-Path $dist "ForexWidget.msix"
$appinstallerPath = Join-Path $dist "ForexWidget.appinstaller"

# 1. Publish self-contained (no requiere .NET runtime en la máquina destino)
Write-Host "[1/5] dotnet publish (self-contained win-x64)..." -ForegroundColor Cyan
Remove-Item $layout -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish (Join-Path $root "ForexWidget.App\ForexWidget.App.csproj") `
    -c Release -r win-x64 --self-contained true -o $layout
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló" }

# 2. Manifest + imágenes al layout
Write-Host "[2/5] Copiando manifest e imágenes..." -ForegroundColor Cyan
$manifest = Get-Content (Join-Path $PSScriptRoot "AppxManifest.xml") -Raw
$manifest = Update-XmlVersion -Xml $manifest -NewVersion $Version
Set-Content (Join-Path $layout "AppxManifest.xml") $manifest -Encoding UTF8
Copy-Item (Join-Path $PSScriptRoot "Images") (Join-Path $layout "Images") -Recurse -Force

# 3. makeappx pack
Write-Host "[3/5] makeappx pack..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Remove-Item $msixPath -Force -ErrorAction SilentlyContinue
& $makeappx pack /o /d $layout /p $msixPath
if ($LASTEXITCODE -ne 0) { throw "makeappx falló" }
Write-Host "MSIX generado: $msixPath" -ForegroundColor Green

# 4. Firma (opcional)
if ($Sign) {
    Write-Host "[4/5] Firmando..." -ForegroundColor Cyan
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
    Write-Host "[4/5] Sin firmar (usa -Sign para firmar). Un MSIX sin firma NO se puede instalar." -ForegroundColor Yellow
}

# 5. .appinstaller estampado con la MISMA $Version que el manifest (ver Update-XmlVersion).
Write-Host "[5/5] Generando .appinstaller..." -ForegroundColor Cyan
$appinstaller = Get-Content (Join-Path $PSScriptRoot "ForexWidget.appinstaller") -Raw
$appinstaller = Update-XmlVersion -Xml $appinstaller -NewVersion $Version
Set-Content $appinstallerPath $appinstaller -Encoding UTF8

Write-Host ""
Write-Host "Listo para publicar (versión $Version):" -ForegroundColor Green
Write-Host "  $msixPath"
Write-Host "  $appinstallerPath"
Write-Host ""
Write-Host "Subir AMBOS a la GitHub Release SIN renombrar: las URLs latest/download" -ForegroundColor Yellow
Write-Host "del .appinstaller dependen de esos nombres exactos." -ForegroundColor Yellow
Write-Host "Instalar/actualizar abriendo el .appinstaller, no el .msix directo." -ForegroundColor Yellow

