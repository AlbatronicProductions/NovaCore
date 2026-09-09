$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$banked = '35c2693e0f5ca01c02c4cc5e73f68323e66ed8e7'
$control = Join-Path $root 'control'
if (Test-Path -LiteralPath $control) { throw 'Refuse to overwrite an existing control' }
git -C E:/NovaCore archive --format=zip --output="$root/banked-source.zip" $banked Directory.Build.props src/NovaCore.Core src/NovaCore.EphemerisFormat src/NovaCore.Simulation tests/NovaCore.Simulation.Tests
if ($LASTEXITCODE -ne 0) { throw 'Banked archive failed' }
Expand-Archive -LiteralPath "$root/banked-source.zip" -DestinationPath $control
$payloadPath = Join-Path $control 'src/NovaCore.Simulation/Timeline/SimulationEventPayload.cs'
$payload = [IO.File]::ReadAllText($payloadPath)
$anchor = '    internal Double3 ForceRoot { get; }'
if (!$payload.Contains($anchor)) { throw 'Banked payload anchor absent' }
$payload = $payload.Replace($anchor, $anchor + "`n" + '    internal InertTimelinePayloadLayout InertLayout { get; }')
$payload = $payload.Replace('        ForceRoot = forceRoot;', '        ForceRoot = forceRoot; InertLayout = default;')
[IO.File]::WriteAllText($payloadPath, $payload)
$dummy = @'
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Transactions;

namespace NovaCore.Simulation.Timeline;

// Disposable storage-only mirror: identical constituent field types/order to the
// candidate's provenance record. No validity, evaluation, routing, or history.
internal readonly record struct InertTimelineProvenanceLayout(
    ContactFeatureIdentity Feature, PhysicalSurfaceAuthorityIdentity Support, ulong ObservationId);

// Disposable storage-only mirror of the candidate intent's field types/order.
// Always default and never read by banked timeline behavior.
internal readonly record struct InertTimelinePayloadLayout(
    SpacecraftId Spacecraft, StateRevision ExpectedStateRevision, SimulationInstant Time,
    ReferenceFrameId RootFrame, Double3 ImpulseRoot, Double3 OffsetFromComRoot,
    InertTimelineProvenanceLayout Provenance, uint Version = 1);
'@
[IO.File]::WriteAllText((Join-Path $control 'src/NovaCore.Simulation/Timeline/InertTimelinePayloadLayout.cs'), $dummy)
$driver = 'E:/NovaCore/.codex/contact-allocation-discriminator/baseline/tests/NovaCore.Simulation.Tests/Program.cs'
if ((Get-FileHash -LiteralPath $driver).Hash -ne '59A8EC6369D1F3141D68C5A97B61C4E6FE7EB250EE416FE287B79A463025E42D') { throw 'Prior driver identity mismatch' }
Copy-Item -LiteralPath $driver -Destination (Join-Path $control 'tests/NovaCore.Simulation.Tests/Program.cs')
$changes = @(Get-ChildItem -LiteralPath (Join-Path $control 'src') -File -Recurse | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($control, $_.FullName)
    $old = Join-Path 'E:/NovaCore/.codex/contact-allocation-discriminator/baseline' $relative
    if (!(Test-Path -LiteralPath $old) -or (Get-FileHash -LiteralPath $_.FullName).Hash -ne (Get-FileHash -LiteralPath $old).Hash) { $relative }
})
if ($changes.Count -ne 2 -or !($changes -match 'SimulationEventPayload.cs') -or !($changes -match 'InertTimelinePayloadLayout.cs')) { throw "Unexpected control source differences: $changes" }
$changes | ConvertTo-Json | Set-Content -LiteralPath "$root/control-source-differences.json"
Get-FileHash -LiteralPath $payloadPath,(Join-Path $control 'src/NovaCore.Simulation/Timeline/InertTimelinePayloadLayout.cs'),(Join-Path $control 'tests/NovaCore.Simulation.Tests/Program.cs') | Format-Table -AutoSize
