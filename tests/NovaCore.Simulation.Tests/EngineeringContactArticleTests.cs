using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Clock;
using NovaCore.Simulation.Spacecraft;
using NovaCore.Simulation.Spacecraft.Contact.Staging;
using NovaCore.Simulation.Spacecraft.Rotation;
using NovaCore.Simulation.Spacecraft.Translation;
using NovaCore.Simulation.Time;
using NovaCore.Simulation.Timeline;
using NovaCore.Simulation.Transactions;

internal static partial class EngineeringContactArticleTests
{
    private static readonly SpacecraftId Craft = new(911);
    private static readonly ReferenceFrameId Root = new(1), Body = new(912);
    private static void Check(bool value, string name)
    { if (!value) throw new InvalidOperationException("Engineering contact article: " + name); }
    private static void Near(double actual, double expected, double tolerance, string name) =>
        Check(Math.Abs(actual - expected) <= tolerance, $"{name}: actual={actual:R} expected={expected:R}");

    private sealed class Fixture : IDisposable
    {
        internal readonly EngineeringContactArticle Article;
        internal readonly SimulationClock Clock;
        internal readonly SimulationState State;
        internal readonly SimulationTransactionEngine Engine;
        internal readonly LocalContactConfiguration Configuration;
        internal readonly LocalContactSource Source;
        internal readonly LocalContactWorld World;
        internal LocalContactWorld.Receipt Receipt;
        internal long Sequence;
        internal readonly double ArticlePreparationMs, WorldPreparationMs;
        internal Fixture(bool tilted = false, bool moving = false, bool publishing = true, long debt = 0, double tiltAngle = .25)
        {
            var cold = Stopwatch.GetTimestamp(); Article = EngineeringContactArticle.Create();
            ArticlePreparationMs = Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
            var origin = moving ? new Double3(1e9, -2e9, 3e9) : Double3.Zero;
            var velocity = moving ? new Double3(11, -7, 3) : Double3.Zero;
            var frame = moving ? DoubleQuaternion.FromAxisAngle(new(1, 2, -1), .7) : DoubleQuaternion.Identity;
            var start = new SimulationInstant(1234567);
            Check(LocalContactConfiguration.TryCreateArticle(1, Root, origin, velocity, frame, Article, 64, out var config)
                == LocalContactStatus.Success, "article configuration");
            Configuration = config!;
            var linear = new SpacecraftTranslationState(Craft, Root, start, origin + frame.Rotate(new(0, 2, 0)), velocity,
                frame.Rotate(new Double3(0, -9.81 * Article.MassKilograms, 0)));
            var angular = new SpacecraftRigidBodyRotationState(Craft, start,
                frame * (tilted ? DoubleQuaternion.FromAxisAngle(Double3.UnitZ, tiltAngle) : DoubleQuaternion.Identity),
                Double3.Zero, Article.PrincipalInertia, Double3.Zero, RigidBodyRotationModel.ConstantBodyTorqueV1);
            var graph = new ReferenceFrameGraphBuilder();
            graph.Add(new ReferenceFrameNode(Root, null, ReferenceFrameKind.Ecl, "root"));
            graph.Add(new ReferenceFrameNode(Body, Root, ReferenceFrameKind.Ccf, "engineering article COM"));
            Check(SpacecraftStateStore.TryCreateTranslating([new(Craft, Root, Body, "Engineering bus/pods v1")],
                [angular], [new(Article.MassKilograms)], [linear], graph.Build(), out var store, out _), "canonical state");
            State = new(spacecraft: store); Clock = new(start, new SimulationTimeline(4));
            if (debt > 0) Clock.AdvanceByHostDuration(new(debt));
            Engine = new(Clock, State, 4, persistentContactHistoryCapacity: publishing ? 1200 : 0);
            Check(LocalContactSource.Capture(Engine, Craft, config, start + new SimulationDuration(20_000_000), out var source)
                == LocalContactStatus.Success, "source");
            Source = source!; cold = Stopwatch.GetTimestamp();
            Check(LocalContactWorld.TryCreate(Engine, Source, config!, out var world, out Receipt) == LocalContactStatus.Success, "compound world");
            World = world!; WorldPreparationMs = Stopwatch.GetElapsedTime(cold).TotalMilliseconds;
            if (publishing) Check(Engine.BeginPersistentContact(World, config!, Receipt) == LocalContactStatus.Success, "publication binding");
        }
        internal ContactHostCreditResult Credit(long ticks)
        {
            var result = Engine.AdmitContactHostTime(World, Configuration, Receipt, new(Sequence + 1, new(ticks)));
            if (result.CanonicalCommitted) Sequence++;
            return result;
        }
        internal ContactServiceResult Service()
        { var result = Engine.ServiceContactDebt(World, Configuration, Receipt); Receipt = result.Receipt; return result; }
        public void Dispose() => World.Dispose();
    }

