// Numerical preflight only. This is not a GPU dual-path renderer or a shader emulator.
// Calls the current CPU canonical family evaluator; never copies or edits its implementation.
using System.Text.Json;
using NovaCore.Core;
using NovaCore.Core.Surface;
using NovaCore.Graphics;

const double Radius=6371008.8;
var identity=new PlanetaryNaturalTerrainFamilyIdentity(6,2,0x4D12D2B1u);
var groups=new Dictionary<string,Dictionary<string,Stats>>();
var anchors=new List<(string Name,Double3 Direction)>();
foreach(var x in new[]{-1,0,1})foreach(var y in new[]{-1,0,1})foreach(var z in new[]{-1,0,1})
    if(x!=0||y!=0||z!=0)anchors.Add(("face-seam-corner",new Double3(x,y,z).Normalized()));
anchors.Add(("Florida",FloridaFacilitySupport.Region.Up));
foreach(double e in new[]{-192d,-128d,-64d,64d,96d,128d,192d})
    anchors.Add(("Florida-support-transition",(FloridaFacilitySupport.Region.Up*Radius+FloridaFacilitySupport.Region.East*e).Normalized()));
foreach(double n in new[]{-184d,-56d,56d,120d,184d})
    anchors.Add(("Florida-support-transition",(FloridaFacilitySupport.Region.Up*Radius+FloridaFacilitySupport.Region.North*n).Normalized()));
for(int i=0;i<24;i++)anchors.Add(("remote",new Double3(Math.Sin(i*1.13+.4),Math.Cos(i*.73+.2),Math.Sin(i*.37+1.2)).Normalized()));
var magnitudes=new[]{.0001,.01,1d,10d,40d,49.5,49.999,50d,64d,128d,192d,1000d,10000d};
var witness=new List<object>();
object? informationLossWitness=null;
long cases=0;
foreach(var entry in anchors)
{
    var up=entry.Direction.Normalized();
    var east=Double3.Cross(Math.Abs(up.Y)<.99?Double3.UnitY:Double3.UnitX,up).Normalized();
    var north=Double3.Cross(up,east).Normalized();
    var anchor=up*Radius;
    // The scalar components are transported exactly as current split-camera input.
    var camera=Split(anchor+up*10.004);
    foreach(var distance in magnitudes)for(int step=0;step<32;step++)
    {
        double angle=step*Math.PI/16d;
        var physical=(anchor+east*(distance*Math.Cos(angle))+north*(distance*Math.Sin(angle))).Normalized()*(Radius+16d);
        var view=F32(camera-physical);
        var point=camera-view;
        var dir=NormalizeShaderOrder(point);
        var input=NormalizeShaderOrder(dir)*Radius;
        var a=Near(input,dir);
        if(informationLossWitness is null && entry.Name=="remote")
        {
            var view2=new Double3(MathF.BitIncrement((float)view.X),view.Y,view.Z);
            var point2=camera-view2;var direction2=NormalizeShaderOrder(point2);
            var input2=NormalizeShaderOrder(direction2)*Radius;var a2=Near(input2,direction2);
            if(!Eq(input,input2)&&Eq(F32(point-anchor),F32(point2-anchor))&&Bits(a.Height)!=Bits(a2.Height))
                informationLossWitness=new{meaning="Same anchor and identical FP32 local offset encode two distinct current-input results; no decoder of that payload alone can recover both",camera=Vec(camera),anchor=Vec(anchor),view1=Vec(view),view2=Vec(view2),point1=Vec(point),point2=Vec(point2),encodedLocal=Vec(F32(point-anchor)),input1=Vec(input),input2=Vec(input2),height1=a.Height,height2=a2.Height};
        }
        // All variants retain the banked prepared base and vary only evaluation coordinates.
        var variants=new Dictionary<string,Double3>{
            ["local-fp32-delta"]=anchor+F32(point-anchor),
            ["local-fp64-roundtrip-control"]=anchor+(point-anchor),
            ["local-fp32-basis"]=anchor+east*(float)Double3.Dot(point-anchor,east)+north*(float)Double3.Dot(point-anchor,north)+up*(float)Double3.Dot(point-anchor,up)
        };
        var group=entry.Name+(Len(view)<=50d?"/near":"/outside-near-stress");
        foreach(var v in variants)
            Compare(v.Key,group,point,dir,input,a,v.Value,NormalizeShaderOrder(v.Value),anchor,view);
        var delta=point-anchor;
        var squared=Double3.Dot(anchor,anchor)+2d*Double3.Dot(anchor,delta)+Double3.Dot(delta,delta);
        Compare("local-fp64-norm-reassociation",group,point,dir,input,a,point,point*(1d/Math.Sqrt(squared)),anchor,view);
        var anchor2=anchor+east*64d;
        var edgeA=anchor+F32(point-anchor);var edgeB=anchor2+F32(point-anchor2);
        var dirA=NormalizeShaderOrder(edgeA);var inputA=NormalizeShaderOrder(dirA)*Radius;
        Compare("shared-point-two-fp32-anchors",group,edgeA,dirA,inputA,Near(inputA,dirA),edgeB,NormalizeShaderOrder(edgeB),anchor2,view);
        cases++;
    }
}

