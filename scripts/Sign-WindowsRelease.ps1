$ErrorActionPreference = "Stop"

$pfxPath = $env:RE_CODESIGN_PFX
$pfxPassword = $env:RE_CODESIGN_PASSWORD
if ([string]::IsNullOrWhiteSpace($pfxPath) -or -not (Test-Path -LiteralPath $pfxPath)) {
    throw "Set RE_CODESIGN_PFX to the trusted ReSoft code-signing PFX path."
}
if ([string]::IsNullOrWhiteSpace($pfxPassword)) {
    throw "Set RE_CODESIGN_PASSWORD without committing it to source control."
}

$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" `
    -Filter signtool.exe -Recurse -ErrorAction Stop |
    Where-Object FullName -Match "\\x64\\signtool\.exe$" |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $signtool) {
    throw "signtool.exe was not found. Install the Windows SDK signing tools."
}

$root = Split-Path -Parent $PSScriptRoot
$publishRoot = Join-Path $root "artifacts\publish-v1.0.1\Re"
$installer = Join-Path $root "artifacts\installer\Re-Setup-Windows-x64.exe"
$targets = @(
    (Join-Path $publishRoot "Re.exe"),
    (Join-Path $publishRoot "Api\Re.Api.exe"),
    $installer
)

foreach ($target in $targets) {
    if (-not (Test-Path -LiteralPath $target)) {
        throw "Signing target not found: $target"
    }
    & $signtool sign /fd SHA256 /f $pfxPath /p $pfxPassword `
        /tr "http://timestamp.digicert.com" /td SHA256 /d "ReSoft Re" $target
    if ($LASTEXITCODE -ne 0) { throw "Signing failed: $target" }
    & $signtool verify /pa /all /v $target
    if ($LASTEXITCODE -ne 0) { throw "Signature verification failed: $target" }
}
