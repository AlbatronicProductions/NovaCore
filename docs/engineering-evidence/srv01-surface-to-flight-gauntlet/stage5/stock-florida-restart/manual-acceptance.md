# Stock SRV-01 Florida manual acceptance

Historical stopped recipe, superseded by [the qualified final-closure manual route](../final-closure/manual-acceptance.md). Engineering closure is now complete; manual acceptance remains PENDING. The original stop record below describes the preceding ticket, not the current execution authorization.

Status: **BLOCKED / PERFORMANCE STOP — NOT EXECUTED.** The first automated live process reported a 74.5905 ms display interval and 72.4557 ms servicing interval, with no bounded cause established. No further live process or manual acceptance was launched. Project Control must resolve this stop before authorizing the recipe below. Earlier box, article and Stage-1/2 visual approvals do not qualify it.

```powershell
& 'E:\NovaCore\docs\engineering-evidence\srv01-surface-to-flight-gauntlet\stage5\stock-florida-restart\launch.ps1' -ProjectControlResume
```

Wait for **Florida graded ground (site-local)** and **READY**, then press Space. This is a bounded local view of the real terrain-v5 full-weight graded ground patch, 48 m east of the Florida site anchor and outside the existing platform. The drawn 16 m surface has exactly the collision patch's top/extent. This view does not depict a complete facility or the future launch pad/stack. It uses copied canonical site-frame endpoints; independent tests verify their full Earth-centred inertial publication.

Check:

- Actual connected seven-part SRV-01 sits coherently on the shown ground; no jump, explosion, detachment, visible penetration or sliding.
- READY → RUNNING → COMPLETED / HELD ENDPOINT is readable.
- Engine and RCS remain OFF; no plumes. Fuel remains 30 kg and oxidizer 45 kg; wet mass is 705 kg.
- Move the camera (WASD/QE, mouse look, R reset): no physical motion caused by the camera or mismatch between parts and support.
- Briefly hold the title bar during the 20-second run, release, and check bounded recovery with support preserved. Note approximately when/how long if practical.
- After 1,200 intervals / 20 simulated seconds, the final endpoint remains held. Visible ascent, departure and dramatic motion are not expected.

Close the window afterward so its log can be retained. Report PASS or specific defects. No numerical result substitutes for this manual decision.

Known pre-existing residual: a white/unpresented startup interval may occur before READY (historical targeted samples included 16.67 s). Its cause remains unresolved. This restart does not fix or investigate it; host sampling begins after preparation and Space.
