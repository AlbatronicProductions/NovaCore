using NovaCore.Core;
using NovaCore.Core.Surface;

namespace NovaCore.Graphics;

public readonly record struct PlanetaryProductionBillboardView(
    double AltitudeMetres,
    Double3 CameraBodyDirection,
    int ViewportWidth,
    int ViewportHeight,
    double VerticalFovRadians,
    ulong CompletedFrame)
{
    public bool IsValid => double.IsFinite(AltitudeMetres) && AltitudeMetres > 0d &&
        CameraBodyDirection.IsFinite && CameraBodyDirection.LengthSquared > 0d &&
        ViewportWidth > 0 && ViewportHeight > 0 && double.IsFinite(VerticalFovRadians) &&
        VerticalFovRadians is > 0d and < Math.PI;
}

public readonly record struct PlanetaryProductionBillboardSelection(
    int Level,
    double BaseErrorPixels,
    uint RequiredTesFactor,
    bool Urgent);

/// <summary>
/// Metadata-driven production selector. The selected level is presentation
/// density only; it never participates in canonical physical identity.
/// </summary>
public sealed class PlanetaryProductionSphericalBillboardSelector
{
    private readonly IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> _levels;
    private int _candidate = -1;
    private ulong _candidateFirstFrame;

    public PlanetaryProductionSphericalBillboardSelector(
        IReadOnlyList<PlanetaryProductionSphericalBillboardTopology> levels)
    {
        ArgumentNullException.ThrowIfNull(levels);
        if (levels.Count != 18 || levels.Select(level => level.Level).Where((level, index) => level != index).Any())
            throw new ArgumentException("The production selector requires the complete ordered 18-level library.", nameof(levels));
        _levels = levels;
    }

    public int CurrentLevel { get; private set; } = -1;
    public int InFlightLevel { get; private set; } = -1;

    public PlanetaryProductionBillboardSelection Evaluate(
        in PlanetaryProductionBillboardView view,
        bool publicationInFlight)
    {
        if (!view.IsValid) throw new ArgumentOutOfRangeException(nameof(view));
        var desired = ChooseCoarsest(view, out var error, out var factor);
        var urgent = error > _levels[desired].Error.UrgentPixels;
        if (CurrentLevel < 0)
        {
            CurrentLevel = desired;
            ResetCandidate();
            return new(desired, error, factor, urgent);
        }

        if (publicationInFlight)
        {
            // A noisy sample cannot reverse a transaction already preparing.
            InFlightLevel = InFlightLevel < 0 ? CurrentLevel : InFlightLevel;
            return Describe(InFlightLevel, view);
        }

        if (desired == CurrentLevel)
        {
            ResetCandidate();
            return Describe(CurrentLevel, view);
        }

        var currentError = ProjectedError(_levels[CurrentLevel], view);
        var crossesSchmittBoundary = desired > CurrentLevel
            ? currentError > _levels[CurrentLevel].Error.EntryPixels
            : currentError < _levels[CurrentLevel].Error.ReturnPixels;
        if (!crossesSchmittBoundary)
        {
            ResetCandidate();
            return Describe(CurrentLevel, view);
        }

        var isUrgent = desired > CurrentLevel && currentError > _levels[CurrentLevel].Error.UrgentPixels;
        if (_candidate != desired)
        {
            _candidate = desired;
            _candidateFirstFrame = view.CompletedFrame;
        }
        var persisted = view.CompletedFrame >= _candidateFirstFrame + 1;
        if (isUrgent || persisted)
        {
            InFlightLevel = desired;
            ResetCandidate();
            return Describe(desired, view) with { Urgent = isUrgent };
        }
        return Describe(CurrentLevel, view);
    }

    public void CommitPublication(int level)
    {
        if (level < 0 || level >= _levels.Count || (InFlightLevel >= 0 && level != InFlightLevel))
            throw new InvalidOperationException("Only the coherently selected incoming level may publish.");
        CurrentLevel = level;
        InFlightLevel = -1;
        ResetCandidate();
    }

    public void CancelInitialSelectionForTests()
    {
        CurrentLevel = -1;
        InFlightLevel = -1;
        ResetCandidate();
    }