    internal static void Cheap()
    {
        var a = EngineeringContactArticle.Create();
        Check(a.Child(0) == new EngineeringBox(new(1.5, 1, 2.5), Double3.Zero, DoubleQuaternion.Identity, 800), "bus authored SI values/order");
        Check(a.Child(1) == new EngineeringBox(new(.5, 1, 1.5), new(-1, 0, .5), DoubleQuaternion.Identity, 100), "left authored SI values/order");
        Check(a.Child(2) == new EngineeringBox(new(.5, 1, 1.5), new(1, 0, .5), DoubleQuaternion.Identity, 100), "right authored SI values/order");
        Check(a.MassKilograms == 1000 && a.CentreOfMassAssembly == new Double3(0, 0, .1), "analytic mass/weighted COM");
        // Independently reduced uniform-box + parallel-axis sums: principal tensor kg m^2.
        Near(a.Tensor.XX, 1155d / 2, 1e-12, "Ixx"); Near(a.Tensor.YY, 2545d / 3, 1e-12, "Iyy");
        Near(a.Tensor.ZZ, 875d / 2, 1e-12, "Izz");
        Check(a.Tensor.XY == 0 && a.Tensor.XZ == 0 && a.Tensor.YZ == 0, "full cross terms vanish by symmetry");
        Check(a.PrincipalInertia == new PrincipalMomentsOfInertia(a.Tensor.XX, a.Tensor.YY, a.Tensor.ZZ), "principal axes use checked full tensor");
        Near(a.BoundingRadius, Math.Sqrt(3.135), 1e-15, "furthest COM-relative bus corner");
        Check(a.SmallestFeature == .5 && a.Dimensions == new Double3(2.5, 1, 2.5), "actual small feature distinct from envelope");
        Check(a.ChildCentreBody(0) == new Double3(0, 0, -.1) && a.ChildCentreBody(1) == new Double3(-1, 0, .4) &&
            a.ChildCentreBody(2) == new Double3(1, 0, .4), "single COM subtraction for collision/presentation products");
        Check(a.Identity == EngineeringContactArticle.Create().Identity && a.Identity.Version == 1, "stable version/content identity");
        bool Create(EngineeringBox b, EngineeringBox l, EngineeringBox r, out EngineeringContactArticle? changed) =>
            EngineeringContactArticle.TryCreate(b, l, r, out changed);
        var bus = a.Child(0); var left = a.Child(1); var right = a.Child(2);
        foreach (var bad in new[] { 0d, -1, double.NaN, double.PositiveInfinity, double.Epsilon, double.MaxValue })
        {
            Check(!Create(bus with { MassKilograms = bad }, left, right, out _), "invalid/unrepresentable mass");
            Check(!Create(bus with { Dimensions = new(1.5, bad, 2.5) }, left, right, out _), "invalid/overflow/underflow dimensions/inertia");
        }
        Check(!Create(bus, left with { Orientation = DoubleQuaternion.FromAxisAngle(Double3.UnitX, .1) }, right, out _), "unsupported rotated principal axes");
        Check(!Create(bus, right, left, out _), "unsupported ordering");
        Check(!Create(bus, left with { CentreAssembly = new(-1, .1, .5) }, right, out _), "unsupported asymmetric axes");
        Check(Create(bus with { Dimensions = new(1.5, 1, 2.6) }, left, right, out var dimensions) && dimensions!.Identity != a.Identity, "dimensions cannot impersonate original");
        Check(Create(bus with { MassKilograms = 801 }, left, right, out var mass) && mass!.Identity != a.Identity, "mass cannot impersonate original");
        Check(Create(bus, left with { CentreAssembly = new(-1, 0, .6) }, right with { CentreAssembly = new(1, 0, .6) }, out var offset) &&
            offset!.Identity != a.Identity, "offset cannot impersonate original");
        using var f = new Fixture(); var before = f.Engine.CaptureContinuationClock(); var revision = f.Engine.State.Revision;
        Check(f.Configuration.ContactTolerance == .0005 && f.Configuration.SlabHalfThickness == 1, "small-feature tolerance and explicit unchanged slab");
        Near(f.Configuration.MaximumSpeed, .25 / .016667, 1e-12, "small-feature swept speed");
        for (var i = 0; i < 3; i++)
        {
            var actual = f.World.ReadArticleChildForTest(i);
            Check(actual.Dimensions == a.Child(i).Dimensions && actual.Orientation == DoubleQuaternion.Identity, "actual convex child surfaces/orientation");
            Near(Math.Sqrt((actual.CentreAssembly - a.ChildCentreBody(i)).LengthSquared), 0, f.Configuration.ContactTolerance / 8, "actual float COM child transport");
        }
        Check(!f.Configuration.AdmitsMassInertia(999, a.PrincipalInertia) &&
            !f.Configuration.AdmitsMassInertia(1000, new(1, 2, 3)), "strict article mass/inertia admission");
        Check(LocalContactConfiguration.TryCreateArticle(1, Root, new(1e18, 0, 0), Double3.Zero, DoubleQuaternion.Identity,
            a, 64, out _) == LocalContactStatus.InvalidConfiguration, "FP64 root spacing refusal before world");
        Check(LocalContactConfiguration.TryCreateArticle(1, Root, Double3.Zero, Double3.Zero, DoubleQuaternion.Identity,
            dimensions, 64, out var other) == LocalContactStatus.Success, "alternative identity setup");
        Check(LocalContactSource.Capture(f.Engine, Craft, other, f.Source.End, out _) == LocalContactStatus.InvalidSource, "canonical inertia/article mismatch");
        Check(LocalContactWorld.TryCreate(f.Engine, f.Source, other!, out _, out _) == LocalContactStatus.ConfigurationMismatch, "foreign article configuration before world allocation");
        Check(f.Engine.CaptureContinuationClock() == before && f.Engine.State.Revision == revision && f.Receipt.Step == 0, "refusal nonmutation");
        var cleanup = LocalContactWorld.VerifyArticleConstructionCleanupForTest(a);
        Check(cleanup.ReclaimedChildSlots == 7 && cleanup.RemainingPoolBytes == 0, "partial compound construction cleans all child slots and pool");
        var receipt = f.Receipt;
        Check(f.World.TryDispose() == LocalContactStatus.Success && f.World.PoolBytes == 0, "compound/children/world disposal");
        Check(f.World.TryDispose() == LocalContactStatus.Disposed && f.World.Read(f.Engine, f.Configuration, receipt, out _) == LocalContactStatus.Disposed, "exactly-once disposal/stale receipt refusal");
        Console.WriteLine("ARTICLE_DEFINITION " + JsonSerializer.Serialize(new { a.MassKilograms, a.CentreOfMassAssembly, a.Tensor,
            a.SmallestFeature, a.BoundingRadius, a.Identity }, new JsonSerializerOptions { IncludeFields = true }));
        Console.WriteLine("PASS engineering article analytical, geometry, identity, admission and lifetime gates");
    }

