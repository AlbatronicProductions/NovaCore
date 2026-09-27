using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.ConstructionEditor;
using NovaCore.Simulation.Spacecraft.Assemblies;

internal static class DiagnosticBrowser
{
internal static void Run(string[] args)
{
string? catalogPath=null,assetRoot=null;var stockPaths=new List<string>();var port=58742;
for(var i=0;i<args.Length;i++)
{
    if(i+1>=args.Length)throw new ArgumentException("Expected --catalog, --asset-root, --stock or --port value.");
    switch(args[i]){case "--catalog":catalogPath=args[++i];break;case "--asset-root":assetRoot=args[++i];break;
        case "--stock":stockPaths.Add(args[++i]);break;case "--port":port=int.Parse(args[++i],CultureInfo.InvariantCulture);break;
        default:throw new ArgumentException("Unknown editor option.");}
}
if(catalogPath is null||assetRoot is null||port is <1024 or >65535)throw new ArgumentException("Provide --catalog and --asset-root; optional --stock and --port.");
static byte[] ReadBounded(string path,int maximum)
{
    using var stream=File.OpenRead(path);if(stream.Length>maximum)throw new InvalidDataException("Oversized input file.");
    var bytes=new byte[(int)stream.Length];stream.ReadExactly(bytes);return bytes;
}
var catalog=AssemblyDefinitionCatalog.Load(ReadBounded(catalogPath,16_000_000));
var assets=new ConstructionAssetLibrary();foreach(var part in catalog.Data.Definitions)assets.Resolve(assetRoot,part.Construction!.Asset);
var stocks=stockPaths.Select(p=>(Name:Path.GetFileNameWithoutExtension(p),Bytes:CompiledConstructionDesign.Load(catalog,ReadBounded(p,CompiledConstructionDesign.MaximumDocumentBytes)).Save())).ToArray();
using var host=new EditorHost(catalog);
var token=Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
var builder=WebApplication.CreateBuilder(new WebApplicationOptions {Args=[],ContentRootPath=AppContext.BaseDirectory,WebRootPath=Path.Combine(AppContext.BaseDirectory,"wwwroot")});
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=6_000_000);
var app=builder.Build();
app.Use(async(context,next)=>{
    if(context.Connection.RemoteIpAddress is not {} remote||!IPAddress.IsLoopback(remote)||context.Request.Host.Host!="127.0.0.1"||context.Request.Host.Port!=port)
    {context.Response.StatusCode=403;return;}
    context.Response.Headers["Cache-Control"]="no-store";context.Response.Headers["X-Content-Type-Options"]="nosniff";
    if(context.Request.Path.StartsWithSegments("/api")&&context.Request.Headers["X-Editor-Token"]!=token){context.Response.StatusCode=403;return;}
    if(context.Request.Method=="POST"&&context.Request.Headers.Origin!=$"http://127.0.0.1:{port}"){context.Response.StatusCode=403;return;}
    try{await next(context);}catch(Exception e) when(e is InvalidDataException or JsonException or OverflowException or FormatException or ArgumentException)
    {context.Response.StatusCode=400;await context.Response.WriteAsJsonAsync(new {error=e.Message});}
});
app.MapGet("/",()=>Results.Content(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"wwwroot","index.html")).Replace("__EDITOR_TOKEN__",token,StringComparison.Ordinal),"text/html"));
app.UseStaticFiles();
object? Document(ConstructionEditorDocument? d)=>d is null?null:new {
    d.Design.Digest,Data=d.Design.Data,Mass=d.Design.DryMass.Mass,Com=d.Design.DryMass.Com,
    // Repeated placements share catalog-local sockets/subparts; do not expand their full fanout into transport DTOs.
    Parts=d.Design.Parts.Select((p,i)=>new {p.Instance.Id,Definition=p.Definition.Id,DefinitionRevision=p.Definition.Revision,p.Instance.Pose,p.Com,
        PowerBus=d.Power.Power.Component[i],DataBus=d.Power.Data.Component[i],CommandReachable=d.Power.CanCommand(i)}).ToArray(),
    Fuel=d.Fuel.Consumers.Select(c=>new {Part=c.Key.Part,Module=c.Key.Consumer,Terms=c.Terms.Select(t=>new {t.Resource,Sources=t.Stores.Select(i=>d.Fuel.Stores[i].Key).ToArray()}).ToArray()}).ToArray(),
    Electrical=d.Power.Modules.Select(m=>new {Part=m.Key.Part,Module=m.Key.Module,m.Role,m.Bus}).ToArray()};
