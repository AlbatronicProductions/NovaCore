# Feasibility proof reproduction

The proof is standalone C# with no NovaCore, Vulkan, UI or GPU reference. It models a reset by discarding unacknowledged volatile/written events; it does not reset hardware, terminate a process or qualify a production recorder.

Authoritative results: `E:\NovaCore\build\ordinary-player-recorder-feasibility-20260926\proof-debug-v2\result.json` and `proof-release-v2\result.json`. Both contain 139 checks and the same retained-prefix hash. Earlier result directories precede the independent review's sequencing correction and are not final qualification.

The source copy under `proof/` in this evidence package matches the reviewed v2 proof. Copy those three source/config files into a **fresh ignored build directory** before building, to keep generated output out of the report tree. Use a fresh result directory for each run; the program refuses to overwrite one.

```powershell
dotnet build <fresh-proof-directory>\BoundaryDurabilityProof.csproj -c Debug -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false --nologo
dotnet <fresh-proof-directory>\bin\Debug\net10.0\BoundaryDurabilityProof.dll <fresh-debug-result-directory>
dotnet build <fresh-proof-directory>\BoundaryDurabilityProof.csproj -c Release -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false --nologo
dotnet <fresh-proof-directory>\bin\Release\net10.0\BoundaryDurabilityProof.dll <fresh-release-result-directory>
```

The local NuGet configuration clears package sources; there are no external package dependencies. These commands build only `BoundaryDurabilityProof`, never the NovaCore solution/player package.

Interpretation: compare the three `*-after-reset.bin` files byte-for-byte. They contain the same already-durable session and completion40 despite different suffix histories. `present-stall-observer-survived.bin` demonstrates that a successfully persisted suffix can identify the last recorded present boundary. Do not report that conditional model as guaranteed recovery from whole-machine reset.

`preserve.py` in the working evidence directory created entry identities for 3,845 repository files, the 127-file canonical package, Git and both protected capture trees. Do not rerun that entry collector in place. `preservation.json` in this report records the final comparison. No input evidence is regenerated or normalized.
