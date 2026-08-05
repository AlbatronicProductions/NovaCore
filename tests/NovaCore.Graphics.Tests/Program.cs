using System.Runtime.InteropServices;
using NovaCore.Core;
using NovaCore.Core.Camera;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Graphics;
using NovaCore.Interop;

var tests = new (string, Action)[]
{
    ("MeshHandle", MeshHandleTest),
    ("Transport layout", LayoutTest),
    ("Transform conversion", TransformTest),
    ("Camera relative", RelativeTest),
    ("Batches and capacity", BatchTest),
    ("Resolved render transport", ResolvedTransportTest),
    ("Camera snapshot allocation", CameraSnapshotAllocationTest),
};
foreach (var (name, test) in tests) { test(); Console.WriteLine($"PASS {name}"); }

static void MeshHandleTest() { Check(!MeshHandle.Invalid.IsValid, "zero invalid"); Check(MeshHandle.Triangle.IsValid, "triangle valid"); }
static void LayoutTest()
{
    Check(Marshal.SizeOf<NativeEncodedPosition>() == 32, "encoded size"); Check(Marshal.SizeOf<NativeCameraData>() == 96, "native camera size"); Check(Marshal.OffsetOf<NativeCameraData>(nameof(NativeCameraData.Position)).ToInt32() == 0, "native camera position offset"); Check(Marshal.OffsetOf<NativeCameraData>(nameof(NativeCameraData.ViewProjection)).ToInt32() == 32, "native camera matrix offset"); Check(Marshal.SizeOf<GpuCameraData>() == 96, "GPU camera size"); Check(Marshal.OffsetOf<GpuCameraData>(nameof(GpuCameraData.Position)).ToInt32() == 0, "GPU camera position offset"); Check(Marshal.OffsetOf<GpuCameraData>(nameof(GpuCameraData.ViewProjection)).ToInt32() == 32, "GPU camera matrix offset"); Check(Marshal.SizeOf<NativeRenderTransform>() == 32, "transform size"); Check(Marshal.SizeOf<NativeRenderObject>() == 80, "object stride"); Check(Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Position)).ToInt32() == 0, "position offset"); Check(Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Transform)).ToInt32() == 32, "transform offset"); Check(Marshal.OffsetOf<NativeRenderObject>(nameof(NativeRenderObject.Mesh)).ToInt32() == 64, "mesh offset"); Check(Marshal.SizeOf<NativeDrawBatch>() == 16, "batch stride"); Check(Marshal.SizeOf<NativeInputState>() == 48, "input size"); Check(Marshal.OffsetOf<NativeInputState>(nameof(NativeInputState.MouseWheelDetents)).ToInt32() == 44, "input wheel offset"); Check(NativeRuntime.GetAbiLayout(out var abi) == NativeResult.Success && abi.CameraDataSize == 96 && abi.CameraPositionOffset == 0 && abi.CameraViewProjectionOffset == 32 && abi.RenderObjectSize == 80 && abi.RenderObjectTransformOffset == 32 && abi.RenderObjectMeshOffset == 64 && abi.InputStateSize == 48 && abi.InputMouseWheelDetentsOffset == 44, "native ABI layout");
}
static void TransformTest() { var t = RenderTransform.FromAuthoritative(new DoubleQuaternion(0, 0, Math.Sqrt(.5), Math.Sqrt(.5)), new Double3(-1, 2, 3)); Check(t.Rotation.W > .7f && t.Scale.X == -1, "conversion/negative scale policy"); Check(FloatQuaternion.Identity == new FloatQuaternion(0, 0, 0, 1), "identity"); }
static void RelativeTest() { var camera = EncodedPosition.Encode(new Double3(4e12, -3e12, 7e12)); var positive = EncodedPosition.Resolve(EncodedPosition.Encode(new Double3(4e12 + .25, -3e12, 7e12)), camera); var negative = EncodedPosition.Resolve(EncodedPosition.Encode(new Double3(4e12 - .25, -3e12, 7e12)), camera); Check(positive.Value.X > 0 && negative.Value.X < 0, "relative signs"); }
static void BatchTest()
{
    var frame = new ReferenceFrameId(1); var position = new UniversePosition(new Double3(4e12, 0, 0), frame); var camera = Camera(position);
    var submission = new RenderFrameSubmission(1000); submission.Begin(camera); for (var i = 0; i < 1000; i++) submission.Add(new UniversePosition(new Double3(4e12 + i, 0, 0), frame), DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Triangle); submission.Complete(); Check(submission.ObjectCount == 1000 && submission.BatchCount == 1 && submission.Batches[0].ObjectCount == 1000, "automatic stable batch");
    var small = new RenderFrameSubmission(1); small.Begin(camera); small.Add(position, DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Triangle); Throws<InvalidOperationException>(() => small.Add(position, DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Triangle));
    var invalid = new RenderFrameSubmission(2); invalid.Begin(camera); Throws<ArgumentOutOfRangeException>(() => invalid.Add(position, DoubleQuaternion.Identity, new Double3(1, 1, 1), MeshHandle.Invalid));
}
static void ResolvedTransportTest()
{
    var root = new ReferenceFrameId(1); var other = new ReferenceFrameId(2); var cameraRoot = new UniversePosition(new Double3(4e12, -3e12, 7e12), root); var camera = Camera(cameraRoot);
    var source = new[] { Object(1, cameraRoot, MeshHandle.Triangle), Object(2, new UniversePosition(cameraRoot.Value + new Double3(.25, 0, 0), root), new MeshHandle(2)), Object(3, new UniversePosition(cameraRoot.Value + new Double3(.5, 0, 0), root), MeshHandle.Triangle) };
    Check(ResolvedRenderSnapshot.TryCreate(source, out var snapshot, out var status) && status == ResolvedRenderSnapshotStatus.Success && snapshot is not null, "valid snapshot");
    var frozenFirst = snapshot!.Objects[0]; source[0] = Object(9, new UniversePosition(Double3.Zero, root), MeshHandle.Invalid); Check(snapshot.Objects[0] == frozenFirst && snapshot.Count == 3, "snapshot copied caller data");
    Check(snapshot.Objects[0].Id.Value == 1 && snapshot.Objects[1].Id.Value == 2 && snapshot.Objects[2].Id.Value == 3, "caller order retained");
    Check(!ResolvedRenderSnapshot.TryCreate([], out _, out status) && status == ResolvedRenderSnapshotStatus.Empty, "empty rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(0, cameraRoot, MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.InvalidObjectId, "zero ID rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, cameraRoot, MeshHandle.Triangle), Object(1, cameraRoot, MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.DuplicateObjectId, "duplicate ID rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, new UniversePosition(new Double3(double.NaN, 0, 0), root), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.NonFinitePosition, "non-finite position rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([new ResolvedRenderObject(new RenderObjectId(1), cameraRoot, default, new Double3(1, 1, 1), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.InvalidOrientation, "invalid orientation rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([new ResolvedRenderObject(new RenderObjectId(1), cameraRoot, DoubleQuaternion.Identity, new Double3(double.NaN, 1, 1), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.NonFiniteScale, "non-finite scale rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, cameraRoot, MeshHandle.Invalid)], out _, out status) && status == ResolvedRenderSnapshotStatus.InvalidMeshHandle, "invalid mesh rejected");
    Check(!ResolvedRenderSnapshot.TryCreate([Object(1, cameraRoot, MeshHandle.Triangle), Object(2, new UniversePosition(cameraRoot.Value, other), MeshHandle.Triangle)], out _, out status) && status == ResolvedRenderSnapshotStatus.MixedRootFrame, "mixed roots rejected");

    var destination = new RenderFrameSubmission(3); Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, destination) == ResolvedRenderSubmissionBuildStatus.Success, "transport build"); Check(destination.ObjectCount == 3 && destination.BatchCount == 3 && destination.Batches[0].FirstObject == 0 && destination.Batches[1].FirstObject == 1 && destination.Batches[2].FirstObject == 2, "stable contiguous batches"); Check(destination.Objects[1].Position == EncodedPosition.Encode(cameraRoot.Value + new Double3(.25, 0, 0)), "sole encoder output"); Check(EncodedPosition.Resolve(destination.Objects[1].Position, camera.Position).Value.X > 0, "large-root relative separation");
    var retainedObject = destination.Objects[0]; var retainedCount = destination.ObjectCount; var retainedBatches = destination.BatchCount;
    Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, new UniversePosition(cameraRoot.Value, other), destination) == ResolvedRenderSubmissionBuildStatus.CameraRootMismatch, "camera root mismatch"); Check(destination.ObjectCount == retainedCount && destination.BatchCount == retainedBatches && destination.Objects[0] == retainedObject, "mismatch atomicity");
    var small = new RenderFrameSubmission(2); Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, small) == ResolvedRenderSubmissionBuildStatus.DestinationCapacityExceeded && small.ObjectCount == 0 && small.BatchCount == 0, "object and batch capacity protected");
    var badCamera = camera; badCamera.ViewProjection.C0R0 = float.NaN; Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, badCamera, cameraRoot, destination) == ResolvedRenderSubmissionBuildStatus.InvalidCameraData && destination.ObjectCount == retainedCount, "invalid camera atomicity");
    var hash = TransportHash(destination); Check(TransportHash(destination) == hash, "transport hash repeatability"); Console.WriteLine($"Deterministic render-transport hash: 0x{hash:X16}");
    _ = ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, destination); var before = GC.GetAllocatedBytesForCurrentThread(); ulong checksum = 14695981039346656037;
    for (var i = 0; i < 100_000; i++) { Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, cameraRoot, destination) == ResolvedRenderSubmissionBuildStatus.Success, "warm build"); checksum = Mix(checksum, (ulong)BitConverter.SingleToInt32Bits(destination.Objects[1].Position.HighX)); }
    Check(GC.GetAllocatedBytesForCurrentThread() == before && checksum != 0, "warm successful builds allocate zero bytes");
    before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 100_000; i++) Check(ResolvedRenderSubmissionBuilder.TryBuild(snapshot, camera, new UniversePosition(cameraRoot.Value, other), destination) == ResolvedRenderSubmissionBuildStatus.CameraRootMismatch, "warm mismatch"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm mismatch builds allocate zero bytes");
}
static void CameraSnapshotAllocationTest()
{
    var root = new ReferenceFrameId(1); var snapshot = new ReferenceFrameSnapshot([(new ReferenceFrameDefinition(root, null, ReferenceFrameKind.Ecl, "root"), CelestialFrameFactory.RootEcl())]); var resolver = new ReferenceFrameResolver(snapshot); var state = new CameraState(new FramePosition(root, new Double3(4e12, -3e12, 7e12)), DoubleQuaternion.Identity, new CameraProjection(Math.PI / 3d, 16d / 9d, .01d, 1000d), CameraMode.Free);
    Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var first, out _, out _), "camera snapshot setup"); var hash = CameraHash(first); Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out var second, out _, out _) && CameraHash(second) == hash, "camera snapshot deterministic result");
    var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 100_000; i++) Check(CameraRenderSnapshotBuilder.TryBuild(state, resolver, root, out _, out _, out _), "warm camera snapshot"); Check(GC.GetAllocatedBytesForCurrentThread() == before, "warm camera snapshots allocate zero bytes"); Console.WriteLine($"Deterministic camera snapshot hash: 0x{hash:X16}");
}
static ResolvedRenderObject Object(uint id, UniversePosition position, MeshHandle mesh) => new(new RenderObjectId(id), position, DoubleQuaternion.Identity, new Double3(1, 1, 1), mesh);
static GpuCameraData Camera(in UniversePosition position) => new() { Position = EncodedPosition.Encode(position.Value), ViewProjection = new Float4x4 { C0R0 = 1, C1R1 = 1, C2R2 = 1, C3R3 = 1 } };
static ulong TransportHash(RenderFrameSubmission submission) { ulong hash = 14695981039346656037; foreach (ref readonly var value in submission.Objects) { hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(value.Position.HighX)); hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(value.Position.LowX)); hash = Mix(hash, value.Mesh.Value); } foreach (ref readonly var batch in submission.Batches) { hash = Mix(hash, batch.Mesh.Value); hash = Mix(hash, batch.FirstObject); hash = Mix(hash, batch.ObjectCount); } return hash; }
static ulong CameraHash(in GpuCameraData camera) { ulong hash = 14695981039346656037; hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.Position.HighX)); hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.ViewProjection.C0R0)); hash = Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.ViewProjection.C1R1)); return Mix(hash, (ulong)BitConverter.SingleToInt32Bits(camera.ViewProjection.C2R2)); }
static ulong Mix(ulong hash, ulong value) => (hash ^ value) * 1099511628211UL;
static void Throws<T>(Action action) where T : Exception { try { action(); throw new Exception($"Expected {typeof(T).Name}"); } catch (T) { } }
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
