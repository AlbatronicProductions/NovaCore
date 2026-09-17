# SRV-01 real application acceptance

Run `launch.ps1` beside this document in PowerShell 7. The normal launcher also
offers **SRV-01 — Reusable Spacecraft Parts**. Both select `stock-assembly` and
the registered `novacore.stock.SRV01.FourHorn` definition.

The window begins READY. Inspect the capsule, tank, main engine and four instances
of the four-horn block. The accepted reusable meshes must remain connected, have
solid exterior surfaces, recognizable nozzle interiors, and appear only once.
Camera movement affects presentation only. Space begins the recorded two-second
episode: 0–0.25 s main, 0.25–0.5 s main plus gimbal, then six 0.25 s RCS rows.
Only realized nozzles receive exhaust; not every horn fires in this bounded plan.
The title reports actual main state, hexadecimal jet mask and exact-store values
rounded only for display. This is no-gravity free flight, not landing or contact.

Observe main thrust, nonzero gimbal, distinct RCS commands, resource decrease,
readable RUNNING and COMPLETED, and stable final hold. At completion the copied
pose is a **frozen endpoint**; active exhaust must be absent. Historical canonical
actuator values do not authorize continued presentation after terminal hold.
Close and relaunch to check cold scene reload; do not expect continuation to
reconstruct assets. A brief delayed frame must recover with at most four exact
intervals per callback and one render submission per display frame.

Report visual defects or PASS, including camera independence and reload. The
launch script retains a dated log under the reviewed disposable build directory.
Numerical tests alone cannot grant this manual acceptance.

## Recorded acceptance, 2026-09-17

The original general PASS was withdrawn after Project Control observed missing
main exhaust. A subsequent corrected-volume PASS remains historical because the
KSA-method clarification required another renderer revision.

The final revised Release window completed and was closed. In reply to the
explicit final checklist (bell attachment, bright core/soft edges, gimbal alignment,
only commanded RCS horns, camera independence, no plume at shutdown/final hold),
Project Control answered **PASS**. Runtime witness: method-manual-release.log,
summarized and hashed in method-results.json. Source and binary identities:
final-identities.json. This does not claim exact artistic identity with KSA or
qualify an atmosphere-dependent plume.
