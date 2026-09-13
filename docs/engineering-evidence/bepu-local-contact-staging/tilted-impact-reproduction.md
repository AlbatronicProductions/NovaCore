# Reproduce bounded staged-contact qualification

Use the accepted M14.18 source identified by `staging-qualification.json`, based on
`b78f5b8c763b8d118b93f48fc4f87e78d7b6b4b2`. Do not reconstruct it from the old blocked
draft or change any dependency bytes. SDK 10.0.303 / runtime 10.0.12 were used.
No profiler, tiering/PGO overrides, debugger timing, renderer or live Earth execution.

## Permanent contract gates

From the repository root, build Debug/Release once:

```powershell
dotnet build NovaCore.sln -c Debug --nologo -v minimal
dotnet build NovaCore.sln -c Release --nologo -v minimal
```

Run the focused candidate separately in each configuration:

```powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Debug/net10.0/NovaCore.Simulation.Tests.dll --local-contact-staging-only
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --local-contact-staging-only
```

The permanent test executes admission/refusal controls, centered/tilted 1200-step
fixtures and their independent repeat worlds, free-flight/fixed-frame transport,
source-force control, 128 warm + 1024 measured full step/export allocation calls,
retained storage and the shared byte[128] positive control. Expected physical values,
zero bytes, 152-byte control and 518096-byte retained upper bound are recorded in JSON.

Fifteen predecessor route names and configuration results are in that JSON. Invoke each
route using the corresponding built Simulation executable. ReferenceFrames, Precision
and BepuDependency executables use the same configuration path and no arguments.
No predecessor test changes or fixture substitutions are needed.

Only after physical/correctness gates pass, run this command in exactly three fresh
processes, with no retries or concurrent builds/tests:

```powershell
dotnet tests/NovaCore.Simulation.Tests/bin/Release/net10.0/NovaCore.Simulation.Tests.dll --local-contact-staging-performance
```

It prints all 1024 chronological samples plus percentiles/cold costs and fails any
absolute gate. Timing uses ordinary runtime behavior, independently of allocation
isolation. For retained evidence, summarize percentiles, worst samples and lifecycle
costs rather than retaining full build trees or console transcripts.

## Recreate the causal geometry control only if separately needed

The permanent test reproduces corrected quality directly. The old geometry is retained
here because its unbanked source is not assumed to exist in Git history. Perform a
future causal comparison in an isolated disposable source copy, never by modifying
canonical candidate source or accepted hashes. All settings remain 8 iterations,
1 substep, .5 friction, 30 Hz / damping 1, recovery cap 2 m/s, maximum speculative cap
.52 m. Keep the fixture exactly as in `LocalContactStagingTests.Create` and its tilted
case: 2x1x1m,1000kg,COM(0,2,0),zero velocities,.25rad Z tilt,force(0,-9810,0).

Replace only the static slab construction in the disposable `LocalContactWorld` with:

```csharp
pool.Take<Triangle>(2, out var triangles);
var h = (float)configuration.PlaneHalfExtent;
var a = new Vector3(-h, 0, -h); var b = new Vector3(h, 0, -h);
var c = new Vector3(-h, 0, h); var d = new Vector3(h, 0, h);
triangles[0] = new Triangle(a, b, c);
triangles[1] = new Triangle(b, d, c);
surfaceShape = simulation.Shapes.Add(new Mesh(triangles, Vector3.One, pool));
plane = simulation.Statics.Add(new StaticDescription(Vector3.Zero, surfaceShape));
```

The existing `RemoveAndDispose(surfaceShape,pool)` retires mesh ownership. Expected old
peaks: centered .0041343607m, tilted .05339229m. Corrected slab: .005455954/.004164368m.
No altered timestep, source, warmup, state patch or geometry offset is needed.

For the retained child/parent diagnostic, source-link canonical Simulation C# files
in a disposable executable named `NovaCore.Simulation`, excluding its obj/bin and the
two replaced world/callback files. Reference canonical Core/EphemerisFormat projects,
set `NovaCoreUsesBepu=true`, and preserve the lunar resource logical name from the
canonical Simulation project. This retains internal access without changing production
visibility. The probe invokes normal source capture, world creation, Step and Read.

At both `ConfigureContactManifold` overloads, record each point **before returning true**:
child indices where applicable, `Count`, `GetFeatureId`, `GetDepth`, `GetOffset`,
`GetNormal`. In the generic overload retain the final parent. From the initialized
simulation, get body `pair.A.BodyHandle`; world point is `body.Pose.Position+offset`,
and normal point velocity is
`Dot(body.Velocity.Linear+Cross(body.Velocity.Angular,offset),normal)`.
Do not alter `PairMaterialProperties` or any manifold data.

Before and after each Step, snapshot body position/orientation, linear/angular velocity,
awake state and `Collidable.SpeculativeMargin`. Evaluate all eight box corners with
`pose.Position+Vector3.Transform(corner,pose.Orientation)` and velocities with
`linear+Cross(angular,transformedCorner)`. Record source/target ticks and derived dt.
Run 1200 steps; only the compact impact window (30-38), settled endpoint (1200) and
step35 child manifolds need retention. Setup and observer allocations are diagnostic
only; never use this executable for allocation or performance qualification.

Original step35 must show child0 first feature32768/three points, child1 first feature6/
four points, and parent exactly child0 features0,5,2. This independently distinguishes
late generation from mesh filtering. Corrected step35 has four parent contacts including
both far corners. The JSON witness preserves measured values and exact pinned-source
branch arithmetic. Its deep overlap is an endpoint after step35, reported by the next
pre-solve manifold at step36; keep this timing distinction.

## Evidence lifetime

Keep report, compact witness, before/current source hashes, final gate results and these
instructions. Raw long traces, temporary reporters, linked builds and benchmark console
transcripts are rebuildable and disposable. Historical trust/dependency investigation
directories are outside this correction's cleanup scope. No banking is authorized here.
