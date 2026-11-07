YouTube Widget 1.7.1

- Handles GitHub checksum downloads correctly when PowerShell returns binary response content. Installer hashes are always verified before execution.

- Self-contained x64, x86 and ARM64 distributions, with the official Microsoft WebView2 runtime included.
- Matching application/tray/desktop icons and startup in the notification area.
- Refreshes Chrome/Edge native-host registration on first launch and after updates.
- MSIX packaging uses Windows startup tasks and declares the native-host interoperability it requires.
- Repeatable build, test, GitHub release and gated Microsoft Store submission workflow.

Microsoft Store publication is blocked while the existing Showmetech company account is under Microsoft business verification. store-submission.json reports the actual state; a GitHub release does not imply Microsoft certification.

Chrome recommendations/handoff require the companion extension and the user's own signed-in YouTube session. Private videos, age/region restrictions, videos that disallow embedding and Windows S mode are not supported by this embedded player.

The GitHub EXE installer is not Authenticode-signed. Store MSIX packages require the exact existing Partner Center product identity and Microsoft certification.
