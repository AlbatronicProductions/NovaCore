using NovaCore.Core;
using NovaCore.Interop;
using NovaCore.Simulation.Spacecraft.Assemblies;

namespace NovaCore.ConstructionEditor;
internal sealed unsafe partial class DesktopEditorForm
{
    private AssemblyPose? freeGhost;
    private int lastHoverX=int.MinValue,lastHoverY=int.MinValue;
    private long hoverRevision=-1;
    private int contextTravel;
    private void HoldPart(PartDefinitionData part)
    {
        contextInspector.Hide();
        CancelGhost();for(var i=0;i<catalogList.Items.Count;i++)if(((CatalogItem)catalogList.Items[i]).Definition.Id==part.Id){catalogList.SelectedIndex=i;break;}
        mode.SelectedIndex=1;lastHoverX=lastHoverY=int.MinValue;hoverRevision=-1;freeGhost=new(camera.Target,Matrix3.Identity);
        foreach(var pair in partCards)pair.Value.FlatAppearance.BorderColor=pair.Key==part.Id?Color.FromArgb(86,211,240):Color.FromArgb(72,86,100);
        message="Move the held part into the viewport. Highlighted connections show where it fits.";
    }
    private void UpdateHeldPreview(NativeEditorViewport input)
    {
        if(input.PointerX<0||input.PointerY<0||input.PointerX>=input.Width||input.PointerY>=input.Height)return;
        if(input.PointerX==lastHoverX&&input.PointerY==lastHoverY&&hoverRevision==session.Revision&&input.Buttons==0&&input.Wheel==0)return;
        lastHoverX=input.PointerX;lastHoverY=input.PointerY;
        var ray=camera.Ray(input.PointerX,input.PointerY,input.Width,input.Height);
        var position=camera.Eye+ray*(camera.Distance/Double3.Dot(ray,camera.Forward));freeGhost=new(position,Matrix3.Identity);
        try{
            if(session.Current is null){
                CancelGhost();if(Chosen.Standard!.RootEligible)session.PreviewRoot(session.Revision,session.Catalog.Reference(Chosen),"core","craft-"+draftIdentity,new(position,Matrix3.Identity),craftName.Text);
            }else PreviewAtSocket(input);
        }catch(Exception e)when(e is InvalidDataException or ArgumentException or OverflowException){diagnostic=e.Message;message=PlayerMessage(e);}
        hoverRevision=session.Revision;UpdateStatus();
    }
    private string draftIdentity=Guid.NewGuid().ToString("N");
    private void RotateHeld()
    {
        if(mode.SelectedIndex!=0){clock.SelectedIndex=(clock.SelectedIndex+1)%clock.Items.Count;return;}
        NeedSelection();var design=session.Current!.Design;
        var group=design.Data.Symmetry.SingleOrDefault(g=>g.Members.Any(m=>m.Part==selection));
        var connection=design.Data.Connections.SingleOrDefault(e=>e.Child==(group?.BasePart??selection));
        if(connection is null)throw new InvalidDataException("Choose a connected part to rotate.");
        var parent=design.Parts[design.Index(connection.Parent)].Definition.Standard!.Mechanical.Single(m=>m.Interface==connection.ParentEndpoint);
        var child=design.Parts[design.Index(connection.Child)].Definition.Standard!.Mechanical.Single(m=>m.Interface==connection.ChildEndpoint);
        var angles=parent.ClockDegrees.Intersect(child.ClockDegrees).Order().ToArray();
        if(angles.Length<2)throw new InvalidDataException("This connection has a fixed orientation.");
        var next=angles[(Array.IndexOf(angles,connection.Construction!.ClockDegrees)+1)%angles.Length];
        CancelGhost();session.PreviewClock(session.Revision,selection!,next);session.AcceptPreview(session.Revision);message="Selected part rotated.";
    }
}
