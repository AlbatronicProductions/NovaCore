using NovaCore.Graphics;
using NovaCore.Interop;

internal static class PlanetarySphericalBillboardGpuRuntimeTests
{
    public static void Run()
    {
        var root=PlanetarySphericalBillboardGpuProof.FindRepositoryRoot(AppContext.BaseDirectory);
        var library=PlanetarySphericalBillboardGpuProofLibrary.Load(Path.Combine(root,"assets","planetary-topology"));
        Require(library.Count==3&&library.Select(value=>value.Level).SequenceEqual(new[]{PlanetarySphericalBillboardProofLevel.Orbital,PlanetarySphericalBillboardProofLevel.IntermediateApproach,PlanetarySphericalBillboardProofLevel.SurfacePupil}),"manifest loads exactly the three P2S2 topology levels");
        Require(library.All(value=>value.RuntimeTopologyGenerationCount==0&&value.LegacyIdentityDependencyCount==0&&value.ImmutableGpuBytes>0),"runtime descriptions are immutable artifact consumers without patch or runtime-generation identity");
        Require(typeof(PlanetarySphericalBillboardGpuRuntimeDescription).GetProperties().All(property=>!property.Name.Contains("Patch",StringComparison.OrdinalIgnoreCase))&&
            typeof(PlanetarySphericalBillboardGpuProofSession).GetMethods().All(method=>!method.Name.Contains("Patch",StringComparison.OrdinalIgnoreCase)),"public P2S3 runtime ABI has no patch-shaped dependency");

        using var session=new PlanetarySphericalBillboardGpuProofSession(Path.Combine(root,"build","native-ninja","shaders"));
        Require(session.TryRunWithoutTopology()==NativeResult.InvalidArgument,"draw publication rejects missing topology/output readiness");
        var uploadCount=0u;var replacementCount=0u;var frameIndex=0u;NativeSphericalBillboardProofMetrics last=default;
        foreach(var description in library)
        {
            if(uploadCount>0)replacementCount++;
            var uploaded=session.Upload(description);uploadCount++;
            Console.WriteLine($"P2S3 upload: level={description.Level}; readiness={uploaded.Readiness}; uploads={uploaded.TopologyUploadCount}; expectedUploads={uploadCount}; hash=0x{uploaded.TopologyHash:X16}");
            Require(uploaded.Readiness==1&&uploaded.TopologyUploadCount==uploadCount&&uploaded.TopologyHash==description.Topology.TopologyHash,"upload publishes topology-ready only with manifest identity");
            Require(session.TryRunWithStaleTopologyIdentity(description)==NativeResult.InvalidArgument,"stale topology identity cannot publish GPU output or a draw");
            Require(uploaded.ActiveTopologyBytes==description.ImmutableGpuBytes&&uploaded.RuntimeTopologyGenerationCount==0,"GPU topology bytes match the immutable ABI and runtime generation remains zero");
            Require(uploaded.TopologyReplacementCount==replacementCount,"first upload/replacement count is deterministic");
            var repeated=session.Upload(description);Require(repeated.TopologyUploadCount==uploadCount,"identical topology upload is reused");
            var rendered=session.RunFrame(description,frameIndex++);last=rendered;
            Require(rendered.Readiness==7&&rendered.PreparedVertices==rendered.BaseVertexCount&&rendered.VisibleTriangles>0&&rendered.PixelChecksum!=0,"GPU output and indirect draw become ready only after completed preparation/culling/render");
            Require(rendered.VisibleTriangles+rendered.BackfaceRejected+rendered.FrustumRejected+rendered.InvalidRejected==rendered.BaseTriangleCount,"conservative culling classifies every actual displaced triangle");
            Require(rendered.IndirectIndexCount==rendered.VisibleTriangles*3&&rendered.IndirectDrawCount==1&&rendered.InvalidCommands==0&&rendered.OverflowCount==0,"visible-index compaction creates one bounded valid indirect draw");
            Require(rendered.ValidationErrors==0&&rendered.FrameSlot==(frameIndex-1)%PlanetarySphericalBillboardGpuProofSession.FrameResourceCount,"Vulkan validation is clean and output is frame-indexed");
            var cameraUpdate=session.RunFrame(description,frameIndex++,cameraDistanceRadii:2.6);last=cameraUpdate;
            Require(cameraUpdate.TopologyUploadCount==uploadCount&&cameraUpdate.FrameOutputWriteCount==rendered.FrameOutputWriteCount+1,"camera-only update writes a new frame output without topology upload");
        }

        // Re-upload the selected surface level is a no-op; synthetic work repeats the
        // immutable artifact in the GPU dispatch and does not create runtime topology.
        var surface=library[^1];session.Upload(surface);
        foreach(var workload in new[]{(100_000u,200_000u),(250_000u,500_000u),(500_000u,1_000_000u)})
        {
            last=session.RunFrame(surface,frameIndex++,workload.Item1,workload.Item2,render:false);
            Require(last.PreparedVertices==workload.Item1&&last.WorkTriangleCount==workload.Item2&&last.RuntimeTopologyGenerationCount==0,"synthetic scaling dispatch preserves authored topology identity");
            Require(last.OverflowCount==0&&last.InvalidCommands==0&&last.ValidationErrors==0,"synthetic scaling remains bounded and validation-clean");
            Console.WriteLine($"P2S3 scaling: vertices={workload.Item1}; triangles={workload.Item2}; prepareMs={last.PreparationMilliseconds:F6}; normalMs={last.NormalMilliseconds:F6}; cullMs={last.CullingMilliseconds:F6}; compactMs={last.CompactionMilliseconds:F6}; gpuMs={last.GpuTotalMilliseconds:F6}");
        }
        Require(last.FramePositionBytes==PlanetarySphericalBillboardGpuProofSession.MaximumVertexWorkItems*16ul*PlanetarySphericalBillboardGpuProofSession.FrameResourceCount&&
            last.FrameNormalBytes==last.FramePositionBytes&&last.FrameVisibilityBytes==PlanetarySphericalBillboardGpuProofSession.MaximumTriangleWorkItems*4ul*PlanetarySphericalBillboardGpuProofSession.FrameResourceCount,"500k-vertex/1M-triangle frame allocations follow the actual three-slot ABI");
        Require(last.FrameWaitCount>0,"frame-slot reuse waits for prior GPU ownership");
        Console.WriteLine($"P2S3 runtime: uploads={last.TopologyUploadCount}; replacements={last.TopologyReplacementCount}; frameWrites={last.FrameOutputWriteCount}; cullDispatches={last.CullingDispatchCount}; indirectSubmissions={last.IndirectSubmissionCount}; bytes={last.TotalAllocatedBytes}; validation={last.ValidationErrors}");
    }

    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
