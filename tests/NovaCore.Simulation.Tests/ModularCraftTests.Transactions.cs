using System.Collections.Immutable;
using System.Text;
using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static partial class ModularCraftTests
{
    private static string EditorFingerprint(ConstructionEditorSession e)=>AssemblyJson.Digest(new {
        Current=e.Current?.Design.Digest,Preview=e.Preview?.Design.Digest,e.Revision,e.Dirty,e.UndoCount,e.RedoCount,e.HistoryPayloadBytes,e.HistoryEvictions,
        SavedDigest=typeof(ConstructionEditorSession).GetField("savedDigest",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(e)});
    private static void RefuseEdit(ConstructionEditorSession e,Action action,string label)
    {var before=EditorFingerprint(e);Reject(action,label);Check(EditorFingerprint(e)==before,label+" preserves complete editor state");}
    private static void RefuseIo(ConstructionEditorSession e,Action action,string label)
    {
        var before=EditorFingerprint(e);var refused=false;
        try{action();}catch(IOException){refused=true;}catch(UnauthorizedAccessException){refused=true;}
        Check(refused&&EditorFingerprint(e)==before,label+" preserves state");
    }
    internal static void CraftTransactionsGate()
    {
        checks=0;var f=CraftFixture(8);var initial=CompiledConstructionDesign.Compile(f.Catalog,f.Data).Save();
        using var editor=new ConstructionEditorSession(f.Catalog);editor.Load(0,initial);
        Check(!editor.Dirty&&editor.UndoCount==1,"load is a clean undoable replacement");
        var candidate=editor.Current!.Design.Data with {Craft=new("Preview",1)};var stale=editor.Revision;
        editor.PreviewEdit(stale,candidate);
        Check(editor.Save(editor.Revision).SequenceEqual(initial)&&!editor.Dirty&&editor.UndoCount==1,"preview isolated from committed state/history");
        RefuseEdit(editor,()=>editor.AcceptPreview(stale),"stale commit");
        RefuseEdit(editor,()=>editor.PreviewEdit(editor.Revision,candidate with {Root="absent"}),"bad replacement preview");
        editor.CancelPreview(editor.Revision);Check(editor.UndoCount==1&&!editor.Dirty,"cancel is not an edit");
        editor.PreviewEdit(editor.Revision,candidate);editor.AcceptPreview(editor.Revision);
        var edited=editor.Save(editor.Revision);
        Check(editor.Dirty&&editor.UndoCount==2&&editor.Current!.Design.Data.Revision==f.Data.Revision+1,"one commit one history entry");
        editor.Undo(editor.Revision);Check(!editor.Dirty&&editor.Save(editor.Revision).SequenceEqual(initial)&&editor.RedoCount==1,"undo exact saved identity clears dirty");
        editor.PreviewEdit(editor.Revision,candidate);editor.CancelPreview(editor.Revision);Check(editor.RedoCount==1,"preview/cancel preserve redo");
        editor.Redo(editor.Revision);Check(editor.Save(editor.Revision).SequenceEqual(edited),"redo restores exact document revision and identity");
        editor.Undo(editor.Revision);RefuseEdit(editor,()=>editor.SetName(editor.Revision,""),"refused edit preserves redo");
        editor.SetName(editor.Revision,"Divergent");Check(editor.RedoCount==0,"successful divergent edit clears redo");
        RefuseEdit(editor,()=>editor.Clear(editor.Revision),"unsaved new protection");
        RefuseEdit(editor,()=>editor.Load(editor.Revision,initial),"unsaved load protection");
        var state=EditorFingerprint(editor);Check(!editor.TryClose(editor.Revision)&&EditorFingerprint(editor)==state,"close cancel protects unsaved work");
        var configs=editor.Current!.Design.Data.Configuration.Where(c=>c.Part!="host").Select(c=>c with {
            Stores=c.Stores.Select(s=>s with {QuantityKg=.25}).ToImmutableArray()}).ToImmutableArray();
        RefuseEdit(editor,()=>editor.SetConfiguration(editor.Revision,configs[0]),"single-member group configuration");
        RefuseEdit(editor,()=>editor.SetConfigurations(editor.Revision,configs.RemoveAt(7)),"7 of 8 group configuration");
        var history=editor.UndoCount;editor.SetConfigurations(editor.Revision,configs);
        Check(editor.UndoCount==history+1&&editor.Current.Design.Data.Configuration.Where(c=>c.Part!="host").All(c=>c.Stores[0].QuantityKg==.25),"whole group one atomic command");
        var groupBytes=editor.Save(editor.Revision);
        RefuseEdit(editor,()=>editor.SetMetadata(editor.Revision,[],[],[]),"metadata path cannot unlink group");
        RefuseEdit(editor,()=>editor.SetConnection(editor.Revision,null!),"null connection edit");
        RefuseEdit(editor,()=>editor.Remove(editor.Revision,null!),"null selected part");
        RefuseEdit(editor,()=>editor.PreviewEdit(editor.Revision,editor.Current!.Design.Data with {Symmetry=[]}),"generic edit cannot unlink group");
        editor.Remove(editor.Revision,"a-member-1");Check(editor.Current!.Design.Parts.Length==1&&editor.Current.Design.Data.Symmetry.IsEmpty,"member removal removes complete group");
        editor.Undo(editor.Revision);Check(editor.Save(editor.Revision).SequenceEqual(groupBytes),"undo restores complete group topology/configuration");
        Task.Run(()=>RefuseEdit(editor,()=>editor.SetName(editor.Revision,"Foreign"),"foreign owner thread")).GetAwaiter().GetResult();
        EditorPersistence(editor,initial);
        var priorClear=editor.Save(editor.Revision);editor.Clear(editor.Revision,true);
        Check(editor.Current is null&&!editor.Dirty,"explicit discard starts clean empty editor");
        editor.Undo(editor.Revision);Check(editor.Save(editor.Revision).SequenceEqual(priorClear),"new/clear replacement is undoable");editor.Redo(editor.Revision);
        var definition=f.Catalog.Data.Definitions.Single(d=>d.Id=="fixture");
        editor.PreviewRoot(editor.Revision,f.Catalog.Reference(definition),"root","root-draft",new(Double3.Zero,Matrix3.Identity));
        editor.AcceptPreview(editor.Revision);editor.Undo(editor.Revision);Check(editor.Current is null,"root placement undo restores empty state");
        editor.Redo(editor.Revision);Check(editor.Current!.Design.Parts.Length==1,"root placement redo restores same identity");
        editor.Remove(editor.Revision,"root");Check(editor.Current is null,"root removal is undoable empty state");
        editor.Undo(editor.Revision);Check(editor.Current is not null,"undo empty restores root");
        editor.Clear(editor.Revision,true);Check(editor.TryClose(editor.Revision),"clean close succeeds");
        RefuseEdit(editor,()=>editor.SetName(editor.Revision,"retired"),"retired session");
        EditorPolicy();EditorHistoryBounds();
        Console.WriteLine($"Modular Gate 3 transactions PASS: {checks} atomic/history/persistence/recovery checks");
    }
    private static void EditorPersistence(ConstructionEditorSession editor,byte[] original)
    {
        var directory=Path.Combine("build","modular-craft-first-playable","gate3-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"craft.json");var recovery=Path.Combine(directory,"recovery.json");
        var export=editor.Save(editor.Revision);Check(editor.Dirty,"byte export never marks saved");
        editor.SaveTo(editor.Revision,path);Check(!editor.Dirty&&File.ReadAllBytes(path).SequenceEqual(export),"successful atomic save marks baseline");
        editor.SetName(editor.Revision,"Unsaved change");
        using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
            RefuseIo(editor,()=>editor.SaveTo(editor.Revision,path),"locked destination refuses replacement");
        Check(File.ReadAllBytes(path).SequenceEqual(export)&&!Directory.EnumerateFiles(directory,"*.tmp").Any(),"failed replacement preserves original file and removes owned temp");
        RefuseEdit(editor,()=>editor.SaveTo(editor.Revision,Path.Combine(directory,"missing","craft.json")),"invalid save directory");
        var bad=Path.Combine(directory,"bad.json");File.WriteAllText(bad,"{\"schema\":\"wrong\"}");
        editor.PreviewEdit(editor.Revision,editor.Current!.Design.Data with {Craft=new("Ghost",1)});
        RefuseEdit(editor,()=>editor.LoadFrom(editor.Revision,bad,true),"failed load retains draft/ghost/history/baseline");
        RefuseEdit(editor,()=>editor.SaveTo(editor.Revision,path),"saving a pending preview refuses");
        editor.CancelPreview(editor.Revision);
        var current=editor.Save(editor.Revision);editor.SaveRecovery(editor.Revision,recovery);
        Check(editor.Dirty,"recovery checkpoint is not user save");
        using(var restored=new ConstructionEditorSession(editor.Catalog)){
            restored.RestoreRecovery(0,recovery);
            Check(restored.Save(restored.Revision).SequenceEqual(current)&&restored.Dirty&&restored.UndoCount==1&&restored.Revision==1,"recovery restores draft and dirty baseline with fresh session authority");
            restored.Undo(restored.Revision);Check(restored.Current is null,"recovery replacement is undoable without restoring old session history");
        }
        var malformed=new CraftRecoveryData(CraftDocumentStore.RecoverySchema,current,"invalid-hash");
        File.WriteAllBytes(bad,AssemblyJson.Write(malformed));
        RefuseEdit(editor,()=>editor.RestoreRecovery(editor.Revision,bad,true),"malformed recovery baseline");
        File.WriteAllBytes(bad,AssemblyJson.Write(malformed with {SavedDigest=null,Draft=Encoding.UTF8.GetBytes("{}") }));
        RefuseEdit(editor,()=>editor.RestoreRecovery(editor.Revision,bad,true),"malformed embedded recovery document");
        File.WriteAllBytes(bad,new byte[CompiledConstructionDesign.MaximumDocumentBytes+1]);
        RefuseEdit(editor,()=>editor.LoadFrom(editor.Revision,bad,true),"bounded file load");
        File.WriteAllText(bad,"Oversize read refused. Regenerate with --modular-gate3.");
        editor.Remove(editor.Revision,editor.Current!.Design.Data.Root);editor.SaveRecovery(editor.Revision,recovery);
        using(var empty=new ConstructionEditorSession(editor.Catalog)){
            empty.RestoreRecovery(0,recovery);Check(empty.Current is null&&empty.Dirty,"empty deletion recovery retains unsaved baseline");
        }
        editor.Load(editor.Revision,original,true);Check(!editor.Dirty&&editor.Preview is null,"successful replacement updates saved baseline");
        editor.Undo(editor.Revision);Check(editor.Current is null,"load replacement restores prior empty draft on undo");editor.Redo(editor.Revision);
        // These tiny diagnostic files are retained under build; no campaign cleanup is performed.
        Console.WriteLine("Gate 3 IO witness directory: "+Path.GetFullPath(directory));
    }
    private static void EditorPolicy()
    {
        var d=DefinitionFixture();d=d with {Standard=d.Standard! with {Configuration=new(false,false,false,false)}};
        var unlocked=d with {Id="unlocked",Standard=d.Standard! with {Configuration=new(true,true,true,true)}};
        var cat=AssemblyDefinitionCatalog.Compile(new(AssemblyDefinitionCatalog.PartStandardSchema,[new("test.fluid",1,"Test fluid")],[d,unlocked]));
        using var editor=new ConstructionEditorSession(cat);editor.Load(0,AssemblyJson.Write(OnePartDocument(cat)));
        var c=editor.Current!.Design.Data.Configuration[0];
        foreach(var bad in new[]{c with {Stores=[c.Stores[0] with {QuantityKg=.25}]},c with {Stores=[c.Stores[0] with {Enabled=false}]},
            c with {Electrical=c.Electrical.Select(e=>e.Module=="battery"?e with {ChargeJ=1}:e).ToImmutableArray()},
            c with {Electrical=c.Electrical.Select(e=>e with {Enabled=false}).ToImmutableArray()}})
            RefuseEdit(editor,()=>editor.SetConfiguration(editor.Revision,bad),"definition configuration policy");
        var before=editor.Current.Design.Data;var replacement=cat.Reference(cat.Data.Definitions.Single(p=>p.Id=="unlocked"));
        RefuseEdit(editor,()=>editor.PreviewEdit(editor.Revision,before with {Instances=before.Instances.SetItem(0,before.Instances[0] with {Definition=replacement})}),"ordinary edit cannot substitute unlocked definition under stable instance ID");
    }
    private static void EditorHistoryBounds()
    {
        var f=CraftFixture(1);using var editor=new ConstructionEditorSession(f.Catalog);editor.Load(0,AssemblyJson.Write(f.Data));
        for(var i=0;i<80;i++)editor.SetName(editor.Revision,"Edit "+i);
        Check(editor.UndoCount==ConstructionEditorSession.MaximumHistoryCount&&editor.HistoryEvictions==17,"deterministic count eviction");
        while(editor.UndoCount>0)editor.Undo(editor.Revision);
        Check(editor.RedoCount==64&&editor.HistoryPayloadBytes<=ConstructionEditorSession.MaximumHistoryPayloadBytes,"both history directions share bound");
        var retained=editor.Current!.Design.Digest;RefuseEdit(editor,()=>editor.Undo(editor.Revision),"oldest retained boundary");
        editor.Redo(editor.Revision);editor.Undo(editor.Revision);Check(editor.Current!.Design.Digest==retained,"eviction preserves next reachable history");
        using(var overflow=new ConstructionEditorSession(f.Catalog)){
            overflow.Load(0,AssemblyJson.Write(f.Data));
            for(var i=0;i<64;i++)overflow.SetName(overflow.Revision,"Boundary "+i);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(ConstructionEditorSession).GetProperty(nameof(overflow.HistoryEvictions),flags)!.SetValue(overflow,long.MaxValue);
            var fingerprint=EditorFingerprint(overflow);var refused=false;
            try{overflow.SetName(overflow.Revision,"Overflow");}catch(OverflowException){refused=true;}
            Check(refused&&EditorFingerprint(overflow)==fingerprint,"history counter carry refuses before publication");
            typeof(ConstructionEditorSession).GetProperty(nameof(overflow.Revision),flags)!.SetValue(overflow,long.MaxValue);
            fingerprint=EditorFingerprint(overflow);refused=false;
            try{overflow.Undo(overflow.Revision);}catch(OverflowException){refused=true;}
            Check(refused&&EditorFingerprint(overflow)==fingerprint,"session revision overflow preserves every owned state");
        }
        var large=LargeHistoryFixture();using var bytes=new ConstructionEditorSession(large.Catalog);bytes.Load(0,AssemblyJson.Write(large.Data));
        Check(bytes.Save(bytes.Revision).Length>ConstructionEditorSession.MaximumHistoryPayloadBytes/ConstructionEditorSession.MaximumHistoryCount,"fixture reaches byte bound before count bound");
        for(var i=0;i<64&&bytes.HistoryEvictions==0;i++)bytes.SetName(bytes.Revision,"Large edit "+i);
        Check(bytes.HistoryEvictions>0&&bytes.UndoCount<ConstructionEditorSession.MaximumHistoryCount&&bytes.HistoryPayloadBytes<=ConstructionEditorSession.MaximumHistoryPayloadBytes,"payload limit independently evicts oldest state");
        var before=bytes.Save(bytes.Revision);bytes.Undo(bytes.Revision);bytes.Redo(bytes.Revision);
        Check(bytes.Save(bytes.Revision).SequenceEqual(before)&&bytes.HistoryPayloadBytes<=ConstructionEditorSession.MaximumHistoryPayloadBytes,"payload eviction preserves exact undo/redo");
        var left=ImmutableArray.Create<byte[]?>(new byte[4_000_000],new byte[4_000_000],new byte[1]);
        var right=ImmutableArray.Create<byte[]?>(new byte[4_000_000],new byte[4_000_000],new byte[4_000_000],new byte[1]);
        var owner=EditorFingerprint(editor);
        var bound=typeof(ConstructionEditorSession).GetMethod("BoundHistory",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var plan=((ImmutableArray<byte[]?> Undo,ImmutableArray<byte[]?> Redo,long Evictions))bound.Invoke(editor,[left,right])!;
        Check(plan.Undo.Length==3&&plan.Redo.Length==3&&ReferenceEquals(plan.Undo[^1],left[^1])&&ReferenceEquals(plan.Redo[^1],right[^1])&&
            ReferenceEquals(plan.Redo[0],right[1])&&EditorFingerprint(editor)==owner,"asymmetric size eviction plans farther redo without publishing or losing nearby entries");
    }
    private static (AssemblyDefinitionCatalog Catalog,ConstructionDesignData Data) LargeHistoryFixture()
    {
        var d=CompactCollision(DefinitionFixture());var plug=d.Standard!.Mechanical[0] with {Interface="in",Role=MateRole.Plug};
        d=d with {Attachments=d.Attachments.Add(new("in",PartStandard.FamilyKey(plug),new(Double3.UnitX,Matrix3.Mate))),
            Construction=d.Construction! with {Interfaces=d.Construction.Interfaces.Add(new("in",ConstructionService.None,false))},
            Standard=d.Standard with {Mechanical=d.Standard.Mechanical.Add(plug)}};
        var cat=Catalog(d);d=cat.Data.Definitions[0];var reference=cat.Reference(d);
        var instances=Enumerable.Range(0,1024).Select(i=>new PartInstanceData(new string('p',112)+"_"+i,i,reference,new(new(-i,0,0),Matrix3.Identity))).ToImmutableArray();
        var edges=Enumerable.Range(1,1023).Select(i=>new StructuralEdgeData(instances[i-1].Id,"mount",instances[i].Id,"in",new("joint-"+i,false,ConstructionService.None,0))).ToImmutableArray();
        return(cat,new(CompiledConstructionDesign.CraftSchema,"history.large",1,cat.DependencyDigest(instances),instances[0].Id,null,instances,edges,[],
            instances.Select(p=>Configuration(p.Id,d)).ToImmutableArray(),[],[],new("Large history witness",1)));
    }
}