    // Independent geometry oracle: authored literals, mass-weighted COM and all 24 corners.
    // It intentionally does not call ChildCentreBody, BEPU bounds or a box's eight-corner oracle.
    private static (double MinY, double RestHeight, double MinX, double MaxX, double MinZ, double MaxZ) Geometry(
        Double3 centre, DoubleQuaternion orientation)
    {
        ReadOnlySpan<Double3> sizes = [new(1.5, 1, 2.5), new(.5, 1, 1.5), new(.5, 1, 1.5)];
        ReadOnlySpan<Double3> positions = [Double3.Zero, new(-1, 0, .5), new(1, 0, .5)];
        var com = new Double3(0, 0, (100 * .5 + 100 * .5) / 1000);
        double min = double.MaxValue, minOffset = double.MaxValue, minX = double.MaxValue, maxX = -double.MaxValue,
            minZ = double.MaxValue, maxZ = -double.MaxValue;
        for (var child = 0; child < 3; child++)
            for (var corner = 0; corner < 8; corner++)
            {
                var d = sizes[child]; var p = positions[child] - com + new Double3((corner & 1) == 0 ? -d.X / 2 : d.X / 2,
                    (corner & 2) == 0 ? -d.Y / 2 : d.Y / 2, (corner & 4) == 0 ? -d.Z / 2 : d.Z / 2);
                var offset = orientation.Rotate(p); var world = centre + offset;
                min = Math.Min(min, world.Y); minOffset = Math.Min(minOffset, offset.Y);
                minX = Math.Min(minX, world.X); maxX = Math.Max(maxX, world.X);
                minZ = Math.Min(minZ, world.Z); maxZ = Math.Max(maxZ, world.Z);
            }
        return (min, -minOffset, minX, maxX, minZ, maxZ);
    }

