# Precision model

Authoritative simulation positions are `UniversePosition` values containing `Double3` and a `ReferenceFrameId`. `RenderOrigin` is the camera's authoritative universe-space position. `ReferenceFrame.Resolve` first verifies the frame relationship, then produces a camera-relative `RelativePosition`.

Flow: `UniversePosition` → reference-frame resolution → camera-relative `RelativePosition` → native GPU upload → shader reconstruction → rasterization. The camera always renders at `(0, 0, 0)`; the world is never shifted. Milestone 1 casts the resolved position to a float push constant immediately before the vertex shader. A future upload encoder may turn each coordinate into high/low floats without changing the public precision types or simulation state.
