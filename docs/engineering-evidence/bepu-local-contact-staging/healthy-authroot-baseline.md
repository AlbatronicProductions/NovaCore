# Phase 3: healthy AuthRoot CRLs baseline unavailable

2026-09-12. **AUTHROOT ACL BASELINE — STILL UNQUALIFIED**.
UNBANKED. No host ACL, service configuration or Windows feature changed.

## Baseline

Branch `codex/bepu-local-contact-staging`.
HEAD/main/origin/main: `3933d832b73511a8d08468b7b27a9b2820e2f8ee`.
Windows 11 Pro (`EditionID=Professional`), 25H2, build 26200.9445, x64.
The [phase-2 trace](phase2-authroot-denial.csv) remains the decisive 12-row witness:
CryptSvc PID 5196, RegCreateKey, AuthRoot CRLs, Read/Write and Delete,
ACCESS DENIED. No repeated denial reproduction was run in this phase.

## Healthy baseline source

**None acquired.** Project Control explicitly reported that no independent
baseline is available. Read-only host inventory found:

- Windows Sandbox optional feature disabled; no Sandbox package or executable.
- Hyper-V components and management tools disabled; no Get-VM command or vmms
  service found. Windows Hypervisor Platform is also disabled.
- Virtual Machine Platform is enabled and a hypervisor is present. Those facts
  do not establish an available, healthy Windows guest.
- No VBoxManage, vmrun or qemu-system-x86_64 command found.
- No ISO/VHD/VHDX/VMCX/VMX/VBOX file found in the top level of E:\ or the user's
  Downloads/Documents directories. This was a bounded search, not an exhaustive
  assertion that no media exists elsewhere.

[Machine-readable availability and comparison](phase3-acl-comparison.json)
preserves these results. Win32_OptionalFeature reports disabled as InstallState 2;
[Microsoft documents that mapping](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-optionalfeature).
[Sandbox installation](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-install)
requires administrative feature enablement and may require restarting the host.
No feature enablement, hypervisor installation, VM creation, ISO download or reboot
was initiated. Provisioning is the remaining prerequisite, not an ACL repair.

## Healthy Windows identity

**UNAVAILABLE.** No healthy guest or independent machine was observed. This
affected host is never labeled as the healthy comparator.

## Current CryptSvc identity

[Current host collection](phase3-current-host.json):

- Service account: `NT AUTHORITY\NETWORK SERVICE`, SID `S-1-5-20`.
- Process: PID 5196, `svchost.exe -k NetworkService -p`.
- State/configuration: running, automatic.
- Service SID type: `UNRESTRICTED`.
- Service SID: `S-1-5-80-242729624-280608522-2219052887-3187409060-2225943459`.

One non-elevated, read-only process-token query was attempted.
`OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION)` failed with Win32 error 5.
Live token membership, group attributes and restricted SID membership are
**UNAVAILABLE**. No privilege adjustment, impersonation, elevated retry or
process-permission change was attempted. The collector's own-process token
read/user-identity control passed.

## Healthy CryptSvc identity

**UNAVAILABLE.** Configured service account, SID type, actual enabled service SID
and any restricted SID list still require observation on each comparator.

## Current CRLs ACL

```text
O:SYG:SYD:PAI(A;CIIO;KA;;;CO)(A;CI;KA;;;SY)(A;CI;KA;;;BA)(A;CI;KR;;;BU)(A;CI;KR;;;AC)(A;CI;KR;;;S-1-15-3-1024-1065365936-1281604716-3511738428-1654721687-432734479-3232135806-4053264122-3456934681)
```

Owner/group SYSTEM, protected canonical DACL, six ordered allow ACEs. No explicit
CryptSvc or NETWORK SERVICE ACE. All five collected descriptors match phase 2
exactly. Exact masks, order, inheritance and propagation flags are retained in
the current-host JSON. SACL was not requested.

## Healthy CRLs ACL

**UNAVAILABLE.** No expected SDDL was constructed from the parent or another child.

## Additional baseline ACL

**UNAVAILABLE.** Qualified healthy baselines: **0 of the required 2**.
Earlier parent-only servicing-manifest findings remain insufficient; they were
not repeated or promoted into child-descriptor authority.

## Exact ACL delta

**NOT COMPUTABLE.** The JSON comparison uses null for the unavailable healthy
descriptor and individual differences. Null does not mean matching ACLs or an
empty delta. Current-versus-prior affected-host descriptors have zero drift.

