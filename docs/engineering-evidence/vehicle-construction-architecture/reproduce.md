# Reproduction

From E:/NovaCore, with .NET10 and Python:

```powershell
python -B tools/vehicle-construction/import-development-library.py --accepted 'E:\NovaCore-Blender-Visual-StepB\visual-evidence\ReusableParts\gates-2026-09-20' --engineering docs/engineering-evidence/development-launch-stack-preblender --asset-root 'E:\NovaCore-Blender-Visual-StepB\assets\visual\Parts' --out assets/vehicles/development
dotnet run --project tests/NovaCore.Simulation.Tests -c Release -- --construction-stage1 'E:\NovaCore-Blender-Visual-StepB\assets\visual\Parts'
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --construction-stage1-measure 'E:\NovaCore-Blender-Visual-StepB\assets\visual\Parts'
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --assembly-production
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --assembly-control
dotnet run --project tests/NovaCore.Simulation.Tests -c Release -- --construction-stage2
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --construction-stage3
dotnet run --project tests/NovaCore.Simulation.Tests -c Release --no-build -- --construction-stage4
python -B docs/engineering-evidence/vehicle-construction-architecture/stage4-width-reconstruct.py
python docs/engineering-evidence/vehicle-construction-architecture/inspect-closeout.py
```

The importer is cold NovaCore content tooling; it never runs Blender or KSA and does not change existing accepted source/evidence files. It emits the generic catalog and an explicit authoring template. Use Python -B to suppress bytecode caches when reading engineering helpers. This run produced one72,462byte calculate.cpython-311.pyc cache; it is listed in cleanup.md and is not engineering authority. To regenerate the canonical stock design after catalog generation, use:

```powershell
dotnet run --project tests/NovaCore.Simulation.Tests -c Release -- --author-construction-design assets/vehicles/development/development-design.authoring.json assets/vehicles/development/development-design.json
```

This is deliberate offline authoring, not repair on runtime load. Normal Load rejects the authoring template and changed used dependencies.

Measurements are exposed by --construction-stage1-measure through --construction-stage6-measure (Stage1 also needs the asset root). Run serially without builds/stress. Current final observations are performance-final-stage1.json through performance-final-stage6.json; earlier stage measurements remain historical. See performance.md for workload and output-size/peak-heap distinctions.

Expected current outcome: all bounded construction stages pass. Run --construction-stage5 (31checks), --construction-stage6 (93), --construction-stage7 (62) and --construction-stage8-reuse (18) with the same Release test executable, plus node tools/NovaCore.ConstructionEditor/tests/geometry.mjs. Stage4's90 checks and independent corrected-order arithmetic reconstruction pass. stage4-scratch-repro.py and its JSON are historical witnesses for the old ordering and intentionally refuse current source. They are not current failing gates. Re-run --assembly-production, --assembly-control, --pilot-demand, --pilot-allocation, --srv01-integration, --orchestration-only, --assembly-powered-contact-cheap, --assembly-contact-authority and --assembly-powered-contact-failures for the retained regression matrix. Camera.Tests and Launcher.Tests run their ordinary Release routes. No manual acceptance, deployment or banking is implied by engineering PASS.

inspect-closeout.py reads every file in the pre-campaign entry seal, current Git refs/index and nonignored additions. It writes JSON to stdout only. identity.json is excluded from its own hash set. No destructive cleanup is part of reproduction.

The manual/editor startup command and acceptance route are in manual-route.md. Debug and Release managed test-project builds and Release editor build pass. The earlier solution-wide Debug attempt lacked native shader/DLL copy prerequisites; this package does not claim a full-solution/deployment PASS.
