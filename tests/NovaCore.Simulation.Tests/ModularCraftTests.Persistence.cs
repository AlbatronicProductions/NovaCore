using System.Collections.Immutable;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    internal static void StarterPersistenceMeasurements()
    {
        var catalog=StarterCatalog();var rows=new List<object>();
        var directory=Path.Combine("build","modular-craft-first-playable","gate6-measurements",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        object Distribution(IEnumerable<double> input){var v=input.Order().ToArray();double P(double p)=>v[(int)Math.Ceiling(p*v.Length)-1];return new{median=P(.5),p95=P(.95),p99=P(.99),maximum=v[^1]};}
        foreach(var longer in new[]{false,true}){
            var source=StarterCraft(catalog,longer).Save();var path=Path.Combine(directory,longer?"long.craft.json":"short.craft.json");File.WriteAllBytes(path,source);
            foreach(var operation in new[]{"serialize","atomic-save","load"})for(var window=0;window<3;window++){
                var time=new List<double>();var allocation=new List<double>();var collections=new int[3];
                for(var sample=-2;sample<16;sample++){
                    using var editor=new ConstructionEditorSession(catalog);if(operation!="load")editor.Load(0,source);
                    var gc=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};var bytes=GC.GetAllocatedBytesForCurrentThread();var start=System.Diagnostics.Stopwatch.GetTimestamp();
                    if(operation=="load")editor.LoadFrom(0,path);else if(operation=="atomic-save")editor.SaveTo(editor.Revision,path);else _=editor.Save(editor.Revision);
                    var elapsed=System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;var used=GC.GetAllocatedBytesForCurrentThread()-bytes;
                    if(sample>=0){time.Add(elapsed);allocation.Add(used);for(var gen=0;gen<3;gen++)collections[gen]+=GC.CollectionCount(gen)-gc[gen];}
                    if(!editor.Save(editor.Revision).SequenceEqual(source))throw new InvalidDataException("Persistence measurement changed document.");
                }
                rows.Add(new{craft=longer?"long":"short",operation,window,samples=16,milliseconds=Distribution(time),allocatedBytes=Distribution(allocation),gc=collections,documentBytes=source.Length});
            }
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{scope="Cold persistence; normal GC; equivalent fresh sessions; setup excluded; actual atomic filesystem writes; report only",rows},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
    }
    internal static void StarterPersistenceGate()
    {
        checks=0;var catalog=StarterCatalog();var source=catalog.Save();
        var directory=Path.Combine("build","modular-craft-first-playable","gate6-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        foreach(var longer in new[]{false,true})foreach(var fraction in new[]{0d,.375,1d}){
            var craft=StarterCraft(catalog,longer);using var s=new ConstructionEditorSession(catalog);s.Load(0,craft.Save());
            s.ConfigureSelection(s.Revision,"tank",fraction,.425,fraction!=.375,false);s.ConfigureSelection(s.Revision,"core",1,.425,true,false);
            s.ConfigureSelection(s.Revision,craft.Data.Symmetry.Single().Members[5].Part,1,1,true,false);
            s.SetName(s.Revision,(longer?"Long":"Short")+" development "+fraction);
            var expected=s.Save(s.Revision);var expectedData=s.Current!.Design.Data;
            var path=Path.Combine(directory,(longer?"long":"short")+"-"+fraction.ToString(System.Globalization.CultureInfo.InvariantCulture)+".craft.json");
            s.SaveTo(s.Revision,path);Check(!s.Dirty&&File.ReadAllBytes(path).SequenceEqual(expected),"atomic exact player file");
            s.Clear(s.Revision);s.LoadFrom(s.Revision,path);
            Check(s.Save(s.Revision).SequenceEqual(expected)&&!s.Dirty,"short/long new/load exact canonical bytes");
            Check(s.Current!.Design.Parts.Length==12&&s.Current.Design.Connections.Length==11&&s.Current.Design.Data.Symmetry.Single().Members.Length==8,"complete twelve-part topology");
            Check(s.Current.Design.Data.Id==expectedData.Id&&s.Current.Design.Data.Revision==expectedData.Revision&&s.Current.Design.Data.DependencyDigest==expectedData.DependencyDigest,"document and dependencies pinned");
            Check(s.Current.Design.Data.Configuration.Single(c=>c.Part=="tank").Stores.Sum(q=>q.QuantityKg)==fraction*(longer?1600:800),"load never refills finite quantities");
            Check(s.Current.Design.Data.Configuration.Single(c=>c.Part=="core").Electrical.Single(e=>e.Module=="battery").ChargeJ==38250,"load never recharges finite battery");
            s.SetName(s.Revision,"Edited");s.Undo(s.Revision);Check(!s.Dirty&&s.Save(s.Revision).SequenceEqual(expected),"saved identity undo restores exact group and configuration");
            s.Redo(s.Revision);var changed=s.Save(s.Revision);
            using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))RefuseIo(s,()=>s.SaveTo(s.Revision,path),"atomic locked file refusal");
            Check(File.ReadAllBytes(path).SequenceEqual(expected)&&s.Save(s.Revision).SequenceEqual(changed),"failed save neither replaces source nor reverts edit");
            foreach(var malformed in new[]{expectedData with {DependencyDigest=new string('0',64)},expectedData with {Instances=expectedData.Instances.SetItem(0,expectedData.Instances[0] with {Definition=expectedData.Instances[0].Definition with {Digest=new string('0',64)}})},expectedData with {Configuration=expectedData.Configuration.Select(c=>c.Part=="tank"?c with {Stores=[]}:c).ToImmutableArray()}}){
                var bad=Path.Combine(directory,"bad.craft.json");File.WriteAllBytes(bad,AssemblyJson.Write(malformed));
                RefuseEdit(s,()=>s.LoadFrom(s.Revision,bad,true),"unresolved/tampered/incomplete file refusal");
            }
            var reordered=AssemblyDefinitionCatalog.Compile(catalog.Data with {Definitions=catalog.Data.Definitions.Reverse().ToImmutableArray(),Resources=catalog.Data.Resources.Reverse().ToImmutableArray()});
            using var reload=new ConstructionEditorSession(reordered);reload.LoadFrom(0,path);
            Check(reload.Save(reload.Revision).SequenceEqual(expected),"catalog enumeration cannot substitute saved dependencies");
        }
        Check(catalog.Save().SequenceEqual(source),"all save/refusal paths preserve source catalog");
        Check(!Directory.EnumerateFiles(directory,"*.tmp").Any(),"atomic IO leaves no owned temporary files");
        Console.WriteLine($"Modular Gate 6 persistence PASS: {checks} checks; witnesses {Path.GetFullPath(directory)}");
    }
}
