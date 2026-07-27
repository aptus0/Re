# Windows Release Signing

Re production releases must use a publicly trusted Authenticode identity. A
self-signed certificate is suitable only for managed internal test devices and
must not be presented as a trusted public release.

## Recommended production options

1. Microsoft Store distribution: Microsoft signs the submitted package and
   provides the most consistent SmartScreen experience.
2. Microsoft Artifact Signing: recommended for automated non-Store releases,
   subject to Microsoft identity verification and regional eligibility.
3. An OV/EV code-signing certificate from a public certificate authority.

The verified certificate subject must contain the legal publisher identity that
users should see in Windows, for example `ReSoft`. A logo is embedded in both
`Re.exe` and the installer through `Re_ERP_Logo.ico`. The publisher name shown by
Windows comes from the certificate, not from the icon or project metadata.

## PFX signing

Install the Windows SDK so `signtool.exe` is available, then set:

```powershell
$env:RE_CODESIGN_PFX = "C:\secure\ReSoft-CodeSigning.pfx"
$env:RE_CODESIGN_PASSWORD = "<secret>"
.\scripts\Sign-WindowsRelease.ps1
```

The script signs the desktop executable, bundled API, and final installer with
SHA-256 and an RFC 3161 timestamp, then verifies every signature.

Never commit the certificate, private key, password, Azure credentials, or
temporary signing output to Git. Prefer a hardware-backed key, Azure Key Vault,
or Microsoft Artifact Signing for production automation.

## SmartScreen expectation

A valid signature displays the verified publisher and preserves publisher
reputation across releases. It does not guarantee that a brand-new publisher
will never receive a SmartScreen warning. Microsoft Store distribution is the
reliable route for eliminating first-download SmartScreen reputation prompts.
