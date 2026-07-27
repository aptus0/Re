# Security Policy

## Supported Versions

Only the latest published release receives security fixes.

## Reporting a Vulnerability

Do not disclose suspected vulnerabilities in a public issue. Contact the
repository owner privately through GitHub and include the affected version,
reproduction steps, impact, and any proposed remediation. Please allow a
reasonable remediation period before public disclosure.

## Release Integrity

Production releases must be Authenticode-signed with the verified ReSoft
publisher identity and RFC 3161 timestamped. Verify both the digital signature
and the SHA-256 checksum before deployment. Never install a release whose
signature is missing, invalid, or names an unexpected publisher.

## Local Data

The portable application stores its SQLite database, signing material generated
for local JWT sessions, logs, and Salesforce WebView profile under the current
Windows user's local application-data directory. Access is limited by the
Windows user profile; managed deployments should additionally apply device
encryption, endpoint protection, backups, and least-privilege access policies.
