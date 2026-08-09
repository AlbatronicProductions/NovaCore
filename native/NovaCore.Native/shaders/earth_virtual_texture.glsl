const uint EARTH_TILE_SIZE=256u;
const uint EARTH_TILE_GUTTER=2u;
const uint EARTH_TILE_EXTENT=260u;
const uint EARTH_MAXIMUM_LEVEL=4u;
layout(set=0,binding=15) uniform sampler2DArray earthAlbedoPool;
layout(set=0,binding=16) uniform sampler2DArray earthElevationPool;
layout(set=0,binding=17) uniform sampler2DArray earthCloudPool;
layout(std430,set=0,binding=18) readonly buffer EarthPages { uvec4 values[]; } earthPageTable;

uint EarthLevelOffset(uint level){uint offset=0u,count=2u;for(uint i=0u;i<level;i++){offset+=count;count*=4u;}return offset;}
uint EarthTileIndex(uint level,uvec2 tile){return EarthLevelOffset(level)+tile.y*(1u<<(level+1u))+tile.x;}
vec2 EarthUv(vec3 direction){vec3 n=normalize(direction);return vec2(fract(atan(n.z,n.x)/6.2831853071795864769+.5),acos(clamp(n.y,-1.0,1.0))/3.14159265358979323846);}
vec3 EarthTileCoordinate(vec2 uv,uint level,uint slot){uvec2 count=uvec2(1u<<(level+1u),1u<<level);vec2 local=fract(uv*vec2(count));if(uv.y>=1.0)local.y=1.0;vec2 guttered=(local*float(EARTH_TILE_SIZE)+float(EARTH_TILE_GUTTER))/float(EARTH_TILE_EXTENT);return vec3(guttered,float(slot));}
uint EarthResidentPage(vec2 uv,uint requestedLevel,out uint residentLevel){for(int level=int(min(requestedLevel,EARTH_MAXIMUM_LEVEL));level>=0;level--){uvec2 count=uvec2(1u<<(uint(level)+1u),1u<<uint(level));uvec2 tile=uvec2(min(floor(uv*vec2(count)),vec2(count-1u)));uint page=EarthTileIndex(uint(level),tile);if(earthPageTable.values[page].x!=0u){residentLevel=uint(level);return page;}}residentLevel=0u;return uint(uv.x>=.5);}
vec4 EarthLayerSample(sampler2DArray pool,vec2 uv,uint page,uint level){uint slot=earthPageTable.values[page].x-1u;return texture(pool,EarthTileCoordinate(uv,level,slot));}
void EarthSurfaceSample(vec3 direction,uint requestedLevel,uint frame,out vec4 albedoLand,out float elevation,out float cloud,out float residencyBlend,out uint residentLevel){
  vec2 uv=EarthUv(direction);uint page=EarthResidentPage(uv,requestedLevel,residentLevel);albedoLand=EarthLayerSample(earthAlbedoPool,uv,page,residentLevel);float encoded=EarthLayerSample(earthElevationPool,uv,page,residentLevel).r;cloud=EarthLayerSample(earthCloudPool,uv,page,residentLevel).r;elevation=-11000.0+encoded*20000.0;
  residencyBlend=1.0;uint ready=earthPageTable.values[page].y;if(residentLevel>0u&&ready>0u){uint parent=earthPageTable.values[page].z;uint age=frame>ready?frame-ready:0u;residencyBlend=smoothstep(0.0,1.0,clamp(float(age)/30.0,0.0,1.0));vec4 parentAlbedo=EarthLayerSample(earthAlbedoPool,uv,parent,residentLevel-1u);float parentElevation=-11000.0+EarthLayerSample(earthElevationPool,uv,parent,residentLevel-1u).r*20000.0;float parentCloud=EarthLayerSample(earthCloudPool,uv,parent,residentLevel-1u).r;albedoLand=mix(parentAlbedo,albedoLand,residencyBlend);elevation=mix(parentElevation,elevation,residencyBlend);cloud=mix(parentCloud,cloud,residencyBlend);}
}
