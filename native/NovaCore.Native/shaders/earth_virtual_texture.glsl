const uint EARTH_TILE_SIZE=256u;
const uint EARTH_TILE_GUTTER=2u;
const uint EARTH_TILE_EXTENT=260u;
const uint EARTH_MAXIMUM_LEVEL=4u;
const uint EARTH_ALBEDO_CHANNEL=0u;
const uint EARTH_ELEVATION_CHANNEL=1u;
const uint EARTH_MASK_CHANNEL=2u;
const uint EARTH_CLOUD_CHANNEL=3u;
layout(set=0,binding=15) uniform sampler2DArray earthAlbedoPool;
layout(set=0,binding=16) uniform sampler2DArray earthElevationPool;
layout(set=0,binding=17) uniform sampler2DArray earthCloudPool;
layout(set=0,binding=19) uniform sampler2DArray earthMaskPool;
struct EarthPage { uvec4 slots;uvec4 readyFrames; };
layout(std430,set=0,binding=18) readonly buffer EarthPages { EarthPage values[]; } earthPageTable;

uint EarthLevelOffset(uint level){uint offset=0u,count=2u;for(uint i=0u;i<level;i++){offset+=count;count*=4u;}return offset;}
uint EarthTileIndex(uint level,uvec2 tile){return EarthLevelOffset(level)+tile.y*(1u<<(level+1u))+tile.x;}
vec2 EarthUv(vec3 direction){vec3 n=normalize(direction);return vec2(fract(atan(n.z,n.x)/6.2831853071795864769+.5),acos(clamp(n.y,-1.0,1.0))/3.14159265358979323846);}
vec3 EarthTileCoordinate(vec2 uv,uint level,uint slot){uvec2 count=uvec2(1u<<(level+1u),1u<<level);vec2 local=fract(uv*vec2(count));if(uv.y>=1.0)local.y=1.0;vec2 guttered=(local*float(EARTH_TILE_SIZE)+float(EARTH_TILE_GUTTER))/float(EARTH_TILE_EXTENT);return vec3(guttered,float(slot));}
uint EarthResidentPageChannel(vec2 uv,uint requestedLevel,uint channel,out uint residentLevel){for(int level=int(min(requestedLevel,EARTH_MAXIMUM_LEVEL));level>=0;level--){uvec2 count=uvec2(1u<<(uint(level)+1u),1u<<uint(level));uvec2 tile=uvec2(min(floor(uv*vec2(count)),vec2(count-1u)));uint page=EarthTileIndex(uint(level),tile);if(earthPageTable.values[page].slots[channel]!=0u){residentLevel=uint(level);return page;}}residentLevel=0u;return uint(uv.x>=.5);}
uint EarthResidentPage(vec2 uv,uint requestedLevel,out uint residentLevel){return EarthResidentPageChannel(uv,requestedLevel,EARTH_ALBEDO_CHANNEL,residentLevel);}
vec4 EarthLayerSample(sampler2DArray pool,vec2 uv,uint page,uint level,uint channel){uint slot=earthPageTable.values[page].slots[channel]-1u;return texture(pool,EarthTileCoordinate(uv,level,slot));}
vec4 EarthChannelSample(sampler2DArray pool,vec2 uv,uint requestedLevel,uint channel,uint frame,out uint residentLevel,out float blend){
  uint page=EarthResidentPageChannel(uv,requestedLevel,channel,residentLevel);vec4 value=EarthLayerSample(pool,uv,page,residentLevel,channel);blend=1.0;uint ready=earthPageTable.values[page].readyFrames[channel];
  if(residentLevel>0u&&ready>0u){uint parentLevel;uint parent=EarthResidentPageChannel(uv,residentLevel-1u,channel,parentLevel);uint age=frame>ready?frame-ready:0u;blend=smoothstep(0.0,1.0,clamp(float(age)/30.0,0.0,1.0));value=mix(EarthLayerSample(pool,uv,parent,parentLevel,channel),value,blend);}return value;
}
void EarthSurfaceSample(vec3 direction,uint requestedLevel,uint frame,out vec4 albedoLand,out float elevation,out float cloud,out float residencyBlend,out uint residentLevel){
  vec2 uv=EarthUv(direction);float albedoBlend,elevationBlend,maskBlend,cloudBlend;uint elevationLevel,maskLevel,cloudLevel;vec4 albedo=EarthChannelSample(earthAlbedoPool,uv,requestedLevel,EARTH_ALBEDO_CHANNEL,frame,residentLevel,albedoBlend);float encoded=EarthChannelSample(earthElevationPool,uv,requestedLevel,EARTH_ELEVATION_CHANNEL,frame,elevationLevel,elevationBlend).r;float land=EarthChannelSample(earthMaskPool,uv,requestedLevel,EARTH_MASK_CHANNEL,frame,maskLevel,maskBlend).r;cloud=EarthChannelSample(earthCloudPool,uv,min(requestedLevel,2u),EARTH_CLOUD_CHANNEL,frame,cloudLevel,cloudBlend).r;albedoLand=vec4(albedo.rgb,land);elevation=-11000.0+encoded*20000.0;residencyBlend=min(min(albedoBlend,elevationBlend),min(maskBlend,cloudBlend));
}