object Snapshot(ConstructionEditorSession editor)=>new {Revision=editor.Revision.ToString(CultureInfo.InvariantCulture),
    Catalog=catalog.Data.Definitions.Select((d,i)=>new {Index=i,d.Id,d.Revision,Name=d.Construction!.Name,Development=d.Construction.Development,d.DryMassKg,
        d.Attachments,Interfaces=d.Construction.Interfaces,Subparts=d.Construction.Subparts,Stores=d.Stores,Electrical=d.Construction.Electrical,Unqualified=d.Construction.UnqualifiedHardware}).ToArray(),
    Stocks=stocks.Select((s,i)=>new {Index=i,s.Name}).ToArray(),Current=Document(editor.Current),Preview=Document(editor.Preview),
    Runtime=host.Runtime is {} runtime?new {Generation=runtime.Binding.Identity.Generation.ToString(CultureInfo.InvariantCulture),runtime.Binding.Design.Digest,
        Parts=runtime.Binding.Parts.Length,Subparts=runtime.Binding.Subparts.Length,Stores=runtime.Binding.Fuel.Stores.Length,Actuators=runtime.Binding.Actuators.Length,
        ControlPart=runtime.Binding.Design.Data.ControlPart,ReferenceMass=runtime.Binding.Initial.ReferenceMass?.Mass,Scope="Static construction; no contact, ignition or flight"}:null};
IResult Json(object value)=>Results.Bytes(JsonSerializer.SerializeToUtf8Bytes(value,AssemblyJson.Options),"application/json");
app.MapGet("/api/state",async()=>Json(await host.Invoke(Snapshot)));
app.MapGet("/api/save",async(string revision)=>Results.File(await host.Invoke(e=>e.Save(long.Parse(revision,CultureInfo.InvariantCulture))),"application/json","vehicle-design.json"));
app.MapPost("/api/edit",async(HttpRequest request)=>{
    using var buffer=new MemoryStream();await request.Body.CopyToAsync(buffer);
    var command=AssemblyJson.Read<EditorCommand>(buffer.ToArray(),6_000_000);
    return Json(await host.Invoke(editor=>{
        var revision=long.Parse(command.Revision,CultureInfo.InvariantCulture);
        DefinitionReference Definition(){if((uint)command.DefinitionIndex>=(uint)catalog.Data.Definitions.Length)throw new InvalidDataException("Unknown catalog selection.");return catalog.Reference(catalog.Data.Definitions[command.DefinitionIndex]);}
        string Need(string? s)=>s??throw new InvalidDataException("Missing editor field.");
        ConstructionConnection Connection()=>new(Need(command.ConnectionId),command.Detachable,command.Services);
        switch(command.Operation)
        {
            case "clear":editor.Clear(revision);break;
            case "previewRoot":editor.PreviewRoot(revision,Definition(),Need(command.Instance),Need(command.DesignId),new(Double3.Zero,Matrix3.Identity));break;
            case "previewAttach":editor.PreviewAttach(revision,Definition(),Need(command.Instance),Need(command.Parent),Need(command.ParentInterface),Need(command.ChildInterface),Connection());break;
            case "previewReconnect":editor.PreviewReconnect(revision,Need(command.Instance),Need(command.Parent),Need(command.ParentInterface),Need(command.ChildInterface),Connection());break;
            case "accept":editor.AcceptPreview(revision);break;
            case "cancel":editor.CancelPreview(revision);break;
            case "remove":editor.Remove(revision,Need(command.Instance));break;
            case "rotate":editor.Rotate(revision,command.Axis switch {"X"=>new(1,0,0,0,0,-1,0,1,0),"Y"=>new(0,0,1,0,1,0,-1,0,0),"Z"=>new(0,-1,0,1,0,0,0,0,1),_=>throw new InvalidDataException("Choose X, Y or Z.")});break;
            case "connection":editor.SetConnection(revision,Connection());break;
            case "config":editor.SetConfiguration(revision,AssemblyJson.Read<ConstructionConfiguration>(Encoding.UTF8.GetBytes(Need(command.Payload))));break;
            case "metadata":var metadata=AssemblyJson.Read<EditorMetadata>(Encoding.UTF8.GetBytes(Need(command.Payload)));editor.SetMetadata(revision,metadata.Symmetry,metadata.Actions,metadata.ServiceLinks);break;
            case "control":editor.SetControl(revision,command.Instance);break;
            case "load":editor.Load(revision,Encoding.UTF8.GetBytes(Need(command.Payload)));break;
            case "stock":if((uint)command.StockIndex>=(uint)stocks.Length)throw new InvalidDataException("Unknown stock design.");editor.Load(revision,stocks[command.StockIndex].Bytes);break;
            case "instantiate":host.Instantiate(editor,revision,command.RuntimeGeneration);break;
            case "retire":host.Retire(editor,revision,command.RuntimeGeneration);break;
            default:throw new InvalidDataException("Unknown editor operation.");
        }
        return Snapshot(editor);
    }));
});
app.Run();

}
}
internal sealed record EditorCommand(string Operation,string Revision,int DefinitionIndex=0,string? Instance=null,string? DesignId=null,
    string? Parent=null,string? ParentInterface=null,string? ChildInterface=null,string? ConnectionId=null,bool Detachable=false,
    ConstructionService Services=ConstructionService.None,string? Axis=null,string? Payload=null,int StockIndex=0,string? RuntimeGeneration=null);
internal sealed record EditorMetadata(ImmutableArray<ConstructionSymmetry> Symmetry,ImmutableArray<ConstructionAction> Actions,ImmutableArray<ConstructionServiceLink> ServiceLinks);
