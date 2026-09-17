# Exact proposed SRV-01 bank manifest

93 included paths; no staging performed. Baseline `09463ec6c323daf233205cb08306d38611c22e6e`.

## PRODUCTION RUNTIME (36)

- `native/NovaCore.Native/CMakeLists.txt`
- `native/NovaCore.Native/NovaCoreNative.cpp`
- `native/NovaCore.Native/NovaCoreNative.h`
- `native/NovaCore.Native/PreparedSubmissionStorage.h`
- `native/NovaCore.Native/shaders/exhaust.frag`
- `native/NovaCore.Native/shaders/exhaust.vert`
- `native/NovaCore.Native/shaders/exhaust_resolve.frag`
- `native/NovaCore.Native/shaders/triangle.frag`
- `native/NovaCore.Native/shaders/triangle.vert`
- `samples/NovaCore.Triangle/NovaCore.Triangle.csproj`
- `samples/NovaCore.Triangle/Program.cs`
- `samples/NovaCore.Triangle/SampleOptions.cs`
- `samples/NovaCore.Triangle/StockAssemblyDevelopmentScene.cs`
- `src/NovaCore.Graphics/ReusablePartVisuals.cs`
- `src/NovaCore.Interop/NativeRuntime.cs`
- `src/NovaCore.Simulation/NovaCore.Simulation.csproj`
- `src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyApplicationSession.cs`
- `src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyDesign.cs`
- `src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyDynamics.cs`
- `src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyFlight.cs`
- `src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyProfileAdmission.cs`
- `src/NovaCore.Simulation/Spacecraft/Assemblies/AssemblyResources.cs`
- `src/NovaCore.Simulation/Spacecraft/Assemblies/Data/SRV01-FourHorn.json`
- `src/NovaCore.Simulation/Spacecraft/Assemblies/Data/SRV01-G0-B.json`
- `src/NovaCore.Simulation/Spacecraft/ReferenceFrames/SpacecraftReferenceFrameEvaluator.cs`
- `src/NovaCore.Simulation/Spacecraft/SpacecraftStateStore.Assemblies.cs`
- `src/NovaCore.Simulation/Spacecraft/SpacecraftStateStore.cs`
- `src/NovaCore.Simulation/Spacecraft/SpacecraftStateView.cs`
- `src/NovaCore.Simulation/Spacecraft/Translation/SpacecraftMotionEvaluator.cs`
- `src/NovaCore.Simulation/Spacecraft/Translation/SpacecraftTranslationState.cs`
- `src/NovaCore.Simulation/Transactions/SimulationState.cs`
- `src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.Assemblies.cs`
- `src/NovaCore.Simulation/Transactions/SimulationTransactionEngine.AssemblySave.cs`
- `tools/NovaCore.Launcher/LaunchCommandBuilder.cs`
- `tools/NovaCore.Launcher/LaunchConfiguration.cs`
- `tools/NovaCore.Launcher/ScenarioCatalog.cs`

## PRODUCTION ASSET (5)

- `assets/visual/SRV01/NC_SRV_Capsule_A.glb`
- `assets/visual/SRV01/NC_SRV_Main_A.glb`
- `assets/visual/SRV01/NC_SRV_Rcs_A.glb`
- `assets/visual/SRV01/NC_SRV_Tank_A.glb`
- `assets/visual/SRV01/manifest.json`

## PERMANENT TEST (14)

- `native/NovaCore.Native/ExhaustPresentationTests.cpp`
- `native/NovaCore.Native/PreparedSubmissionStorageTests.cpp`
- `tests/NovaCore.Graphics.Tests/GraphicsTestHarness.cs`
- `tests/NovaCore.Graphics.Tests/NovaCore.Graphics.Tests.csproj`
- `tests/NovaCore.Graphics.Tests/Program.cs`
- `tests/NovaCore.Graphics.Tests/StockAssemblyPresentationTests.cs`
- `tests/NovaCore.Launcher.Tests/Program.cs`
- `tests/NovaCore.Simulation.Tests/AssemblyProductionMeasurements.cs`
- `tests/NovaCore.Simulation.Tests/AssemblyProductionTests.cs`
- `tests/NovaCore.Simulation.Tests/Data/Assembly-FourHorn-Reference-Endpoints.json`
- `tests/NovaCore.Simulation.Tests/Data/Assembly-Reference-Endpoints.json`
- `tests/NovaCore.Simulation.Tests/NovaCore.Simulation.Tests.csproj`
- `tests/NovaCore.Simulation.Tests/Program.cs`
- `tests/NovaCore.Simulation.Tests/Srv01IntegrationTests.cs`

## DOCUMENTATION (5)

