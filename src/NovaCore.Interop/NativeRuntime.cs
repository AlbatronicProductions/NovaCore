using System.Runtime.InteropServices;

namespace NovaCore.Interop;

public enum NativeResult : int { Success = 0, Failure = 1, InvalidArgument = 2 }
public enum NativePlanetaryMode : uint { CpuReference = 0, GpuProduction = 1, CpuGpuValidation = 2 }
public enum NativePlanetarySurfaceMode : uint { Bounded = 0, ProductionCubeSphere = 2 }
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
[StructLayout(LayoutKind.Sequential)]
public struct NativeAnchoredSurfacePatch
{
    public uint BodyIdLow,BodyIdHigh,TerrainVersion,PhysicalSurfaceGeneration;
    public uint Face,Level,X,Y;
    public uint CacheSlot,CacheGeneration,StitchMask,Flags;
    public uint MaterialLevel,MaterialX,MaterialY,MaterialGeneration;
    public float BoundsX,BoundsY,BoundsZ,BoundsRadius;
}
[StructLayout(LayoutKind.Sequential)]
public struct NativeAnchoredSurfacePresentation
{
    public NativeEncodedPosition Origin;
    public NativeEncodedPosition East;
    public NativeEncodedPosition North;
    public NativeEncodedPosition Up;
    public uint BodyIdLow, BodyIdHigh, SnapIdentity, PresentationGeneration;
}
[StructLayout(LayoutKind.Sequential)] public struct NativePlanetaryGpuConstants
{
    public float CameraBodyHighX,CameraBodyHighY,CameraBodyHighZ,RadiusHigh;
    public float CameraBodyLowX,CameraBodyLowY,CameraBodyLowZ,RadiusLow;
    public float RefinementThreshold,NearFieldAltitudeRadii,SurfaceAltitudeMetres,MaximumTerrainHeightMetres;
    public uint MaximumLevel,OutputCapacity,TerrainVersion,TerrainFrame;
    public float ViewForwardX,ViewForwardY,ViewForwardZ,ViewHalfAngleRadians;
    public float ViewportHeightPixels,VerticalTanHalfFov,TargetTexelPixels,RequestedAlbedoLevel;
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
    public float CenterLowX,CenterLowY,CenterLowZ,CenterLowPadding;
}
[StructLayout(LayoutKind.Sequential)] public struct NativeSolarLighting { public float SourceCenterX,SourceCenterY,SourceCenterZ,Exposure; public float PhotosphereR,PhotosphereG,PhotosphereB,AmbientFloor; public float SourceRadiance,GlowStrength; public uint Enabled,SpeedHud; }
 [StructLayout(LayoutKind.Sequential)] public struct NativeOrbitLineVertex { public float X, Y, Z, LowX, LowY, LowZ; }

