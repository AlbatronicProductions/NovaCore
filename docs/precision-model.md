# Precision model

Authoritative simulation positions are `UniversePosition` values containing `Double3` and a `ReferenceFrameId`. `RenderOrigin` is the camera's authoritative universe-space position. `ReferenceFrame.Resolve` first verifies the frame relationship, then produces a camera-relative `RelativePosition`. These Core types are the only authoritative spatial state.

Flow: `UniversePosition` → reference-frame resolution → GPU-only `EncodedPosition` → batched storage-buffer upload → shader reconstruction → rasterization. The camera always renders at `(0, 0, 0)`; the world is never shifted. `EncodedPosition` is explicitly not simulation state and must not be consumed by gameplay or simulation.

The current encoding is `high = (float)value` and `low = (float)(value - (double)high)`. The shader resolves `(object.high - camera.high) + (object.low - camera.low)` before normal FP32 vertex placement. At four trillion units, the 0.25-unit sample separation survives while a single world-space float produces zero. This is a transport representation, not GPU FP64: residual quantization grows with absolute magnitude. The included `10^15` test observes a 0.25-unit reconstruction error.
