using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Graphics;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;

/// <summary>Player presentation over the sole ConstructionEditorSession owner.
/// A refused draft may be painted red; it never becomes an accepted preview.</summary>
internal sealed unsafe partial class DesktopEditorForm : Form, IApplicationPresentation
{
    private readonly ConstructionEditorSession session;
    private readonly ReusablePartVisuals visuals;
    private readonly Dictionary<string,PartVisualAsset> assets;
    private readonly Dictionary<string,PartVisualAsset> picking;
    private readonly string assetRoot;
    private CompiledCraft? compiled;
    private readonly Panel viewport=new(){Dock=DockStyle.Fill,BackColor=Color.FromArgb(15,20,30)};
    private readonly FlowLayoutPanel sidebar=new(){Dock=DockStyle.Left,Width=280,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new(12)};
    private readonly Label status=new(){Dock=DockStyle.Bottom,Height=94,Padding=new(12),AutoEllipsis=true};
    private readonly PartDefinitionData[] catalogParts;
    private PartDefinitionData chosenPart;
    private int editorIntent,clockDegrees;
    private readonly ComboBox count=new(){Width=112,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly NumericUpDown fill=new(){Width=112,Minimum=0,Maximum=100,Value=100,DecimalPlaces=1,Increment=.5m};
    private readonly NumericUpDown charge=new(){Width=112,Minimum=0,Maximum=100,Value=100,DecimalPlaces=1,Increment=.5m};
    private readonly CheckBox stores=new(){Text="Propellant enabled",Checked=true,AutoSize=true};
    private readonly CheckBox electrical=new(){Text="Electrical enabled",Checked=true,AutoSize=true};
    private readonly TextBox craftName=new(){Width=244,Text="Development craft"};
    private readonly EditorCamera camera=new();
    private readonly int renderCapacity;
    private NativeEditorViewport* native;
    private bool running,approvedClose;
    private string? selection,savePath,targetKey;
    private readonly Dictionary<string,string> savedPaths=new(StringComparer.Ordinal);
    private readonly Queue<string> savedPathOrder=new();
    private ConstructionDesignData? refusedGhost;
    private long nextInstance;
    private int previousX,previousY;
    private uint previousButtons;
    private string message="Choose your display settings, then start NovaCore.";
    private string? diagnostic;
    private readonly List<EditorSocketTarget> sockets=new(EditorSocketTargets.DisplayCapacity);
    private readonly record struct SocketView(long Revision,int Mode,string Child,int Count,int Clock,string? Selected,Double3 Eye,Double3 Target,double Aspect,double Width,double Height);
    private SocketView? socketView;
    private string? activeSocketParent,activeSocketTarget;
    private ConstructionDesignData? renderedCurrent,renderedPreview;
    private string? renderedSelection;
    private bool renderedRefused;
    private readonly List<(uint Mesh,Double3 Position,Matrix3 Rotation,uint Tint)> renderEntries;
    private bool socketOverflow;
    private PartDefinitionData Chosen=>chosenPart;
    internal DesktopEditorForm(AssemblyDefinitionCatalog catalog,string assetRoot,string? qualification=null,string tankDefinition="nc.tank.short-2")
    {
        Text="NovaCore · Craft construction";Width=1280;Height=900;MinimumSize=new(960,700);StartPosition=FormStartPosition.CenterScreen;
        Font=new("Consolas",10);BackColor=Color.FromArgb(235,239,244);ForeColor=Color.FromArgb(25,38,55);
        session=new(catalog);this.assetRoot=assetRoot;qualificationPath=qualification;qualificationTankDefinition=tankDefinition;
        SurfaceRetrySetup();
        var library=new ConstructionAssetLibrary();
        assets=catalog.Data.Definitions.ToDictionary(d=>d.Id,d=>{
            var a=d.Construction!.Asset;library.Resolve(assetRoot,a);
            return PartVisualLoader.Load(Path.Combine(assetRoot,a.RelativePath),a.Id+"/"+a.Revision,a.Sha256);
        },StringComparer.Ordinal);
        picking=assets.ToDictionary(p=>p.Key,p=>PartVisualPicking.NeutralComposite(p.Value),StringComparer.Ordinal);
        visuals=EditorVisualAssets.Prepare(assets.Values);
        foreach(var id in assets.Keys.ToArray())assets[id]=visuals.Resolve(assets[id].Identity);
        renderCapacity=EditorRenderCapacity.Required(assets.Values.Max(a=>a.Meshes.Length),
            catalog.Data.Definitions.SelectMany(d=>d.Standard!.SocketGroups).SelectMany(g=>g.Placements).Select(p=>p.Count).DefaultIfEmpty(1).Max(),
            catalog.Data.Definitions.Max(d=>d.Construction!.Consumers.Length));
        renderEntries=new(renderCapacity);
        catalogParts=catalog.Data.Definitions.OrderByDescending(d=>d.Standard!.RootEligible).ThenBy(d=>d.Construction!.Name,StringComparer.Ordinal).ToArray();
        chosenPart=catalogParts[0];
        count.Items.AddRange([1,2,4,8]);count.SelectedIndex=0;
        count.SelectedIndexChanged+=(_,_)=>ChangedIntent();
        FormBorderStyle=FormBorderStyle.None;Bounds=Screen.FromPoint(Cursor.Position).Bounds;
        BuildPlayerShell();
        Shown+=(_,_)=>ShowStartup();FormClosing+=OnClosing;
        FormClosed+=(_,_)=>{flight?.Dispose();foreach(var bitmap in thumbnails.Values)bitmap.Dispose();visuals.Dispose();session.Dispose();};UpdateStatus();
    }
    private void NeedSelection(){if(selection is null)throw new InvalidDataException("Select a part in the viewport first.");}
    private void Attempt(Action action)
    {try{action();}catch(Exception e)when(e is InvalidDataException or IOException or UnauthorizedAccessException or ArgumentException or OverflowException or System.Text.Json.JsonException){diagnostic=e.Message;message=PlayerMessage(e);if(running&&!loading)ShowInformation("ACTION REFUSED",message);}UpdateStatus();}
    private static string PlayerMessage(Exception e)=>e.Message switch {
        _ when e is System.Text.Json.JsonException=>"This craft file is incomplete or malformed. Choose a valid saved craft. Your current craft is unchanged.",
        var s when s.StartsWith("FIT_COLLISION",StringComparison.Ordinal)=>"Parts would overlap. Choose a clear socket or move the obstructing part. Details are available below.",
        var s when s.StartsWith("FIT_CLEARANCE",StringComparison.Ordinal)=>"A part blocks required equipment clearance. Choose a clear socket. Details are available below.",
        var s when s.StartsWith("FIT_INTERFACE",StringComparison.Ordinal)=>"Those connections do not fit at this angle. Choose a compatible socket and permitted clock.",
        var s when s.Contains("occupied",StringComparison.OrdinalIgnoreCase)||s.Contains("reused",StringComparison.OrdinalIgnoreCase)=>"One or more sockets are already occupied. Clear the complete placement set before trying again.",
        var s when s.Contains("Stale editor",StringComparison.Ordinal)=>"The craft changed. Choose the placement target again.",
        _=>e.Message};
    private void ChangedIntent(){Attempt(()=>{CancelGhost();lastHoverX=lastHoverY=int.MinValue;message="Move the held part to a highlighted connection. Click to attach.";});}
    private void CancelGhost(){if(session.Preview is not null)session.CancelPreview(session.Revision);refusedGhost=null;targetKey=null;activeSocketParent=activeSocketTarget=null;}
    private void NavigateHistory(bool backwards)
    {
        CancelGhost();freeGhost=null;editorIntent=0;if(backwards)session.Undo(session.Revision);else session.Redo(session.Revision);
        // Copies intentionally share a logical document Id. Only an exact saved
        // snapshot can restore a known destination; other history states ask.
        savePath=session.Current is {} current&&savedPaths.TryGetValue(current.Design.Digest,out var path)?path:null;
        selection=null;craftName.Text=session.Current?.Design.Data.Craft?.Name??"Development craft";RefreshInspector();
    }
    private void NewCraft()=>RequestLeave(()=>{session.Clear(session.Revision,true);selection=null;savePath=null;CancelGhost();freeGhost=null;editorIntent=0;draftIdentity=Guid.NewGuid().ToString("N");craftName.Text="Development craft";RefreshInspector();});
    private void RememberPath(string path)
    {
        var digest=session.Current!.Design.Digest;
        if(!savedPaths.ContainsKey(digest)){
            if(savedPathOrder.Count>=ConstructionEditorSession.MaximumHistoryCount+1)savedPaths.Remove(savedPathOrder.Dequeue());
            savedPathOrder.Enqueue(digest);
        }
        savedPaths[digest]=path;savePath=path;
    }
    private void OnClosing(object? sender,FormClosingEventArgs e)
    {
        if(approvedClose)return;
        if(loading){e.Cancel=true;CloseApproved();return;}
        e.Cancel=true;Attempt(()=>RequestLeave(CloseApproved));
    }
    private void FocusCraft()
    {
        if(session.Current is not {} d){camera.Target=Double3.Zero;camera.Distance=8;return;}
        camera.Target=d.Design.Parts.Aggregate(Double3.Zero,(v,p)=>v+p.Instance.Pose.Position)/d.Design.Parts.Length;
        camera.Distance=Math.Max(5,d.Design.Parts.Max(p=>Math.Sqrt((p.Instance.Pose.Position-camera.Target).LengthSquared))+2)*2;
    }
    private void UpdateStatus()
    {
        if(!editing){status.Text=message;return;}
        var design=session.Current?.Design;var propellant=design?.Data.Configuration.SelectMany(c=>c.Stores).Sum(s=>s.QuantityKg)??0;
        var dry=design?.Parts.Sum(p=>p.Definition.DryMassKg)??0;
        status.Text=$"{design?.Parts.Length??0} parts · dry {dry:0.##} kg · propellant {propellant:0.##} kg · {(session.Dirty?"unsaved changes":"saved / empty")}\n{message}";
    }
    private void Input(NativeEditorViewport input)
    {
        if(input.Width==0||input.Height==0)return;
        if(EditorUiOwnsPointer(input)){previousX=input.PointerX;previousY=input.PointerY;previousButtons=0;return;}
        camera.Aspect=(double)input.Width/input.Height;
        var dx=input.PointerX-previousX;var dy=input.PointerY-previousY;
        if((input.Pressed&2)!=0)contextTravel=0;
        else if((input.Buttons&2)!=0)contextTravel+=Math.Abs(dx)+Math.Abs(dy);
        if((input.Released&2)!=0&&contextTravel<=3&&editorIntent==0){PickPart(input);ShowPartContext(input);}
        var leftPress=(input.Pressed&1)!=0;
        // A left press owns selection/placement at its latched position. Rebase
        // dragging after this callback so concurrent motion cannot move the
        // camera under the pending click or cause a later camera jump.
        if(input.Focused!=0&&!leftPress){
            if((input.Buttons&previousButtons&2)!=0){camera.Yaw-=dx*.008;camera.Pitch=Math.Clamp(camera.Pitch+dy*.008,-1.45,1.45);}
            if((input.Buttons&previousButtons&4)!=0)camera.Target+=(-camera.Right*dx+camera.Up*dy)*(2*camera.Distance*EditorCamera.TanHalfFov/input.Height);
            camera.Distance=Math.Clamp(camera.Distance*Math.Exp(-input.Wheel*.12),.3,1000);
        }
        previousX=input.PointerX;previousY=input.PointerY;previousButtons=leftPress?0:input.Buttons;
        if(editorIntent!=0)UpdateHeldPreview(input);
        if((input.Pressed&1)==0||input.Focused==0)return;
        if(editorIntent==0){contextInspector.Hide();PickPart(input);RefreshInspector();UpdateStatus();return;}
        Attempt(()=>{
            if(session.Preview is null||refusedGhost is not null)throw new InvalidDataException("Move to a highlighted, clear connection before placing this part.");
            session.AcceptPreview(session.Revision);refusedGhost=null;targetKey=null;nextInstance++;editorIntent=0;freeGhost=null;
            message="Part attached.";RefreshInspector();
        });
    }
    private void PickPart(NativeEditorViewport input)
    {
        if(session.Current is not {} d)return;var ray=camera.Ray(input.PointerX,input.PointerY,input.Width,input.Height);var nearest=double.PositiveInfinity;string? picked=null;
        foreach(var p in d.Design.Parts){var inv=p.Instance.Pose.Rotation.Transpose();var hit=PartVisualPicking.Intersect(picking[p.Definition.Id],inv.Apply(camera.Eye-p.Instance.Pose.Position),inv.Apply(ray));
            if(hit is {} distance&&distance<nearest){nearest=distance;picked=p.Instance.Id;}}
        selection=picked;message=selection is null?"No part under pointer.":$"Selected {session.SelectionMembers(selection).Length} linked part(s).";
    }
    private void PreviewAtSocket(NativeEditorViewport input)
    {
        var design=session.Current?.Design??throw new InvalidDataException("Place the root first.");
        if(editorIntent==2)NeedSelection();
        var child=editorIntent==2?design.Parts[design.Index(selection!)].Definition:Chosen;
        var clockValue=clockDegrees;
        PrepareSockets(input.Width,input.Height);
        var chosen=EditorSocketTargets.Pick(sockets,input.PointerX,input.PointerY);
        if(chosen is not {} socket){CancelGhost();return;}
        var parent=socket.Parent;var target=socket.Target;var mount=socket.Mount;
        var key=$"{parent}|{target}|{child.Id}|{count.SelectedItem}|{clockValue}|{selection}|{editorIntent}";if(key==targetKey&&(session.Preview is not null||refusedGhost is not null))return;
        CancelGhost();targetKey=key;activeSocketParent=parent;activeSocketTarget=target;
        while(design.Data.Instances.Any(p=>p.Id.StartsWith("part-"+nextInstance+"-",StringComparison.Ordinal))||
            design.Data.Symmetry.Any(g=>g.Id=="group-part-"+nextInstance)||
            design.Data.Connections.Any(e=>e.Construction!.Id.StartsWith("joint-part-"+nextInstance+"-",StringComparison.Ordinal)))nextInstance++;
        var data=editorIntent==2?session.PrepareReconnect(session.Revision,selection!,parent,target!,clockValue):session.PreparePlacement(session.Revision,new(session.Catalog.Reference(child),parent,target!,mount!,clockValue,socket.Count,"part-"+nextInstance));
        try{session.PreviewEdit(session.Revision,data);message="Connection ready. Click to attach the complete preview.";}
        catch(InvalidDataException){refusedGhost=data;throw;}
    }
    private void PrepareSockets(double width,double height)
    {
        var view=new SocketView(session.Revision,editorIntent,Chosen.Id,Convert.ToInt32(count.SelectedItem),clockDegrees,selection,camera.Eye,camera.Target,camera.Aspect,width,height);
        if(socketView==view)return;socketView=view;
        RebuildSockets(width,height,view);
    }
    private void RebuildSockets(double width,double height,SocketView view)
    {
        sockets.Clear();socketOverflow=false;if(editorIntent==0||session.Current is not {} doc)return;
        var child=editorIntent==2&&selection is not null?doc.Design.Parts[doc.Design.Index(selection)].Definition:Chosen;
        var moving=editorIntent==2&&selection is not null?session.SelectionMembers(selection):[];
        socketOverflow=EditorSocketTargets.Rebuild(sockets,doc.Design,child,view.Count,view.Clock,selection,moving,camera,width,height,picking);
    }
    private void Render(NativeFrameSubmission* frame)
    {
        var size=viewport.ClientSize;camera.Aspect=(double)Math.Max(1,size.Width)/Math.Max(1,size.Height);frame->Camera=camera.Native();
        var length=0;var current=session.Current?.Design.Data;var preview=refusedGhost??session.Preview?.Design.Data;
        if(!ReferenceEquals(current,renderedCurrent)||!ReferenceEquals(preview,renderedPreview)||selection!=renderedSelection||renderedRefused!=(refusedGhost is not null))RebuildRenderEntries(current,preview);
        foreach(var entry in renderEntries)Add(entry.Mesh,entry.Position,entry.Rotation,1,entry.Tint);
        if(freeGhost is {} pose&&preview is null&&editorIntent!=0){var asset=assets[Chosen.Id];foreach(var mesh in asset.Meshes)Add(mesh.Handle.Value,pose.Point(mesh.Gimballed?asset.GimbalPivot:Double3.Zero),pose.Rotation,1,6);}
        PrepareSockets(size.Width,size.Height);foreach(var socket in sockets)if(socket.Visible){
            var active=socket.Parent==activeSocketParent&&socket.Target==activeSocketTarget;
            Add(5,socket.Marker,Matrix3.Identity,socket.Scale*(active?1.35:1),socket.Available?(active?7u:4u):5u);
        }
        if(socketOverflow){message="Some socket markers are hidden by the display limit. Select a target part to prioritize its sockets.";UpdateStatus();}
        if(length==0)Add(5,Double3.Zero,Matrix3.Identity,.04,4);
        frame->ObjectCount=(uint)length;frame->BatchCount=(uint)length;
        void Add(uint mesh,Double3 position,Matrix3 rotation,double scale,uint tint){
            if(length>=renderCapacity)throw new InvalidDataException("Viewport instance capacity exceeded.");
            var q=Quaternion.CreateFromRotationMatrix(new((float)rotation.A,(float)rotation.D,(float)rotation.G,0,(float)rotation.B,(float)rotation.E,(float)rotation.H,0,(float)rotation.C,(float)rotation.F,(float)rotation.I,0,0,0,0,1));q=Quaternion.Normalize(q);
            var relative=position-camera.Eye;var high=new Double3((float)relative.X,(float)relative.Y,(float)relative.Z);var low=relative-high;
            frame->Objects[length]=new(){Position=new(){HighX=(float)high.X,HighY=(float)high.Y,HighZ=(float)high.Z,LowX=(float)low.X,LowY=(float)low.Y,LowZ=(float)low.Z},
                Transform=new(){RotationX=q.X,RotationY=q.Y,RotationZ=q.Z,RotationW=q.W,ScaleX=(float)scale,ScaleY=(float)scale,ScaleZ=(float)scale},Mesh=new(){Value=mesh},Padding0=tint==0?0:0x4e434500u|tint};
            frame->Batches[length]=new(){Mesh=new(){Value=mesh},FirstObject=(uint)length,ObjectCount=1};length++;
        }
    }
    private void RebuildRenderEntries(ConstructionDesignData? current,ConstructionDesignData? preview)
    {
        renderedCurrent=current;renderedPreview=preview;renderedSelection=selection;renderedRefused=refusedGhost is not null;renderEntries.Clear();
        var shown=preview??current;var selected=selection is not null&&current?.Instances.Any(p=>p.Id==selection)==true?session.SelectionMembers(selection):[];
        if(shown is null)return;
        foreach(var p in shown.Instances){
            var original=current?.Instances.FirstOrDefault(x=>x.Id==p.Id);var ghost=preview is not null&&(original is null||original.Pose!=p.Pose);
            var tint=ghost?(refusedGhost is null?1u:2u):selected.Contains(p.Id)?3u:0u;
            var asset=assets[p.Definition.Id];foreach(var mesh in asset.Meshes)renderEntries.Add((mesh.Handle.Value,p.Pose.Point(mesh.Gimballed?asset.GimbalPivot:Double3.Zero),p.Pose.Rotation,tint));
        }
    }
}