    private int ChooseCoarsest(in PlanetaryProductionBillboardView view, out double error, out uint factor)
    {
        for (var i = 0; i < _levels.Count; i++)
        {
            var candidate = _levels[i];
            var candidateError = ProjectedError(candidate, view);
            var required = RequiredTesFactor(candidate, candidateError);
            var altitudeAllowed = i == 0 || view.AltitudeMetres <= candidate.Error.MaximumAltitudeMetres;
            if (altitudeAllowed && required <= candidate.Error.MaximumTesFactor &&
                candidateError / Math.Max(1u, required) <= candidate.Error.TesTargetMaximumPixels)
            {
                error = candidateError;
                factor = required;
                return i;
            }
        }
        var finest = _levels[^1];
        error = ProjectedError(finest, view);
        factor = RequiredTesFactor(finest, error);
        return finest.Level;
    }

    private PlanetaryProductionBillboardSelection Describe(int level, in PlanetaryProductionBillboardView view)
    {
        var error = ProjectedError(_levels[level], view);
        return new(level, error, RequiredTesFactor(_levels[level], error),
            error > _levels[level].Error.UrgentPixels);
    }

    private static double ProjectedError(
        PlanetaryProductionSphericalBillboardTopology topology,
        in PlanetaryProductionBillboardView view)
    {
        var radius = PlanetaryProductionSphericalBillboardTopologyGenerator.EarthRadiusMetres;
        var angular = topology.Error.PupilSpacingRadians;
        var halfAngle = Math.Atan2(radius * Math.Sin(angular),
            radius + view.AltitudeMetres - radius * Math.Cos(angular));
        return halfAngle / (view.VerticalFovRadians * .5d) * (view.ViewportHeight * .5d);
    }

    private static uint RequiredTesFactor(
        PlanetaryProductionSphericalBillboardTopology topology,
        double pixels)
    {
        return Math.Clamp((uint)Math.Ceiling(pixels / topology.Error.TesTargetMaximumPixels), 1u, 64u);
    }

    private void ResetCandidate()
    {
        _candidate = -1;
        _candidateFirstFrame = 0;
    }
}

/// <summary>Retained, pole-safe FP64 body-fixed pupil frame.</summary>
public readonly record struct PlanetaryProductionBillboardPupil(
    Double3 PivotDirection,
    SurfaceEnuFrame Tangent,
    long SnapEastCells,
    long SnapNorthCells,
    uint Generation)
{
    public bool IsValid => PivotDirection.IsFinite && Math.Abs(PivotDirection.LengthSquared - 1d) <= 1e-12d &&
        Tangent.IsValid && Generation != 0;

    public static PlanetaryProductionBillboardPupil Resolve(
        in PlanetaryProductionBillboardPupil previous,
        in Double3 requestedDirection,
        PlanetaryProductionSphericalBillboardTopology topology)
    {
        if (!requestedDirection.IsFinite || requestedDirection.LengthSquared <= 0d)
            throw new ArgumentOutOfRangeException(nameof(requestedDirection));
        ArgumentNullException.ThrowIfNull(topology);
        var requested = requestedDirection.Normalized();
        if (!previous.IsValid)
        {
            var tangent = BuildFrame(requested);
            return new(requested, tangent, 0, 0, 1);
        }

        var cell = topology.Snap.PupilCellRadians;
        var delta = requested - previous.PivotDirection;
        var eastCells = Double3.Dot(delta, previous.Tangent.East) / cell;
        var northCells = Double3.Dot(delta, previous.Tangent.North) / cell;
        var threshold = topology.Snap.CandidateShiftMultiple;
        if (Math.Abs(eastCells) < threshold && Math.Abs(northCells) < threshold) return previous;

        var eastShift = checked((long)Math.Round(eastCells / threshold, MidpointRounding.AwayFromZero) * threshold);
        var northShift = checked((long)Math.Round(northCells / threshold, MidpointRounding.AwayFromZero) * threshold);
        var pivot = (previous.PivotDirection + previous.Tangent.East * (eastShift * cell) +
            previous.Tangent.North * (northShift * cell)).Normalized();
        var tangentTransported = Transport(previous.Tangent, pivot);
        var generation = previous.Generation == uint.MaxValue ? 1u : previous.Generation + 1u;
        return new(pivot, tangentTransported, previous.SnapEastCells + eastShift,
            previous.SnapNorthCells + northShift, generation);
    }

    public Double3 RotateCanonical(in Double3 localDirection)
    {
        if (!IsValid || !localDirection.IsFinite || localDirection.LengthSquared <= 0d)
            throw new InvalidOperationException("Cannot resolve a canonical direction from an invalid pupil frame.");
        var local = localDirection.Normalized();
        return (Tangent.East * local.X + Tangent.North * local.Y + Tangent.Up * local.Z).Normalized();
    }

    private static SurfaceEnuFrame BuildFrame(in Double3 up)
    {
        var reference = Math.Abs(up.Y) < .9d ? Double3.UnitY : Double3.UnitX;
        var east = Double3.Cross(reference, up).Normalized();
        return new(east, Double3.Cross(up, east).Normalized(), up);
    }

    private static SurfaceEnuFrame Transport(in SurfaceEnuFrame old, in Double3 newUp)
    {
        // Minimal parallel transport: project the retained east into the new
        // tangent plane. Only the antipodal singularity falls back to a frame.
        var eastProjected = old.East - newUp * Double3.Dot(old.East, newUp);
        if (eastProjected.LengthSquared <= 1e-24d) return BuildFrame(newUp);
        var east = eastProjected.Normalized();
        var north = Double3.Cross(newUp, east).Normalized();
        if (Double3.Dot(north, old.North) < 0d) { east = -east; north = -north; }
        return new(east, north, newUp);
    }
}