    internal static void Contacts()
    {
        foreach (var tilted in new[] { false, true }) ContactSequence(tilted, false);
    }

    internal static void CoverageTilted() => ContactSequence(true, false, reportCoverage: true);
    internal static void CoverageRegressions()
    {
        _ = ContactSequence(false, false);
        _ = ContactSequence(true, false, tiltAngle: -.25);
        _ = ContactSequence(true, true);
        var firstSelections = new int[1200 * 5]; var secondSelections = new int[1200 * 5];
        var firstHistory = new ProcessedPersistentContact[1200]; var secondHistory = new ProcessedPersistentContact[1200];
        var first = ContactSequence(true, false, selectionHistory: firstSelections, publicationHistory: firstHistory);
        var second = ContactSequence(true, false, selectionHistory: secondSelections, publicationHistory: secondHistory);
        Check(first.SequenceEqual(second), "same-build complete endpoint repeat identity, 1200/1200");
        Check(firstSelections.SequenceEqual(secondSelections), "same-build complete selected-contact order and identity, 1200/1200");
        Check(firstHistory.SequenceEqual(secondHistory), "same-build complete publication history, 1200/1200");
        Console.WriteLine("COVERAGE_REPEAT same-build endpoint_identity=1200/1200 selection_identity=1200/1200 publication_history=1200/1200 PASS");
    }

