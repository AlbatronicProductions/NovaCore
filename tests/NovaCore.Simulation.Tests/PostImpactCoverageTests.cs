using NovaCore.Simulation.Spacecraft.Contact;
using R = CertifiedResponseOracle.R;
using B = CertifiedResponseOracle.B;

/// <summary>
/// Independent analytic gap controls. These exercise proof classification, not sampled collision
/// detection, and do not create authoritative spacecraft, propagation or coverage receipts.
/// </summary>
internal static class PostImpactCoverageTests
{
    // Predeclared before execution: the search must never expand these qualification budgets.
    private const int MaximumVisits = 255;
    private const int MaximumDepth = 24;

    private enum Shape : byte
    {
        Linear, OutwardAcceleration, InwardAcceleration, Reacquisition, MultipleRoots,
        Grazing, PersistentContact, Ambiguous, GradingEscape, SeparatingComRotation,
        InelasticRotation, AmbiguousCloseRoots, VisitExhaustion
    }

    /// <summary>Coefficient doubles denote their exact represented rational values.</summary>
    private readonly record struct AnalyticGap(Shape Kind, double A = 0, double B = 0)
        : IPostImpactCoverageGap
    {
        public CoverageFailure Enclose(FloridaBound s, out CoverageGap gap)
        {
            FloridaBound a = A, b = B;
            switch (Kind)
            {
                case Shape.Linear:
                    gap = new(s, 1, 0);
                    break;
                case Shape.OutwardAcceleration:
                    gap = new(s.Square(), 2 * s, 2);
                    break;
                case Shape.InwardAcceleration:
                    gap = new(-s.Square(), -2 * s, -2);
                    break;
                case Shape.Reacquisition:
                case Shape.VisitExhaustion:
                    gap = new(s * (a - s), a - 2 * s, -2);
                    // Same exact contact polynomial; coarse sound inclusions force breadth work.
                    // No production budget or physics changes to manufacture the exhaustion.
                    if (Kind == Shape.VisitExhaustion && s.Upper - s.Lower > 1d / 256)
                        gap = new(gap.Value.Expand(10), gap.First.Expand(10), gap.Second.Expand(10));
                    break;
                case Shape.MultipleRoots:
                case Shape.AmbiguousCloseRoots:
                    // Expanded derivatives are independently differentiated from s(a-s)(b-s).
                    gap = new(s * (a - s) * (b - s),
                        a * b - 2 * (a + b) * s + 3 * s.Square(), 6 * s - 2 * (a + b));
                    if (Kind == Shape.AmbiguousCloseRoots)
                    {
                        // A sound, deliberately coarse enclosure models unresolved trajectory context.
                        // The actual two simple roots remain present; uncertainty must not erase them.
                        gap = new(gap.Value.Expand(1e-12), gap.First.Expand(1e-12), gap.Second);
                    }
                    break;
                case Shape.Grazing:
                    gap = new(s.Square() * (s - a).Square(),
                        2 * s * (s - a) * (2 * s - a),
                        12 * s.Square() - 12 * a * s + 2 * a.Square());
                    break;
                case Shape.PersistentContact:
                    gap = new(0, 0, 0);
                    break;
                case Shape.Ambiguous:
                    gap = new(new(-1, 1), new(-1, 1), new(-1, 1));
                    break;
                case Shape.GradingEscape:
                    // Point evaluations at sigma=0 and sigma=1 are valid, but the trajectory exits
                    // the terrain branch in the interior. Endpoint-only acceptance is unsound.
                    if (s.Lower < .625 && s.Upper > .375)
                    {
                        gap = default;
                        return CoverageFailure.GradingDomain;
                    }
                    gap = new(s, 1, 0);
                    break;
                case Shape.SeparatingComRotation:
                {
                    var sin = FloridaBound.Sin(s);
                    var cos = FloridaBound.Cos(s);
                    // COM separates at positive constant speed a; a rotating unit lever descends.
                    gap = new(a * s + cos - 1, a - sin, -cos);
                    break;
                }
                case Shape.InelasticRotation:
                {
                    var sin = FloridaBound.Sin(s);
                    var cos = FloridaBound.Cos(s);
                    // Principal-axis constant spin, with the exact inelastic departure relation:
                    // g(0)=g'(0)=0, g''(0)=a>0, then a later strictly approaching contact.
                    gap = new(a * (1 - cos) + sin - s, a * sin + cos - 1, a * cos - sin);
                    break;
                }
                default:
                    throw new InvalidOperationException("Unknown analytical coverage control");
            }
            return CoverageFailure.None;
        }
    }

