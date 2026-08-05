using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Interop;
internal static class DebugCameraInput
{
    public static int Map(in NativeInputState input,Span<CameraCommand> commands,bool includeLook=true,bool includeSpeed=true)
    { var n=0;var move=new Double3((input.MoveRight!=0?1d:0d)-(input.MoveLeft!=0?1d:0d),(input.MoveUp!=0?1d:0d)-(input.MoveDown!=0?1d:0d),(input.MoveBackward!=0?1d:0d)-(input.MoveForward!=0?1d:0d));if(move.LengthSquared>0)commands[n++]=new(CameraCommandKind.MoveLocal,move.Normalized(),default);if(includeLook&&input.LookActive!=0&&(input.MouseDeltaX!=0||input.MouseDeltaY!=0))commands[n++]=new(CameraCommandKind.Look,default,new Double2(input.MouseDeltaX,input.MouseDeltaY));if(includeSpeed&&input.MouseWheelDetents!=0)commands[n++]=new(CameraCommandKind.AdjustSpeed,new Double3(input.MouseWheelDetents,0,0),default);if(input.Reset!=0)commands[n++]=new(CameraCommandKind.Reset,default,default);return n; }
}
