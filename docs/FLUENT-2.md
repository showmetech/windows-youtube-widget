# Fluent 2 interface contract

Use the Windows guidance at https://fluent2.microsoft.design/ as the design reference for this WPF application. Keep native platform behavior and semantic resources; do not import mobile-only patterns indiscriminately.

- Prioritize the video, with transport centered and secondary actions grouped in the footer.
- Fit the available monitor work area, preserve the video aspect ratio and reflow controls instead of overlapping them.
- Keep four clear settings destinations. In narrow settings windows, retain named accessible navigation with compact icons.
- Segoe UI Variable; body 14, captions 12 and page headings 20; support 100–200% text scaling.
- Use semantic theme resources and contrast checks. Retain 100% opacity by default and Windows high contrast behavior.
- Name interactive controls, show keyboard focus, and use clear disabled/pressed/hover states. Status messages must remain accessible.
- Prefer brief, purposeful motion. Page transitions use a 120 ms fade and respect the app and Windows motion settings.
- Keep privacy details available without overwhelming the extension popup. Never request additional permissions for a visual change.

References: design-principles, layout, accessibility, typography, iconography, design-tokens, color, motion, content-design and wait-ux on the official Fluent 2 website. Native Media Player is a comparative reference for content priority and mini-player behavior, not a source of product branding.

Validate builds with scripts/Build.ps1 and scripts/Test.ps1. UI tests include settings pages at 400 px, normal and enlarged text; geometry fixtures cover small and short work areas. Run real playback and animation checks before distributing changes. Cross-compilation does not prove physical ARM64 or Windows 10 compatibility.
