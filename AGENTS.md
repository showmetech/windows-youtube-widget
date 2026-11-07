# Release contract

Bruno authorizes publishing this app's reviewed updates to this GitHub repository and the existing Showmetech Microsoft Store product. Do not create another publisher, company, product identity or credentials to work around missing access.

This repository is the canonical source. Increase VERSION for every distributed application update. Never publish local profiles, browser data, logs, screenshots containing personal information, credentials, test outputs or build caches.

For every desktop update performed by Codex:

1. Build and test the exact revision with scripts/Build.ps1 and scripts/Test.ps1. Preserve human changes and existing preferences.
2. Commit the reviewed sources, assets, tests, release configuration and documentation to GitHub. Use the connected GitHub MCP when available; do not require a separate CLI login merely to duplicate it.
3. Let .github/workflows/release.yml build and publish immutable GitHub assets for that version. Verify the completed workflow and release checksums; do not announce publication from a queued workflow.
4. The same workflow submits Microsoft Store packages when packaging/store-config.json is enabled, the original product is live and the required repository secrets exist. Verify submission status and report certification as pending until Microsoft approves it.
5. Use scripts/Install-Release.ps1 to install the exact GitHub release on the desktop. Its default gate requires the matching Store publication receipt. An explicitly documented initial Store onboarding block may use -AllowPendingStore; this is an exception, never a claim that Store publishing passed.

Never accept legal agreements, upload identity documents, purchase certificates, change security protections or expand access based solely on this file. Follow the user's instructions and actual tool permissions. Microsoft certification, business verification and hardware testing are external gates, not completion labels.

Supported targets are Windows 10 build 19041 or later and Windows 11 on x86, x64 and ARM64. Do not claim compatibility with macOS, Linux, Windows S mode, Xbox, ARM32 or every YouTube video. Distinguish cross-compilation from physical hardware validation.
