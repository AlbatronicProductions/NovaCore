using System.Diagnostics;
using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class AssemblyConstructionTests
{
    private sealed record Measurement(string Name,int Samples,double MedianUs,double P95Us,double P99Us,double MaxUs,
        long AllocatedBytes,double BytesPerOperation,int Gen0,int Gen1,int Gen2,int LongestP95Run);
    private static Measurement Measure(string name,Action operation,int count=128,int warmup=12)
    {
        for(var i=0;i<warmup;i++)operation();
        var elapsed=new double[count];var before=GC.GetAllocatedBytesForCurrentThread();
        var g0=GC.CollectionCount(0);var g1=GC.CollectionCount(1);var g2=GC.CollectionCount(2);
        for(var i=0;i<count;i++){var start=Stopwatch.GetTimestamp();operation();elapsed[i]=Stopwatch.GetElapsedTime(start).TotalMicroseconds;}
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        var ordered=elapsed.Order().ToArray();double Percentile(double p)=>ordered[Math.Clamp((int)Math.Ceiling(count*p)-1,0,count-1)];
        var p95=Percentile(.95);var longest=0;var run=0;foreach(var v in elapsed){run=v>p95?run+1:0;longest=Math.Max(longest,run);}
        return new(name,count,Percentile(.5),p95,Percentile(.99),ordered[^1],allocated,(double)allocated/count,
            GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2,longest);
    }
    internal static void MeasureDefinitions(string assetRoot)
    {
        var bytes=File.ReadAllBytes(Path.Combine(Root,"assets/vehicles/development/development-catalog.json"));var catalog=AssemblyDefinitionCatalog.Load(bytes);
        var results=new List<Measurement>();
        for(var window=0;window<3;window++)results.Add(Measure("catalog-load-window-"+window,()=>AssemblyDefinitionCatalog.Load(bytes)));
        results.Add(Measure("catalog-serialize",()=>catalog.Save()));
        results.Add(Measure("cold-asset-content-resolution",()=>{var resolver=new ConstructionAssetLibrary();foreach(var part in catalog.Data.Definitions)resolver.Resolve(assetRoot,part.Construction!.Asset);},16,2));
        var cached=new ConstructionAssetLibrary();foreach(var part in catalog.Data.Definitions)cached.Resolve(assetRoot,part.Construction!.Asset);
        results.Add(Measure("cached-asset-reference-validation",()=>{foreach(var part in catalog.Data.Definitions)cached.Resolve(assetRoot,part.Construction!.Asset);},256));
        Console.WriteLine(JsonSerializer.Serialize(new {scope="Stage 1; no runtime service evolution claimed",catalog.Digest,CatalogBytes=bytes.Length,
            DefinitionCount=catalog.Data.Definitions.Length,BoundPerAssetBytes=ConstructionAssetLibrary.MaximumAssetBytes,BoundSessionBytes=ConstructionAssetLibrary.MaximumSessionBytes,Results=results},new JsonSerializerOptions{WriteIndented=true}));
    }
    internal static void MeasureModularDefinitions()
    {
        const string root="assets/vehicles/modular-starter";
        var bytes=File.ReadAllBytes(Path.Combine(root,"catalog.json"));var catalog=AssemblyDefinitionCatalog.Load(bytes);
        var rows=new List<Measurement>();
        for(var window=0;window<3;window++)rows.Add(Measure("six-part-catalog-parse-validate-hash-"+window,()=>AssemblyDefinitionCatalog.Load(bytes)));
        rows.Add(Measure("six-part-catalog-serialize",()=>catalog.Save()));
        rows.Add(Measure("six-asset-cold-file-hash-node-validation",()=>{var resolver=new ConstructionAssetLibrary();foreach(var d in catalog.Data.Definitions)resolver.Resolve(root,d.Construction!.Asset);},64,2));
        var retained=new ConstructionAssetLibrary();foreach(var d in catalog.Data.Definitions)retained.Resolve(root,d.Construction!.Asset);
        rows.Add(Measure("six-asset-cached-identity-validation",()=>{foreach(var d in catalog.Data.Definitions)retained.Resolve(root,d.Construction!.Asset);},256));
        Console.WriteLine(JsonSerializer.Serialize(new {scope="Gate 4 cold immutable content; report-only, not render/runtime timing",catalog.Digest,CatalogSourceBytes=bytes.Length,
            CanonicalCatalogBytes=catalog.Save().Length,ContentFiles=Directory.GetFiles(root).Length,ContentBytes=Directory.GetFiles(root).Sum(p=>new FileInfo(p).Length),
            RetainedAssetBytes=catalog.Data.Definitions.Sum(d=>new FileInfo(Path.Combine(root,d.Construction!.Asset.RelativePath)).Length),Results=rows},new JsonSerializerOptions{WriteIndented=true}));
    }
    internal static void MeasureGraph()
    {
        var design=StockDevelopment();var data=design.Data;var bytes=design.Save();
        var rows=new List<Measurement>();
        for(var window=0;window<3;window++)rows.Add(Measure("graph-compile-window-"+window,()=>CompiledConstructionDesign.Compile(design.Catalog,data)));
        rows.Add(Measure("design-serialize",()=>design.Save()));
        rows.Add(Measure("design-load",()=>CompiledConstructionDesign.Load(design.Catalog,bytes)));
        rows.Add(Measure("logical-partition-membership",()=>design.Partition("DLV.Connection.BoosterUpper.Lower.Release")));
        Console.WriteLine(JsonSerializer.Serialize(new {scope="Stage 2 topology; service network solver not included",design.Digest,DesignBytes=bytes.Length,Results=rows},new JsonSerializerOptions{WriteIndented=true}));
    }
    internal static void MeasureFuel()
    {
        var dlv=StockDevelopment();var local=LocalFuel([("f","F",1,true),("o","O",1,true)],
            [Consumer("mix",5,[new("F",2),new("O",3)],["f","o"])]);
        var initial=local.Initial();bool[] on=[true],off=[false];var saved=ConstructionFuelSolver.Advance(initial,1_000_000,on).State.Save();
        var rows=new List<Measurement>{Measure("fuel-network-DLV-cold",()=>ConstructionFuelNetwork.Compile(dlv)),
            Measure("fuel-network-nonDLV-cold",()=>ConstructionFuelNetwork.Compile(local.Design))};
        for(var window=0;window<3;window++)rows.Add(Measure("active-exact-mixture-event-window-"+window,()=>ConstructionFuelSolver.Advance(initial,1_000_000,on)));
        rows.Add(Measure("active-no-event",()=>ConstructionFuelSolver.Advance(initial,1,on)));
        rows.Add(Measure("warmed-no-demand",()=>ConstructionFuelSolver.Advance(initial,1,off),1024));
        rows.Add(Measure("rational-save-restore",()=>ConstructionFuelState.Load(local,saved)));
        Console.WriteLine(JsonSerializer.Serialize(new {scope="Stage 3 pure accounting kernel; no canonical runtime or DLV ignition",local.Digest,
            StateBytes=saved.Length,local.QuantityBits,local.RateBits,local.ScratchBits,Results=rows},new JsonSerializerOptions{WriteIndented=true}));
    }
    internal static void MeasurePower()
    {
        var modules=System.Collections.Immutable.ImmutableArray.Create(new ElectricalDefinition("battery",ElectricalRole.Battery,10,0,null),
            new ElectricalDefinition("solar",ElectricalRole.SolarGenerator,0,4,null),new ElectricalDefinition("load",ElectricalRole.Load,0,3,null));
        var net=LocalPower(modules,[new("battery",0,true),new("solar",0,true),new("load",0,true)]);var state=net.Initial();
        var fuel=ConstructionFuelSolver.Advance(net.Fuel.Initial(),1_000_000,[]);var saved=state.Save();
        var dlv=ConstructionFuelNetwork.Compile(StockDevelopment());var idleNet=ConstructionPowerNetwork.Compile(dlv);var idle=idleNet.Initial();
        var idleFuel=ConstructionFuelSolver.Advance(dlv.Initial(),1_000_000,new bool[dlv.Consumers.Length]);
        var rows=new List<Measurement>{Measure("power-data-DLV-cold",()=>ConstructionPowerNetwork.Compile(dlv)),Measure("power-data-nonDLV-cold",()=>ConstructionPowerNetwork.Compile(net.Fuel))};
        for(var window=0;window<3;window++)rows.Add(Measure("power-generation-load-window-"+window,()=>ConstructionPowerSolver.Advance(state,1_000_000,fuel)));
        rows.Add(Measure("power-warmed-no-demand-DLV",()=>ConstructionPowerSolver.Advance(idle,1_000_000,idleFuel),1024));
        rows.Add(Measure("power-save-restore",()=>ConstructionPowerState.Load(net,saved)));
        Console.WriteLine(JsonSerializer.Serialize(new {scope="Stage 4 pure energy kernel; no canonical runtime, DLV roles intentionally unparameterized",net.Digest,StateBytes=saved.Length,net.IntegerBits,net.ScratchBits,Results=rows},new JsonSerializerOptions{WriteIndented=true}));
    }
}
