using NovaCore.Core;
using NovaCore.Core.ReferenceFrames;
using NovaCore.Simulation.Time;

namespace NovaCore.Simulation.Celestial;

/// <summary>Compact runtime orientation source derived from the named NAIF planetary-constants frame.</summary>
internal readonly record struct CelestialBodyOrientationSource(
    CelestialBodyId BodyId,
    string FrameName,
    string Authority,
    bool IsHighAccuracyLunarFrame);

/// <summary>One directly evaluated body-fixed frame at an authoritative simulation instant.</summary>
internal readonly record struct CelestialBodyOrientation(
    CelestialBodyId BodyId,
    SimulationInstant Time,
    DoubleQuaternion BodyFixedToInertial,
    Double3 AngularVelocityInInertial,
    CelestialBodyOrientationSource Source)
{
    internal Double3 BodyFixedDirectionToInertial(in Double3 direction) => BodyFixedToInertial.Rotate(direction);
    internal Double3 InertialDirectionToBodyFixed(in Double3 direction) => BodyFixedToInertial.Conjugate().Normalized().Rotate(direction);
}

/// <summary>Persistent latitude/longitude/altitude identity in NovaCore's right-handed, +Y-pole body-fixed convention.</summary>
internal readonly record struct CelestialSurfaceAnchor(CelestialBodyId BodyId, double LatitudeRadians, double LongitudeRadians, double AltitudeMetres)
{
    internal bool IsValid => BodyId.IsValid && double.IsFinite(LatitudeRadians) && LatitudeRadians is >= -Math.PI / 2d and <= Math.PI / 2d && double.IsFinite(LongitudeRadians) && double.IsFinite(AltitudeMetres);
    internal Double3 BodyFixedPosition(double referenceRadiusMetres)
    {
        if (!IsValid || !double.IsFinite(referenceRadiusMetres) || referenceRadiusMetres <= 0d || referenceRadiusMetres + AltitudeMetres <= 0d) throw new ArgumentOutOfRangeException(nameof(referenceRadiusMetres));
        var radius = referenceRadiusMetres + AltitudeMetres;
        var latitudeCosine = Math.Cos(LatitudeRadians);
        return new Double3(latitudeCosine * Math.Cos(LongitudeRadians), Math.Sin(LatitudeRadians), latitudeCosine * Math.Sin(LongitudeRadians)) * radius;
    }
}

/// <summary>Body-centered CCF evaluation layered beside, never into, the translational CCI hierarchy.</summary>
internal static class CelestialBodyFixedFrameEvaluator
{
    internal static bool TryEvaluate(CelestialBodyId bodyId, SimulationInstant time, in Double3 centerInInertial, in Double3 centerVelocityInInertial, out EvaluatedReferenceFrame frame)
    {
        frame = default;
        if (!centerInInertial.IsFinite || !centerVelocityInInertial.IsFinite || !CelestialBodyOrientationEvaluator.TryEvaluate(bodyId, time, out var orientation)) return false;
        frame = new(new FrameTransform(centerInInertial, orientation.BodyFixedToInertial), centerVelocityInInertial, orientation.AngularVelocityInInertial, false);
        return true;
    }

