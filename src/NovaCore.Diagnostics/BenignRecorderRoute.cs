namespace NovaCore.Diagnostics;

public readonly record struct BenignRecorderObservation(ulong FocusBody,ulong SubmittedBody,double Radius,double Distance,double SubmittedDistance,double ExpectedDistance,bool Paused,bool Editing,bool Flight,bool RateOne,bool SurfaceEnabled);
public enum BenignRecorderStep { Warmup, StartMeasurement, Hold, Complete }

/// <summary>Pure qualification gate; it never drives or modifies a camera.</summary>
public sealed class BenignRecorderRoute
{
    public const double WarmupSeconds=5,HoldSeconds=20;
    bool focusRequested,observed,measuring,complete;long first,held,last;int heldFrames;
    public bool RequestFocus(){if(focusRequested)return false;focusRequested=true;return true;}
    public int HeldFrames=>heldFrames;
    public static bool Valid(in BenignRecorderObservation s)
    {
        double tolerance=Math.Max(1,s.ExpectedDistance*1e-8);
        return s.FocusBody==6&&s.SubmittedBody==6&&s.SurfaceEnabled&&!s.Paused&&!s.Editing&&!s.Flight&&s.RateOne&&
            double.IsFinite(s.Radius)&&s.Radius>0&&double.IsFinite(s.ExpectedDistance)&&s.ExpectedDistance>=4*s.Radius&&
            double.IsFinite(s.Distance)&&double.IsFinite(s.SubmittedDistance)&&s.Distance>=4*s.Radius&&
            Math.Abs(s.Distance-s.ExpectedDistance)<=tolerance&&Math.Abs(s.SubmittedDistance-s.Distance)<=tolerance;
    }
    public BenignRecorderStep Observe(long now,long frequency,in BenignRecorderObservation value)
    {
        if(!focusRequested||!Valid(value)||frequency<=0||now<0||observed&&now<last||complete)throw new InvalidDataException("Benign recorder camera coverage refused.");
        last=now;if(!observed){observed=true;first=now;}
        if(!measuring){if((now-first)/(double)frequency<WarmupSeconds)return BenignRecorderStep.Warmup;measuring=true;held=now;return BenignRecorderStep.StartMeasurement;}
        heldFrames++;
        if((now-held)/(double)frequency<HoldSeconds)return BenignRecorderStep.Hold;
        if(heldFrames<120)throw new InvalidDataException("Benign recorder hold has insufficient completed frame observations.");
        complete=true;return BenignRecorderStep.Complete;
    }
}
