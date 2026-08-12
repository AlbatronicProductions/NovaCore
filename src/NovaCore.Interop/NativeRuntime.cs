using System.Runtime.InteropServices;

namespace NovaCore.Interop;

public enum NativeResult : int { Success = 0, Failure = 1, InvalidArgument = 2 }
public enum NativePlanetaryMode : uint { CpuReference = 0, GpuProduction = 1, CpuGpuValidation = 2 }
public enum NativePlanetaryRenderRegime : uint { DistantOnly = 0, Transition = 1, DetailedOnly = 2 }
public enum NativePresentationFocus : uint { None = 0, Sun = 1, Mercury = 2, Venus = 3, Earth = 4, Moon = 5, Mars = 6, Jupiter = 7, Saturn = 8, Uranus = 9, Neptune = 10 }

[StructLayout(LayoutKind.Sequential)]
public struct NativeEncodedPosition
{
    public float HighX, HighY, HighZ, HighPadding;
    public float LowX, LowY, LowZ, LowPadding;
}
[StructLayout(LayoutKind.Sequential)] public struct NativeFloat4x4 { public float C0R0,C0R1,C0R2,C0R3,C1R0,C1R1,C1R2,C1R3,C2R0,C2R1,C2R2,C2R3,C3R0,C3R1,C3R2,C3R3; }
[StructLayout(LayoutKind.Sequential)] public struct NativeCameraData { public NativeEncodedPosition Position; public NativeFloat4x4 ViewProjection; }

[StructLayout(LayoutKind.Sequential)]
public struct NativeMeshHandle { public uint Value; }