- `README.md`
- `docs/CODEX_HANDOFF.md`
- `docs/NOVACORE_CURRENT_STATE.md`
- `docs/architecture.md`
- `docs/engineering-evidence/README.md`

## RETAINED ENGINEERING EVIDENCE (33)

- `docs/engineering-evidence/srv01-production-integration/README.md`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/README.md`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/audit.json`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/bank-actions.ps1.txt`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/bank-manifest.json`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/bank-manifest.txt`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/disposable.json`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/external-preservation.json`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/manifest.md`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/preflight.json`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/validate.ps1`
- `docs/engineering-evidence/srv01-production-integration/bank-preparation/validation.json`
- `docs/engineering-evidence/srv01-production-integration/cleanup.md`
- `docs/engineering-evidence/srv01-production-integration/disposable-inventory.json`
- `docs/engineering-evidence/srv01-production-integration/exhaust-before-identities.json`
- `docs/engineering-evidence/srv01-production-integration/exhaust-correction.md`
- `docs/engineering-evidence/srv01-production-integration/exhaust-ksa-identities.json`
- `docs/engineering-evidence/srv01-production-integration/exhaust-method-identities.json`
- `docs/engineering-evidence/srv01-production-integration/exhaust-method.md`
- `docs/engineering-evidence/srv01-production-integration/final-identities.json`
- `docs/engineering-evidence/srv01-production-integration/gate0.md`
- `docs/engineering-evidence/srv01-production-integration/imported-candidate.json`
- `docs/engineering-evidence/srv01-production-integration/inspect-current-ksa.ps1`
- `docs/engineering-evidence/srv01-production-integration/intake-visuals.ps1`
- `docs/engineering-evidence/srv01-production-integration/launch.ps1`
- `docs/engineering-evidence/srv01-production-integration/manual-acceptance.md`
- `docs/engineering-evidence/srv01-production-integration/method-results.json`
- `docs/engineering-evidence/srv01-production-integration/oracle-report.md`
- `docs/engineering-evidence/srv01-production-integration/preflight.json`
- `docs/engineering-evidence/srv01-production-integration/reproduce-oracle.py`
- `docs/engineering-evidence/srv01-production-integration/reproduction.md`
- `docs/engineering-evidence/srv01-production-integration/results.json`
- `docs/engineering-evidence/srv01-production-integration/visual-intake.json`

## Excluded changed/untracked paths

- `docs/engineering-evidence/srv01-production-integration/closeout.json` — Superseded/duplicated local narrative or status; decisive current method, hashes and failure history retained elsewhere
- `docs/engineering-evidence/srv01-production-integration/exhaust-ksa.md` — Superseded/duplicated local narrative or status; decisive current method, hashes and failure history retained elsewhere
- `docs/legal/CREATOR-CONTENT-POLICY-DRAFT.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/LEGAL-REVIEW-TODO.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/MODDING-POLICY-DRAFT.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/ROOT-LICENSE-CANDIDATE.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/COMMERCIAL-LICENSING-PROPOSED.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/CREATOR-CONTENT-POLICY-PROPOSED.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/IDENTITY.json` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/INSTALLATION-PLAN.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/LEGAL-REVIEW-BRIEF.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/LICENSING-PROPOSED.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/MODDING-POLICY-PROPOSED.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/NOTICE-PROPOSED.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/PROJECT-CONTROL-DECISIONS.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/REPORT.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/RIGHTS-MATRIX.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/ROOT-LICENSE-PROPOSED.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/THIRD-PARTY-SCOPE.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/COMMERCIAL-LICENSING-preinstallation.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/FINAL-REPORT.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/commit.json` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/final-bank-paths.json` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/preflight.json` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/prospective.json` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/release-gates.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/remote-tags-before.txt` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/remote-verification-stop.json` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/remote-verification.json` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/rights-matrix.md` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/staged.json` — Preexisting legal review; not part of SRV-01
- `docs/legal/finalization/m15.1-release/withheld-paths.json` — Preexisting legal review; not part of SRV-01

## Excluded generated/external categories

- `build/**; **/bin/**; **/obj/**` — All ignored rebuildable output; current owned root build/srv01-bank-preparation
- `.codex/**; .novacore/cache/**` — Ignored scratch/acquisition/cache; current terrain is preserved outside bank
- `E:/NovaCore-Blender-Visual-StepB/**` — Editable source, review and tooling remain in separate worktree; only four accepted GLBs plus manifest enter production
- `E:/Kitten Space Agency/**` — Read-only reference; no source/assets copied into bank
- `E:/NovaCore-Archives/authorized-historical-cleanup-20260917/manifest.json targets` — Authorized retired historical terrain/diagnostic payloads remain absent
