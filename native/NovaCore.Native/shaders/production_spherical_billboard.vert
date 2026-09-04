#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_ARB_gpu_shader_fp64 : require
#include "production_cube_surface.glsl"
// P2S5G: carry only vertex-dependent data into tessellation. TES reads body-wide
// presentation values from the same immutable frame buffers and computes its
// own anchored address; per-vertex copies of those values are redundant.
struct EncodedPosition{vec4 high;vec4 low;};struct GpuCameraData{EncodedPosition position;mat4 viewProjection;};
struct Presentation { vec4 centerRadius; vec4 colorDistant; vec4 blendMetricState; uvec4 identity; vec4 surface; uvec4 hooks; vec4 ringGeometry; vec4 ringOrientation; vec4 ringColor; vec4 bodyOrientation; vec4 localDetail; vec4 centerLow; };
layout(set=0,binding=0,std430) readonly buffer Frame{GpuCameraData camera;}frameData;
layout(set=0,binding=2,std430) readonly buffer Input{vec4 cameraHighRadiusHigh;vec4 cameraLowRadiusLow;vec4 thresholds;uvec4 controls;vec4 viewForwardHalfAngle;vec4 textureDemand;}inputData;
layout(set=0,binding=6,std430) readonly buffer Presentations{Presentation values[];}presentations;
struct PhysicalVertex{dvec4 body;vec4 normal;vec4 reserved;};layout(set=0,binding=38,std430) readonly buffer Physical{PhysicalVertex values[];}physical;
layout(location=1) out vec3 normal;layout(location=2) flat out vec3 lightDirection;layout(location=5) out vec3 viewDirection;layout(location=6) out vec3 bodyDirection;layout(location=7) out float terrainHeight;layout(location=17) out vec3 conservativeTrianglePosition;
layout(push_constant) uniform StellarLighting{vec4 sourceCenterExposure;vec4 sourceColorAmbient;vec4 radianceGlowEnabled;}lighting;
vec3 rotateQ(vec3 p,vec4 q){return p+2.0*cross(q.xyz,cross(q.xyz,p)+q.w*p);}
void main(){Presentation p=presentations.values[0];PhysicalVertex pv=physical.values[uint(gl_VertexIndex)];dvec4 bp=pv.body;vec4 pn=pv.normal;dvec3 direction=normalize(bp.xyz),camera=dvec3(inputData.cameraHighRadiusHigh.xyz)+dvec3(inputData.cameraLowRadiusLow.xyz);vec3 relativeBody=vec3(bp.xyz-camera),relative=rotateQ(relativeBody,p.bodyOrientation);normal=normalize(pn.xyz);lightDirection=normalize(rotateQ(lighting.sourceCenterExposure.xyz-p.centerRadius.xyz,vec4(-p.bodyOrientation.xyz,p.bodyOrientation.w)));viewDirection=-relativeBody;bodyDirection=vec3(direction);terrainHeight=float(bp.w);conservativeTrianglePosition=relative;gl_Position=frameData.camera.viewProjection*vec4(relative,1);}
