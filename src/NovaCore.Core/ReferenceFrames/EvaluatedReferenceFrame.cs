namespace NovaCore.Core.ReferenceFrames;

/// <summary>One explicit simulation instant of frame state, expressed in its parent frame.</summary>
public readonly record struct EvaluatedReferenceFrame(FrameTransform LocalToParent, Double3 OriginVelocityInParent, Double3 AngularVelocityInParent, bool IsInertial);
