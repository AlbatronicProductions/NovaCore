# Project Control manual Florida acceptance — PENDING

**Current correction, 2026-09-17: Florida manual acceptance is PENDING / NOT YET TESTED CORRECTLY.** Project Control limits the prior PASS to supported-craft visual/physical behavior. [manual-result.json](manual-result.json) records both decisions. The unchanged recipe below is the route under investigation, not a corrected acceptance route: it renders a generic support cube rather than recognizable Florida site presentation. See [manual-route-ownership.md](manual-route-ownership.md). Do not use this recipe to claim Florida acceptance or promotion.

Engineering closure is complete. No manual run was launched by this ticket. The three timing populations are automated diagnostic captures and are not visual acceptance. This recipe supersedes the stopped recipe in the historical stock-restart packet, without rewriting that packet.

Exact uninstrumented Release executable:
`E:\NovaCore\build\srv01-stage5-stock\artifacts\bin\NovaCore.Triangle\release\NovaCore.Triangle.exe`

Reviewed PowerShell route, checking source/binary seals before opening:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage5\final-closure\launch.ps1'
```

Underlying scenario command (repository working directory required):

```powershell
Set-Location -LiteralPath 'E:\NovaCore'
& 'E:\NovaCore\build\srv01-stage5-stock\artifacts\bin\NovaCore.Triangle\release\NovaCore.Triangle.exe' --scene=srv01-florida-support --log=renderer,vulkan
```

There are no benchmark/autostart flags. Wait for **Florida graded ground (site-local)** / **READY**, then press Space. Use WASD/QE, mouse look and R camera reset. Observe the 20-second/1,200-interval episode, move the camera, and inspect the final held endpoint. Close afterward to save the log and report PASS or specific defects.

Required observations:

- Actual connected seven-part stock SRV-01 at the authenticated Florida site; correct placement on the visible ground.
- Main engine and RCS OFF, no exhaust, fuel 30 kg/oxidizer 45 kg unchanged, mass 705 kg.
- No sinking, visible creep/sliding, sustained jitter, explosion or part separation.
- Camera motion is independent of physics; readable READY/RUNNING/COMPLETED state; stable final hold.
- The support surface is the actual full-weight terrain-v5 graded patch, 48 m east of the Florida anchor. This is its bounded 16×16 m planar representation admitted from the physical query, not an unrelated fallback qualification floor. Missing authoritative terrain fails preparation; it is not replaced.
- A brief title-bar delay may pause Windows callbacks; on release, servicing drains at most four intervals/call while preserving debt and physical ownership. Report any recovery issue if checked.

Limits: the site-local viewport does not depict full Florida scenery or a launch facility. It is outside the existing platform and does not qualify that platform or a future stack. The known white/unpresented startup residual may precede READY. First contact has a disclosed 53.7912–58.8670 ms cold servicing cost in the final diagnostic captures; the original 72.4557 ms event remains retained. Four rare warmed display intervals 11.1948–11.5206 ms occurred outside servicing in one capture; their precise external owner is unproven. No performance optimization is claimed. Rotation appears in canonical inertial publication, not visible relative ground motion. Stationary support is the expected behavior. Rotating-site runtime departure remains NOT YET QUALIFIED.

Manual acceptance is a Project Control decision. No automated result grants it. Stage 6 remains CLOSED; no banking or automatic promotion follows this launch.