## Service SID analysis

NETWORK SERVICE and NT SERVICE\CryptSvc are distinct principals. A grant to
the latter does not amount to a general NETWORK SERVICE grant.
[Microsoft describes SERVICE_SID_TYPE_UNRESTRICTED](https://learn.microsoft.com/en-us/windows/win32/api/winsvc/ns-winsvc-service_sid_info)
as adding the service SID to the process token when it is created. That configured
behavior is not a substitute for observed live token attributes. Furthermore,
a current primary process token would not prove the effective impersonation-token
state of a thread during the earlier denied operation.

## Root cause decision

**E. ANOTHER PROVEN RESULT:** no healthy comparison baseline could be acquired
from available systems. The CRLs denial remains proven; the expected descriptor,
healthy difference and causal sufficiency of any proposed difference remain
unqualified. No claim of healthy equality, disagreement or ACL corruption is made.

## Repair qualification

**UNQUALIFIED.** No protected permission change is justified by this phase.

## Minimum repair proposal

None. The next Project Control decision concerns provisioning/access to a healthy
baseline. A clean Windows 11 Pro 25H2 VM from
[official Microsoft media](https://www.microsoft.com/en-us/software-download/windows11)
must be updated normally, with exact build and policy context recorded. A Sandbox
on this host would be screening evidence only and still requires an independent
healthy comparison. If two sufficiently equivalent healthy baselines agree, then
compare their complete CRLs descriptors and service-token semantics before
designing the exact one-key repair. No FullControl grant is inferred here.

## Rollback proposal

None because no mutation is proposed. Existing owner/group/DACL evidence remains
preserved; a later authorized repair must capture a fresh descriptor before change.

## Read-only collection prepared

[collect-authroot-baseline.ps1](collect-authroot-baseline.ps1) collects OS edition,
build/architecture, domain/join/MDM indicators, root-auto-update policy values,
CryptSvc configuration and token information where access permits, and all five
AuthRoot descriptors without normalizing ACE order. It writes one new JSON file,
refuses to overwrite existing output, and performs no update, repair or elevation.
Its provenance label never automatically establishes that a machine is healthy.

On a future comparator, copy only this small script, then use a normal PowerShell
session; adjust the example paths to the actual copied location:

```powershell
& 'C:\AuthRootBaseline\collect-authroot-baseline.ps1' `
  -OutputPath 'C:\AuthRootBaseline\healthy-before.json' `
  -SourceDescription 'Record official installation provenance and update history here' `
  -SourceKind IndependentCandidate
```

Use `SandboxScreening` for Sandbox. Do not change execution policy to run the
collector; surface any policy refusal. Any necessary administrative read must
be separately reviewed; the script never elevates itself. Missing token fields
remain explicit rather than inferred.

Successful normal trust servicing, actual EncodedCtl freshness and chain trust
must be independently witnessed on the comparator before calling it healthy.
No NovaCore/Bepu installation is required. Blank MDM indicators alone do not prove
absence of all enrollment or security-policy sources. Preserve pre/post identity
if normal VM updates or servicing occur. Do not retain full VM images as evidence.

The collector was executed successfully on the affected host under PowerShell
7.6.6. Its five SDDLs exactly reproduce phase 2, and its token self-control passed.
It has not been executed on a healthy system. Production validation was not run.

## Security posture

No protected ACL, owner, inheritance, service SID configuration, policy, root,
signature/revocation setting, Windows feature or repository production content
changed. No ProcMon, profiler, trace, service restart or package restore ran.

## NovaCore repository state

501 baseline file identities remain checked with only the previously retained
project-file proposal different from banked production. That proposal remains
SHA-256 `B36A1385F5B603AEFAE79FF910A895AD68F4856B7509DA8EFBB530845563712D`.
All branch/remote/tag refs remain unchanged. Nothing staged or banked.
`git diff --check` passes, with only the existing line-ending warning.

## Architecture status

**ADAPT BEPU BEHIND NOVACORE AUTHORITY** remains unchanged. Adapter implementation
is blocked. No new milestone or production responsibility was started.

## Lead judgment

**ESCALATE TO PROJECT CONTROL**

## Final classification

**AUTHROOT ACL BASELINE — STILL UNQUALIFIED**

UNBANKED. STOP FOR PROJECT CONTROL.