public readonly record struct PlanetaryProductionBillboardReuse(
    int ActiveSamples,
    int ReusedSamples,
    int NewSamples)
{
    public double ReusePercent => ActiveSamples == 0 ? 0d : 100d * ReusedSamples / ActiveSamples;
}

public static class PlanetaryProductionSphericalBillboardReuse
{
    public static PlanetaryProductionBillboardReuse CrossLevel(
        PlanetaryProductionSphericalBillboardTopology current,
        PlanetaryProductionSphericalBillboardTopology incoming)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(incoming);
        if (incoming.Level == current.Level + 1)
            return new(incoming.Vertices.Count, incoming.ParentVertexMap.Count,
                incoming.Vertices.Count - incoming.ParentVertexMap.Count);
        if (current.Level == incoming.Level + 1)
            return new(incoming.Vertices.Count, incoming.Vertices.Count, 0);
        return new(incoming.Vertices.Count, 0, incoming.Vertices.Count);
    }

    public static PlanetaryProductionBillboardReuse Snapped(
        PlanetaryProductionSphericalBillboardTopology topology,
        long eastCellDelta,
        long northCellDelta)
    {
        ArgumentNullException.ThrowIfNull(topology);
        if (eastCellDelta == 0 && northCellDelta == 0)
            return new(topology.Vertices.Count, topology.Vertices.Count, 0);
        var shift = Math.Abs(eastCellDelta) + Math.Abs(northCellDelta);
        var strips = Math.Max(1L, shift / Math.Max(1, topology.Snap.CandidateShiftMultiple));
        var entering = (int)Math.Min(topology.Vertices.Count,
            strips * topology.Snap.EnteringStripCells * Math.Max(1, topology.Snap.OverlapFootprintCells));
        return new(topology.Vertices.Count, topology.Vertices.Count - entering, entering);
    }
}

public enum PlanetaryProductionBillboardResidencyState : byte
{
    Empty,
    TopologyResident,
    PhysicalReady,
    NormalReady,
    CullCompactReady,
    DrawReady,
    FenceComplete,
    Authoritative,
    Retired
}

public sealed record PlanetaryProductionBillboardResidency(
    PlanetaryProductionSphericalBillboardTopology Topology,
    ulong PublicationGeneration,
    PlanetaryProductionBillboardResidencyState State,
    PlanetaryProductionBillboardReuse Reuse);

/// <summary>
/// Exactly-one-owner publication state machine. It models the runtime contract
/// independently of renderer ownership so the opt-in candidate cannot suppress
/// the current complete Earth while an incoming level is incomplete.
/// </summary>
public sealed class PlanetaryProductionSphericalBillboardPublication
{
    private ulong _generation;
    public PlanetaryProductionBillboardResidency? Current { get; private set; }
    public PlanetaryProductionBillboardResidency? Incoming { get; private set; }
    public ulong ZeroOwnerFrames { get; private set; }
    public ulong OverlapOwnerFrames { get; private set; }
    public ulong StaleGenerationDraws { get; private set; }

    public void Bootstrap(PlanetaryProductionSphericalBillboardTopology topology)
    {
        if (Current is not null || Incoming is not null) throw new InvalidOperationException("Publication is already initialized.");
        Current = new(topology, ++_generation, PlanetaryProductionBillboardResidencyState.Authoritative,
            new(topology.Vertices.Count, 0, topology.Vertices.Count));
    }