// A separate domain-reduction witness: even exact point/delta storage does not
// preserve rounded division after splitting the quotient into anchor and delta.
var reduction=new Dictionary<string,object>();
foreach(double cell in new[]{120d,180d,220d,240d,260d,320d,520d,240000d})
{
    long n=0,exact=0,cellMismatch=0;double max=0;object? example=null;
    for(int i=0;i<12000;i++)
    {
        double anchor=(i%2==0?1d:-1d)*(Radius-((i*7919)%5000000));
        double point=anchor+(float)(Math.Sin(i*.17)*49.99);
        double direct=point/cell,local=anchor/cell+(point-anchor)/cell;
        n++;if(Bits(direct)==Bits(local))exact++;
        if(Math.Floor(direct)!=Math.Floor(local))cellMismatch++;
        max=Math.Max(max,Math.Abs(direct-local));
        if(example is null&&Bits(direct)!=Bits(local))example=new{anchor,point,cell,direct,local,directBits=Bits(direct),localBits=Bits(local)};
    }
    reduction[cell.ToString(System.Globalization.CultureInfo.InvariantCulture)]=new{n,exact,cellMismatch,maxQuotientDelta=max,example};
}

Console.WriteLine(JsonSerializer.Serialize(new{kind="CPU numerical preflight; not actual TES or raster samples",cases,
    reference="CPU binary64 operation order, current canonical composed.Near + current facility attenuation",
    groups=groups.ToDictionary(g=>g.Key,g=>g.Value.ToDictionary(s=>s.Key,s=>s.Value.Summary())),witness,informationLossWitness,reduction,
    canonicalGradientBound=PlanetaryNaturalTerrainFamilies.ComposedBounds().TotalGradient,
    facilityIdentity=FloridaFacilitySupport.DefinitionIdentity,
    runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription},new JsonSerializerOptions{WriteIndented=true}));