    internal static void Run()
    {
        DepartureAndClear();
        CrossingAndHiddenContact();
        BoundaryAndGrazingRefusals();
        RotatingControls();
        DomainAndWorkRefusals();
        RootAnchoredEnclosures();
        Console.WriteLine("POSTIMPACT_COVERAGE_ANALYTICAL departure/clear/crossing-refusal/interior-contact/boundary/grazing/rotation/domain/work/replay PASS");
    }

    internal static void Cost()
    {
        foreach(var pair in new[]{(new AnalyticGap(Shape.Linear),CoverageStatus.EventFreeThroughTarget,"analytical clear certification"),
            (new AnalyticGap(Shape.Reacquisition,.1),CoverageStatus.Unresolved,"analytical crossing refusal"),
            (new AnalyticGap(Shape.Ambiguous),CoverageStatus.Unresolved,"depth exhaustion"),
            (new AnalyticGap(Shape.VisitExhaustion,.9),CoverageStatus.Unresolved,"visit exhaustion"),
            (new AnalyticGap(Shape.GradingEscape),CoverageStatus.Unresolved,"domain refusal")})
        {
            var (model,expected,name)=pair;
            PostImpactCoverageCost.Measure(name,()=>PostImpactCoverageSearch.Evaluate(in model).Status==expected);
            var result=PostImpactCoverageSearch.Evaluate(in model);
            Console.WriteLine($"COVERAGE_WORK {name}: visits={result.Visits} depth={result.MaximumDepth} enclosures={result.Evaluations} searchStackBytes={PostImpactCoverageSearch.SearchStackBytes}");
        }
        OrdinaryAllocationMeasurement.PositiveControl();
    }

    private static void Check(bool condition, string contract)
    {
        if (!condition) throw new InvalidOperationException("Post-impact coverage: " + contract);
    }

    private static void RootAnchoredEnclosures()
    {
        // Independent exact polynomial oracle: g(s)=rho*s+a*s²/2, g'(s)=rho+a*s.
        // Large raw boxes model lost root correlation; no source/terrain authority is fabricated.
        foreach(var rho in new[]{0d,.01})
        foreach(var a in new[]{-2d,0d,2d})
        {
            var raw=new CoverageGap(new(-10,10),new(-10,10),a);
            var s=new FloridaBound(.125,.25);
            var bounded=FloridaPostImpactGap.IntersectRootAnchor(raw,s,rho,a);
            Check(bounded.IsFinite,"root anchor finite polynomial enclosure");
            foreach(var exactS in new[]{new R(1,8),new R(3,16),new R(1,4)})
            {
                var value=R.From(rho)*exactS+R.From(a)*exactS*exactS/2;
                var first=R.From(rho)+R.From(a)*exactS;
                Check(CertifiedResponseOracle.Contains(bounded.Value,value),"root anchor exact polynomial value");
                Check(CertifiedResponseOracle.Contains(bounded.First,first),"root anchor exact polynomial derivative");
            }
            Check(bounded.Value.Lower>=raw.Value.Lower && bounded.Value.Upper<=raw.Value.Upper,
                "root anchor only intersects existing enclosure");
            Check(bounded.Second==raw.Second,"root anchor preserves local curvature");
            if(rho==0 && a<0)Check(bounded.Value.Upper<0,"root anchor never clears immediate penetration");
            if(rho==0 && a>0)Check(bounded.Value.Lower>0,"root anchor zero-speed outward departure");
        }
        var incompatible=FloridaPostImpactGap.IntersectRootAnchor(new(new(10,20),new(10,20),2),new(.125,.25),0,2);
        Check(!incompatible.IsFinite,"contradictory independent enclosures refuse");
        Console.WriteLine("ROOT_ANCHOR exact-polynomial/positive-negative-departure/intersection/contradiction PASS");
    }

