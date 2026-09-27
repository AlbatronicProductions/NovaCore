$ErrorActionPreference='Stop'
$tool='E:\NovaCore\build\construction-connector-revision\reference-tools\ilspycmd.exe'
$binary='E:\Kitten Space Agency\KSA.dll'
$targets=[ordered]@{
 'KSA.ThrusterController'=@('CreateComponents','RecomputeDynamicData','GetSaveData','ApplySaveData')
 'KSA.RocketThrusterControllerTemplate'=@('Create(')
 'KSA.Rocket'=@('UpdateRockets','UpdateThrusterCache')
 'KSA.ModuleBase'=@('InstanceId','ModuleBase(','SaveDataBase')
 'KSA.Module`1'=@('public int Add','public bool Remove','MoveModule')
 'KSA.ModuleStateful`4'=@('public void Add(','public void AddNew','StateUpdater(','public void Prepare','public void Dispose','public bool MoveNext')
 'KSA.FlightComputer'=@('VehicleConfigInfo','ReadUpdatedVehicleConfiguration','SelectJetsToFire')
 'KSA.VehicleUpdateData'=@('VehicleUpdateData(','public void Prepare')
 'KSA.VehicleUpdateState'=@('PrepareFromWorker','UpdateActiveNozzles','Dispose')
 'KSA.Combustor'=@('ComputePropellantAvailable','ConsumePropellant','RecreateManager')
 'KSA.RocketCore'=@('UpdateState(')
 'KSA.Part'=@('GetReferenceWithChildren','RestoreSymmetryLinks','RegenerateWholePartInstanceTree')
 'KSA.PartTree'=@('TransferPart(','RebuildResourceGraphs','RecreateResourceManagers','RecomputeRocketControls')
 'KSA.ConnectorCapability'=@('enum ConnectorCapability')
}
$result=@()
foreach($entry in $targets.GetEnumerator()) {
 $lines=@(& $tool -t $entry.Key $binary 2>$null)
 if($LASTEXITCODE -ne 0){throw "Inspection failed: $($entry.Key)"}
 $normalized=($lines -join "`n")+"`n"
 $hash=[Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($normalized))).ToLowerInvariant()
 $anchors=@()
 foreach($term in $entry.Value){for($i=0;$i -lt $lines.Count;$i++){if($lines[$i].Contains($term)){$anchors+=@{symbol=$term;line=$i+1}}}}
 $result+=@{type=$entry.Key;normalizedSourceSha256=$hash;lineCount=$lines.Count;anchors=$anchors}
}
$identity=Get-Item -LiteralPath $binary
@{observedUtc=[DateTime]::UtcNow.ToString('o');installedPath=$binary;version=$identity.VersionInfo.ProductVersion;bytes=$identity.Length;sha256=(Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash.ToLowerInvariant();contentPath='E:\Kitten Space Agency\Content\Core\CorePropulsionBGameData.xml';contentSha256=(Get-FileHash -LiteralPath 'E:\Kitten Space Agency\Content\Core\CorePropulsionBGameData.xml' -Algorithm SHA256).Hash.ToLowerInvariant();tool=$tool;toolVersion='11.0.0.9375';normalization='stdout lines joined LF with final LF, UTF-8; source text held only in memory';sourceTextRetained=$false;ksaWrites=0;types=$result}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'current-installed-boundaries.json') -Encoding utf8
