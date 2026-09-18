# Project Control manual acceptance — PASS

Authority: Project Control's explicit resume instruction in this task, received 2026-09-18. This supersedes the partial/manual-failure entry state; the entry manifest and historical evidence remain unchanged.

Project Control reports manually deploying the **already-built corrected Release candidate** after `prepare-launcher.ps1` failed only because Windows PowerShell's .NET Framework lacks `System.IO.Path.GetRelativePath`. Both candidate build configurations had already passed. Project Control copied the candidate Release outputs into the existing Triangle and Launcher paths and verified candidate/deployed hashes.

Project Control then tested the FL Launchpad route and reported PASS:

- SRV-01 remains supported on the Florida slab.
- With no camera orbit input, the player-selected camera/horizon relationship remains coherent while Earth rotates under time warp.
- Vessel focus, follow, orbit and zoom pass.
- Celestial focus and F-refocus pass.
- FREE remains deferred; HOME remains unbound.

This is **user-reported physical/manual acceptance**, not an assistant declaration based on headless tests or screenshots. The exact user wording for the corrected behavior was: “Manual FL Launchpad testing now PASSES the previously failing rotating-Earth/time-warp camera behavior.”

Closure reconciliation independently checked every candidate file against its deployed counterpart: **88/88 SHA-256 matches** across:

- `E:\NovaCore\samples\NovaCore.Triangle\bin\Release\net10.0`
- `E:\NovaCore\tools\NovaCore.Launcher\bin\Release\net10.0-windows`

The helper now uses a containment-checked relative suffix compatible with Windows PowerShell, and provides `-VerifyOnly`. Closure used **VerifyOnly**: it did not copy files, rebuild or replace the manually accepted runtime. The complete manifest remains in `build/active-vessel-camera-warp-correction/deployment.json`; final identity retains its hash and the deployed executable/library hashes. Isolated performance overlays are not the manual executable.

The accepted route remains the existing Launcher **FL Launchpad** route. HOME is excluded. No additional manual retest is requested by this closure. No milestone, commit, tag, push, banking or next production front is authorized.
