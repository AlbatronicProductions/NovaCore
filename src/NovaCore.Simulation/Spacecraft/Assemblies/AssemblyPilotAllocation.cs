using NovaCore.Core;
using NovaCore.Simulation.Spacecraft.Resources;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

internal enum AssemblyControlExecution { DemandOnly, PhysicalActuators }

/// <summary>Cold, bounded qualification and 54 immutable actuator rows. No stores or state ownership.</summary>
internal sealed class AssemblyPilotAllocation
{
    private readonly CompiledAssemblyCommand[] rows=new CompiledAssemblyCommand[54];
    private static int Index(bool main,AssemblyPilotDemand p)=>(main?27:0)+(p.Pitch+1)*9+(p.Yaw+1)*3+p.Roll+1;
    internal CompiledAssemblyCommand Resolve(AssemblyControlRequest request,long ticks)
    {
        var row=rows[Index(request.MainOn,request.Pilot)];
        return row with {Request=row.Request with {Ticks=ticks}};
    }
    internal AssemblyPilotAllocation(CompiledAssemblyDesign d)
    {
        if(!d.HasIndependentBlockJets||d.Jets.Length!=16||d.Development is not null)
            throw new InvalidDataException("Pilot allocation requires the qualified independent-jet profile.");
        var masks=new ushort[6];var largestPair=0d;
        var dry=d.ObserveMass(d.DryMass);var wet=d.ObserveMass(d.MaximumMass);
        foreach(var pair in d.Data.Design.Pairs)
        {
            ushort mask=0;
            for(var j=0;j<d.Jets.Length;j++)if(d.Jets[j].Matches(pair.First,pair.FirstActuator)||d.Jets[j].Matches(pair.Second,pair.SecondActuator))mask|=(ushort)(1<<j);
            if(System.Numerics.BitOperations.PopCount((uint)mask)!=2)throw new InvalidDataException("Invalid pilot pair mask.");
            masks[pair.Axis*2+(pair.Sign>0?1:0)]=mask;
            var a=AssemblyActuation.Jet(d.Jet(pair.First,pair.FirstActuator));var b=AssemblyActuation.Jet(d.Jet(pair.Second,pair.SecondActuator));
            foreach(var mass in new[]{dry,wet})largestPair=Math.Max(largestPair,Math.Sqrt((a.MomentAtOrigin+b.MomentAtOrigin-Double3.Cross(mass.Com,a.Force+b.Force)).LengthSquared));
        }
        ushort Pair(int axis,int sign)=>sign==0?(ushort)0:masks[axis*2+(sign>0?1:0)];
        var g=d.Main.Definition.Gimbal!;
        // Exactly the existing global proof, not a new motion or time envelope.
        var torqueBound=d.Main.Definition.Propulsion!.FullThrustN*(1.1+dry.Com.X)*Math.Sqrt(1-Math.Pow(Math.Cos(.05),4))+largestPair;
        var rateBound=AssemblyResources.Rate(43d/1024);
        for(var engine=0;engine<2;engine++)for(sbyte pitch=-1;pitch<=1;pitch++)for(sbyte yaw=-1;yaw<=1;yaw++)for(sbyte roll=-1;roll<=1;roll++)
        {
            var main=engine!=0;var pilot=new AssemblyPilotDemand(pitch,yaw,roll);
            // Branch on this admitted main request, never the prior realized state.
            var mask=Pair(0,roll);
            if(!main)mask|=(ushort)(Pair(1,pitch)|Pair(2,yaw));
            var y=main?-pitch*g.LimitY:0;var z=main?-yaw*g.LimitZ:0;
            var rate=main?AssemblyResources.Rate(d.Main.Definition.Propulsion.ExtentRateKgS):default(PropellantInteger);
            var scalarThrust=main?d.Main.Definition.Propulsion.FullThrustN:0;
            var wrench=main?AssemblyActuation.Main(d,y,z):default;
            for(var j=0;j<d.Jets.Length;j++)if((mask&(1<<j))!=0)
            {
                var jet=d.Jets[j];var w=AssemblyActuation.Jet(jet);
                wrench=new(wrench.Force+w.Force,wrench.MomentAtOrigin+w.MomentAtOrigin);
                rate=AssemblyResources.Add(rate,AssemblyResources.Rate(jet.Propulsion.ExtentRateKgS));
                scalarThrust+=jet.Propulsion.FullThrustN;
            }
            if(scalarThrust>645||!PropellantInteger.TrySubtract(rateBound,rate,out _))throw new InvalidDataException("Pilot allocation exceeds existing force/resource bound.");
            foreach(var mass in new[]{dry,wet})
            {
                var tau=wrench.MomentAtOrigin-Double3.Cross(mass.Com,wrench.Force);
                if(!tau.IsFinite||Math.Sqrt(tau.LengthSquared)>torqueBound)throw new InvalidDataException("Pilot allocation exceeds existing COM torque bound.");
                static bool Signed(double torque,int requested)=>requested==0?Math.Abs(torque)<1e-9:torque*requested>1e-9;
                if(!Signed(tau.X,roll)||!Signed(tau.Y,pitch)||!Signed(tau.Z,yaw))throw new InvalidDataException("Coupled or missing pilot axis authority.");
            }
            rows[Index(main,pilot)]=new(new(main,null,y,z,0,mask),mask,rate);
        }
        // COM torque is affine in 1/mass; its norm is convex. Endpoint inclusion
        // preserves the existing global dynamic bound for every intermediate mass.
    }
}
