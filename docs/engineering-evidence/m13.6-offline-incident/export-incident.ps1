# Read two existing incident artifacts through ordinary Windows administrator consent.
# No ACL/ownership/registry/production change. Output paths and copy limits are fixed.
$ErrorActionPreference = 'Stop'
$m13Workspace = [IO.Path]::GetFullPath('E:\NovaCore')
$m13Evidence = [IO.Path]::GetFullPath('E:\NovaCore\docs\engineering-evidence\m13.6-offline-incident')
$m13Scratch = [IO.Path]::GetFullPath('E:\NovaCore\build\m13.6-offline-incident')
foreach ($m13Target in @($m13Evidence,$m13Scratch)) {
    if (-not $m13Target.StartsWith($m13Workspace + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Outside workspace' }
    $m13Check = $m13Target
    while ($m13Check -and $m13Check.StartsWith($m13Workspace,[StringComparison]::OrdinalIgnoreCase)) {
        if (Test-Path -LiteralPath $m13Check) {
            if ((Get-Item -LiteralPath $m13Check -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse path rejected' }
        }
        $m13Check = [IO.Path]::GetDirectoryName($m13Check)
    }
}
$m13Identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$m13Principal = New-Object Security.Principal.WindowsPrincipal($m13Identity)
$m13Result = [ordered]@{
    capturedUtc = [DateTime]::UtcNow.ToString('o')
    administratorToken = $m13Principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    operation = 'Read existing incident metadata and export bounded copies; no source or security changes'
    copyBudgetBytes = 268435456
    records = @()
}
$m13Sources = @(
    [pscustomobject]@{Path='C:\Windows\LiveKernelReports\WATCHDOG\WATCHDOG-20260908-0013.dmp';Name='WATCHDOG-20260908-0013.dmp';Limit=267386880},
    [pscustomobject]@{Path='C:\ProgramData\Microsoft\Windows\WER\ReportQueue\Kernel_141_8fd8944c0573f6a90fdeed091b6736e541e5bff_00000000_5880d346-5ecd-4c93-a8eb-576e32823930\Report.wer';Name='Report.wer';Limit=1048576}
)
foreach ($m13Source in $m13Sources) {
    $m13Row = [ordered]@{source=$m13Source.Path}
    try {
        $m13Acl = Get-Acl -LiteralPath $m13Source.Path
        $m13Row.owner = $m13Acl.Owner
        $m13Row.sddl = $m13Acl.Sddl
        $m13File = Get-Item -LiteralPath $m13Source.Path -Force
        if ($m13File.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Source reparse file rejected' }
        $m13Row.bytes = $m13File.Length
        $m13Row.lastWriteUtc = $m13File.LastWriteTimeUtc.ToString('o')
        $m13Row.sha256 = (Get-FileHash -LiteralPath $m13Source.Path -Algorithm SHA256).Hash.ToLowerInvariant()
        $m13Input = [IO.File]::Open($m13Source.Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        try {
            $m13Header = New-Object byte[] 128
            $m13Read = $m13Input.Read($m13Header,0,$m13Header.Length)
            $m13Row.firstBytesHex = [BitConverter]::ToString($m13Header,0,$m13Read).Replace('-','')
            if ($m13File.Length -le $m13Source.Limit) {
                if (-not (Test-Path -LiteralPath $m13Scratch)) { New-Item -ItemType Directory -Path $m13Scratch | Out-Null }
                $m13Destination = [IO.Path]::GetFullPath((Join-Path $m13Scratch $m13Source.Name))
                if ([IO.Path]::GetDirectoryName($m13Destination) -ne $m13Scratch) { throw 'Invalid destination' }
                $m13Output = [IO.File]::Open($m13Destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
                try { $m13Input.Position=0; $m13Input.CopyTo($m13Output); $m13Output.Flush($true) }
                finally { $m13Output.Dispose() }
                $m13Row.destination = $m13Destination
                $m13Row.copySha256 = (Get-FileHash -LiteralPath $m13Destination -Algorithm SHA256).Hash.ToLowerInvariant()
                if ($m13Row.copySha256 -ne $m13Row.sha256) { throw 'Copy hash mismatch' }
                $m13Row.status = 'COPIED_VERIFIED'
            } else { $m13Row.status = 'METADATA_ONLY_COPY_LIMIT' }
        } finally { $m13Input.Dispose() }
    } catch { $m13Row.status='UNAVAILABLE'; $m13Row.error=$_.Exception.Message; $m13Row.errorId=$_.FullyQualifiedErrorId }
    $m13Result.records += [pscustomobject]$m13Row
}
$m13ReportPath = Join-Path $m13Evidence 'elevated-export.json'
if (Test-Path -LiteralPath $m13ReportPath) { throw 'Evidence already exists; refusing overwrite' }
$m13Json = $m13Result | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText($m13ReportPath,$m13Json + [Environment]::NewLine,(New-Object Text.UTF8Encoding($false)))