    public PlanetaryProductionBillboardResidency BeginIncoming(
        PlanetaryProductionSphericalBillboardTopology topology,
        PlanetaryProductionBillboardReuse reuse)
    {
        if (Current is null || Incoming is not null) throw new InvalidOperationException("Only one incoming residency is permitted.");
        Incoming = new(topology, ++_generation, PlanetaryProductionBillboardResidencyState.TopologyResident, reuse);
        return Incoming;
    }

    public void AdvanceIncoming(PlanetaryProductionBillboardResidencyState next)
    {
        if (Incoming is null || next <= Incoming.State || next > PlanetaryProductionBillboardResidencyState.FenceComplete)
            throw new InvalidOperationException("Incoming readiness must advance monotonically through fence completion.");
        Incoming = Incoming with { State = next };
    }

    public PlanetaryProductionBillboardResidency PublishAtFrameBoundary()
    {
        if (Current is null || Incoming is null || Incoming.State != PlanetaryProductionBillboardResidencyState.FenceComplete)
            throw new InvalidOperationException("An incomplete incoming level cannot publish.");
        Current = Current with { State = PlanetaryProductionBillboardResidencyState.Retired };
        Incoming = Incoming with { State = PlanetaryProductionBillboardResidencyState.Authoritative };
        Current = Incoming;
        Incoming = null;
        return Current;
    }

    public void RecordFrame(ulong drawnGeneration)
    {
        var owners = Current?.State == PlanetaryProductionBillboardResidencyState.Authoritative ? 1 : 0;
        if (Incoming?.State == PlanetaryProductionBillboardResidencyState.Authoritative) owners++;
        if (owners == 0) ZeroOwnerFrames++;
        if (owners > 1) OverlapOwnerFrames++;
        if (Current is null || drawnGeneration != Current.PublicationGeneration) StaleGenerationDraws++;
    }
}

public static class PlanetaryProductionSphericalBillboardTes
{
    public static uint SharedEdgeFactor(
        in Double3 firstCameraRelative,
        in Double3 secondCameraRelative,
        int viewportHeight,
        double verticalFovRadians,
        double rangeMetres,
        double minimumPhysicalWavelengthMetres,
        double targetPixels = 5d)
    {
        if (!firstCameraRelative.IsFinite || !secondCameraRelative.IsFinite || viewportHeight <= 0 ||
            verticalFovRadians is <= 0d or >= Math.PI || rangeMetres <= 0d ||
            minimumPhysicalWavelengthMetres <= 0d || targetPixels <= 0d) throw new ArgumentOutOfRangeException();
        var midpoint = (firstCameraRelative + secondCameraRelative) * .5d;
        var distance = Math.Sqrt(midpoint.LengthSquared);
        if (distance >= rangeMetres) return 1;
        var focal = viewportHeight / (2d * Math.Tan(verticalFovRadians * .5d));
        var aDepth = Math.Max(1e-6d, -firstCameraRelative.Z);
        var bDepth = Math.Max(1e-6d, -secondCameraRelative.Z);
        var ax = focal * firstCameraRelative.X / aDepth;
        var ay = focal * firstCameraRelative.Y / aDepth;
        var bx = focal * secondCameraRelative.X / bDepth;
        var by = focal * secondCameraRelative.Y / bDepth;
        var screen = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
        var edge = secondCameraRelative - firstCameraRelative;
        var edgeLength = Math.Sqrt(edge.LengthSquared);
        var viewDirection = midpoint.LengthSquared > 0d ? midpoint.Normalized() : Double3.UnitZ;
        var alignment = edgeLength > 0d ? Math.Abs(Double3.Dot(viewDirection, edge / edgeLength)) : 0d;
        if (alignment > .8d)
        {
            var skew = (alignment - .8d) / .2d;
            var compensated = focal * (.6d * edgeLength) / Math.Max(distance, 1e-6d);
            screen = screen + (compensated - screen) * skew;
        }
        var distanceFade = 1d - Math.Clamp(distance / rangeMetres, 0d, 1d);
        var screenFactor = Math.Ceiling(screen * distanceFade / targetPixels);
        var physicalFactor = Math.Max(1d, Math.Ceiling(edgeLength / minimumPhysicalWavelengthMetres));
        return (uint)Math.Clamp(Math.Min(screenFactor, physicalFactor), 1d, 64d);
    }
}
