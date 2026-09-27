using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.ConstructionEditor;

internal static unsafe class ModularViewportTests
{
    internal static void Run()
    {
        var checks=0;void Need(bool value,string why){checks++;if(!value)throw new InvalidDataException("Modular viewport: "+why);}
        Need(Marshal.SizeOf<NativeEditorViewport>()==56,"explicit input size");
        var names=new[]{nameof(NativeEditorViewport.Size),nameof(NativeEditorViewport.Version),nameof(NativeEditorViewport.ParentWindow),nameof(NativeEditorViewport.Width),nameof(NativeEditorViewport.Height),nameof(NativeEditorViewport.PointerX),nameof(NativeEditorViewport.PointerY),nameof(NativeEditorViewport.Buttons),nameof(NativeEditorViewport.Pressed),nameof(NativeEditorViewport.Released),nameof(NativeEditorViewport.Focused),nameof(NativeEditorViewport.Wheel),nameof(NativeEditorViewport.Stop)};
        int[] offsets=[0,4,8,16,20,24,28,32,36,40,44,48,52];
        for(var i=0;i<names.Length;i++)Need(Marshal.OffsetOf<NativeEditorViewport>(names[i]).ToInt32()==offsets[i],"fixed-width offset "+names[i]);
        NativeRuntime.HostCallback callback=(_,_)=>throw new InvalidOperationException("Refused lease cannot callback.");NativeRuntime.EditorMessageCallback preprocess=(_,_,_,_,_)=>0;
        var input=new NativeEditorViewport(){Size=55,Version=1,ParentWindow=1};
        Need(NativeRuntime.RunEditorViewport(null,callback,IntPtr.Zero,null,0,0,&input,preprocess)==NativeResult.InvalidArgument,"malformed lease refused before ownership");
        input.Size=56;Need(NativeRuntime.RunEditorViewport(null,callback,IntPtr.Zero,null,0,0,&input,preprocess)==NativeResult.InvalidArgument,"invalid parent/content refused before ownership");
        Need(Marshal.SizeOf<NativeApplicationViewport>()==64&&Marshal.OffsetOf<NativeApplicationViewport>(nameof(NativeApplicationViewport.Input)).ToInt32()==0&&Marshal.OffsetOf<NativeApplicationViewport>(nameof(NativeApplicationViewport.Mode)).ToInt32()==56&&Marshal.OffsetOf<NativeApplicationViewport>(nameof(NativeApplicationViewport.Reserved)).ToInt32()==60,"application lease layout independent of editor v1");
        var app=new NativeApplicationViewport(){Input=new(){Size=64,Version=1,ParentWindow=1}};
        foreach(var malformed in new[]{app with {Mode=3},app with {Reserved=1},app with {Input=new(){Size=56,Version=1,ParentWindow=1}},app with {Input=new(){Size=64,Version=2,ParentWindow=1}},app}){
            var refused=malformed;Need(NativeRuntime.RunApplicationViewport(null,callback,IntPtr.Zero,null,null,0,0,&refused,preprocess)==NativeResult.InvalidArgument,"malformed application lease refuses before callback or ownership");
        }
        const string root="assets/vehicles/modular-starter";using var document=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root,"catalog.json")));
        var assets=document.RootElement.GetProperty("definitions").EnumerateArray().Select(d=>d.GetProperty("construction").GetProperty("asset")).Select(a=>PartVisualLoader.Load(Path.Combine(root,a.GetProperty("relativePath").GetString()!),a.GetProperty("id").GetString()+"/1",a.GetProperty("sha256").GetString()!)).ToImmutableArray();
        using(var first=new ReusablePartVisuals(assets)){
            var handles=first.Assets.SelectMany(a=>a.Meshes).Select(m=>m.Handle).ToArray();
            using var second=new ReusablePartVisuals(assets.Reverse().ToImmutableArray());
            Need(first.Assets.SelectMany(a=>a.Meshes).Select(m=>m.Handle).SequenceEqual(handles),"second lease cannot rewrite first handles");
            Need(assets.SelectMany(a=>a.Meshes).All(m=>m.Handle==MeshHandle.Invalid),"input assets immutable across upload leases");
        }
        foreach(var duplicate in new[]{assets[0],assets[0] with {Sha256=new string('0',64)}}){
            var refused=false;try{using var invalid=new ReusablePartVisuals(assets.Add(duplicate));}catch(InvalidDataException){refused=true;}
            Need(refused,"upload lease still refuses duplicate identity including conflicting content");
        }
        var references=new Dictionary<string,PartVisualAsset>{{"first-definition",assets[0]},{"second-definition",assets[0]}};
        using(var shared=EditorVisualAssets.Prepare(references.Values)){
            using var baseline=new ReusablePartVisuals([assets[0]]);
            Need(shared.UniqueMeshCount==baseline.UniqueMeshCount&&shared.BufferBytes==baseline.BufferBytes,"shared definition references upload one complete asset");
            Need(ReferenceEquals(shared.Resolve(references["first-definition"].Identity),shared.Resolve(references["second-definition"].Identity)),"every definition resolves the same lease-owned asset");
            var handles=shared.Assets.SelectMany(a=>a.Meshes).Select(m=>m.Handle).ToArray();
            using var reversed=EditorVisualAssets.Prepare(references.Values.Reverse());
            Need(shared.Assets.SelectMany(a=>a.Meshes).Select(m=>m.Handle).SequenceEqual(handles)&&assets.SelectMany(a=>a.Meshes).All(m=>m.Handle==MeshHandle.Invalid),"shared/reversed leases preserve original handles and immutable inputs");
        }
        var conflictRefused=false;try{using var conflict=EditorVisualAssets.Prepare([assets[0],assets[0] with {Sha256=new string('0',64)}]);}catch(InvalidDataException){conflictRefused=true;}
        Need(conflictRefused,"shared-asset preparation cannot hide conflicting content identity");
        using(var distinct=EditorVisualAssets.Prepare([assets[0],assets[0] with {Identity="independent-test-asset/1"}]))
            Need(distinct.Assets.Length==2,"equal content does not merge distinct asset identities");
        foreach(var asset in assets){
            var composite=PartVisualPicking.NeutralComposite(asset);Need(composite.Meshes.Length==1&&composite.Meshes[0].VertexCount==asset.Meshes.Sum(m=>m.VertexCount)&&composite.Meshes[0].TriangleCount==asset.Meshes.Sum(m=>m.TriangleCount),"neutral composition preserves all geometry");
            foreach(var origin in new[]{new Double3(10,1,.1),new Double3(-10,0,0),new Double3(0,10,.1),new Double3(0,0,10)}){
                var ray=(-origin).Normalized();var a=PartVisualPicking.Intersect(asset,origin,ray);var b=PartVisualPicking.Intersect(composite,origin,ray);
                Need(a.HasValue==b.HasValue&&(!a.HasValue||Math.Abs(a.Value-b!.Value)<1e-6),"composite and articulated-neutral ray results");
            }
        }
        var core=assets.Single(a=>a.Identity.StartsWith("nc.greybox.core/",StringComparison.Ordinal));
        var hit=PartVisualPicking.Intersect(core,new(0,10,0),new(0,-1,0));Need(hit is {} h&&Math.Abs(h-9.4)<1e-6,"independent core side radius");
        Need(PartVisualPicking.Intersect(core,new(5,10,0),new(0,-1,0)) is null,"ray misses body outside axial extent");
        var camera=new EditorCamera(){Aspect=16d/9};
        foreach(var yaw in new[]{0d,.8,Math.PI})foreach(var pitch in new[]{-1.4,0d,1.4}){
            camera.Yaw=yaw;camera.Pitch=pitch;var point=camera.Target+camera.Right*.5+camera.Up*.3;var p=camera.Project(point,1600,900);var ray=camera.Ray(p.X,p.Y,1600,900);
            Need((ray-(point-camera.Eye).Normalized()).LengthSquared<1e-24,"screen/ray reciprocal across orbit");
            var v=point-camera.Eye;var m=camera.Native().ViewProjection;var w=m.C0R3*v.X+m.C1R3*v.Y+m.C2R3*v.Z;
            var x=(m.C0R0*v.X+m.C1R0*v.Y+m.C2R0*v.Z)/w;var y=(m.C0R1*v.X+m.C1R1*v.Y+m.C2R1*v.Z)/w;
            Need(Math.Abs((x+1)*800-p.X)<1e-4&&Math.Abs((y+1)*450-p.Y)<1e-4&&w>0,"GPU projection matches pick pixels");
        }
        GC.KeepAlive(callback);GC.KeepAlive(preprocess);
        Console.WriteLine($"Modular viewport CPU/ABI PASS: {checks} checks; native window route separately required");
    }
}