    private static CoverageSearchResult Evaluate(AnalyticGap model, string contract)
    {
        var result = PostImpactCoverageSearch.Evaluate(in model);
        Check(result.Visits > 0 && result.Visits <= MaximumVisits, contract + " fixed visit bound");
        Check(result.MaximumDepth >= 0 && result.MaximumDepth <= MaximumDepth, contract + " fixed depth bound");
        Check(result == PostImpactCoverageSearch.Evaluate(in model), contract + " deterministic complete replay");
        Console.WriteLine($"COVERAGE_PROOF {contract}: status={result.Status} failure={result.Failure} prefixBits={BitConverter.DoubleToInt64Bits(result.ClearPrefix):X16} visits={result.Visits} depth={result.MaximumDepth} evaluations={result.Evaluations}");
        return result;
    }

    private static void DepartureAndClear()
    {
        // Exact polynomial facts, rather than sampled positive values, establish these controls.
        foreach (var kind in new[] { Shape.Linear, Shape.OutwardAcceleration })
        {
            var result = Evaluate(new(kind), kind.ToString());
            Check(result.Status == CoverageStatus.EventFreeThroughTarget && result.Failure == CoverageFailure.None,
                kind + " complete positive interval, including target");
        }
        foreach (var kind in new[] { Shape.InwardAcceleration, Shape.PersistentContact })
        {
            var result = Evaluate(new(kind), kind.ToString());
            Check(result.Status == CoverageStatus.Unresolved, kind + " cannot establish positive departure");
        }
    }

    private static void CrossingAndHiddenContact()
    {
        const double beta = .1;
        var single = Evaluate(new(Shape.Reacquisition, beta), "quadratic reacquisition");
        RefusedPolynomialContact(single, new(Shape.Reacquisition, beta), R.From(beta), "quadratic exact represented root");

        const double first = .23, second = .71;
        var a = R.From(first); var b = R.From(second);
        var inside = (a + b) / 2;
        Check(a * b > 0 && (1 - a) * (1 - b) > 0 && inside * (a - inside) * (b - inside) < 0,
            "exact rational initial separation and positive final gap enclose penetration");
        var multiple = Evaluate(new(Shape.MultipleRoots, first, second), "two roots with clear final endpoint");
        RefusedPolynomialContact(multiple, new(Shape.MultipleRoots, first, second), a, "earliest of two roots");
    }

    private static void BoundaryAndGrazingRefusals()
    {
        foreach (var beta in new[] { Math.ScaleB(.1, -20), 1 - Math.ScaleB(.1, -20) })
        {
            var result = Evaluate(new(Shape.Reacquisition, beta), "boundary-adjacent root");
            Check(result.Status == CoverageStatus.Unresolved,
                "near-alpha/near-target contact never authorizes clear");
            Check(PolynomialValue(R.From(beta), new(Shape.Reacquisition, beta)) == 0,
                "exact boundary-adjacent contact oracle retained");
        }

        var exactTarget = Evaluate(new(Shape.Reacquisition, 1), "exact target root");
        Check(exactTarget.Status == CoverageStatus.Unresolved,
            "unproved equality at target is retained, never excluded from coverage");
        var grazing = Evaluate(new(Shape.Grazing, .37), "double root grazing");
        Check(grazing.Status == CoverageStatus.Unresolved, "double root is outside strict-approach event class");

        var close = Evaluate(new(Shape.AmbiguousCloseRoots, .37, Math.BitIncrement(.37)), "close roots with context uncertainty");
        Check(close.Status == CoverageStatus.Unresolved, "ambiguous close-root ordering refuses");
    }

