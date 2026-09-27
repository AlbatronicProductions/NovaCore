using System.Collections.Concurrent;
using System.Globalization;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;

// Transport dispatch only; runtime state stays in the existing simulation owners.
internal sealed class EditorHost : IDisposable
{
    private readonly BlockingCollection<Action<ConstructionEditorSession>> queue=new(64);
    private readonly Thread owner;
    internal ConstructionApplicationSession? Runtime {get;private set;}
    internal EditorHost(AssemblyDefinitionCatalog catalog)
    {
        owner=new Thread(()=>{
            CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
            using var editor=new ConstructionEditorSession(catalog);
            try{foreach(var operation in queue.GetConsumingEnumerable())operation(editor);}
            finally{Runtime?.Dispose();}
        }){IsBackground=true,Name="Construction editor owner"};owner.Start();
    }
    private void VerifyRuntime(ConstructionEditorSession editor,long revision,string? generation)
    {
        if(Environment.CurrentManagedThreadId!=owner.ManagedThreadId||editor.Revision!=revision||
            generation!=Runtime?.Binding.Identity.Generation.ToString(CultureInfo.InvariantCulture))throw new InvalidDataException("Stale editor/runtime selection.");
    }
    internal void Instantiate(ConstructionEditorSession editor,long revision,string? generation)
    {
        VerifyRuntime(editor,revision,generation);_=editor.Save(revision);
        if(editor.Preview is not null)throw new InvalidDataException("Accept or cancel the preview before instantiation.");
        var next=ConstructionApplicationSession.Create(editor.Current!.Design);
        try{Runtime?.Dispose();Runtime=next;}catch{next.Dispose();throw;}
    }
    internal void Retire(ConstructionEditorSession editor,long revision,string? generation)
    {VerifyRuntime(editor,revision,generation);Runtime?.Dispose();Runtime=null;}
    internal Task<T> Invoke<T>(Func<ConstructionEditorSession,T> operation)
    {
        var result=new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if(!queue.TryAdd(editor=>{try{result.SetResult(operation(editor));}catch(Exception e){result.SetException(e);}}))
            result.SetException(new InvalidDataException("Editor request queue is full."));
        return result.Task;
    }
    public void Dispose(){queue.CompleteAdding();owner.Join();queue.Dispose();}
}
