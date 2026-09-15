using NovaCore.Simulation.Spacecraft.Actuation;
using S=PieceKernel.State;
using V=PieceKernel.V;

internal enum OperatorStatus { Ready,WrongThread,Busy,Invalidated,StaleLineage,InvalidDuration,UnsupportedSupport,
    BasisRefusal,DurationRefusal,LoadRefusal,DRefusal,ArithmeticFailure,StaleProposal,InstallFailure }
internal readonly record struct OwnedState(CacheIdentity Identity,long Generation,long Piece,PieceKind Kind,
    ContactGeometry ProducingGeometry,CacheDuration ProducingDuration,CacheVector Cache,Load Load,S Endpoint);

// Standalone private numerical prerequisite, deliberately absent from production/Simulation routing.
internal sealed class RetainedOperator
{
    private readonly int thread=Environment.CurrentManagedThreadId;
    private readonly CurrentPatch patch=new();
    private readonly Scaled[] work=new Scaled[7];
    private OwnedState accepted,installing;
    private Proposal installingProposal;
    private bool busy,invalidated;
    internal OwnedState Snapshot=>accepted;
    internal bool Invalidated=>invalidated;
    internal CurrentPatch Current=>patch;
    internal readonly record struct Diagnostics(GeneralizedTransport.Witness Transport,CacheVector Transported,
        CacheVector DurationScaled,Scaled LoadShift,CacheVector BeforeD,Scaled DDelta,Scaled DNumerator,double DDenominator,
        CacheVector AfterD,int NormalClamps,int TangentClamps,int TwistClamps);
    internal readonly struct Proposal
    {
        private readonly RetainedOperator owner;
        private readonly OwnedState source;
        internal readonly OwnedState Target;
        internal readonly ScaledVector LinearIncrement,AngularIncrement;
        internal readonly Diagnostics Proof;
        internal Proposal(RetainedOperator owner,OwnedState source,OwnedState target,ScaledVector linear,ScaledVector angular,Diagnostics proof)
        {this.owner=owner;this.source=source;Target=target;LinearIncrement=linear;AngularIncrement=angular;Proof=proof;}
        internal bool Matches(RetainedOperator candidate)=>ReferenceEquals(owner,candidate)&&source==candidate.accepted;
        internal bool SameAs(in Proposal other)=>ReferenceEquals(owner,other.owner)&&source==other.source&&Target==other.Target&&
            LinearIncrement==other.LinearIncrement&&AngularIncrement==other.AngularIncrement&&Proof==other.Proof;
    }
    internal RetainedOperator(in OwnedState seed)
    {
        if(!seed.Identity.Valid||seed.Generation<1||seed.Piece<0||!seed.ProducingGeometry.Valid||!seed.ProducingDuration.Valid||!seed.Cache.Valid||!seed.Load.Finite)
            throw new ArgumentException("Invalid accepted private provenance");
        accepted=seed;
    }
    private OperatorStatus Access()=>Environment.CurrentManagedThreadId!=thread?OperatorStatus.WrongThread:
        busy?OperatorStatus.Busy:invalidated?OperatorStatus.Invalidated:OperatorStatus.Ready;
    internal void Invalidate(){if(Environment.CurrentManagedThreadId!=thread)throw new InvalidOperationException("Wrong owner");invalidated=true;busy=false;}
    private static CacheVector Vector(ReadOnlySpan<Scaled> x)=>CacheVector.FromScales(x);
    internal OperatorStatus Prepare(CacheIdentity identity,long generation,long piece,PieceKind kind,
        in ContactGeometry current,in CacheDuration duration,in InverseBody body,in Load load,in S source,
        ReadOnlySpan<double> nativeCache,out Proposal proposal,out Diagnostics proof)
    {
        proposal=default;proof=default;var access=Access();if(access!=OperatorStatus.Ready)return access;
        if(identity!=accepted.Identity||generation!=accepted.Generation||generation==long.MaxValue||accepted.Piece==long.MaxValue||piece!=accepted.Piece+1||
            source!=accepted.Endpoint||current.Features!=accepted.ProducingGeometry.Features||!accepted.Cache.MatchesTransport(nativeCache))return OperatorStatus.StaleLineage;
        if(kind is not PieceKind.Powered and not PieceKind.Coast)return OperatorStatus.StaleLineage;
        if(!duration.Valid||duration.Numerical.Value>16667d/1e6)return OperatorStatus.InvalidDuration;
        if(!current.Valid||!body.Valid||!load.Finite||current.Omega!=accepted.ProducingGeometry.Omega)return OperatorStatus.UnsupportedSupport;
        var a0=accepted.Load.Gravity+body.Mass*accepted.Load.Force;var a1=load.Gravity+body.Mass*load.Force;
        var w0=body.Apply(accepted.Load.Torque);var w1=body.Apply(load.Torque);
        if(!a0.Finite||!a1.Finite||!w0.Finite||!w1.Finite)return OperatorStatus.ArithmeticFailure;
        patch.Set(current,body);
        for(int i=0;i<4;i++)if(D3.Dot(current.Normal,a0)+D3.Dot(current.Angular(i),w0)>=0||
            D3.Dot(current.Normal,a1)+D3.Dot(current.Angular(i),w1)>=0||Math.Abs(patch.J(i,source))>.0005/(16667d/1e6))return OperatorStatus.UnsupportedSupport;
        Span<Scaled> old=stackalloc Scaled[7];for(int i=0;i<7;i++)old[i]=accepted.Cache.At(i);
        if(!GeneralizedTransport.TryMove(accepted.ProducingGeometry,current,old,work,out var moved))return OperatorStatus.BasisRefusal;
        var transported=Vector(work);
        if(!DurationInitialization.TryScale(transported,accepted.ProducingDuration,duration,out var scaled))return OperatorStatus.DurationRefusal;
        for(int i=0;i<7;i++)work[i]=scaled.At(i);
        var h=Scaled.From(duration.Numerical);var omega=current.Omega;var a=h.Times(omega);var k=a.Times(a.Value+2);
        var d=1+k.Value;var c=k.Divide(d);var inverseD=1/d;
        if(!double.IsFinite(d)||d<=0)return OperatorStatus.ArithmeticFailure;
        D3 sumN=default,sumR=default;double diagonal=0;
        for(int i=0;i<4;i++){sumN+=current.Normal;sumR+=current.Angular(i);diagonal+=patch.K[i,i];}
        var response=body.Mass*D3.Dot(sumN,sumN)+D3.Dot(sumR,body.Apply(sumR));
        var projected=D3.Dot(sumN,a1-a0)+D3.Dot(sumR,w1-w0);
        double loadDenominator=k.Value*response+diagonal;
        if(!double.IsFinite(loadDenominator)||loadDenominator<=0)return OperatorStatus.LoadRefusal;
        var shift=h.Times(k).Times(-projected).Divide(loadDenominator);
        for(int i=0;i<4;i++)work[i]=ScaleMath.Add(work[i],shift);
        var beforeD=Vector(work);
        if(!StudyControl.ArithmeticValid(work))return OperatorStatus.LoadRefusal;
        var linearKick=ScaledVector.Product(h,a1);var angularKick=ScaledVector.Product(h,w1);
        // For ordinary represented h preserve the selected arithmetic. At tiny h use the same
        // normal equations multiplied by c=k/(1+k); no alpha overflow or depth/h projection.
        Scaled numerator=default,delta=default;double denominator=0;
        double ordinaryH=duration.Numerical.Value;
        bool ordinary=ordinaryH>0&&double.IsFinite(1/(omega*ordinaryH*(omega*ordinaryH+2)));
        var free=new S(source.Linear+linearKick.Projection,source.Angular+angularKick.Projection);
        if(StudyControl.UseD)
        {
        if(ordinary)
        {
            double alpha=1/(omega*ordinaryH*(omega*ordinaryH+2)),num=0;
            for(int i=0;i<4;i++)
            {
                double residual=patch.J(i,free)-Math.Min(current.Depth(i)/ordinaryH,Math.Min(current.Depth(i)/(ordinaryH+2/omega),2));
                for(int j=0;j<7;j++){double coefficient=patch.K[i,j]+(i==j?alpha*patch.K[i,i]:0);residual+=coefficient*work[j].Value;if(j<4)denominator+=coefficient;}
                num+=residual;
            }
            numerator=Scaled.From(num);delta=Scaled.From(-num/denominator);
        }
        else
        {
            for(int i=0;i<4;i++)
            {
                var residual=ScaleMath.Subtract(c.Times(FreeJ(i,source,linearKick,angularKick)),Bias(i,a,c,d));
                for(int j=0;j<7;j++)
                {
                    residual=ScaleMath.Add(residual,c.Times(work[j]).Times(patch.K[i,j]));
                    if(i==j)residual=ScaleMath.Add(residual,work[j].Times(inverseD*patch.K[i,i]));
                    if(j<4)denominator+=c.Value*patch.K[i,j]+(i==j?inverseD*patch.K[i,i]:0);
                }
                numerator=ScaleMath.Add(numerator,residual);
            }
            delta=ScaleMath.Negate(numerator).Divide(denominator);
        }
        if(!double.IsFinite(denominator)||denominator<=0||!ScaleMath.Valid(delta))return OperatorStatus.DRefusal;
        for(int i=0;i<4;i++)work[i]=ScaleMath.Add(work[i],delta);
        }
        proof=new(moved,transported,scaled,shift,beforeD,delta,numerator,denominator,Vector(work),0,0,0);
        StudyControl.Override(work);
        if(!StudyControl.CurrentFeasible(current,work))return OperatorStatus.DRefusal;
        StudyControl.Begin(patch,free,ordinaryH,work);
        StudyControl.Observe("initial",0,-1,work,0,0,0);
        int nc=0,tc=0,wc=0;
        for(int sweep=0;sweep<StudyControl.Sweeps;sweep++)
        {
            for(int i=0;i<4;i++)
            {
                var others=ordinary?Scaled.From(patch.J(i,free)):FreeJ(i,source,linearKick,angularKick);
                for(int j=0;j<7;j++)if(j!=i)others=ScaleMath.Add(others,work[j].Times(patch.K[i,j]));
                var next=ScaleMath.Subtract(Bias(i,a,c,d),c.Times(others)).Divide(patch.K[i,i]);
                if(next.Mantissa<0){nc++;next=default;}work[i]=next;
                StudyControl.Observe("normal-row",sweep+1,i,work,nc,tc,wc);
            }
            StudyControl.Observe("before-tangent",sweep+1,-1,work,nc,tc,wc);
            var v0=ordinary?Scaled.From(patch.J(4,free)):FreeJ(4,source,linearKick,angularKick);
            var v1=ordinary?Scaled.From(patch.J(5,free)):FreeJ(5,source,linearKick,angularKick);
            for(int j=0;j<7;j++)if(j is not 4 and not 5){v0=ScaleMath.Add(v0,work[j].Times(patch.K[4,j]));v1=ScaleMath.Add(v1,work[j].Times(patch.K[5,j]));}
            double det=patch.K[4,4]*patch.K[5,5]-patch.K[4,5]*patch.K[5,4];
            if(!double.IsFinite(det)||det<=0)return OperatorStatus.ArithmeticFailure;
            var x=ScaleMath.Add(v0.Times(-patch.K[5,5]),v1.Times(patch.K[4,5])).Divide(det);
            var y=ScaleMath.Add(v0.Times(patch.K[5,4]),v1.Times(-patch.K[4,4])).Divide(det);
            Scaled cap=default;for(int i=0;i<4;i++)cap=ScaleMath.Add(cap,work[i].Times(.125));
            var length=ScaleMath.Length(x,y);var guard=Scaled.From((double)1e-16f);
            var maximum=ScaleMath.Compare(guard,length)>0?guard:length;
            var scale=ScaleMath.Min(Scaled.From(1),ScaleMath.Divide(cap,maximum));
            if(ScaleMath.Compare(scale,Scaled.From(1))<0)tc++;
            work[4]=x.Times(scale);work[5]=y.Times(scale);
            StudyControl.Observe("after-tangent",sweep+1,-1,work,nc,tc,wc);
            var spin=ordinary?Scaled.From(patch.J(6,free)):FreeJ(6,source,linearKick,angularKick);
            for(int j=0;j<6;j++)spin=ScaleMath.Add(spin,work[j].Times(patch.K[6,j]));
            var twist=spin.Times(-1).Divide(patch.K[6,6]);Scaled twistCap=default;
            for(int j=0;j<4;j++)twistCap=ScaleMath.Add(twistCap,work[j].Times(.125*current.Radius(j)));
            if(ScaleMath.Compare(ScaleMath.Abs(twist),twistCap)>0){wc++;twist=twistCap.Times(Math.Sign(twist.Mantissa));}work[6]=twist;
            StudyControl.Observe("after-sweep",sweep+1,-1,work,nc,tc,wc);
        }
        var dv=linearKick;var dw=angularKick;
        for(int i=0;i<7;i++)
        {dv+=ScaledVector.Product(work[i].Times(body.Mass),patch.Linear[i]);dw+=ScaledVector.Product(work[i],body.Apply(patch.Angular[i]));}
        var endpoint=new S(source.Linear+dv.Projection,source.Angular+dw.Projection);
        var solved=Vector(work);if(!solved.Valid||!double.IsFinite(endpoint.Linear.Length)||!double.IsFinite(endpoint.Angular.Length))return OperatorStatus.ArithmeticFailure;
        proof=proof with{NormalClamps=nc,TangentClamps=tc,TwistClamps=wc};
        var target=new OwnedState(identity,checked(generation+1),piece,kind,current,duration,solved,load,endpoint);
        proposal=new(this,accepted,target,dv,dw,proof);return OperatorStatus.Ready;
    }
    private Scaled FreeJ(int i,in S source,in ScaledVector linear,in ScaledVector angular)=>
        ScaleMath.Add(Scaled.From(patch.J(i,source)),ScaleMath.Add(linear.Dot(patch.Linear[i]),angular.Dot(patch.Angular[i])));
    private Scaled Bias(int i,Scaled a,Scaled c,double d)
    {
        double depth=patch.Geometry.Depth(i),omega=patch.Geometry.Omega;
        return ScaleMath.Min(Scaled.From(depth*omega*(a.Value+2)/d),
            ScaleMath.Min(a.Times(depth*omega/d),c.Times(2)));
    }
    internal OperatorStatus BeginInstall(in Proposal proposal,in S projectedEndpoint)
    {
        var status=Access();if(status!=OperatorStatus.Ready)return status;
        if(!proposal.Matches(this))return OperatorStatus.StaleProposal;
        var expected=proposal.Target.Endpoint;
        if(projectedEndpoint!=new S(new((double)(float)expected.Linear.X,(double)(float)expected.Linear.Y,(double)(float)expected.Linear.Z),
            new((double)(float)expected.Angular.X,(double)(float)expected.Angular.Y,(double)(float)expected.Angular.Z)))return OperatorStatus.InstallFailure;
        installing=proposal.Target with{Endpoint=projectedEndpoint};installingProposal=proposal;busy=true;invalidated=true;return OperatorStatus.Ready;
    }
    internal OperatorStatus CompleteInstall(in Proposal proposal)
    {
        if(Environment.CurrentManagedThreadId!=thread)return OperatorStatus.WrongThread;
        if(!busy||!proposal.Matches(this)||!proposal.SameAs(installingProposal)){busy=false;invalidated=true;return OperatorStatus.InstallFailure;}
        accepted=installing;busy=false;invalidated=false;return OperatorStatus.Ready;
    }
}
