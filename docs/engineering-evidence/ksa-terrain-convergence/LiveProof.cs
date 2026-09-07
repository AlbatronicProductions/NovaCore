using System.Reflection;
var root=Path.GetFullPath(args[0]);var output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
var assembly=Assembly.Load("NovaCore.Graphics.Tests");
object? Call(string type,string method,params object?[] values)
{
    try{return assembly.GetType(type,true)!.GetMethod(method,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,values);}
    catch(TargetInvocationException e){throw e.InnerException!;}
}
using var environment=(IDisposable)Activator.CreateInstance(assembly.GetType("VulkanValidationEnvironment",true)!,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{false},null)!;
var sample=(string)Call("WindowLifecycleTests","VerifyDeployment",root)!;
Call("RegionalPhysicalResidencyTests","VerifyPreparationScheduling",root);
var log=(string)Call("RegionalPhysicalResidencyTests","RunSample",sample,root,output,
    "--scene=sol --focus=earth --surface-site=florida-launch --solar-epoch=j2000 --benchmark-frames=1000 --log=vulkan --log=validation","regional")!;
foreach(var value in new[]{"requests=670;","loaded=670;","uploadedBytes=93392640;","body=8; earthEligible=False","body=10; earthEligible=False"})
    if(!log.Contains(value))throw new Exception("Missing live evidence: "+value);
Call("RegionalPhysicalResidencyTests","VerifySlicePublication",log);
Call("RegionalPhysicalResidencyTests","Analyze",root,output);
Console.WriteLine("Candidate live moving Florida contact, residency, owner, publication and strict validation PASS. Bounded raw evidence retained for consolidation at "+output);
