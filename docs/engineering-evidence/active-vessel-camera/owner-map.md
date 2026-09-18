> SUPERSEDED after Project Control manual acceptance FAILED at Step 5 HOME. FREE is now DEFERRED; HOME is unbound. This file records the prior candidate, not current acceptance. Use [the correction package](../active-vessel-camera-free-correction/README.md) and [revised manual route](../active-vessel-camera-free-correction/manual-route.md).

# Ownership

The accepted [post-M15.3 review](../post-m15.3-first-playable-launch-review/README.md) remains the design basis.

| Responsibility | Owner |
| --- | --- |
| Canonical spacecraft identity, mass, motion, resources, commands | Existing AssemblyLaunch / session / canonical state store; unchanged |
| Copied physical observation and material-origin mapping | StockAssemblyDevelopmentScene and existing AssemblyFloridaPresentation |
| Active camera binding | One canonical ID, one presentation lifetime generation, one copied SceneObjectFocusObservation in SolarSystemScene |
| Viewed target, orbit distance/angles, refocus memory, environment index | Existing SolarSystemScene |
| Detached translation and look | Existing FreeCameraController, dispatched once per display callback |
| Terrain exclusion, planetary context, rendering | Existing Solar/planetary pipeline |
| Native key edges and transport | Existing input callback; appended camera-only action mask |

The camera binding is a presentation reference to the session's already active spacecraft. It cannot select, control, reset or mutate the physical spacecraft. F changes the view target; Home detaches the view. Selecting Earth/Moon changes the view and its celestial environment while preserving the independent vessel binding.

No new camera class, simulation API, propulsion input, flight controller, scenario or renderer was introduced.
