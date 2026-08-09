using NovaCore.NaifEphemerisAdapter;
if(args.Length==1&&args[0].StartsWith("--build-lunar-orientation-pack=",StringComparison.Ordinal))
{
    var destination=Path.GetFullPath(args[0]["--build-lunar-orientation-pack=".Length..]);
    if(!LunarOrientationPackBuilder.TryBuild(Environment.CurrentDirectory,destination,out var report,out var error))throw new InvalidOperationException(error);
    Console.WriteLine(report);
    return;
}
Console.WriteLine($"NAIF adapter contracts only; policy=0x{NaifContracts.HashPolicy():X16}");
