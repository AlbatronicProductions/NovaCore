param(
    [Parameter(Mandatory=$true)][string]$OutputPath,
    [Parameter(Mandatory=$true)][string]$SourceDescription,
    [ValidateSet('AffectedHost','IndependentCandidate','SandboxScreening')]
    [string]$SourceKind = 'IndependentCandidate'
)
# Read-only collection. Does not elevate, adjust privileges, start/stop services,
# change policy/ACLs, download roots, or exercise trust servicing.
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $OutputPath) { throw 'Output already exists; preserve prior evidence.' }
if (-not ('AuthRootTokenRead' -as [type])) {
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
public static class AuthRootTokenRead {
 [StructLayout(LayoutKind.Sequential)] struct SidAttributes { public IntPtr Sid; public uint Attributes; }
 [StructLayout(LayoutKind.Sequential)] struct GroupHeader { public uint Count; public SidAttributes First; }
 [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr OpenProcess(uint access, bool inherit, uint id);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 [DllImport("advapi32.dll", SetLastError=true)] static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
 [DllImport("advapi32.dll", SetLastError=true)] static extern bool GetTokenInformation(IntPtr token,int kind,IntPtr info,uint size,out uint needed);
 public sealed class Group { public string SID; public string Mask; public bool Enabled; public bool EnabledByDefault; public bool DenyOnly; public bool Owner; }
 public sealed class Result { public uint ProcessId; public string Status; public string ErrorOperation; public int ErrorCode; public string UserSID; public Group[] Groups; public Group[] RestrictedSIDs; }
 static IntPtr Read(IntPtr token,int kind) {
  uint size; GetTokenInformation(token,kind,IntPtr.Zero,0,out size);
  if(size==0 || size>1048576) throw new Win32Exception(Marshal.GetLastWin32Error());
  IntPtr buffer=Marshal.AllocHGlobal((int)size);
  if(!GetTokenInformation(token,kind,buffer,size,out size)) { int error=Marshal.GetLastWin32Error(); Marshal.FreeHGlobal(buffer); throw new Win32Exception(error); }
  return buffer;
 }
 static Group[] Groups(IntPtr token,int kind) {
  IntPtr buffer=Read(token,kind);
  try {
   int count=Marshal.ReadInt32(buffer); int offset=(int)Marshal.OffsetOf(typeof(GroupHeader),"First");
   int stride=Marshal.SizeOf(typeof(SidAttributes)); var groups=new List<Group>();
   for(int i=0;i<count;i++) {
    var item=(SidAttributes)Marshal.PtrToStructure(IntPtr.Add(buffer,offset+i*stride),typeof(SidAttributes));
    groups.Add(new Group{SID=new SecurityIdentifier(item.Sid).Value,Mask="0x"+item.Attributes.ToString("X8"),Enabled=(item.Attributes&4)!=0,EnabledByDefault=(item.Attributes&2)!=0,DenyOnly=(item.Attributes&16)!=0,Owner=(item.Attributes&8)!=0});
   }
   return groups.ToArray();
  } finally { Marshal.FreeHGlobal(buffer); }
 }
 public static Result Capture(uint id) {
  var result=new Result{ProcessId=id,Status="UNAVAILABLE"}; IntPtr process=IntPtr.Zero,token=IntPtr.Zero; string operation="OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION)";
  try {
   process=OpenProcess(0x1000,false,id); if(process==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
   operation="OpenProcessToken(TOKEN_QUERY)";
   if(!OpenProcessToken(process,8,out token)) throw new Win32Exception(Marshal.GetLastWin32Error());
   operation="GetTokenInformation(TokenUser/TokenGroups/TokenRestrictedSids)";
   IntPtr user=Read(token,1);
   try {result.UserSID=new SecurityIdentifier(Marshal.ReadIntPtr(user)).Value;} finally {Marshal.FreeHGlobal(user);}
   result.Groups=Groups(token,2); result.RestrictedSIDs=Groups(token,11); result.Status="OBSERVED";
  } catch(Win32Exception error) {result.ErrorOperation=operation;result.ErrorCode=error.NativeErrorCode;}
  finally {if(token!=IntPtr.Zero)CloseHandle(token);if(process!=IntPtr.Zero)CloseHandle(process);}
  return result;
 }
}
'@
}
$serviceSid = 'S-1-5-80-242729624-280608522-2219052887-3187409060-2225943459'
$base = 'HKLM:\SOFTWARE\Microsoft\SystemCertificates\AuthRoot'
$keys = @(foreach ($path in @($base,"$base\AutoUpdate","$base\Certificates","$base\CRLs","$base\CTLs")) {
    if (-not (Test-Path -LiteralPath $path)) { [pscustomobject]@{Key=$path;Status='ABSENT'}; continue }
    $acl=Get-Acl -LiteralPath $path
    $raw=[Security.AccessControl.RawSecurityDescriptor]::new($acl.GetSecurityDescriptorBinaryForm(),0)
    $aces=@(for($i=0;$i -lt $raw.DiscretionaryAcl.Count;$i++) {
        $ace=$raw.DiscretionaryAcl[$i]
        [pscustomobject]@{Order=$i;Type=$ace.AceType.ToString();SID=$ace.SecurityIdentifier.Value;AccessMask=('0x{0:X8}' -f $ace.AccessMask);AceFlags=$ace.AceFlags.ToString();Inherited=$ace.IsInherited;InheritanceFlags=($ace.AceFlags -band [Security.AccessControl.AceFlags]'ContainerInherit,ObjectInherit').ToString();PropagationFlags=($ace.AceFlags -band [Security.AccessControl.AceFlags]'NoPropagateInherit,InheritOnly').ToString()}
    })
    [pscustomobject]@{Key=$path;Status='OBSERVED';OwnerSID=$raw.Owner.Value;GroupSID=$raw.Group.Value;Protected=$acl.AreAccessRulesProtected;Canonical=$acl.AreAccessRulesCanonical;ControlFlags=$raw.ControlFlags.ToString();SDDL=$acl.GetSecurityDescriptorSddlForm([Security.AccessControl.AccessControlSections]'Owner,Group,Access');ACEs=$aces;CryptSvcACEs=@($aces | Where-Object SID -eq $serviceSid);NetworkServiceACEs=@($aces | Where-Object SID -eq 'S-1-5-20')}
})
$os=Get-CimInstance Win32_OperatingSystem
$computer=Get-CimInstance Win32_ComputerSystem
$version=Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
$service=Get-CimInstance Win32_Service -Filter "Name='CryptSvc'"
$policy=@(foreach($path in @('HKLM:\SOFTWARE\Policies\Microsoft\SystemCertificates\AuthRoot','HKLM:\SOFTWARE\Microsoft\SystemCertificates\AuthRoot\AutoUpdate')) {
    if(Test-Path -LiteralPath $path) {
        $values=Get-ItemProperty -LiteralPath $path
        foreach($name in @('DisableRootAutoUpdate','EnableDisallowedCertAutoUpdate','RootDirUrl')) {
            $property=$values.PSObject.Properties[$name]
            [pscustomobject]@{Path=$path;Name=$name;Present=($null -ne $property);Value=if($property){$property.Value}else{$null}}
        }
    } else {[pscustomobject]@{Path=$path;Status='ABSENT'}}
})
$registration=@(& "$env:SystemRoot\System32\dsregcmd.exe" /status | Select-String -Pattern '^\s*(AzureAdJoined|EnterpriseJoined|DomainJoined|WorkplaceJoined|MdmUrl)\s*:' | ForEach-Object {$_.Line.Trim()})
$autoUpdate=Get-ItemProperty -LiteralPath "$base\AutoUpdate" -ErrorAction SilentlyContinue
$ctlIdentity=if($autoUpdate -and $autoUpdate.EncodedCtl) {
    $hasher=[Security.Cryptography.SHA256]::Create()
    try {[pscustomobject]@{Bytes=$autoUpdate.EncodedCtl.Length;SHA256=([BitConverter]::ToString($hasher.ComputeHash($autoUpdate.EncodedCtl))).Replace('-','')}} finally {$hasher.Dispose()}
} else {'UNAVAILABLE'}
$selfToken=[AuthRootTokenRead]::Capture([uint32]$PID)
if($selfToken.Status -ne 'OBSERVED' -or $selfToken.UserSID -ne [Security.Principal.WindowsIdentity]::GetCurrent().User.Value) {throw 'Collector token self-check failed'}
$serviceToken=if($service.ProcessId -gt 0){[AuthRootTokenRead]::Capture([uint32]$service.ProcessId)}else{'UNAVAILABLE: service not running'}
[pscustomobject]@{
    Observed=(Get-Date -Format o);Computer=$env:COMPUTERNAME;SourceDescription=$SourceDescription;SourceKind=$SourceKind
    HealthyQualification='NOT ESTABLISHED BY THIS READ-ONLY COLLECTION; requires source provenance and successful normal trust-servicing evidence'
    Windows=[pscustomobject]@{Caption=$os.Caption;EditionID=$version.EditionID;DisplayVersion=$version.DisplayVersion;Build=$version.CurrentBuild;UBR=$version.UBR;Architecture=$os.OSArchitecture;Domain=$computer.Domain;PartOfDomain=$computer.PartOfDomain;JoinAndMDMIndicators=$registration;MDMLimit='Indicators do not prove every possible enrollment/policy source absent'}
    CryptSvc=[pscustomobject]@{Configuration=($service | Select-Object Name,ProcessId,StartName,StartMode,State,PathName);SIDType=@(& sc.exe qsidtype CryptSvc);SID=@(& sc.exe showsid CryptSvc);Token=$serviceToken;TokenLimit='Primary process token only; no claim about an impersonating thread token at the earlier denial'}
    Collector=[pscustomobject]@{PowerShell=$PSVersionTable.PSVersion.ToString();TokenSelfCheck='PASS';Elevated=([Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator);SHA256=(Get-FileHash -LiteralPath $PSCommandPath).Hash}
    RootPolicy=$policy;RegistryCTL=$ctlIdentity;ACLSections='Owner,Group,DACL; SACL not requested';Keys=$keys
} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