[StructLayout(LayoutKind.Sequential)]
public struct NativeProductionBillboardLatticeVertex { public int CubeX,CubeY,CubeZ; public uint Metadata; }

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeProductionSphericalBillboardSubmission
{
    public uint Size,Version,Enabled,Level;
    public uint VertexCount,IndexCount,LatticeScale,PhysicalGeneration;
    public uint TerrainDataGeneration,PupilGeneration,Reserved0,Reserved1;
    public ulong TopologyHash,PublicationGeneration;
    public NativeProductionBillboardLatticeVertex* LatticeVertices;
    public uint* Indices;
    public NativeSphericalBillboardPhysicalVertex* PhysicalVertices;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeFrameSubmission { public NativeCameraData Camera; public NativeRenderObject* Objects; public uint ObjectCount; public NativeDrawBatch* Batches; public uint BatchCount; public NativeOrbitLineVertex* OrbitVertices; public uint OrbitVertexCount; public NativeOrbitLineVertex* PreviousOrbitVertices; public uint PreviousOrbitVertexCount; public NativeOrbitLineVertex* BodyForwardVertices; public uint BodyForwardVertexCount; public NativeOrbitLineVertex* TargetDirectionVertices; public uint TargetDirectionVertexCount; public NativePlanetaryPatch* PlanetaryPatches; public uint PlanetaryPatchCount; public uint PlanetaryGpuAlignmentPadding; public NativePlanetaryGpuConstants PlanetaryGpu; public NativePlanetaryMode PlanetaryMode; public NativePlanetarySurfaceMode PlanetarySurfaceMode; public uint PhysicalSurfaceGeneration,PlanetaryPadding2; public NativePlanetaryPresentation PlanetaryPresentation; public NativePlanetaryPresentation* DistantBodies; public uint DistantBodyCount, DistantBodyPadding; public NativeSolarLighting SolarLighting; public NativeAnchoredSurfacePatch* AnchoredSurfacePatches; public uint AnchoredSurfacePatchCount,AnchoredSurfaceCacheSlotCount,AnchoredSurfaceActiveGeneration,AnchoredSurfaceFlags,AnchoredSurfaceGpuReadyGeneration,AnchoredSurfacePadding1,AnchoredSurfacePadding2,AnchoredSurfacePadding3,AnchoredSurfacePadding4,AnchoredSurfacePadding5; public NativeAnchoredSurfacePresentation AnchoredSurfacePresentation; public NativeProductionSphericalBillboardSubmission* ProductionBillboard; public uint ProductionBillboardFlags,ProductionBillboardPadding; }

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeRuntimeAssets
{
    public uint Size, Version;
    public byte* ProductionTerrainPathUtf8;
    public byte* LocalTerrainPathUtf8;
    public byte* ElevationOraclePathUtf8;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativePlanetaryHeightQuery
{
    public float AnchorHighX,AnchorHighY,AnchorHighZ,AnchorHighPadding;
    public float AnchorLowX,AnchorLowY,AnchorLowZ,AnchorLowPadding;
    public float LocalDeltaX,LocalDeltaY,LocalDeltaZ,LocalDeltaPadding;
    public double OracleU,OracleV;
    public uint BodyIdLow,BodyIdHigh,TerrainVersion,AnchoredTier;
    public uint TopologyVersion,SourcePolicy,Reserved0,Reserved1;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativePlanetaryHeightResult
{
    public float ReconstructedHighX,ReconstructedHighY,ReconstructedHighZ,ReconstructedHighPadding;
    public float ReconstructedLowX,ReconstructedLowY,ReconstructedLowZ,ReconstructedLowPadding;
    public double FaceU,FaceV;
    public double OracleElevationMetres,TerrainV5ElevationMetres;
    public double LocalResidualMetres,PhysicalHeightMetres;
    public double BaseHeightMetres,ModifierHeightMetres;
    public double TiledModifierHeightMetres,ErosionModifierHeightMetres;
    public double EastGradient,NorthGradient;
    public float PhysicalNormalX,PhysicalNormalY,PhysicalNormalZ,ModifierWeight;
    public double ReconstructedX,ReconstructedY;
    public double ReconstructedZ,ReconstructedLength;
    public uint GlobalFace,GlobalLevel,GlobalX,GlobalY;
    public uint LocalAvailable,LocalLevel,LocalX,LocalY;
    public uint Valid,SourceHasLocal,ResultTerrainVersion,Reserved;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativePlanetaryHeightQueryAssets
{
    public uint Size,Version;
    public byte* ElevationOraclePathUtf8;
    public byte* ProductionTerrainPathUtf8;
    public byte* LocalTerrainPathUtf8;
    public byte* ComputeShaderPathUtf8;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativePlanetaryHeightQueryMetrics
{
    public uint Size,Version,QueryCount,DispatchGroups;
    public uint ValidationErrors,GlobalRecordCount,LocalRecordCount,Reserved;
    public double CpuMilliseconds,GpuMilliseconds;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativePlanetaryDisplacedVertex
{
    public float BodyHighX,BodyHighY,BodyHighZ,PhysicalHeightMetres;
    public float BodyLowX,BodyLowY,BodyLowZ,TerrainV5HeightMetres;
    public float CameraRelativeX,CameraRelativeY,CameraRelativeZ,LocalResidualMetres;
    public double FaceU,FaceV;
    public uint GlobalFace,GlobalLevel,GlobalX,GlobalY;
    public uint LocalAvailable,LocalLevel,LocalX,LocalY;
    public uint Valid,SourceHasLocal,ResultTerrainVersion,TopologyVersion;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativePlanetaryPhysicalNormal { public float X,Y,Z,Validity; }

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativePlanetaryMeshPreparationAssets
{
    public uint Size,Version;
    public byte* ElevationOraclePathUtf8;
    public byte* ProductionTerrainPathUtf8;
    public byte* LocalTerrainPathUtf8;
    public byte* DisplacementShaderPathUtf8;
    public byte* NormalShaderPathUtf8;
    public uint MaximumVertexCount,MaximumIndexCount,MaximumAdjacencyCount,Reserved;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativePlanetaryMeshPreparationDispatch
{
    public uint Size,Version,VertexCount,IndexCount;
    public uint AdjacencyCount,TopologyVersion,TerrainVersion,SourcePolicy;
    public float CameraHighX,CameraHighY,CameraHighZ,CameraHighPadding;
    public float CameraLowX,CameraLowY,CameraLowZ,CameraLowPadding;
    public double BodyRadiusMetres;
    public uint Reserved0,Reserved1;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativePlanetaryMeshPreparationMetrics
{
    public uint Size,Version,VertexCount,TriangleCount;
    public uint AdjacencyCount,DisplacementGroups,NormalGroups,ValidationErrors;
    public uint InitializationCount,PreparationCount,PipelineCreationCount,ShaderModuleCreationCount;
    public ulong PersistentBufferBytes;
    public double SetupMilliseconds,DisplacementMilliseconds,NormalMilliseconds,TotalMilliseconds;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeSphericalBillboardProofVertex { public float X,Y,Z,W; }

[StructLayout(LayoutKind.Sequential)]
public struct NativeSphericalBillboardProofLatticeVertex
{
    public int CubeX,CubeY,CubeZ;
    public uint Metadata;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeSphericalBillboardProofAssets
{
    public uint Size,Version;
    public byte* ResetShaderPathUtf8,PrepareShaderPathUtf8,NormalShaderPathUtf8,CullShaderPathUtf8,CompactShaderPathUtf8,VertexShaderPathUtf8,FragmentShaderPathUtf8;
    public uint MaximumVertexWorkItems,MaximumTriangleWorkItems,FrameResourceCount,RenderExtent;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeSphericalBillboardProofTopology
{
    public uint Size,Version,FormatVersion,GeneratorVersion;
    public uint Level,VertexCount,IndexCount,NeighborOffsetCount;
    public uint NeighborCount,Reserved0,Reserved1,Reserved2;
    public ulong TopologyHash;
    public NativeSphericalBillboardProofVertex* Vertices;
    public uint* Indices;
    public uint* NeighborOffsets;
    public uint* Neighbors;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeSphericalBillboardPhysicalVertex
{
    public double BodyX,BodyY,BodyZ,PhysicalHeightMetres;
    public float NormalX,NormalY,NormalZ,NormalValidity;
    public float Reserved0,Reserved1,Reserved2,Reserved3;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeSphericalBillboardPhysicalSurface
{
    public uint Size,Version,VertexCount,PhysicalGeneration;
    public uint TerrainDataGeneration,Reserved0,Reserved1,Reserved2;
    public ulong ExpectedTopologyHash;
    public NativeSphericalBillboardPhysicalVertex* Vertices;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeSphericalBillboardProofFrame
{
    public uint Size,Version,FrameIndex,RenderEnabled;
    public uint WorkVertexCount,WorkTriangleCount,Reserved0,Reserved1;
    public ulong ExpectedTopologyHash;
    public double BodyRadiusMetres,CameraDistanceMetres;
    public float VerticalTanHalfFov,AspectRatio;
    public uint Reserved2,Reserved3;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeSphericalBillboardProofMetrics
{
    public uint Size,Version,ActiveLevel,Readiness;
    public uint BaseVertexCount,BaseTriangleCount,WorkVertexCount,WorkTriangleCount;
    public uint PreparedVertices,VisibleTriangles,BackfaceRejected,FrustumRejected;
    public uint InvalidRejected,OverflowCount,IndirectIndexCount,IndirectDrawCount;
    public uint InvalidCommands,ValidationErrors,FrameSlot,FrameWaitCount;
    public uint TopologyUploadCount,FrameOutputWriteCount,CullingDispatchCount,IndirectSubmissionCount;
    public uint TopologyReplacementCount,RuntimeTopologyGenerationCount,PipelineCreationCount,ShaderModuleCreationCount;
    public ulong TopologyHash,TopologyBytesUploaded,ActiveTopologyBytes,PixelChecksum;
    public ulong ImmutableVertexBytes,ImmutableIndexBytes,ImmutableAdjacencyBytes;
    public ulong FramePositionBytes,FrameNormalBytes,FrameVisibilityBytes,FrameCompactedIndexBytes;
    public ulong FrameIndirectBytes,FrameCounterBytes,TemporaryScratchBytes,TotalAllocatedBytes;
    public double SetupMilliseconds,TopologyUploadMilliseconds,CpuFrameMilliseconds;
    public double PreparationMilliseconds,NormalMilliseconds,CullingMilliseconds,CompactionMilliseconds;
    public double DrawMilliseconds,GpuTotalMilliseconds;
    public uint PhysicalGeneration,TerrainDataGeneration,PreparedPhysicalSamples,PhysicalPreparationDispatchCount;
    public uint PhysicalReuseCount,StaleGenerationRejections,NonFinitePhysicalOutputs,ReservedPhysical;
    public ulong ImmutablePhysicalBytes;
    public double DirectionDecodeMaximumErrorRadians;
    public uint IncomingLevel,IncomingReadiness,PublicationCount,DeferredRetirementCount;
    public ulong IncomingTopologyHash,IncomingTopologyBytes,SelectedIncomingBytes;
    public uint ZeroOwnerFrames,OverlapOwnerFrames,StaleGenerationDraws,ReservedProduction;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeAbiLayout
{
    public uint EncodedPositionSize, CameraDataSize, CameraPositionOffset, CameraViewProjectionOffset, RenderTransformSize, RenderObjectSize, RenderObjectPositionOffset, RenderObjectTransformOffset, RenderObjectMeshOffset;
    public uint DrawBatchSize, OrbitLineVertexSize, FrameSubmissionSize, FrameObjectsOffset, FrameBatchesOffset, FrameOrbitVerticesOffset, FrameOrbitVertexCountOffset;
    public uint InputStateSize, InputDeltaSecondsOffset, InputMoveLeftOffset, InputMoveRightOffset, InputMoveForwardOffset, InputMoveBackwardOffset, InputMoveDownOffset, InputMoveUpOffset, InputResetOffset, InputLookActiveOffset, InputMouseDeltaXOffset, InputMouseDeltaYOffset, InputMouseWheelDetentsOffset, InputPauseToggleOffset, InputRateDecreaseOffset, InputRateIncreaseOffset, InputSasModeKeyOffset, InputFastModifierOffset, InputSlowModifierOffset;
    public uint FramePlanetaryGpuOffset, FramePlanetaryModeOffset, FramePlanetaryPresentationOffset, InputPresentationFocusOffset, FrameSolarLightingOffset, InputViewportWidthOffset, InputViewportHeightOffset;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeInputState { public float DeltaSeconds; public uint MoveLeft, MoveRight, MoveForward, MoveBackward, MoveDown, MoveUp, Reset, LookActive; public float MouseDeltaX, MouseDeltaY; public int MouseWheelDetents; public uint PauseToggle, RateDecrease, RateIncrease, SasModeKey, FastModifier, SlowModifier; public NativePresentationFocus PresentationFocus; public uint ViewportWidthPixels, ViewportHeightPixels; }

public enum NativeHostEventType : uint { Diagnostic = 1, UpdateFrame = 2 }

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeHostEvent { public NativeHostEventType Type; public uint LogCategory; public byte* Utf8Message; public NativeInputState Input; public NativeFrameSubmission* Submission; }

public static partial class NativeRuntime
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void HostCallback(NativeHostEvent* hostEvent, IntPtr userData);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_run_renderer")]
    public static unsafe partial NativeResult RunRenderer(NativeFrameSubmission* submission, HostCallback callback, IntPtr userData);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_run_renderer_with_assets")]
    public static unsafe partial NativeResult RunRendererWithAssets(NativeFrameSubmission* submission, HostCallback callback, IntPtr userData, NativeRuntimeAssets* assets);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_get_abi_layout")]
    public static partial NativeResult GetAbiLayout(out NativeAbiLayout layout);
    [LibraryImport("NovaCore.Native", EntryPoint = "nc_validate_planetary_patches")]
    public static unsafe partial NativeResult ValidatePlanetaryPatches(NativePlanetaryPatch* patches, uint count);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_validate_terrain_asset", StringMarshalling = StringMarshalling.Utf8)]
    public static partial NativeResult ValidateTerrainAsset(string path, ulong bodyId, uint terrainVersion, uint expectedRecordCount);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_query_planetary_physical_heights")]
    public static unsafe partial NativeResult QueryPlanetaryPhysicalHeights(NativePlanetaryHeightQuery* queries, uint count, NativePlanetaryHeightResult* results, NativePlanetaryHeightQueryAssets* assets, NativePlanetaryHeightQueryMetrics* metrics);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_initialize_planetary_mesh_preparation")]
    public static unsafe partial NativeResult InitializePlanetaryMeshPreparation(NativePlanetaryMeshPreparationAssets* assets, NativePlanetaryMeshPreparationMetrics* metrics);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_prepare_planetary_mesh")]
    public static unsafe partial NativeResult PreparePlanetaryMesh(NativePlanetaryHeightQuery* vertices, uint* indices, uint* adjacencyWords, NativePlanetaryMeshPreparationDispatch* dispatch, NativePlanetaryDisplacedVertex* displaced, NativePlanetaryPhysicalNormal* normals, NativePlanetaryMeshPreparationMetrics* metrics);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_shutdown_planetary_mesh_preparation")]
    public static partial NativeResult ShutdownPlanetaryMeshPreparation();

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_initialize_spherical_billboard_gpu_proof")]
    public static unsafe partial NativeResult InitializeSphericalBillboardGpuProof(NativeSphericalBillboardProofAssets* assets, NativeSphericalBillboardProofMetrics* metrics);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_upload_spherical_billboard_gpu_proof_topology")]
    public static unsafe partial NativeResult UploadSphericalBillboardGpuProofTopology(NativeSphericalBillboardProofTopology* topology, NativeSphericalBillboardProofMetrics* metrics);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_publish_spherical_billboard_physical_surface")]
    public static unsafe partial NativeResult PublishSphericalBillboardPhysicalSurface(NativeSphericalBillboardPhysicalSurface* surface, NativeSphericalBillboardProofMetrics* metrics);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_run_spherical_billboard_gpu_proof_frame")]
    public static unsafe partial NativeResult RunSphericalBillboardGpuProofFrame(NativeSphericalBillboardProofFrame* frame, NativeSphericalBillboardProofMetrics* metrics);

    [LibraryImport("NovaCore.Native", EntryPoint = "nc_shutdown_spherical_billboard_gpu_proof")]
    public static partial NativeResult ShutdownSphericalBillboardGpuProof();
}
