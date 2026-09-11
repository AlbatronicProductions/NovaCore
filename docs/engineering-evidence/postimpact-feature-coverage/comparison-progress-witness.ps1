param([int]$ExpectedAttempts = 1)
# Copy this script into a disposable directory before running; it generates its own build project.
'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>' | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'control.csproj')
$source=Get-Content 'E:\NovaCore\src\NovaCore.Simulation\Spacecraft\Contact\PostImpactCoverageProvider.cs' -Raw
$start=$source.IndexOf('internal FloridaRootOrder Compare(')
$open=$source.IndexOf('{',$start);$level=1;$end=$open+1
while($level -gt 0){if($source[$end] -eq '{'){$level++};if($source[$end] -eq '}'){$level--};$end++}
$method=$source.Substring($start,$end-$start)
$prefix=@'
var refusing=new State(true);var progressing=new State(false);
var left=new NextRoot(refusing);var right=new NextRoot(progressing);
var order=left.Compare(right,default,default,default,default);
Console.WriteLine($"order={order} refusingAttempts={refusing.Attempts} progressingAttempts={progressing.Attempts}");
if(order!=FloridaRootOrder.Unresolved || refusing.Attempts!=EXPECTED)throw new Exception("Comparison refusal attempts");
enum CoverageStatus {NextEventCertified,Unresolved}
enum FloridaRootOrder {Less,Equal,Greater,Unresolved}
readonly record struct PrivateCanonicalState;
readonly record struct FloridaContactUse;
readonly record struct Bound(double Lower,double Upper);
readonly record struct Values(Bound Time);
static class PostImpactCoverageSearch {public const int MaximumRefinements=24;}
sealed class State(bool refuses) {public readonly bool Refuses=refuses;public int Attempts;}
readonly struct NextRoot(State owner,int depth=0) {
readonly State owner=owner;readonly int depth=depth;
CoverageStatus Read(in PrivateCanonicalState state,in FloridaContactUse use,out Values value) {
value=new(new(.25+depth*.001,.75-depth*.001));return CoverageStatus.NextEventCertified;}
CoverageStatus Refine(in PrivateCanonicalState state,in FloridaContactUse use,int steps,out NextRoot result,out int evaluations) {
owner.Attempts++;evaluations=1;result=default;if(owner.Refuses)return CoverageStatus.Unresolved;
result=new(owner,depth+steps);return CoverageStatus.NextEventCertified;}
'@
$program=$prefix.Replace('EXPECTED',"$ExpectedAttempts")+"`n"+$method+"`n}"
Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Program.cs') -Value $program
& dotnet run --project (Join-Path $PSScriptRoot 'control.csproj') -c Debug --nologo
exit $LASTEXITCODE
