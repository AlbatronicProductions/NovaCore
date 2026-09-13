# Engineering article manual acceptance

Status: PASS. The user explicitly accepted both article routes on 2026-09-13.
Previous qualification-box acceptance was not used as article acceptance.

Recorded user result: centered PASS; tilted PASS; shape/slab agreement correct;
settling/tipping correct; camera independent; no COM jump/explosion; brief title-bar hold
recovered boundedly; READY/RUNNING/COMPLETED readable; final endpoint held.
The user did not provide the delay duration/index, so no individual telemetry tail is
attributed to that interaction. FAILED behavior was checked automatically with negative
host input; it was not deliberately induced in the two successful manual episodes.

The following instructions preserve the reproduction route.

Use PowerShell 7. Run one case at a time and close each window after checking completion:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\compound-contact-coverage\formal-qualification\launch.ps1' centered
& 'E:\NovaCore\docs\engineering-evidence\compound-contact-coverage\formal-qualification\launch.ps1' tilted
```

The window must identify **Engineering spacecraft article** and initially say READY.
Preparation completes before Space starts live host-time sampling. No future debt is prefunded.

The blue article has a 1.5 x 1 x 2.5 m central bus and two 0.5 x 1 x 1.5 m side pods.
Its overall dimensions are 2.5 x 1 x 2.5 m. The gray slab top is the support plane.
This is an engineering fixture, not final spacecraft art.

Check centered fall/contact and tilted angular contact/tipping. Require stable support and
visible agreement between the finite three-piece shape and the slab; no COM-origin jump,
visible collision mismatch, explosion or reconstruction. Camera controls remain WASD/QE,
mouse look and R reset. Move the camera and confirm physics remains independent.

Briefly hold the title bar once during each RUNNING episode, then release. Note approximately
when and for how long, so the deliberate frame delay can be distinguished in the retained
whole-frame observations. Catch-up must remain bounded and complete without lost work.

After 1,200 intervals / 20 simulated seconds, require readable COMPLETED and a held final
endpoint. Move the camera during that hold. A failure must say FAILED and preserve the last
authoritative state; do not repeat a defective run to obtain a pass.

Report each case separately: shape/contact agreement; support/tipping; camera independence;
delay/recovery; status readability; final hold; any visible defect. Manual acceptance cannot
be inferred from numerical tests.

The existing callback-to-callback timing captures rendering, present and host scheduling.
Tail records separately report preceding owner credit/service time. Logs are saved on window
close to the bounded ignored folder build/engineering-article-formal-live (four files total).
No GPU-only or synchronization attribution is implied where telemetry is unavailable.
