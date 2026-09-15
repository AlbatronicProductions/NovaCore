# Supported powered-contact visible route

Launch in PowerShell7:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\powered-contact-production-integration\final-qualification\launch.ps1'
```

Uses existing renderer, existing qualification box2x1x1m and slab16x2x16m, and unchanged
CenteredEndpoint numerical fixture. Source mass, force, fuel, lattice and solver8/1 remain
unchanged. No spacecraft asset, departure, Florida or new frontend.

The fixture exhausts after33333 canonical ticks. Two explicit inspection inputs expose that
fast event without inventing a longer burn:

1. READY: Space admits16666ticks, runs one complete production operation, holds copied
   canonical POWERED endpoint. Actual ProducingOutput and reduced nonzero fuel.
2. Space admits16667ticks, publishes exact exhaustion at33333, holds EXHAUSTED endpoint.
   Actual EnabledNoFeed, endpoint throttle0 and fuel0. This is not a shutdown command.
3. Space begins live wall-time sampling AFTER the inspection hold; remaining dry contact
   runs at1:1 to20,000,000ticks. At most4 intervals per display callback, all debt retained.

First two inputs are explicit inspection intervals, not elapsed wall-time samples. Holds
do not mutate canonical pause/rate or reimport native state. All rendering uses copied
canonical endpoints; no interpolation or live BEPU pose reads. Render once per callback.
Camera WASD/QE/mouse/R is presentation-only. Closing disposes the one retained world.

Manual acceptance required: verify readable READY/POWERED HELD/EXHAUSTED HELD/RUNNING/
COMPLETED, box/slab agreement and stable support, no lift/explosion, camera independence,
brief title-bar delay with bounded recovery, final hold. Close the window after completion
to retain frame/work measurements. Automated presentation tests cannot grant manual PASS.

Status: supplied observation and saved log accepted; Project Control also explicitly confirmed
camera/title-bar checks with no issue. See manual-acceptance.md for exact scope.
Whole-frame measurements include present and host scheduling. Credit/service work is timed
separately. The scene is bounded evidence, not a claim that all Earth/renderer headroom is
closed. Presentation timing arrays total320,000 payload bytes, separate from solver storage.