    internal static void CoverageRestWitness()
    {
        using var f = new Fixture(tilted:true);
        var raw = new CompoundContactSelector.Candidate[12]; var predicted = new double[12];
        var selected = new CompoundContactSelector.Candidate[4]; var selectedPrediction = new double[4];
        var native = new int[4]; var previous = new int[4]; var current = new int[4];
        var disappeared=0;var equivalent=0;var distinctNativeKept=0;var distinctNativeDropped=0;var witnesses=0;var changes=0;
        var transitionSteps = new List<int>();
        var rawChanges = 0; var nativeChanges = 0; var priorRaw = Array.Empty<int>(); var priorNative = new int[4];
        var maximumRestDepthSpread = 0d;
        for(var step=1;step<=1200;step++)
        {
            Check(f.Credit(step%3==1?16666:16667).Status==ContactHostCreditStatus.Accepted,"rest witness exact credit");
            Check(f.Service().Published==1,"rest witness publication");
            if(step<=600)continue;
            var count=f.World.CopyCompoundSelectionForTest(selected,selectedPrediction);
            var rawCount=f.World.CopyCompoundRawForTest(raw,predicted,native);
            Check(count==4,"rest witness four selected");
            var rawFeatures = raw.Take(rawCount).Select(c=>c.Contact.FeatureId).Order().ToArray();
            var nativeFeatures = native.Order().ToArray();
            maximumRestDepthSpread = Math.Max(maximumRestDepthSpread,
                (double)raw.Take(rawCount).Max(c=>c.Contact.Depth)-raw.Take(rawCount).Min(c=>c.Contact.Depth));
            if(step>601)
            {
                if(!rawFeatures.SequenceEqual(priorRaw)) rawChanges++;
                if(!nativeFeatures.SequenceEqual(priorNative)) nativeChanges++;
            }
            for(var i=0;i<4;i++)current[i]=selected[i].Contact.FeatureId;
            if(step>601)
            {
                var changed=false;
                foreach(var old in previous)
                {
                    if(current.Contains(old))continue;
                    changed=true;
                    var oldIndex=Array.FindIndex(raw,0,rawCount,c=>c.Contact.FeatureId==old);
                    if(oldIndex<0){disappeared++;continue;}
                    var oldContact=raw[oldIndex].Contact;var sameCoverage=false;
                    for(var j=0;j<count;j++)
                    {
                        if(previous.Contains(current[j]))continue;
                        var incoming=selected[j].Contact;
                        if(incoming.Normal==oldContact.Normal &&
                            System.Numerics.Vector3.Distance(incoming.Offset,oldContact.Offset)<=f.Configuration.ContactTolerance &&
                            Math.Abs(incoming.Depth-oldContact.Depth)<=f.Configuration.ContactTolerance) sameCoverage=true;
                    }
                    if(sameCoverage){equivalent++;continue;}
                    if(native.Contains(old))distinctNativeKept++;else distinctNativeDropped++;
                    if(witnesses<4)
                    {
                        witnesses++;
                        Console.WriteLine("COVERAGE_REST_TRANSITION "+JsonSerializer.Serialize(new {step,removed=old,native_kept=native.Contains(old),
                            previous,selected=current,native,
                            raw=raw.Take(rawCount).Select((c,j)=>new {c.ChildA,c.RawFeature,c.Contact.FeatureId,c.Contact.Depth,prediction=predicted[j],
                                offset=new[]{c.Contact.Offset.X,c.Contact.Offset.Y,c.Contact.Offset.Z},normal=new[]{c.Contact.Normal.X,c.Contact.Normal.Y,c.Contact.Normal.Z}})}));
                    }
                }
                if(changed){changes++;transitionSteps.Add(step);}
            }
            current.CopyTo(previous,0);
            priorRaw=rawFeatures;priorNative=nativeFeatures;
        }
        Console.WriteLine("COVERAGE_REST_COUNTS "+JsonSerializer.Serialize(new {changes,disappeared,equivalent,distinctNativeKept,distinctNativeDropped,
            resolution=f.Configuration.ContactTolerance,comparedTransitions=599,transitionSteps,rawChanges,nativeChanges,maximumRestDepthSpread}));
    }

