# Actual Windows non-96-DPI qualification

2026-09-30, canonical Release NovaCore, automated native Windows input through
Computer Use. This is not physical human input acceptance.

Windows Display Settings identified display 3 at 3440 x 1440, Landscape, HDR on,
100% recommended scale. Only its scale was changed to 125%. The application was
started after that change. The native transition receipt reports **DeviceDpi 120**;
this was actual Windows scaling, not a resized-window or mocked-DPI substitute.

The existing `player-entry` qualification exercised paused/menu/dropdown/editor/
return/windowed/fullscreen transitions. Its 17,275 checks passed. At 120 DPI,
parent client, viewport host and native drawable/backbuffer were all 3440 x 1440;
the windowed phase was 1920 x 1080 for all three, then returned to 3440 x 1440.
Projection aspect was respectively 2.388888888888889 and 1.7777777777777777.
The raw receipt's `realOsDpiTransition: false` remains unchanged: that probe does
not operate Windows Settings. The OS change was performed externally as recorded
here. No live cross-monitor transition is inferred from this run.

An initial automatic-start attempt failed the foreground-window precondition
while Settings held focus. The application exited; this was not a rendering or
DPI failure. The retained second attempt used the existing
`NOVACORE_QUALIFICATION_MANUAL_START=1` option, activated its window, then clicked
START. No production focus behavior was changed for the test.

Ordinary `tools/NovaCore.App/bin/Release/net10.0-windows/NovaCore.exe`, without
qualification arguments or environment overrides, then exercised:

- Configuration Game Type dropdown and its Exploration selection; coordinate
  click on the visible START button entered Loading then full-client Exploration.
- Escape opened the pause menu, showing both Simulation Paused and Exploration
  Paused. New Vehicle opened the full-client editor with its owned panels.
- Save / Load selected existing `AD4 Editor Route 20260929`: 12 parts, dry 504 kg,
  propellant 800 kg. No saved craft was overwritten.
- Clicking the visibly rendered command core selected and highlighted that part;
  the status became `Selected 1 linked part(s)`. This is an actual native pointer
  to render-coordinate witness in addition to the extent assertions.
- Coordinate click on Launch vehicle entered Flight 1x with the craft on the
  Florida slab. Escape showed Flight Paused; Quit closed the ordinary application.

Configuration, menu, editor and gameplay controls remained inside their panels
and client bounds, with no panel overlap or inaccessible control observed. The
125% captures are 2752 x 1152 downsampled images of the 3440 x 1440 client; fine
text/border raster detail is reduced in these captures (also in Windows Settings).
They must not be mistaken for the native drawable dimensions. Selected witnesses
and the native receipt are retained beside this report.

Windows Settings was restored to **100% (Recommended)** with **3440 x 1440**,
Landscape and HDR unchanged, and Settings was closed. Full original/restoration
Settings witnesses remain local under `build/prebank-convergence/dpi/`.

The exercised Release package identity is recorded in `dpi-package.json`:
`528199b8fb1a7b3bc9c0d545a1314fcbab51480fe825aac26399d820a819a9c3`.
This matches the previously accepted Release package receipt byte identity and
the post-smoke verification. The ordinary process was PID 43784; its local log
session is `20260930-220259-718-43784-62cd8843a17647d6968111f5c1655cfb`.
The qualification recorder session was
`140ee2f9a02b44a2a11af384174ce2a5`; its raw runtime data remains local under the
existing recorder owner. No system anomaly was observed.

Held hardware keys, focus-loss permutations, live monitor transitions and physical
human input remain **NOT RUN / unclaimed**. This closes the required actual
non-96-DPI routes above, not every possible DPI or input permutation.