    internal static bool TryTransformAnchor(in CelestialSurfaceAnchor anchor, SimulationInstant time, double referenceRadiusMetres, in Double3 centerInInertial, out Double3 positionInInertial)
    {
        positionInInertial = default;
        if (!anchor.IsValid || !centerInInertial.IsFinite || !CelestialBodyOrientationEvaluator.TryEvaluate(anchor.BodyId, time, out var orientation)) return false;
        try { positionInInertial = centerInInertial + orientation.BodyFixedToInertial.Rotate(anchor.BodyFixedPosition(referenceRadiusMetres)); return positionInInertial.IsFinite; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
}

/// <summary>
/// Zero-allocation, direct-epoch implementation of official NAIF body frames.
/// SimulationInstant zero is J2000 ET zero. The Moon uses an embedded compact residual extracted
/// from the DE440 binary lunar PCK/frame chain and deterministically falls back to IAU_MOON.
/// </summary>
internal static class CelestialBodyOrientationEvaluator
{
    private const double DegreesToRadians = Math.PI / 180d;
    private const double SecondsPerDay = 86_400d;
    private const double DaysPerCentury = 36_525d;
    private const string Authority = "NAIF pck00010.tpc (IAU 2009 report constants)";

    private enum ModelKind : byte { Linear, Mercury, Moon, Jupiter, Neptune }
    private readonly record struct Model(CelestialBodyId BodyId, string FrameName, ModelKind Kind, double Ra0, double RaT, double Dec0, double DecT, double W0, double Wd, double Wt2);

    // Sorted by stable NovaCore body ID. This table is intentionally separate from the translational definition and its hash.
    private static readonly Model[] Models =
    [
        new(SolarSystemBodyIds.Mercury, "IAU_MERCURY", ModelKind.Mercury, 281.0097d, -.0328d, 61.4143d, -.0049d, 329.5469d, 6.1385025d, 0d),
        new(SolarSystemBodyIds.Venus, "IAU_VENUS", ModelKind.Linear, 272.76d, 0d, 67.16d, 0d, 160.20d, -1.4813688d, 0d),
        new(SolarSystemBodyIds.Earth, "IAU_EARTH", ModelKind.Linear, 0d, -.641d, 90d, -.557d, 190.147d, 360.9856235d, 0d),
        new(SolarSystemBodyIds.Moon, "IAU_MOON", ModelKind.Moon, 269.9949d, .0031d, 66.5392d, .0130d, 38.3213d, 13.17635815d, -1.4e-12d),
        new(SolarSystemBodyIds.Mars, "IAU_MARS", ModelKind.Linear, 317.68143d, -.1061d, 52.88650d, -.0609d, 176.630d, 350.89198226d, 0d),
        new(SolarSystemBodyIds.Jupiter, "IAU_JUPITER", ModelKind.Jupiter, 268.056595d, -.006499d, 64.495303d, .002413d, 284.95d, 870.5360000d, 0d),
        new(SolarSystemBodyIds.Saturn, "IAU_SATURN", ModelKind.Linear, 40.589d, -.036d, 83.537d, -.004d, 38.90d, 810.7939024d, 0d),
        new(SolarSystemBodyIds.Uranus, "IAU_URANUS", ModelKind.Linear, 257.311d, 0d, -15.175d, 0d, 203.81d, -501.1600928d, 0d),
        new(SolarSystemBodyIds.Neptune, "IAU_NEPTUNE", ModelKind.Neptune, 299.36d, 0d, 43.46d, 0d, 253.18d, 536.3128492d, 0d),
    ];

    internal static int SupportedBodyCount => Models.Length;
    internal static CelestialBodyOrientationSource GetSource(int index)
    {
        if ((uint)index >= (uint)Models.Length) throw new ArgumentOutOfRangeException(nameof(index));
        var model = Models[index];
        return model.Kind==ModelKind.Moon&&LunarHighPrecisionOrientation.IsAvailable
            ?new(model.BodyId,LunarHighPrecisionOrientation.FrameName,LunarHighPrecisionOrientation.Authority,true)
            :new(model.BodyId,model.FrameName,Authority,false);
    }

    internal static bool TryEvaluate(CelestialBodyId bodyId, SimulationInstant time, out CelestialBodyOrientation orientation)
    {
        orientation = default;
        if (!TryFind(bodyId, out var model)) return false;
        var fallback=EvaluateCore(model,time.SecondsSinceEpoch);var packed=default(DoubleQuaternion);
        var highPrecision=model.Kind==ModelKind.Moon&&LunarHighPrecisionOrientation.TryEvaluate(time.SecondsSinceEpoch,fallback,out packed);
        var current=highPrecision?packed:fallback;
        const double halfStepSeconds = .5d;
        var beforeFallback=EvaluateCore(model,time.SecondsSinceEpoch-halfStepSeconds);var afterFallback=EvaluateCore(model,time.SecondsSinceEpoch+halfStepSeconds);
        var before=highPrecision&&LunarHighPrecisionOrientation.TryEvaluate(time.SecondsSinceEpoch-halfStepSeconds,beforeFallback,out var beforePacked)?beforePacked:beforeFallback;
        var after=highPrecision&&LunarHighPrecisionOrientation.TryEvaluate(time.SecondsSinceEpoch+halfStepSeconds,afterFallback,out var afterPacked)?afterPacked:afterFallback;
        var delta = (after * before.Conjugate()).Normalized();
        if (delta.W < 0d) delta = new(-delta.X, -delta.Y, -delta.Z, -delta.W);
        var vectorLength = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
        Double3 angularVelocity;
        if (vectorLength <= 1e-18d) angularVelocity = Double3.Zero;
        else
        {
            var angle = 2d * Math.Atan2(vectorLength, delta.W);
            angularVelocity = new Double3(delta.X, delta.Y, delta.Z) * (angle / vectorLength / (2d * halfStepSeconds));
        }
        orientation = new(bodyId,time,current,angularVelocity,highPrecision?new(bodyId,LunarHighPrecisionOrientation.FrameName,LunarHighPrecisionOrientation.Authority,true):new(bodyId,model.FrameName,Authority,false));
        return current.IsFinite && angularVelocity.IsFinite;
    }

    internal static ulong DeterministicHash(SimulationInstant time)
    {
        ulong hash = 14695981039346656037UL;
        for (var index = 0; index < Models.Length; index++)
        {
            _ = TryEvaluate(Models[index].BodyId, time, out var value);
            hash = Mix(hash, value.BodyId.Value);
            hash = Mix(hash, (ulong)value.Time.Ticks);
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.BodyFixedToInertial.X));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.BodyFixedToInertial.Y));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.BodyFixedToInertial.Z));
            hash = Mix(hash, (ulong)BitConverter.DoubleToInt64Bits(value.BodyFixedToInertial.W));
        }
        return hash;
    }

    private static bool TryFind(CelestialBodyId bodyId, out Model model)
    {
        for (var index = 0; index < Models.Length; index++) if (Models[index].BodyId == bodyId) { model = Models[index]; return true; }
        model = default; return false;
    }

    private static DoubleQuaternion EvaluateCore(in Model model, double secondsSinceJ2000)
    {
        var days = secondsSinceJ2000 / SecondsPerDay;
        var centuries = days / DaysPerCentury;
        var ra = model.Ra0 + model.RaT * centuries;
        var dec = model.Dec0 + model.DecT * centuries;
        var w = model.W0 + model.Wd * days + model.Wt2 * days * days;
        switch (model.Kind)
        {
            case ModelKind.Mercury:
                w += .00993822d * SinDegrees(174.791086d + 4.092335d * days)
                    - .00104581d * SinDegrees(349.582171d + 8.184670d * days)
                    - .00010280d * SinDegrees(164.373257d + 12.277005d * days)
                    - .00002364d * SinDegrees(339.164343d + 16.369340d * days)
                    - .00000532d * SinDegrees(153.955429d + 20.461675d * days);
                break;
            case ModelKind.Moon:
                var e1=125.045d-.0529921d*days;var e2=250.089d-.1059842d*days;var e3=260.008d+13.0120009d*days;var e4=176.625d+13.3407154d*days;var e5=357.529d+.9856003d*days;var e6=311.589d+26.4057084d*days;var e7=134.963d+13.0649930d*days;var e8=276.617d+.3287146d*days;var e9=34.226d+1.7484877d*days;var e10=15.134d-.1589763d*days;var e11=119.743d+.0036096d*days;var e12=239.961d+.1643573d*days;var e13=25.053d+12.9590088d*days;
                ra += -3.8787d*SinDegrees(e1)-.1204d*SinDegrees(e2)+.0700d*SinDegrees(e3)-.0172d*SinDegrees(e4)+.0072d*SinDegrees(e6)-.0052d*SinDegrees(e10)+.0043d*SinDegrees(e13);
                dec += 1.5419d*CosDegrees(e1)+.0239d*CosDegrees(e2)-.0278d*CosDegrees(e3)+.0068d*CosDegrees(e4)-.0029d*CosDegrees(e6)+.0009d*CosDegrees(e7)+.0008d*CosDegrees(e10)-.0009d*CosDegrees(e13);
                w+=3.5610d*SinDegrees(e1);w+=.1208d*SinDegrees(e2);w+=-.0642d*SinDegrees(e3);w+=.0158d*SinDegrees(e4);w+=.0252d*SinDegrees(e5);w+=-.0066d*SinDegrees(e6);w+=-.0047d*SinDegrees(e7);w+=-.0046d*SinDegrees(e8);w+=.0028d*SinDegrees(e9);w+=.0052d*SinDegrees(e10);w+=.0040d*SinDegrees(e11);w+=.0019d*SinDegrees(e12);w+=-.0044d*SinDegrees(e13);
                break;
            case ModelKind.Jupiter:
                var ja=99.360714d+4850.4046d*centuries;var jb=175.895369d+1191.9605d*centuries;var jc=300.323162d+262.5475d*centuries;var jd=114.012305d+6070.2476d*centuries;var je=49.511251d+64.3d*centuries;
                ra+=.000117d*SinDegrees(ja);dec+=.000050d*CosDegrees(ja);ra+=.000938d*SinDegrees(jb);dec+=.000404d*CosDegrees(jb);ra+=.001432d*SinDegrees(jc);dec+=.000617d*CosDegrees(jc);ra+=.000030d*SinDegrees(jd);dec+=-.000013d*CosDegrees(jd);ra+=.002150d*SinDegrees(je);dec+=.000926d*CosDegrees(je);
                break;
            case ModelKind.Neptune:
                var n = 357.85d + 52.316d * centuries;
                ra += .70d * SinDegrees(n); dec -= .51d * CosDegrees(n); w -= .48d * SinDegrees(n);
                break;
        }
        // SPICE text-PCK convention: J2000->IAU is R3(W) R1(90-DEC) R3(RA+90).
        // NovaCore surface coordinates are +Y pole; +90 degrees about local X maps them to IAU +Z pole.
        var inertialToIau = DoubleQuaternion.FromAxisAngle(Double3.UnitZ, ReduceRadians(-w * DegreesToRadians))
            * DoubleQuaternion.FromAxisAngle(Double3.UnitX, -(90d - dec) * DegreesToRadians)
            * DoubleQuaternion.FromAxisAngle(Double3.UnitZ, -(ra + 90d) * DegreesToRadians);
        var novaSurfaceToIau = DoubleQuaternion.FromAxisAngle(Double3.UnitX, Math.PI / 2d);
        var bodyFixedToInertial = inertialToIau.Conjugate().Normalized() * novaSurfaceToIau;
        bodyFixedToInertial = bodyFixedToInertial.Normalized();
        return bodyFixedToInertial.W < 0d ? new(-bodyFixedToInertial.X,-bodyFixedToInertial.Y,-bodyFixedToInertial.Z,-bodyFixedToInertial.W) : bodyFixedToInertial;
    }

    internal static DoubleQuaternion EvaluateMoonFallbackForTest(double secondsSinceJ2000)=>EvaluateCore(Models[3],secondsSinceJ2000);

    private static double SinDegrees(double degrees) => Math.Sin(ReduceRadians(degrees * DegreesToRadians));
    private static double CosDegrees(double degrees) => Math.Cos(ReduceRadians(degrees * DegreesToRadians));
    private static double ReduceRadians(double radians) => Math.IEEERemainder(radians, Math.Tau);
    private static ulong Mix(ulong hash, ulong value) { for (var index=0;index<8;index++){hash^=(byte)value;hash*=1099511628211UL;value>>=8;}return hash; }
}
