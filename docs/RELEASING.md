# Traceable releases

VERSION is the application version authority; the extension keeps its own manifest version. An unchanged extension does not need reloading for a desktop packaging update.

Use PowerShell 7, .NET 10 SDK, Node.js, Inno Setup 6 and Windows SDK:

```powershell
./scripts/Build.ps1 -Architecture x64
./scripts/Test.ps1 -Architecture x64 -Ui
./scripts/Package.ps1 -Architecture x64
```

Repeat for x86 and arm64. An ARM64 compilation is not an ARM64 hardware test. UI tests need an interactive desktop and network access to the public YouTube test video. CI runs source/extension/native-parser checks; physical hardware and actual playback have separate evidence.

For playback changes, run scripts/Test-Playback.ps1 with a fresh isolated profile. Use -Video VIDEO_ID for the affected video and -DropFirstPlay to simulate one ignored iframe play call. The latter is fault injection; report it separately from ordinary real playback and physical mouse input. The test must start unmuted, observe time advancing, verify pause/resume, and verify pending playback cannot restart after hiding.

Build.ps1 verifies the pinned official WebView2 CAB SHA-256 and Microsoft's executable signature, and includes its complete contents and license notices. Update packaging/webview2-runtime.json during reviewed runtime updates. Redistribution authorization was given on 2026-11-07. Do not silently download an unpinned latest binary.

Package.ps1 creates an EXE installer, portable ZIP and schema-validated MSIX per architecture. Without assigned Partner Center identity, the MSIX is explicitly an unsigned development artifact, not Store-ready or a regular sideload installer.

Unpackaged installations register native-host.json beside the executable. MSIX uses its declared shared AppData/NativeMessaging directory because the package directory is read-only. Validate the real Chrome handoff after installation; valid JSON and registry readback alone do not prove browser discovery.

## Every desktop update performed by Codex

1. Bump VERSION and update release notes. Review public files; preserve human changes. Do not publish workspace output trees or local profiles.
2. Commit/push through the connected GitHub MCP or an already authorized Git client. Export-GitHubTree.ps1 emits allowlisted files for MCP blob/tree/commit operations. Preserve remote files and use an expected-head lease.
3. Wait for the GitHub release workflow. It builds three architectures and publishes vVERSION. Existing tags require a version bump, never replacement of binary assets.
4. Verify release-source.json, SHA256SUMS.txt and the completed workflow. A queued run/commit is not publication proof.
5. Check store-submission.json. blocked-onboarding/failed stop normal desktop promotion. submitted means Microsoft received the package and certification remains pending; published requires a separate live-version check.
6. Install that exact GitHub version with Install-Release.ps1 -Version X.Y.Z. It selects the OS architecture and verifies checksums. Initial onboarding can explicitly use -AllowPendingStore while the original company is in review; report that exception, never label it Store publication.
7. Verify the installed version, fresh runtime-health.json, tray, one GUI process, player, preferences and native-host registration. Invoking --startup is distinct from a physical reboot/logon test.

Do not run a watcher that publishes arbitrary edits or unfinished desktop builds. The reviewed release is the coupling point: source commit, GitHub assets and actual Store receipt must match each desktop update.

Supported scope: Windows desktop only. Windows S mode, ARM32, Xbox, macOS and Linux are not supported.
