namespace NovaCore.Diagnostics;

/// <summary>Observer-only brackets around publication of a sole-writer watermark.
/// Produced is monotonic: its value after the write bounds every value before it.
/// These are bounds on the peak, not a claim of an exact producer-side high water.</summary>
public sealed class OrdinaryProgressBounds
{
    public long Lower {get;private set;}
    public long Upper {get;private set;}
    private long watermark,previousProduced;
    public void Published(long before,long next,long after)
    {
        if(before<previousProduced||before<watermark||next<watermark||next>before||after<before)
            throw new InvalidDataException("Nonmonotonic observer watermark bracket.");
        Lower=Math.Max(Lower,before-watermark);
        Upper=Math.Max(Upper,after-watermark);
        watermark=next;previousProduced=after;
    }
}
