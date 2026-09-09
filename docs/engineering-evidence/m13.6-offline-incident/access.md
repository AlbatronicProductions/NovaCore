# Protected incident artifacts: supported access result

This ticket tested ordinary read access and one standard Windows administrator-consent path. It did not change ACLs, take ownership, enable backup privileges, alter UAC, use another account token, modify registry state or launch NovaCore.

## Readable facts and remaining limits

- `whoami /all` reports Medium Mandatory Level, with Administrators membership marked **deny only**. This is a filtered token; it does not grant administrator access to protected artifacts. [Microsoft UAC token model](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/user-account-control/how-it-works).
- Reading the security descriptor of `C:\Windows\LiveKernelReports`, its `WATCHDOG` child and the exact dump returns `UnauthorizedAccessException`. The dump's owner and exact denying/missing ACE cannot be resolved from this token. Do not report an imagined SYSTEM-only dump ACL as observed fact.
- The parent `C:\ProgramData\Microsoft\Windows\WER\ReportQueue` is owned by `NT AUTHORITY\SYSTEM`. Its readable protected DACL grants Administrators full access and other principals limited rights. This is the **parent** descriptor, not proof of the incident child descriptor.
- Reading the incident `Report.wer` security descriptor also returns `UnauthorizedAccessException`. Its owner and effective child ACEs remain unavailable.

Readable ReportQueue SDDL:

```text
O:SYG:SYD:P(A;OICI;0x13019f;;;SU)(A;OICI;0x13019f;;;AU)(A;OICI;0x13019f;;;LS)(A;OICI;0x13019f;;;NS)(A;OICI;0x13019f;;;WR)(A;OICI;FA;;;BA)(A;OICI;0x13019f;;;AC)(A;OICI;0x13019f;;;S-1-15-2-2)
```

Filtered-token access is a demonstrated boundary consistent with the denial. The exact child ACL reason is unverified until an authorized elevated read succeeds. Event Viewer/WER event payloads provide an existing readable metadata path; Work A exhausted that bounded route and found no new process, engine or module attribution.

## Exact artifacts

```text
C:\Windows\LiveKernelReports\WATCHDOG\WATCHDOG-20260908-0013.dmp
C:\ProgramData\Microsoft\Windows\WER\ReportQueue\Kernel_141_8fd8944c0573f6a90fdeed091b6736e541e5bff_00000000_5880d346-5ecd-4c93-a8eb-576e32823930\Report.wer
```

For both artifacts: original bytes, size, SHA-256, dump header/streams, kernel modules, TDR context, process/command association and stack were **not read**. No offline dump analysis is claimed.

## Concrete supported export and actual outcome

`export-incident.ps1` is a parsed, fixed-path helper. It queries the two files' metadata/ACL/hash, permits only bounded copies under `E:\NovaCore\build\m13.6-offline-incident`, verifies copy hashes and writes a compact manifest to this evidence package. It refuses existing destinations, checks workspace paths/reparse points and preserves source files/security. Total copy limit is 256 MiB: at most 255 MiB for the dump and 1 MiB for Report.wer. Oversized originals receive metadata-only treatment. Its retained SHA-256 is recorded by closeout.

One attempt used `Start-Process` with the ordinary `RunAs` verb, `-WindowStyle Hidden`, signed Windows PowerShell, `-NoProfile -NonInteractive -File` and the exact helper. Windows returned:

```text
The operation was canceled by the user.
InvalidOperationException,Microsoft.PowerShell.Commands.StartProcessCommand
```

This records the OS result, not an inference about why consent was unavailable or who dismissed it. The process launch failed, no helper output or scratch directory exists, and zero bytes were copied. No automatic retry was attempted. This was not an automatic tool-approval review rejection.

If Project Control later chooses to provide authorized administrator consent, the prepared helper is the concrete reviewable action. It can be run from a user-opened elevated Windows PowerShell:

```powershell
& 'E:\NovaCore\docs\engineering-evidence\m13.6-offline-incident\export-incident.ps1'
```

That is a future optional evidence step, not a command that succeeded here. Readable dump evidence should be reviewed before proceeding to the instrumented runtime protocol. A successful export still does not establish a driver defect or NovaCore cause by itself.

## Available offline debugger tools

No `cdb`, `windbg`, `WinDbgX` or `dumpchk` command was found on PATH. The installed Windows SDK root is `C:\Program Files (x86)\Windows Kits\10`; it contains no Debuggers directory. WinDbg package/alias searches in the installed Appx and user WindowsApps locations returned no match. A focused search of retained engineering/tooling recipes found no current debugger executable path.

No debugger was downloaded or installed. These are bounded discovery results, not a claim that no debugger exists anywhere on any disk. Because the original bytes remain unreadable, downloading more analysis software would not resolve the current access boundary. There is no invented module/stack result.
