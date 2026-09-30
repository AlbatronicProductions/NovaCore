using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NovaCore.ConstructionEditor;

/// <summary>A bounded Windows-composited child surface, independent of render extent.
/// Input ownership is enforced by the application, never by opacity/hit testing.</summary>
internal sealed class PlayerOverlayLayer : Panel
{
    private byte alpha=255;
    protected override CreateParams CreateParams
    {
        get { var value=base.CreateParams;value.ExStyle|=0x00080000;return value; }
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public byte Alpha
    {
        get=>alpha;
        set { if(alpha==value)return;alpha=value;ApplyAlpha(); }
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);ApplyAlpha();}
    private void ApplyAlpha()
    {
        if(IsHandleCreated&&!SetLayeredWindowAttributes(Handle,0,alpha,2))
            throw new Win32Exception(Marshal.GetLastWin32Error(),"Player overlay composition failed.");
    }
    [DllImport("user32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetLayeredWindowAttributes(IntPtr window,uint color,byte alpha,uint flags);
}
