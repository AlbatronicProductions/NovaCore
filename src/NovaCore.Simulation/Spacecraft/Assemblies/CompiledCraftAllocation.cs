using System.Collections.Immutable;
using NovaCore.Core;

namespace NovaCore.Simulation.Spacecraft.Assemblies;

/// <summary>Cold pure-couple allocation geometry for the bounded manual-control
/// profile. It issues no command and has no resource or flight ownership.</summary>
internal sealed class CompiledCraftAllocation
{
    internal ImmutableArray<int> JetActuators {get;}
    // Canonical jet indices, never bit positions in a machine-sized word.
    internal ImmutableArray<ImmutableArray<int>> AxisJets {get;}
    internal bool Complete {get;}
    internal double ForceTolerance {get;}
    internal double TorqueTolerance {get;}
    private static double Norm(Double3 v){var scale=Math.Max(Math.Abs(v.X),Math.Max(Math.Abs(v.Y),Math.Abs(v.Z)));return scale==0?0:scale*Math.Sqrt((v/scale).LengthSquared);}
    internal CompiledCraftAllocation(ImmutableArray<CraftActuator> actuators,CraftMassLaw mass)
    {
        JetActuators=actuators.Select((a,i)=>(a,i)).Where(x=>x.a.Model=="nc.actuator.attitude/1").Select(x=>x.i).ToImmutableArray();
        if(JetActuators.IsEmpty){AxisJets=[];return;}
        var jets=JetActuators.Select(i=>actuators[i]).ToArray();var columns=jets.Select(a=>new CraftControlColumn(a.Axis*a.Thrust,Double3.Cross(a.Point,a.Axis*a.Thrust))).ToArray();
        // Authored axes are exact coefficients of this static wrench model.
        // This residual band covers their rigid-frame transport, not a relaxation
        // of the distinct authored axis-admission policy. Four frame factors at
        // the existing 1e-12 bound scale by total force and maximum lever arm.
        ForceTolerance=4e-12*jets.Sum(a=>a.Thrust);
        TorqueTolerance=ForceTolerance*Math.Max(1,jets.Max(a=>Norm(a.Point))+Math.Max(Norm(mass.ComMinimum),Norm(mass.ComMaximum)));
        AssemblyConstructionFacts.Require(double.IsFinite(ForceTolerance)&&double.IsFinite(TorqueTolerance)&&ForceTolerance>0&&TorqueTolerance>0&&columns.All(c=>c.Force.IsFinite&&c.MomentAtOrigin.IsFinite),"Craft allocation numerical range exceeded.");
        var selections=new ImmutableArray<int>[6];static double Axis(Double3 v,int axis)=>axis==0?v.X:axis==1?v.Y:v.Z;
        // Prove all pair additions before streaming can skip a selected pair.
        // Extrema bound each component of every reachable two-column sum.
        if(jets.Length>=2)for(var component=0;component<6;component++){
            double hi=double.NegativeInfinity,hi2=hi,lo=double.PositiveInfinity,lo2=lo;
            foreach(var c in columns){var v=Axis(component<3?c.Force:c.MomentAtOrigin,component%3);
                if(v>=hi){hi2=hi;hi=v;}else hi2=Math.Max(hi2,v);
                if(v<=lo){lo2=lo;lo=v;}else lo2=Math.Min(lo2,v);}
            AssemblyConstructionFacts.Require(double.IsFinite(hi+hi2)&&double.IsFinite(lo+lo2),"Craft allocation pair range exceeded.");
        }
        for(var axis=0;axis<3;axis++)for(var sign=-1;sign<=1;sign+=2){
            // Each row i streams partners in monotone signed-moment order.
            // Only one head per row is resident. Cursors never retreat: <=J²
            // pair examinations, O(J) scratch, O(J² log J) cold heap work.
            // Equal rounded strengths break by first identity then partner
            // rank (signed component, identity); no collection-order ties.
            var partners=Enumerable.Range(0,jets.Length).OrderByDescending(j=>Axis(columns[j].MomentAtOrigin,axis)*sign).ThenBy(j=>j).ToArray();
            var cursor=new int[jets.Length];var selected=new bool[jets.Length];
            var heads=new PriorityQueue<(int First,int Second),(double NegativeStrength,int First,int Rank)>(jets.Length);
            void Next(int i){
                while(cursor[i]<partners.Length){var rank=cursor[i]++;var j=partners[rank];if(j<=i||selected[j])continue;
                    var force=columns[i].Force+columns[j].Force;var torque=columns[i].MomentAtOrigin+columns[j].MomentAtOrigin;
                    var strength=Axis(torque,axis)*sign;
                    if(Norm(force)>ForceTolerance||strength<=TorqueTolerance||Math.Abs(Axis(torque,(axis+1)%3))>TorqueTolerance||Math.Abs(Axis(torque,(axis+2)%3))>TorqueTolerance)continue;
                    heads.Enqueue((i,j),(-strength,i,rank));return;
                }
            }
            for(var i=0;i<jets.Length;i++)Next(i);
            while(heads.TryDequeue(out var pair,out _)){
                if(selected[pair.First])continue;
                if(selected[pair.Second]){Next(pair.First);continue;}
                selected[pair.First]=true;selected[pair.Second]=true;
            }
            selections[axis*2+(sign>0?1:0)]=Enumerable.Range(0,jets.Length).Where(j=>selected[j]).ToImmutableArray();
        }
        AxisJets=selections.ToImmutableArray();Complete=selections.All(s=>!s.IsEmpty);
        // Union, not arithmetic addition: shared jets are actuated only once.
        // Prove every simultaneous request retains signs and zero unused axes
        // throughout the conservative COM box, before exposing FUNCTION.
        for(var roll=-1;roll<=1;roll++)for(var pitch=-1;pitch<=1;pitch++)for(var yaw=-1;yaw<=1;yaw++){
            int[] demand=[roll,pitch,yaw];var selected=new bool[jets.Length];for(var axis=0;axis<3;axis++)if(demand[axis]!=0)foreach(var j in selections[axis*2+(demand[axis]>0?1:0)])selected[j]=true;
            var force=Double3.Zero;var moment=Double3.Zero;for(var j=0;j<jets.Length;j++)if(selected[j]){force+=columns[j].Force;moment+=columns[j].MomentAtOrigin;}
            AssemblyConstructionFacts.Require(force.IsFinite&&moment.IsFinite,"Craft allocation union range exceeded.");
            if(Norm(force)>ForceTolerance)Complete=false;
            foreach(var x in new[]{mass.ComMinimum.X,mass.ComMaximum.X})foreach(var y in new[]{mass.ComMinimum.Y,mass.ComMaximum.Y})foreach(var z in new[]{mass.ComMinimum.Z,mass.ComMaximum.Z}){
                var torque=moment-Double3.Cross(new(x,y,z),force);AssemblyConstructionFacts.Require(torque.IsFinite,"Craft COM-torque range exceeded.");for(var axis=0;axis<3;axis++)if(demand[axis]==0?Math.Abs(Axis(torque,axis))>TorqueTolerance:Axis(torque,axis)*demand[axis]<=TorqueTolerance)Complete=false;
            }
        }
    }
}