    private static SpacecraftMotion[] ContactSequence(bool tilted, bool moving, bool reportCoverage = false, double tiltAngle = .25,
        int[]? selectionHistory = null, ProcessedPersistentContact[]? publicationHistory = null)
    {
        using var f = new Fixture(tilted, moving, tiltAngle: tiltAngle);
        var endpoints = new SpacecraftMotion[1200];
        var generation = f.World.Generation; var originalTimeline = f.Clock.Timeline.Revision;
        var peak = 0d; var peakStep = 0; var maxOmega = 0d; var supported = 0; var childMask = 0;
        var supportStart = Double3.Zero; var maxDrift = 0d; var n16666 = 0; var n16667 = 0;
        var firstStepMs = 0d; var firstContactMs = 0d; var firstContact = 0;
        Span<CompoundContactSelector.Candidate> supportSelection = stackalloc CompoundContactSelector.Candidate[4];
        Span<double> supportPrediction = stackalloc double[4];
        Span<int> priorFeatures = stackalloc int[4]; priorFeatures.Fill(int.MinValue);
        Span<int> features = stackalloc int[4];
        var restingSetChanges = 0;
        for (var i = 1; i <= 1200; i++)
        {
            var duration = i % 3 == 1 ? 16666 : 16667;
            Check(f.Credit(duration).Status == ContactHostCreditStatus.Accepted, "exact owner credit");
            var timer = Stopwatch.GetTimestamp(); var result = f.Service(); var stepMs = Stopwatch.GetElapsedTime(timer).TotalMilliseconds;
            Check(result.Published == 1 && result.Status == (i == 1200 ? ContactServiceStatus.Completed : ContactServiceStatus.AwaitingDebt),
                $"article service step={i} status={result.Status} authority={result.AuthorityStatus}");
            Check(f.World.Read(f.Engine, f.Configuration, f.Receipt, out var output) == LocalContactStatus.Success, "acknowledged endpoint");
            endpoints[i - 1] = output.Motion;
            if (i == 1) firstStepMs = stepMs;
            if (firstContact == 0 && output.ArticleContactChildMask != 0) { firstContact = i; firstContactMs = stepMs; }
            Check(result.Observation.Translation.PositionRoot == output.Motion.PositionRoot && result.Observation.Rotation.OrientationLocalToParent == output.Motion.BodyToRoot,
                "exact copied canonical endpoint");
            f.Source.TryEndpoint(i, out var target);
            Check(f.Clock.CurrentTime == target && target.Ticks == f.Source.Motion.Time.Ticks + (long)i * 1_000_000 / 60 &&
                f.Clock.PendingSimulationDebt.Ticks == 0, "original T0 exact lattice/debt conservation");
            if (duration == 16666) n16666++; else n16667++;
            Check(f.Engine.State.Revision.Value == (ulong)i && f.Clock.Timeline.Revision == originalTimeline &&
                f.Engine.ProcessedPersistentContactCount == i && f.World.Generation == generation, "revision/history/world continuity");
            Check(f.Engine.TryGetProcessedPersistentContact(i - 1, out var history) && history.Episode.Article == f.Article.Identity &&
                history.Episode.Start == f.Source.Motion.Time && history.Episode.Origin == f.Configuration.OriginRoot, "immutable deterministic article/frame provenance");
            if(publicationHistory is not null) publicationHistory[i-1]=history;
            if(selectionHistory is not null)
            {
                var count=f.World.CopyCompoundSelectionForTest(supportSelection,supportPrediction);
                selectionHistory[(i-1)*5]=count;
                for(var j=0;j<4;j++) selectionHistory[(i-1)*5+j+1]=j<count?supportSelection[j].Contact.FeatureId:int.MinValue;
            }
            var elapsed = (target.Ticks - f.Source.Motion.Time.Ticks) / 1_000_000d;
            var local = f.Configuration.LocalToRoot.Conjugate().Rotate(output.Motion.PositionRoot -
                f.Configuration.OriginRoot - f.Configuration.OriginVelocityRoot * elapsed);
            var q = f.Configuration.LocalToRoot.Conjugate() * output.Motion.BodyToRoot;
            var geometry = Geometry(local, q);
            if (-geometry.MinY > peak) { peak = -geometry.MinY; peakStep = i; }
            if (reportCoverage && (peakStep == i || i is >= 30 and <= 40))
            {
                var contacts = new CompoundContactSelector.Candidate[4]; var prediction = new double[4];
                var count = f.World.CopyCompoundSelectionForTest(contacts, prediction);
                Console.WriteLine("COVERAGE_STEP " + JsonSerializer.Serialize(new { step=i, minY=geometry.MinY,
                    omega=new[]{output.Motion.AngularVelocityBody.X,output.Motion.AngularVelocityBody.Y,output.Motion.AngularVelocityBody.Z},
                    contacts=contacts.Take(count).Select((c,j)=>new {c.ChildA,c.ChildB,c.RawFeature,c.Contact.FeatureId,
                        offset=new[]{c.Contact.Offset.X,c.Contact.Offset.Y,c.Contact.Offset.Z},
                        normal=new[]{c.Contact.Normal.X,c.Contact.Normal.Y,c.Contact.Normal.Z},c.Contact.Depth,prediction=prediction[j],
                        selectionOrder=j}) }));
            }
            maxOmega = Math.Max(maxOmega, Math.Sqrt(output.Motion.AngularVelocityBody.LengthSquared));
            Check(peak <= .020, $"peak penetration step={i} actual={peak:R} limit=.020");
            if (i == 601) supportStart = local;
            if (i > 600)
            {
                var selectionCount = f.World.CopyCompoundSelectionForTest(supportSelection,supportPrediction);
                features.Fill(int.MinValue);
                for(var j=0;j<selectionCount;j++) features[j]=supportSelection[j].Contact.FeatureId;
                features.Sort();
                if(i>601 && !features.SequenceEqual(priorFeatures)) restingSetChanges++;
                features.CopyTo(priorFeatures);
                var drift = Math.Sqrt((local - supportStart).LengthSquared); maxDrift = Math.Max(maxDrift, drift);
                var speed = Math.Sqrt((output.Motion.VelocityRoot - f.Configuration.OriginVelocityRoot).LengthSquared);
                var tolerance = f.Configuration.ContactTolerance;
                Check(Math.Abs(geometry.MinY) <= 2 * tolerance && Math.Abs(local.Y - geometry.RestHeight) <= 2 * tolerance &&
                    drift <= tolerance && speed <= tolerance / (16667d / 1_000_000),
                    $"support step={i} minY={geometry.MinY:R} expectedY={geometry.RestHeight:R} drift={drift:R} speed={speed:R}");
                Check(geometry.MinX >= -64 && geometry.MaxX <= 64 && geometry.MinZ >= -64 && geometry.MaxZ <= 64 &&
                    local.X >= geometry.MinX && local.X <= geometry.MaxX && local.Z >= geometry.MinZ && local.Z <= geometry.MaxZ,
                    "independent article footprint remains on slab and contains COM projection");
                Check(output.ConstraintCount > 0 && System.Numerics.BitOperations.PopCount((uint)output.ArticleContactChildMask) >= 2,
                    $"distinct supporting child manifolds step={i} mask={output.ArticleContactChildMask}");
                childMask |= output.ArticleContactChildMask; supported++;
            }
        }
        Check(supported == 600 && childMask == 7 && n16666 == 400 && n16667 == 800, "all final support/children/exact intervals");
        Check(!tilted || maxOmega > .1, "off-centre angular response");
        Console.WriteLine("ARTICLE_CONTACT " + JsonSerializer.Serialize(new { tilted, tiltAngle, moving, supported, childMask, peak_m = peak, peakStep, restingSetChanges,
            max_drift_m = maxDrift, max_omega = maxOmega, n16666, n16667, ticks = 20_000_000, revision = 1200, history = 1200,
            article_preparation_ms = f.ArticlePreparationMs, world_preparation_ms = f.WorldPreparationMs, first_step_ms = firstStepMs,
            first_contact_step = firstContact, first_contact_ms = firstContactMs }));
        return endpoints;
    }
}
