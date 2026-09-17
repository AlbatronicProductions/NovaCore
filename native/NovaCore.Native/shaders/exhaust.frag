#version 460
// Original nozzle-volume code; see exhaust-method.md for method/provenance.
layout(input_attachment_index=0,set=0,binding=59) uniform subpassInput opaqueDepth;
layout(std430,set=0,binding=0) readonly buffer Frame { vec4 cameraHigh; vec4 cameraLow; mat4 viewProjection; } frame;
layout(location=0) in vec3 localPosition;
layout(location=1) flat in vec3 localEye;
layout(location=2) flat in vec4 rotation;
layout(location=3) flat in vec3 scale;
layout(location=4) flat in vec3 profile; // committed seconds, actual exhaust speed, stable nozzle index
layout(location=0) out vec4 weightedRadiance;
layout(location=1) out float opticalDepth;
vec3 Rotate(vec4 q,vec3 v){return v+2.0*cross(q.xyz,cross(q.xyz,v)+q.w*v);}
// Independently generated smooth noise replaces proprietary texture content.
float Corner(ivec2 p){uint h=uint(p.x)*1597334677u^uint(p.y)*3812015801u;h^=h>>16;h*=2246822519u;return float(h>>8)*(1.0/16777216.0);}
float Noise(vec2 p){ivec2 i=ivec2(floor(p));vec2 f=fract(p);f=f*f*f*(f*(f*6.0-15.0)+10.0);return mix(mix(Corner(i),Corner(i+ivec2(1,0)),f.x),mix(Corner(i+ivec2(0,1)),Corner(i+ivec2(1)),f.x),f.y);}
// Vacuum specialization: straight expansion (curvature zero), inverse-area
// density and density-limited visibility, capped by the original visual template.
const float exitRadius=.32,expansionSlope=.60,visibilityRatio=.25;
const float visibleLength=min(1.0,(exitRadius/expansionSlope)*(sqrt(2.0/visibilityRatio)-1.0));
float Radius(float x){return exitRadius+expansionSlope*x;}
void Field(vec3 p,out float extinction,out vec3 radiance){
    extinction=0.0;radiance=vec3(0);
    if(p.x<0.0||p.x>=visibleLength)return;
    // Visual advection is slowed explicitly for a readable metre-scale plume;
    // its carrier speed comes from immutable qualified engine state, not art.
    float x=p.x,visualSpeed=profile.y*.0004;
    float emitted=(profile.x-x*scale.x/max(visualSpeed,.001))*3.0+profile.z*5.13;
    vec2 displacement=vec2(Noise(vec2(emitted*1.6,7.0)),Noise(vec2(emitted*1.6,19.0)))-.5;
    vec2 crossSection=p.yz+displacement*(.10*x);
    float radius=Radius(x);
    // Sample azimuth through its unit vector to avoid an angular texture seam.
    vec2 radialDirection=crossSection/max(length(crossSection),1e-6);
    float surface=Noise(radialDirection*2.1+vec2(emitted*2.0,emitted*.7))-.5;
    float radial=length(crossSection)/(radius*(1.0+.10*x*surface));
    float body=1.0-smoothstep(.88-.36*x,1.0,radial);
    float shell=(1.0-smoothstep(.07,.22,abs(radial-.78)))*(1.0-x);
    // Normalized exit density is one at the lip. The radial term spreads
    // the same visual material across expansion; there is no Gaussian cone.
    float dilution=min(1.0,2.0*exitRadius*exitRadius/(radius*radius+dot(crossSection,crossSection)));
    float visibility=smoothstep(visibilityRatio,visibilityRatio*1.12,dilution);
    float axial=1.0-x/visibleLength;
    float density=dilution*visibility*axial*mix(.78,1.12,Noise(vec2(emitted*4.0,3.0)));
    // Separate profiles: clean emissive main; weakly absorbing RCS. These are
    // original visual coefficients, not inferred temperature/pressure/thrust.
    extinction=(profile.z>.5?.38:0.0)*density*body;
    vec3 color=mix(vec3(1.0,.97,.83),vec3(1.0,.46,.12),smoothstep(.08,.94,x));
    radiance=color*(body*(1.0-.7*x)+shell*.65)*density*30.0;
}
// Capped frustum intersection. Distances are along the local view ray.
vec2 Frustum(vec3 eye,vec3 ray,float x0,float x1,float r0,float r1){
    float enter=-1e20,leave=1e20;
    if(abs(ray.x)<1e-7){if(eye.x<x0||eye.x>x1)return vec2(1,-1);}
    else{vec2 caps=(vec2(x0,x1)-eye.x)/ray.x;enter=min(caps.x,caps.y);leave=max(caps.x,caps.y);}
    float slope=(r1-r0)/(x1-x0),r=r0+slope*(eye.x-x0);
    float a=dot(ray.yz,ray.yz)-slope*slope*ray.x*ray.x;
    float b=dot(eye.yz,ray.yz)-r*slope*ray.x;
    float c=dot(eye.yz,eye.yz)-r*r;
    if(abs(a)<1e-7){if(abs(b)<1e-7){if(c>0.0)return vec2(1,-1);}else if(b>0.0)leave=min(leave,-c/(2*b));else enter=max(enter,-c/(2*b));}
    else{
        float discriminant=b*b-a*c;
        if(discriminant<0.0){if(a>0.0)return vec2(1,-1);}
        else{float d=sqrt(discriminant);vec2 roots=vec2(-b-d,-b+d)/a;float low=min(roots.x,roots.y),high=max(roots.x,roots.y);
            if(a>0.0){enter=max(enter,low);leave=min(leave,high);}
            else if(slope*ray.x>0.0)enter=max(enter,high);else leave=min(leave,low);}
    }
    return vec2(enter,leave);
}
void main(){
    vec3 ray=normalize(localPosition-localEye);
    // Conservative frustum bounds for the density-limited vacuum expansion.
    // The raster box itself is never shaded. Modest padding encloses noise.
    vec2 bounds=Frustum(localEye,ray,0.0,visibleLength,.37,Radius(visibleLength)+.09);
    float begin=max(0.0,bounds.x),end=bounds.y;
    if(end<=begin)discard;
    float depth=subpassLoad(opaqueDepth).x;
    if(depth>0.0){vec4 o=frame.viewProjection*vec4(0,0,0,1);vec4 d=frame.viewProjection*vec4(Rotate(rotation,ray*scale),0);
        float denominator=d.z-depth*d.w;if(abs(denominator)>1e-12)end=min(end,(depth*o.w-o.z)/denominator);}
    if(end<=begin)discard;
    // Emitter-importance intervals cover the complete ray segment. Static pixel
    // stratification avoids temporal shimmer; full-resolution output needs no upscale.
    float closest=-dot(localEye,ray),perpendicular=max(length(localEye+ray*closest),.025);
    float angle0=atan((begin-closest)/perpendicular),angle1=atan((end-closest)/perpendicular);
    float sampleFraction=.35+.30*Corner(ivec2(gl_FragCoord.xy)+ivec2(int(profile.z)*31));
    const int samples=40;float previous=begin,transmission=1.0,integratedDepth=0.0;vec3 emission=vec3(0);
    for(int i=0;i<samples;i++){
        float next=i==samples-1?end:clamp(closest+perpendicular*tan(mix(angle0,angle1,float(i+1)/float(samples))),previous,end);
        float delta=next-previous;vec3 p=localEye+ray*mix(previous,next,sampleFraction);previous=next;
        float density;vec3 light;Field(p,density,light);
        float segmentDepth=density*delta,segmentTransmission=exp(-segmentDepth);
        float integral=density>1e-5?(1.0-segmentTransmission)/density:delta;
        emission+=transmission*light*integral;transmission*=segmentTransmission;integratedDepth+=segmentDepth;
    }
    if(max(emission.r,max(emission.g,emission.b))<=0.0&&integratedDepth<=0.0)discard;
    // Weighted blended OIT. exp(-sum opticalDepth) is product transmission.
    // Both attachments therefore use the same additive hardware blend state.
    float distanceToNozzle=length(localEye*scale);
    float weight=clamp((1.0-transmission+dot(emission,vec3(.2126,.7152,.0722))*.25)/(1.0+.01*distanceToNozzle*distanceToNozzle),.001,8.0);
    weightedRadiance=vec4(emission*weight,weight);opticalDepth=integratedDepth;
}
