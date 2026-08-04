using NovaCore.Core.ReferenceFrames;
namespace NovaCore.Core.Camera;
public enum CameraCommandKind { MoveLocal, Look, Reset, AdjustSpeed }
public readonly record struct CameraCommand(CameraCommandKind Kind,Double3 LocalDirection,Double2 LookDelta);
