# Project Control manual acceptance

Required: **YES**. Status: **PENDING**. Automated engineering/browser qualification is not manual acceptance. No deployment, commit, tag, push or banking is authorized by this route.

Open http://127.0.0.1:58742/ while the supplied local development process is running. Restart from E:/NovaCore:

```powershell
dotnet build tools/NovaCore.ConstructionEditor/NovaCore.ConstructionEditor.csproj -c Release
dotnet tools/NovaCore.ConstructionEditor/bin/Release/net10.0/NovaCore.ConstructionEditor.dll --catalog assets/vehicles/development/development-catalog.json --asset-root 'E:/NovaCore-Blender-Visual-StepB/assets/visual/Parts' --stock assets/vehicles/development/development-design.json --port 58742
```

The tool validates accepted GLBs at startup and presents a frame/socket/COM schematic. Full mesh rendering and flight are not this route; the accepted SRV launcher deployment remains unchanged.

1. Confirm the catalog appears. Retire any runtime, then Clear draft. Choose nc.booster-body.3600; Preview root, Accept preview. New stores are empty.
2. Choose nc.booster-engine.liquid-a. Try parent RELEASE_MOUNT with child ENGINE_MOUNT: Preview connection must refuse without changing the draft. Choose parent ENGINE_0 instead; check Propellant and Command/data, leave Electricity/Detachable off; Preview connection, Accept preview.
3. Add another copy of the same engine definition at ENGINE_1. Confirm three parts share two definitions. Inspect Selected part and Connection, fuel and electrical graphs. Fuel sources/services are explicit; Data must not imply Electricity. DLV electrical modules are unparameterized, so an empty module list is expected.
4. Select the second engine; Operation: Reconnect selected part; Parent: root; parent interface ENGINE_2; child ENGINE_MOUNT. Preview/accept using the connection ID field. Rotate Z +90°. Remove that selected engine subtree and confirm two parts remain.
5. Save design, then Reload file with the downloaded JSON. The same design revision/digest, quantities and connections must return.
6. Instantiate static runtime. Note identity/digest. Rotate or Clear draft and confirm the runtime remains unchanged. Retire runtime. Reinstantiation must create a fresh identity.
7. Load stock design. Confirm nine parts, three shared booster engines, Data-only detachable planes and36 stores. Instantiate static runtime: reference mass approximately247283.615kg,38 actuators and Capsule control. Retire it.
8. Clear draft; choose nc.capsule.return-3500; Preview root, Accept preview. Save/reload this non-DLV design and instantiate through the same button. Empty-store reference mass approximately2882.350kg; four pods, ten stores and24 actuators. Retire it.

No DLV ignition, separation, contact or flight step is offered. Stage/action metadata is descriptive. Report PASS or the exact failing step. Manual PASS does not itself authorize banking or the next production front. STOP FOR PROJECT CONTROL.
