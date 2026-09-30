using System.Runtime.InteropServices;
using NovaCore.Interop;

namespace NovaCore.ConstructionEditor;

internal sealed unsafe partial class DesktopEditorForm
{
    private readonly ComboBox categoryPicker=new(){DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat};
    private readonly Label editorTitle=new(){Text="VEHICLE EDITOR",AutoSize=false};
    private readonly FlowLayoutPanel editorTools=new(){WrapContents=false};
    private readonly FlowLayoutPanel editorSymmetry=new(){WrapContents=false};
    private readonly Label editorHelp=new(){Text="Click a part, then a connection.\nRight drag: orbit · middle drag: pan\nWheel: zoom · X: symmetry · Delete: cancel"};

    private void BuildEditorCatalogue()
    {
        sidebar.AutoScroll=false;
        sidebar.Controls.Add(editorTitle);
        categoryPicker.Items.Add("All");
        foreach(var name in catalogParts.Select(d=>d.Standard!.Category).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))categoryPicker.Items.Add(name);
        categoryPicker.SelectedIndex=0;
        categoryPicker.SelectedIndexChanged+=(_,_)=>{category=(string)categoryPicker.SelectedItem!;RefreshCatalog();};
        sidebar.Controls.Add(categoryPicker);sidebar.Controls.Add(cards);
        foreach(var d in catalogParts)
        {
            thumbnails[d.Id]=PartThumbnail.Draw(assets[d.Id]);
            var card=new Button{Text=d.Construction!.Name,Image=thumbnails[d.Id],TextImageRelation=TextImageRelation.ImageAboveText,FlatStyle=FlatStyle.Flat,BackColor=RowColor,ForeColor=ForeColor,Tag=d.Id,AccessibleName=d.Construction.Name};
            card.FlatAppearance.BorderColor=Color.FromArgb(76,78,82);
            card.Click+=(_,_)=>Attempt(()=>HoldPart(d));
            tooltips.SetToolTip(card,$"{d.Construction.Name}\n{d.Standard!.Purpose}\nDry mass {d.DryMassKg:0.##} kg");partCards.Add(d.Id,card);
        }
        ActionButton(editorTools,"Select",()=>{InvalidatePlayerInput();CancelGhost();freeGhost=null;editorIntent=0;},90);
        ActionButton(editorTools,"Move",()=>{NeedSelection();InvalidatePlayerInput();CancelGhost();editorIntent=2;},90);
        ActionButton(editorTools,"Cancel",()=>{InvalidatePlayerInput();CancelGhost();freeGhost=null;editorIntent=0;message="Held part cancelled.";},90);
        sidebar.Controls.Add(editorTools);
        editorSymmetry.Controls.Add(new Label{Text="Symmetry",AutoSize=true,Margin=new(3,8,3,0)});
        count.FlatStyle=FlatStyle.Flat;editorSymmetry.Controls.Add(count);
        ActionButton(editorSymmetry,"Rotate",RotateHeld,90);sidebar.Controls.Add(editorSymmetry);sidebar.Controls.Add(editorHelp);
        StyleEditorControls(sidebar);
    }
    private void StyleEditorControls(Control parent)
    {
        foreach(Control control in parent.Controls)
        {
            control.ForeColor=ForeColor;
            control.BackColor=control is Button or TextBoxBase or UpDownBase or ComboBox?RowColor:PanelColor;
            if(control is Button button){button.FlatAppearance.BorderColor=Color.FromArgb(76,78,82);button.FlatAppearance.MouseOverBackColor=Color.FromArgb(62,64,68);button.FlatAppearance.MouseDownBackColor=Color.FromArgb(82,76,60);}
            if(control.HasChildren)StyleEditorControls(control);
        }
    }
    private void LayoutEditorPanels()
    {
        var margin=Ui(12);var top=Ui(42);var statusHeight=Ui(60);
        sidebar.SuspendLayout();inspector.SuspendLayout();
        sidebar.Padding=new(Ui(10));inspector.Padding=new(Ui(12));
        sidebar.Bounds=new(margin,top,Math.Min(Ui(340),ClientSize.Width*37/100),Math.Max(1,ClientSize.Height-top-statusHeight-margin*2));
        var inner=sidebar.ClientSize.Width-sidebar.Padding.Horizontal;
        editorTitle.Size=new(inner,Ui(25));categoryPicker.Width=inner-Ui(6);
        cards.Margin=new(0,Ui(8),0,Ui(8));
        var columns=inner>=Ui(290)?2:1;
        foreach(var card in partCards.Values){card.Size=new((inner-SystemInformation.VerticalScrollBarWidth)/columns-Ui(6),Ui(140));card.Margin=new(Ui(3));}
        editorTools.Size=new(inner,Ui(38));editorSymmetry.Size=new(inner,Ui(38));count.Width=Ui(65);
        foreach(var button in editorTools.Controls.OfType<Button>())button.Size=new((inner-Ui(18))/3,Ui(30));
        foreach(var button in editorSymmetry.Controls.OfType<Button>())button.Size=new(Math.Max(Ui(70),inner-count.Width-editorSymmetry.Controls[0].PreferredSize.Width-Ui(28)),Ui(30));
        editorHelp.Size=new(inner,TextRenderer.MeasureText(editorHelp.Text,Font,new(inner,int.MaxValue),TextFormatFlags.WordBreak).Height+Ui(4));
        var fixedHeight=sidebar.Padding.Vertical+sidebar.Controls.Cast<Control>().Where(c=>c!=cards).Sum(c=>c.Height+c.Margin.Vertical)+cards.Margin.Vertical;
        cards.Size=new(inner,Math.Max(1,sidebar.ClientSize.Height-fixedHeight));
        inspector.Size=new(Math.Min(Ui(280),ClientSize.Width*31/100),Math.Min(Ui(400),ClientSize.Height-top-statusHeight-margin*2));
        inspector.Location=new(ClientSize.Width-inspector.Width-margin,ClientSize.Height-inspector.Height-statusHeight-margin*2);
        foreach(Control control in inspector.Controls)
        {
            control.Width=inspector.ClientSize.Width-inspector.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-Ui(6);
            if(control is Button)control.Height=Ui(control.Text=="Launch vehicle"?42:32);
            if(control is FlowLayoutPanel row){row.Height=Ui(42);foreach(Control child in row.Controls)child.Size=new((row.Width-Ui(12))/2,Ui(32));}
        }
        contextInspector.Size=new(Ui(280),Ui(420));
        status.Bounds=new(margin,ClientSize.Height-statusHeight-margin,Math.Max(1,ClientSize.Width-margin*2),statusHeight);
        sidebar.ResumeLayout();inspector.ResumeLayout();
        RaiseEditorPanels();
    }
    private void RaiseEditorPanels()
    {
        if(!editing||overlay is not null)return;
        sidebar.BringToFront();inspector.BringToFront();status.BringToFront();
        if(contextInspector.Visible)contextInspector.BringToFront();
    }
    private bool EditorUiOwnsPointer(in NativeEditorViewport input)
    {
        var point=new Point(input.PointerX,input.PointerY);
        return sidebar.Visible&&sidebar.Bounds.Contains(point)||inspector.Visible&&inspector.Bounds.Contains(point)||
            contextInspector.Visible&&contextInspector.Bounds.Contains(point)||status.Visible&&status.Bounds.Contains(point)||
            frameHud.Visible&&frameHud.Bounds.Contains(point)||
            topBar.Visible&&topBar.Bounds.Contains(point);
    }
    [DllImport("user32.dll")]private static extern IntPtr WindowFromPoint(Point point);
    // This tests the actual composed Windows hit target, not just managed Visible.
    private bool EditorControlReceivesPointer(Control control)
    {
        var point=control.PointToScreen(new(control.Width/2,control.Height/2));
        var hit=Control.FromChildHandle(WindowFromPoint(point));
        return control.Visible&&control.Enabled&&control.RectangleToScreen(control.ClientRectangle).Contains(point)&&
            (hit==control||hit is not null&&control.Contains(hit));
    }
}