Double3 F32(Double3 p)=>new((float)p.X,(float)p.Y,(float)p.Z);
Double3 Split(Double3 p){var h=F32(p);return h+F32(p-h);}
Double3 NormalizeShaderOrder(Double3 p)=>p*(1d/Math.Sqrt(Double3.Dot(p,p)));
PlanetaryNaturalTerrainFieldSample Near(Double3 p,Double3 d)
{
    var value=PlanetaryNaturalTerrainFamilies.EvaluateComposed(p,identity).Near;
    var support=FloridaFacilitySupport.Region.Sample(d);
    return new(value.Height*(1d-support.Weight),value.BodyGradient*(1d-support.Weight)-support.WeightGradient*value.Height);
}
void Compare(string variant,string group,Double3 p,Double3 d,Double3 h,PlanetaryNaturalTerrainFieldSample a,
    Double3 bp,Double3 bd,Double3 anchor,Double3 view)
{
    var bh=NormalizeShaderOrder(bd)*Radius;
    var b=Near(bh,bd);
    if(!groups.TryGetValue(variant,out var byGroup))groups[variant]=byGroup=new();
    if(!byGroup.TryGetValue(group,out var s))byGroup[group]=s=new();
    double inputError=Len(bh-h),positionError=Len(bp-p);
    double angle=Math.Atan2(Len(Double3.Cross(d,bd)),Double3.Dot(d,bd));
    double heightError=Math.Abs(a.Height-b.Height);
    double gradientError=Len(a.BodyGradient-b.BodyGradient);
    // Same prepared position, unit near weight: isolates near physical contribution.
    double displacementError=Len(d*a.Height-bd*b.Height);
    double floatDisplacementError=Len(F32(d*a.Height)-F32(bd*b.Height));
    var supports=FloridaFacilitySupport.Region.Sample(d);var supportB=FloridaFacilitySupport.Region.Sample(bd);
    bool directionExact=Eq(d,bd),inputExact=Eq(h,bh),heightExact=Bits(a.Height)==Bits(b.Height),gradientExact=Eq(a.BodyGradient,b.BodyGradient);
    s.Add(Eq(p,bp),directionExact,inputExact,heightExact,gradientExact,positionError,inputError,angle,heightError,gradientError,displacementError,floatDisplacementError,Math.Abs(supports.Weight-supportB.Weight));
    if(witness.Count(x=>((Witness)x).Variant==variant)<2&&!inputExact)
        witness.Add(new Witness(variant,group,Vec(anchor),Vec(view),Vec(p),Vec(bp),Vec(h),Vec(bh),a.Height,b.Height,positionError,inputError,heightError,angle));
}
static string Bits(double x)=>BitConverter.DoubleToUInt64Bits(x).ToString("x16");
static double Len(Double3 p)=>Math.Sqrt(p.LengthSquared);
static bool Eq(Double3 a,Double3 b)=>Bits(a.X)==Bits(b.X)&&Bits(a.Y)==Bits(b.Y)&&Bits(a.Z)==Bits(b.Z);
static object Vec(Double3 p)=>new{p.X,p.Y,p.Z,bits=new[]{Bits(p.X),Bits(p.Y),Bits(p.Z)}};

record Witness(string Variant,string Group,object Anchor,object View,object PointA,object PointB,object InputA,object InputB,double HeightA,double HeightB,double PositionError,double InputError,double HeightError,double AngularError);
class Stats
{
    long n,pExact,dExact,hExact,heightExact,gradientExact;double pMax,hMax,angleMax,heightMax,gradientMax,displacementMax,floatDisplacementMax,supportMax,pSquared,heightSquared;
    public void Add(bool pe,bool de,bool he,bool ve,bool ge,double pd,double hd,double angle,double vd,double gd,double dd,double fd,double sd)
    {n++;if(pe)pExact++;if(de)dExact++;if(he)hExact++;if(ve)heightExact++;if(ge)gradientExact++;pMax=Math.Max(pMax,pd);hMax=Math.Max(hMax,hd);angleMax=Math.Max(angleMax,angle);heightMax=Math.Max(heightMax,vd);gradientMax=Math.Max(gradientMax,gd);displacementMax=Math.Max(displacementMax,dd);floatDisplacementMax=Math.Max(floatDisplacementMax,fd);supportMax=Math.Max(supportMax,sd);pSquared+=pd*pd;heightSquared+=vd*vd;}
    public object Summary()=>new{samples=n,preparedPointExact=pExact,directionExact=dExact,hInputExact=hExact,nearHeightExact=heightExact,nearGradientExact=gradientExact,pointMaxMetres=pMax,pointRmsMetres=Math.Sqrt(pSquared/n),hInputMaxMetres=hMax,angularMaxRadians=angleMax,nearHeightMaxMetres=heightMax,nearHeightRmsMetres=Math.Sqrt(heightSquared/n),gradientMax,nearDisplacementMaxMetres=displacementMax,fp32NearDisplacementMaxMetres=floatDisplacementMax,supportWeightMax=supportMax};
}
