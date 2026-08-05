namespace NovaCore.Core.Camera;
public sealed class FreeCameraController
{
    private readonly double _defaultSpeed,_lookSensitivity,_pitchLimitRadians;
    public const double MaximumDeltaSeconds=.1d,MinimumSpeed=.00625d,MaximumSpeed=64d,WheelStep=.25d;
    public double PitchRadians {get; private set;} public double MovementSpeed {get; private set;}
    public FreeCameraController(double movementSpeed,double lookSensitivity,double pitchLimitRadians){_defaultSpeed=movementSpeed;_lookSensitivity=lookSensitivity;_pitchLimitRadians=pitchLimitRadians;MovementSpeed=movementSpeed;}
    public void Reset(){PitchRadians=0d;MovementSpeed=_defaultSpeed;}
    public void Update(CameraState state,ReadOnlySpan<CameraCommand> commands,double deltaSeconds){if(state.Mode!=CameraMode.Free)throw new InvalidOperationException("Only Free camera mode is implemented.");var dt=Math.Clamp(deltaSeconds,0d,MaximumDeltaSeconds);foreach(ref readonly var c in commands){if(c.Kind==CameraCommandKind.MoveLocal&&c.LocalDirection.LengthSquared>0d)state.Position=state.Position with{Value=state.Position.Value+state.Orientation.Rotate(c.LocalDirection.Normalized())*(MovementSpeed*dt)};else if(c.Kind==CameraCommandKind.Look){var yaw=DoubleQuaternion.FromAxisAngle(Double3.UnitY,-c.LookDelta.X*_lookSensitivity);var target=Math.Clamp(PitchRadians-c.LookDelta.Y*_lookSensitivity,-_pitchLimitRadians,_pitchLimitRadians);state.Orientation=(state.Orientation*yaw*DoubleQuaternion.FromAxisAngle(Double3.UnitX,target-PitchRadians)).Normalized();PitchRadians=target;}else if(c.Kind==CameraCommandKind.AdjustSpeed)MovementSpeed=Math.Clamp(MovementSpeed*Math.Pow(2d,c.LocalDirection.X*WheelStep),MinimumSpeed,MaximumSpeed);}state.Validate();}
}
