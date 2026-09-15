using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using BepuPhysics;

// Disposable read-only observer. This source is not linked into production or the retained test project.
internal static class CoastProbe
{
    internal static bool CaptureContacts;
    internal static readonly List<object> Records=new();
    private static object? lastCallback;
    private static string? active;
    private static List<object> predicates=new();
    private static string? firstFalse;
    private static int order;
    internal static int Row=-1;
    private static AcceptedCache before;
    private static object? inputs;
    internal static object Vec(Vector3 v)=>new {x=(double)v.X,y=(double)v.Y,z=(double)v.Z,
        bits=new[]{BitConverter.SingleToUInt32Bits(v.X).ToString("X8"),BitConverter.SingleToUInt32Bits(v.Y).ToString("X8"),BitConverter.SingleToUInt32Bits(v.Z).ToString("X8")}};
    private static object QuaternionValue(Quaternion q)=>new{x=(double)q.X,y=(double)q.Y,z=(double)q.Z,w=(double)q.W,
        bits=new[]{BitConverter.SingleToUInt32Bits(q.X).ToString("X8"),BitConverter.SingleToUInt32Bits(q.Y).ToString("X8"),
            BitConverter.SingleToUInt32Bits(q.Z).ToString("X8"),BitConverter.SingleToUInt32Bits(q.W).ToString("X8")}};
    private static object Duration(CacheDuration h)
    {
        var exact=h.Exact;
        return new{origin=h.Origin.ToString(),solverBits=h.SolverBits.ToString(),scale=h.Numerical,
            exactPositive=!exact.IsZero,exactValueBytesHex=Convert.ToHexString(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref exact,1)))};
    }
    internal static void Callback(Capture c)
    {
        if(!CaptureContacts)return;
        lastCallback=new{count=c.Count,rows=Enumerable.Range(0,c.Count).Select(i=>new{index=i,
            feature=c.Features[i],normal=Vec(c.Contacts[i].Normal),offset=Vec(c.Contacts[i].Offset),depth=(double)c.Contacts[i].Depth}).ToArray()};
    }
    internal static void World(string label,Simulation simulation,BodyHandle body,ConstraintHandle handle,Capture c,CacheHistory owner)
    {
        var b=simulation.Bodies[body];var pose=b.Pose;var v=b.Velocity;var q=pose.Orientation;
        // Independent binary64 transform of the stored quaternion, no renormalization.
        var x=(double)q.X;var y=(double)q.Y;var z=(double)q.Z;var w=(double)q.W;
        D3 Rotate(D3 r)=>new(
            (1-2*(y*y+z*z))*r.X+2*(x*y-z*w)*r.Y+2*(x*z+y*w)*r.Z,
            2*(x*y+z*w)*r.X+(1-2*(x*x+z*z))*r.Y+2*(y*z-x*w)*r.Z,
            2*(x*z-y*w)*r.X+2*(y*z+x*w)*r.Y+(1-2*(x*x+y*y))*r.Z);
        var p=new D3(pose.Position.X,pose.Position.Y,pose.Position.Z);
        var linear=new D3(v.Linear.X,v.Linear.Y,v.Linear.Z);var angular=new D3(v.Angular.X,v.Angular.Y,v.Angular.Z);
        var corners=new List<object>();double deepest=double.NegativeInfinity;double shallowest=double.PositiveInfinity;
        foreach(var cx in new[]{-1d,1d})foreach(var cy in new[]{-.5,.5})foreach(var cz in new[]{-.5,.5})
        {
            var local=new D3(cx,cy,cz);var r=Rotate(local);var point=p+r;var velocity=linear+D3.Cross(angular,r);
            deepest=Math.Max(deepest,-point.Y);shallowest=Math.Min(shallowest,-point.Y);
            corners.Add(new{local,world=point,velocity,planeGap=point.Y,withinSlabXZ=Math.Abs(point.X)<=8&&Math.Abs(point.Z)<=8});
        }
        var supportFunctionGap=p.Y-(Math.Abs(2*(x*y+z*w))+Math.Abs(1-2*(x*x+z*z))*.5+Math.Abs(2*(y*z-x*w))*.5);
        var worldRows=Enumerable.Range(0,c.Count).Select(i=>{
            var r=new D3(c.Contacts[i].Offset.X,c.Contacts[i].Offset.Y,c.Contacts[i].Offset.Z);
            var n=new D3(c.Contacts[i].Normal.X,c.Contacts[i].Normal.Y,c.Contacts[i].Normal.Z);
            var point=p+r;var velocity=linear+D3.Cross(angular,r);
            return new{index=i,feature=c.Features[i],normal=Vec(c.Contacts[i].Normal),offset=Vec(c.Contacts[i].Offset),
                worldPoint=point,depth=(double)c.Contacts[i].Depth,pointNormalVelocity=D3.Dot(n,velocity),verticalVelocity=velocity.Y,
                normalDotUp=n.Y,normalSquared=D3.Dot(n,n),planeGap=point.Y,withinSlabXZ=Math.Abs(point.X)<=8&&Math.Abs(point.Z)<=8};
        }).ToArray();
        Records.Add(new{type="world_capture",label,beforeRefusal=true,position=Vec(pose.Position),orientation=QuaternionValue(pose.Orientation),
            linearVelocity=Vec(v.Linear),angularVelocity=Vec(v.Angular),nativeInverseMass=(double)b.LocalInertia.InverseMass,
            nativeInverseInertia=new{xx=(double)b.LocalInertia.InverseInertiaTensor.XX,yy=(double)b.LocalInertia.InverseInertiaTensor.YY,
                zz=(double)b.LocalInertia.InverseInertiaTensor.ZZ,yx=(double)b.LocalInertia.InverseInertiaTensor.YX,
                zx=(double)b.LocalInertia.InverseInertiaTensor.ZX,zy=(double)b.LocalInertia.InverseInertiaTensor.ZY},
            body=body.Value,constraint=handle.Value,constraintCount=b.Constraints.Count,ownerIdentity=owner.Snapshot.Identity,
            piece=owner.Snapshot.Piece,kind=owner.Snapshot.Kind.ToString(),acceptedNormal=owner.Snapshot.Normal,invalidated=owner.Invalidated,
            callback=lastCallback,solverRows=worldRows,corners,supportFunctionGap,deepestCornerPenetration=deepest,
            shallowestCornerPenetration=shallowest,frameEpoch="NOT APPLICABLE: standalone fixed-origin box/slab fixture; no canonical/frame epoch capability"});
    }
    internal static void Before(string label,CacheHistory owner,Capture c,PieceKernel.State source,InverseBody body,Load load,CacheDuration h)
    {
        active=label is "original_owned_powered_piece" or "owned_coast_continuation" or "successor_powered_comparison"?label:null;
        if(active is null)return;
        before=owner.Snapshot;predicates=new();firstFalse=null;order=0;Row=-1;
        var rows=Enumerable.Range(0,4).Select(i=>new NormalRow(c.Features[i],new(c.Contacts[i].Normal.X,c.Contacts[i].Normal.Y,c.Contacts[i].Normal.Z),
            new(c.Contacts[i].Offset.X,c.Contacts[i].Offset.Y,c.Contacts[i].Offset.Z))).ToArray();
        var depths=c.Contacts.Select(v=>(double)v.Depth).ToArray();var features=new Features(c.Features[0],c.Features[1],c.Features[2],c.Features[3]);
        var a0=before.Load.Gravity+body.Mass*before.Load.Force;var a1=load.Gravity+body.Mass*load.Force;
        var w0=body.Apply(before.Load.Torque);var w1=body.Apply(load.Torque);
        var v=new D3(source.Linear.X,source.Linear.Y,source.Linear.Z);var spin=new D3(source.Angular.X,source.Angular.Y,source.Angular.Z);
        var center=.25*rows.Aggregate(default(D3),(sum,row)=>sum+row.Lever);
        var total=Enumerable.Range(0,4).Sum(i=>before.Impulses.At(i).Value);
        var twistCap=Enumerable.Range(0,4).Sum(i=>.125*before.Impulses.At(i).Value*(rows[i].Lever-center).Length);
        var t0=before.Impulses.T0.Value;var t1=before.Impulses.T1.Value;
        inputs=new{identity=before.Identity,features,previousPiece=before.Piece,nextPiece=before.Piece+1,previousKind=before.Kind.ToString(),
            nextKind=load.Force==default&&load.Torque==default?"Coast":"Powered",invalidated=owner.Invalidated,
            previousDuration=Duration(before.Duration),currentDuration=Duration(h),source,body,previousLoad=before.Load,currentLoad=load,
            currentOmega=(double)c.Omega,twiceDamping=(double)c.TwiceDamping,friction=(double)c.Friction,recovery=(double)c.Recovery,
            expectedNormal=before.Normal,cache=before.Impulses,cacheValues=Enumerable.Range(0,7).Select(i=>before.Impulses.At(i).Value).ToArray(),
            nativeCache=c.Impulses.Select(i=>(double)i).ToArray(),nativeCacheBits=c.Impulses.Select(i=>BitConverter.SingleToUInt32Bits(i).ToString("X8")).ToArray(),
            previousAcceleration=a0,currentAcceleration=a1,previousAngularAcceleration=w0,currentAngularAcceleration=w1,
            rows=rows.Select((r,i)=>new{index=i,row=r,depth=depths[i],normalSquared=D3.Dot(r.Normal,r.Normal),
                normalError=Math.Abs(D3.Dot(r.Normal,r.Normal)-1),unitBound=8*Math.ScaleB(1d,-23),
                previousNormalDrive=D3.Dot(r.Normal,a0)+D3.Dot(r.Moment,w0),currentNormalDrive=D3.Dot(r.Normal,a1)+D3.Dot(r.Moment,w1),
                pointNormalVelocity=D3.Dot(r.Normal,v)+D3.Dot(r.Moment,spin),speedBound=.0005/(16667d/1e6),
                normalCacheMantissa=before.Impulses.At(i).Mantissa}).ToArray(),center,totalNormal=total,
            tangentMagnitude=Math.Sqrt(t0*t0+t1*t1),tangentCap=.125*total,twistMagnitude=Math.Abs(before.Impulses.Twist.Value),twistCap,
            cacheFreeSupport=CacheHistory.DiagnosticSupportWithoutHistory(features,rows,depths,source,body,before.Load,load,before.Impulses,before.Normal,c.Omega),
            cacheFreeMeaning="diagnostic only: excludes historical normal equality, previous-load drive and impulse feasibility; retains current geometry/load/speed checks"};
    }
    internal static bool Reject(string expression,bool rejected)
    {
        if(active is not null){order++;predicates.Add(new{order,rowIndex=Row,rejectionExpression=expression,result=rejected?"FAIL":"PASS"});if(rejected&&firstFalse is null)firstFalse=expression;}
        return rejected;
    }
    internal static bool Accept(string expression,bool accepted)=>!Reject(expression,!accepted);
    internal static void After(CacheHistory owner,CacheStatus status)
    {
        if(active is null)return;
        Records.Add(new{type="admission",label=active,status=status.ToString(),inputs,predicates,firstFalse,
            laterPredicates="NOT EVALUATED after first false in actual short-circuit order",acceptedTupleUnchanged=owner.Snapshot==before,
            invalidated=owner.Invalidated,coastSolves=0});active=null;
    }
    internal static void Flush(string path)=>File.WriteAllText(path,JsonSerializer.Serialize(new{records=Records,
        productionChanges=0,canonicalMutations=0,coastSolves=0},new JsonSerializerOptions{WriteIndented=true})+Environment.NewLine);
}
