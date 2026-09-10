using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

// A copied view of the existing linear Earth orientation owner, not a second model table.
internal readonly record struct EarthContactOrientation(double Ra0, double RaT, double Dec0,
    double DecT, double W0, double Wd, double SecondsPerDay, double DaysPerCentury);

internal static partial class CelestialBodyOrientationEvaluator
{
    internal static bool TryGetContactProofModel(out EarthContactOrientation value)
    {
        value = default;
        if (!TryFind(SolarSystemBodyIds.Earth, out var model) || model.Kind != ModelKind.Linear || model.Wt2 != 0)
            return false;
        value = new(model.Ra0, model.RaT, model.Dec0, model.DecT, model.W0, model.Wd, SecondsPerDay, DaysPerCentury);
        return true;
    }
}
