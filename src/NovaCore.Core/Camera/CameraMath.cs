namespace NovaCore.Core.Camera;
public static class CameraMath { public static Double3 Right(in DoubleQuaternion q)=>q.Rotate(Double3.UnitX); public static Double3 Up(in DoubleQuaternion q)=>q.Rotate(Double3.UnitY); public static Double3 Forward(in DoubleQuaternion q)=>q.Rotate(-Double3.UnitZ); }