[StructLayout(LayoutKind.Sequential)]
public struct NativeRenderTransform
{
    public float RotationX, RotationY, RotationZ, RotationW;
    public float ScaleX, ScaleY, ScaleZ, ScalePadding;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeRenderObject { public NativeEncodedPosition Position; public NativeRenderTransform Transform; public NativeMeshHandle Mesh; public uint Padding0, Padding1, Padding2; }

[StructLayout(LayoutKind.Sequential)]
public struct NativeDrawBatch { public NativeMeshHandle Mesh; public uint FirstObject, ObjectCount, Padding; }
/// <summary>64-byte presentation-only planetary patch record; face numbering and edge-mask bits equal Graphics contracts.</summary>
[StructLayout(LayoutKind.Sequential)] public struct NativePlanetaryPatch { public uint Face,Level,X,Y; public float CenterX,CenterY,CenterZ,Radius; public float ColorR,ColorG,ColorB,ColorA; public uint StitchMask,Reserved0,Reserved1,Reserved2; }
[StructLayout(LayoutKind.Sequential)] public struct NativePlanetaryGpuConstants
{
    public float CameraBodyHighX,CameraBodyHighY,CameraBodyHighZ,RadiusHigh;
    public float CameraBodyLowX,CameraBodyLowY,CameraBodyLowZ,RadiusLow;
    public float RefinementThreshold,NearFieldAltitudeRadii,SurfaceAltitudeMetres,MaximumTerrainHeightMetres;
    public uint MaximumLevel,OutputCapacity,TerrainVersion,TerrainFrame;
    public float ViewForwardX,ViewForwardY,ViewForwardZ,ViewHalfAngleRadians;
}
[StructLayout(LayoutKind.Sequential)] public struct NativePlanetaryEyeball
{
    public float CameraBodyHighX,CameraBodyHighY,CameraBodyHighZ,RadiusHigh;
    public float CameraBodyLowX,CameraBodyLowY,CameraBodyLowZ,RadiusLow;
    public float SurfaceAltitudeMetres,MaximumTerrainHeightMetres,OceanSeaLevelMetres,BlendAlpha;
    public uint BodyIdLow,BodyIdHigh,TerrainVersion,Enabled;
    public float TangentAnchorX,TangentAnchorY,TangentAnchorZ,MaximumAngleRadians;
    public float RadialWarpExponent,DetailFrequency,NormalStepMetres,RegionalAlpha;
    public uint VertexCount,IndexCount,RadialRingCount,AzimuthSegmentCount;
    public uint Reserved0,Reserved1,Reserved2,Reserved3;
}
[StructLayout(LayoutKind.Sequential)] public struct NativePlanetaryPresentation
{
    public float CenterX,CenterY,CenterZ,Radius;
    public float ColorR,ColorG,ColorB,DistantAlpha;
    public float DetailedAlpha,DistanceRadii; public NativePlanetaryRenderRegime Regime; public uint Enabled;
    public uint BodyIdLow,BodyIdHigh,MaterialKind,AlbedoSource;
    public float Roughness,Specular,Emissive,PresentationRotationRadians;
    public uint ProjectionKind,RingAssociation,AtmosphereHook,CloudHook;
    public float RingInnerRadiusRatio,RingOuterRadiusRatio,RingOpacity,RingBandFrequency;
    public float RingOrientationX,RingOrientationY,RingOrientationZ,RingOrientationW;
    public float RingColorR,RingColorG,RingColorB,RingColorA;
    public float BodyOrientationX,BodyOrientationY,BodyOrientationZ,BodyOrientationW;
    public float LocalDetailScaleMeters,LocalDetailMicroScaleMeters,LocalDetailFadeStartMetres,LocalDetailFadeEndMetres;
}
[StructLayout(LayoutKind.Sequential)] public struct NativeSolarLighting { public float SourceCenterX,SourceCenterY,SourceCenterZ,Exposure; public float PhotosphereR,PhotosphereG,PhotosphereB,AmbientFloor; public float SourceRadiance,GlowStrength; public uint Enabled,SpeedHud; }
[StructLayout(LayoutKind.Sequential)] public struct NativePlanetaryEnvironment
{
    public float CenterX,CenterY,CenterZ,Radius;
    public uint BodyIdLow,BodyIdHigh,EnabledLayers,SourceVersion;
    public float AtmosphereHeightMetres,RayleighScaleHeightMetres,MieScaleHeightMetres,MieAnisotropy;
    public float RayleighR,RayleighG,RayleighB,MieScattering;
    public float CloudBaseHeightMetres,CloudTopHeightMetres,CloudCoverage,CloudDensity;
    public float CloudGlobalScale,CloudDetailScale,CloudShadowStrength,MaximumTerrainHeightMetres;
    public float OceanSeaLevelMetres,OceanRoughness,OceanWaveScale,OceanWaveStrength;
    public float OceanColorR,OceanColorG,OceanColorB,ExposureAdjustment;
}
 [StructLayout(LayoutKind.Sequential)] public struct NativeOrbitLineVertex { public float X, Y, Z; }

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeFrameSubmission { public NativeCameraData Camera; public NativeRenderObject* Objects; public uint ObjectCount; public NativeDrawBatch* Batches; public uint BatchCount; public NativeOrbitLineVertex* OrbitVertices; public uint OrbitVertexCount; public NativeOrbitLineVertex* PreviousOrbitVertices; public uint PreviousOrbitVertexCount; public NativeOrbitLineVertex* BodyForwardVertices; public uint BodyForwardVertexCount; public NativeOrbitLineVertex* TargetDirectionVertices; public uint TargetDirectionVertexCount; public NativePlanetaryPatch* PlanetaryPatches; public uint PlanetaryPatchCount; public uint PlanetaryGpuAlignmentPadding; public NativePlanetaryGpuConstants PlanetaryGpu; public NativePlanetaryMode PlanetaryMode; public uint PlanetaryPadding0,PlanetaryPadding1,PlanetaryPadding2; public NativePlanetaryPresentation PlanetaryPresentation; public NativePlanetaryPresentation* DistantBodies; public uint DistantBodyCount, DistantBodyPadding; public NativeSolarLighting SolarLighting; public NativePlanetaryEnvironment PlanetaryEnvironment; public NativePlanetaryEyeball PlanetaryEyeball; }

[StructLayout(LayoutKind.Sequential)]
public struct NativeAbiLayout
{
    public uint EncodedPositionSize, CameraDataSize, CameraPositionOffset, CameraViewProjectionOffset, RenderTransformSize, RenderObjectSize, RenderObjectPositionOffset, RenderObjectTransformOffset, RenderObjectMeshOffset;
    public uint DrawBatchSize, OrbitLineVertexSize, FrameSubmissionSize, FrameObjectsOffset, FrameBatchesOffset, FrameOrbitVerticesOffset, FrameOrbitVertexCountOffset;
    public uint InputStateSize, InputDeltaSecondsOffset, InputMoveLeftOffset, InputMoveRightOffset, InputMoveForwardOffset, InputMoveBackwardOffset, InputMoveDownOffset, InputMoveUpOffset, InputResetOffset, InputLookActiveOffset, InputMouseDeltaXOffset, InputMouseDeltaYOffset, InputMouseWheelDetentsOffset, InputPauseToggleOffset, InputRateDecreaseOffset, InputRateIncreaseOffset, InputSasModeKeyOffset;
    public uint FramePlanetaryGpuOffset, FramePlanetaryModeOffset, FramePlanetaryPresentationOffset, InputPresentationFocusOffset, FrameSolarLightingOffset, FramePlanetaryEnvironmentOffset, FramePlanetaryEyeballOffset;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeInputState { public float DeltaSeconds; public uint MoveLeft, MoveRight, MoveForward, MoveBackward, MoveDown, MoveUp, Reset, LookActive; public float MouseDeltaX, MouseDeltaY; public int MouseWheelDetents; public uint PauseToggle, RateDecrease, RateIncrease, SasModeKey; public NativePresentationFocus PresentationFocus; }

public enum NativeHostEventType : uint { Diagnostic = 1, UpdateFrame = 2 }

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeHostEvent { public NativeHostEventType Type; public uint LogCategory; public byte* Utf8Message; public NativeInputState Input; public NativeFrameSubmission* Submission; }

public static partial class NativeRuntime
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void HostCallback(NativeHostEvent* hostEvent, IntPtr userData);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_run_renderer")]
    public static unsafe partial NativeResult RunRenderer(NativeFrameSubmission* submission, HostCallback callback, IntPtr userData);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_get_abi_layout")]
    public static partial NativeResult GetAbiLayout(out NativeAbiLayout layout);
    [LibraryImport("NovaCore.Native", EntryPoint = "nc_validate_planetary_patches")]
    public static unsafe partial NativeResult ValidatePlanetaryPatches(NativePlanetaryPatch* patches, uint count);
}
