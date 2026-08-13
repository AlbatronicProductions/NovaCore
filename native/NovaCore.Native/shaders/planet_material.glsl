#include "earth_virtual_texture.glsl"
float PlanetHash(vec3 p){p=fract(p*0.1031);p+=dot(p,p.yzx+33.33);return fract((p.x+p.y)*p.z);}
float PlanetNoise(vec3 p){vec3 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(mix(PlanetHash(i),PlanetHash(i+vec3(1,0,0)),f.x),mix(PlanetHash(i+vec3(0,1,0)),PlanetHash(i+vec3(1,1,0)),f.x),f.y),mix(mix(PlanetHash(i+vec3(0,0,1)),PlanetHash(i+vec3(1,0,1)),f.x),mix(PlanetHash(i+vec3(0,1,1)),PlanetHash(i+vec3(1)),f.x),f.y),f.z);}
float PlanetFbm(vec3 p){float value=0.0,weight=.55;for(int octave=0;octave<5;octave++){value+=weight*PlanetNoise(p);p=p*2.03+vec3(11.7,4.3,7.1);weight*=.5;}return value;}
float PlanetHash2(vec2 p){vec3 q=fract(vec3(p.x,p.y,p.x)*vec3(.1031,.1030,.0973));q+=dot(q,q.yzx+33.33);return fract((q.x+q.y)*q.z);}
float PlanetNoise2(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(PlanetHash2(i),PlanetHash2(i+vec2(1,0)),f.x),mix(PlanetHash2(i+vec2(0,1)),PlanetHash2(i+vec2(1)),f.x),f.y);}
vec3 PlanetTriplanarWeights(vec3 n){vec3 w=pow(abs(normalize(n)),vec3(4.0));return w/max(w.x+w.y+w.z,1e-5);}
float PlanetTriplanarNoise(vec3 position,vec3 n){vec3 w=PlanetTriplanarWeights(n);return PlanetNoise2(position.yz)*w.x+PlanetNoise2(position.xz+vec2(17.7,43.1))*w.y+PlanetNoise2(position.xy+vec2(31.3,11.9))*w.z;}
float PlanetFilteredTriplanarNoise(vec3 position,vec3 n,float fadeStart,float fadeEnd){float footprint=max(max(length(dFdx(position)),length(dFdy(position))),0.0);return mix(.5,PlanetTriplanarNoise(position,n),1.0-smoothstep(fadeStart,fadeEnd,footprint));}
vec3 PlanetBodyPosition(vec3 direction,float radius,float height){return normalize(direction)*(radius+height);}
vec3 PlanetTangentDetail(vec3 position,vec3 up,vec3 normal,float scale){vec3 p=position/scale;vec3 vector=vec3(PlanetTriplanarNoise(p+vec3(7.1,0,0),normal),PlanetTriplanarNoise(p+vec3(0,19.3,0),normal),PlanetTriplanarNoise(p+vec3(0,0,37.7),normal))-.5;return vector-up*dot(vector,up);}
vec3 PlanetWrappedBodyCoordinate(vec3 position,vec3 up,float scale){
  vec3 normalizedUp=normalize(up);
  vec3 reference=abs(normalizedUp.y)<.9?vec3(0,1,0):vec3(1,0,0);
  vec3 east=normalize(cross(reference,normalizedUp));
  vec3 north=normalize(cross(normalizedUp,east));
  return mod(vec3(dot(position,east),dot(position,north),dot(position,normalizedUp))/scale,4096.0);
}
vec3 PlanetTangentDetailCoordinate(vec3 coordinate,vec3 up,vec3 normal){vec3 vector=vec3(PlanetTriplanarNoise(coordinate+vec3(7.1,0,0),normal),PlanetTriplanarNoise(coordinate+vec3(0,19.3,0),normal),PlanetTriplanarNoise(coordinate+vec3(0,0,37.7),normal))-.5;return vector-up*dot(vector,up);}
vec3 DecodeBc5Normal(vec2 encodedXY)
{
  vec2 signedXY = encodedXY * 2.0 - 1.0;
  float z = sqrt(max(0.0, 1.0 - dot(signedXY, signedXY)));
  return normalize(vec3(signedXY, z));
}
vec2 ProjectWorldVectorToBc5Tangent(vec3 up, vec3 vector)
{
  vec3 normalizedUp = normalize(up);
  vec3 reference = abs(normalizedUp.y) < .9 ? vec3(0,1,0) : vec3(1,0,0);
  vec3 east = normalize(cross(reference, normalizedUp));
  vec3 north = normalize(cross(normalizedUp, east));
  return clamp(vec2(dot(vector, east), dot(vector, north)) * 0.5 + 0.5, 0.0, 1.0);
}
vec3 ComposeDecodedMicroNormal(vec3 macroNormal, vec3 localMicroNormal, float localContribution, float detailStrength);
vec3 ComposeMicroNormal(vec3 macroNormal, vec2 encodedMicroXY, float localContribution, float detailStrength)
{
  return ComposeDecodedMicroNormal(macroNormal,DecodeBc5Normal(encodedMicroXY),localContribution,detailStrength);
}
vec3 ComposeDecodedMicroNormal(vec3 macroNormal, vec3 localMicroNormal, float localContribution, float detailStrength)
{
  vec3 up = normalize(macroNormal);
  localMicroNormal=normalize(localMicroNormal);
  vec3 reference = abs(up.y) < .9 ? vec3(0,1,0) : vec3(1,0,0);
  vec3 east = normalize(cross(reference, up));
  vec3 north = normalize(cross(up, east));
  vec3 microWorld = normalize(east * localMicroNormal.x + north * localMicroNormal.y + up * localMicroNormal.z);
  float blend = clamp(localContribution * detailStrength, 0.0, 1.0);
  return normalize(mix(up, microWorld, blend));
}
vec3 RotatePlanetY(vec3 direction,float angle){float c=cos(angle),s=sin(angle);return vec3(c*direction.x+s*direction.z,direction.y,-s*direction.x+c*direction.z);}
vec3 PlanetAlbedo(uint source,vec3 direction,vec3 tint,float rotation){
  vec3 n=normalize(direction);float latitude=abs(n.y);float longitude=atan(n.z,n.x);float broad=PlanetFbm(n*2.4);float detail=PlanetFbm(n*9.0);
  if(source==10u){vec4 albedoLand;float elevation,cloud,blend;uint level;EarthSurfaceSample(n,0u,0u,albedoLand,elevation,cloud,blend,level);return albedoLand.rgb*tint;}
  if(source==1u||source==4u){float craters=smoothstep(.66,.42,abs(fract(PlanetNoise(floor(n*14.0))*7.0+detail)-.5));float shade=.55+.45*broad-.13*craters;return tint*shade;}
  if(source==2u){float bands=.5+.5*sin(n.y*42.0+PlanetFbm(n*5.0)*7.0+longitude*.8);return mix(tint*.68,vec3(1.0,.76,.43),.35+.45*bands);}
  if(source==3u){
    float continental=broad+.16*PlanetFbm(n*7.0+vec3(2.7,9.1,4.3))-.10*latitude;
    vec3 ocean=mix(vec3(.012,.055,.18),vec3(.025,.19,.42),detail);
    float regional=PlanetFbm(n*31.0+vec3(17.0,3.0,29.0));
    float kilometer=PlanetFbm(n*620.0+vec3(5.0,41.0,11.0));
    float localFrequency=24500.0,meterFrequency=310000.0;
    float localFootprint=max(length(dFdx(n*localFrequency)),length(dFdy(n*localFrequency)));
    float meterFootprint=max(length(dFdx(n*meterFrequency)),length(dFdy(n*meterFrequency)));
    float local=(PlanetNoise(n*localFrequency+vec3(19.0,7.0,31.0))-.5)*(1.0-smoothstep(.16,.9,localFootprint));
    float meter=(PlanetNoise(n*meterFrequency+vec3(53.0,13.0,23.0))-.5)*(1.0-smoothstep(.12,.85,meterFootprint));
    float aridity=clamp(.30+.55*regional-.18*latitude,0.0,1.0);
    vec3 fertile=mix(vec3(.055,.16,.035),vec3(.22,.34,.09),kilometer);
    vec3 dry=mix(vec3(.28,.20,.09),vec3(.52,.39,.19),regional);
    vec3 land=mix(fertile,dry,aridity);land*=.84+.18*kilometer+.12*local+.06*meter;
    vec3 albedo=mix(ocean,land,smoothstep(.50,.57,continental));
    return mix(albedo,vec3(.82,.90,.94),smoothstep(.82,.94,latitude));
  }
  if(source==5u){vec3 soil=mix(vec3(.24,.055,.025),tint,clamp(.35+.8*broad,0.0,1.0));soil*=.72+.38*detail;return mix(soil,vec3(.78,.73,.65),smoothstep(.88,.97,latitude));}
  if(source==6u){float belt=.5+.5*sin(n.y*73.0+PlanetFbm(vec3(longitude*1.3,n.y*8.0,2.0))*4.0);vec3 bands=mix(vec3(.30,.13,.055),vec3(.92,.76,.53),belt);float storm=exp(-pow((longitude+1.05)/.34,2.0)-pow((n.y+.31)/.12,2.0));return mix(bands,vec3(.75,.22,.08),storm*.8);}
  if(source==7u){float belt=.5+.5*sin(n.y*90.0+PlanetFbm(n*4.0)*2.2);return mix(vec3(.43,.32,.18),vec3(.92,.80,.57),.35+.55*belt);}
  if(source==8u){float belt=.5+.5*sin(n.y*38.0+detail*1.2);return mix(vec3(.20,.59,.64),tint,.55+.25*belt);}
  if(source==9u){float belt=.5+.5*sin(n.y*48.0+PlanetFbm(n*5.0)*2.5);vec3 blue=mix(vec3(.025,.08,.34),tint,.45+.45*belt);float storm=exp(-pow((longitude-.6)/.25,2.0)-pow((n.y+.18)/.13,2.0));return mix(blue,vec3(.13,.16,.30),storm*.75);}
  return tint;
}
vec3 PlanetLighting(vec3 albedo,vec3 normal,vec3 lightDirection,vec3 viewDirection,float roughness,float specular,float emissive,float ambientFloor){vec3 n=normalize(normal),l=normalize(lightDirection),v=normalize(viewDirection);float diffuse=max(dot(n,l),0.0);vec3 h=normalize(l+v);float exponent=mix(96.0,5.0,clamp(roughness,0.0,1.0));float highlight=pow(max(dot(n,h),0.0),exponent)*specular*diffuse;return albedo*mix(ambientFloor,1.0,diffuse)+vec3(highlight)+albedo*emissive;}
