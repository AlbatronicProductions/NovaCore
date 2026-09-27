using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    internal static void PlacementMeasurements()
    {
        var catalog=StarterCatalog();var fit=new PartCompatibilityEvaluator(catalog);var full=StarterCraft(catalog,false);var fullBytes=full.Save();
        using var preparer=new ConstructionEditorSession(catalog,fit);preparer.Load(0,fullBytes);preparer.Remove(preparer.Revision,full.Data.Symmetry[0].BasePart);var bare=preparer.Save(preparer.Revision);
        var block=catalog.Data.Definitions.Single(d=>d.Id=="nc.rcs.block-r1");var tank=full.Data.Instances.Single(p=>p.Definition.Id=="nc.tank.short-2").Id;
        void Preview(ConstructionEditorSession s,int count)=>s.PreviewPlacement(s.Revision,new(catalog.Reference(block),tank,"radial-0","mount",0,count,"measurement"));
        var jobs=new (string Name,byte[] Input,Action<ConstructionEditorSession>? Prepare,Action<ConstructionEditorSession> Run)[]{
            ("preview-one",bare,null,s=>Preview(s,1)),("preview-eight",bare,null,s=>Preview(s,8)),
            ("refused-eight",fullBytes,null,s=>{try{Preview(s,8);throw new Exception("Expected occupied socket refusal.");}catch(InvalidDataException){}}),
            ("commit-eight",bare,s=>Preview(s,8),s=>s.AcceptPreview(s.Revision)),
            ("group-configuration",fullBytes,null,s=>s.ConfigureSelection(s.Revision,full.Data.Symmetry[0].BasePart,1,1,true,false)),
            ("group-delete",fullBytes,null,s=>s.Remove(s.Revision,full.Data.Symmetry[0].BasePart)),
            ("group-reconnect",fullBytes,null,s=>s.PreviewCraftReconnect(s.Revision,full.Data.Symmetry[0].BasePart,tank,"radial-1",0)),
            ("undo",fullBytes,s=>s.Remove(s.Revision,full.Data.Symmetry[0].BasePart),s=>s.Undo(s.Revision)),
            ("redo",fullBytes,s=>{s.Remove(s.Revision,full.Data.Symmetry[0].BasePart);s.Undo(s.Revision);},s=>s.Redo(s.Revision))};
        var rows=new List<object>();
        foreach(var job in jobs)for(var window=0;window<3;window++){
            var times=new List<double>();var allocations=new List<double>();var gc=new int[3];var results=new HashSet<string>();
            for(var sample=-2;sample<16;sample++){
                using var s=new ConstructionEditorSession(catalog,fit);s.Load(0,job.Input);job.Prepare?.Invoke(s);
                var collections=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};
                var allocated=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();job.Run(s);var time=Stopwatch.GetElapsedTime(start).TotalMicroseconds;var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
                if(sample<0)continue;times.Add(time);allocations.Add(bytes);for(var g=0;g<3;g++)gc[g]+=GC.CollectionCount(g)-collections[g];
                results.Add((s.Current?.Design.Digest??"empty")+"/"+(s.Preview?.Design.Digest??"empty"));
            }
            if(results.Count!=1)throw new Exception("Measurement fixtures produced different outputs: "+job.Name);
            static object Stats(List<double> v){var a=v.Order().ToArray();double P(double p)=>a[(int)Math.Ceiling(p*a.Length)-1];var p95=P(.95);var run=0;var longest=0;foreach(var x in v){run=x>=p95?run+1:0;longest=Math.Max(longest,run);}return new {median=P(.5),p95,p99=P(.99),max=a[^1],longestP95Run=longest};}
            rows.Add(new {job.Name,window,samples=times.Count,microseconds=Stats(times),allocatedBytes=Stats(allocations),gc,result=results.Single()});
        }
        Console.WriteLine(JsonSerializer.Serialize(new {scope="Gate 5 cold operations, equivalent fresh fixtures; report-only; preparation excluded; normal GC",catalog=catalog.Digest,source=full.Digest,rows},new JsonSerializerOptions{WriteIndented=true}));
    }
}