    private static void RotatingControls()
    {
        const double leverHeight = .1;
        var a = R.From(leverHeight);
        // Independent alternating-series rational bounds prove the signs bracketing each event.
        Check(RotatingValue(new R(19, 100), a, false).L > 0 &&
            RotatingValue(new R(21, 100), a, false).H < 0,
            "separating COM rotating-lever analytical event bracket");
        var separating = Evaluate(new(Shape.SeparatingComRotation, leverHeight), "separating COM rotating lever");
        RefusedCrossing(separating, "separating COM does not imply feature clear");

        Check(RotatingValue(new R(29, 100), a, true).L > 0 &&
            RotatingValue(new R(31, 100), a, true).H < 0,
            "inelastic principal-spin analytical event bracket");
        var inelastic = Evaluate(new(Shape.InelasticRotation, leverHeight), "inelastic principal-axis reacquisition");
        RefusedCrossing(inelastic, "zero-speed outward departure followed by reacquisition");
    }

    private static void RefusedCrossing(CoverageSearchResult result, string contract)
    {
        Check(result.Status == CoverageStatus.Unresolved, contract + " denies clearance");
        Check(result.Failure == CoverageFailure.StrictCrossingDetected, contract + " retains crossing reason");
        Check(result.ClearPrefix > 0 && result.ClearPrefix < 1, contract + " bounded clear prefix only");
    }

    private static void RefusedPolynomialContact(CoverageSearchResult result, AnalyticGap model, R root, string contract)
    {
        RefusedCrossing(result, contract);
        Check(root > 0 && root < 1, contract + " exact oracle inside target");
        Check(PolynomialValue(root, model) == 0, contract + " exact polynomial contact remains oracle");
        Check(R.From(result.ClearPrefix) < root, contract + " never clears through first contact");
        Check(PolynomialValue(root / 2, model) > 0, contract + " positive precontact oracle");
        var after = model.Kind == Shape.MultipleRoots ? (root + R.From(model.B)) / 2 : (root + 1) / 2;
        Check(PolynomialValue(after, model) < 0, contract + " negative postcontact oracle");
    }
    private static R PolynomialValue(R s, AnalyticGap model) => model.Kind switch
    {
        Shape.Reacquisition => s * (R.From(model.A) - s),
        Shape.MultipleRoots => s * (R.From(model.A) - s) * (R.From(model.B) - s),
        _ => throw new InvalidOperationException("Expected polynomial root control")
    };

    private static B RotatingValue(R s, R a, bool inelastic)
    {
        var sin = Trig(s, false); var cos = Trig(s, true);
        return inelastic ? B.Point(a) * (1 - cos) + sin - B.Point(s)
            : B.Point(a * s) + cos - 1;
    }

    private static B Trig(R x, bool cosine)
    {
        Check(x >= 0 && x <= 1, "alternating-series control domain");
        R term = cosine ? (R)1 : x; var sum = term;
        for (var k = 1; k <= 24; k++)
        {
            var n = cosine ? 2 * k - 1 : 2 * k;
            term = -term * x * x / (n * (n + 1));
            sum += term;
        }
        var next = cosine ? 49 : 50;
        var remainder = -term * x * x / (next * (next + 1));
        return remainder < 0 ? new(sum + remainder, sum) : new(sum, sum + remainder);
    }

    private static void DomainAndWorkRefusals()
    {
        var escape = Evaluate(new(Shape.GradingEscape), "whole-interval grading escape");
        Check(escape.Status is CoverageStatus.Unsupported or CoverageStatus.Unresolved,
            "interior grading escape never admits endpoints alone");
        Check(escape.Failure == CoverageFailure.GradingDomain, "grading refusal retains its cause");

        var ambiguous = Evaluate(new(Shape.Ambiguous), "unresolved trajectory context");
        Check(ambiguous.Status == CoverageStatus.Unresolved, "bounded ambiguity refuses");
        Check(ambiguous.MaximumDepth == MaximumDepth || ambiguous.Visits == MaximumVisits,
            "uniform ambiguity reaches a declared work bound without retries");

        var exhausted = Evaluate(new(Shape.VisitExhaustion, .9), "visit-limited contact search");
        Check(exhausted.Status == CoverageStatus.Unresolved && exhausted.Failure == CoverageFailure.WorkLimit,
            "visit exhaustion refuses without a false clear");
        Check(exhausted.Visits == MaximumVisits && R.From(exhausted.ClearPrefix) < R.From(.9),
            "fixed visit cap stops before exact retained contact");
    }
}
