#version 450
layout(set=0,binding=4,std430) readonly buffer PreparedPositions { vec4 values[]; } positions;
layout(set=0,binding=5,std430) readonly buffer PreparedNormals { vec4 values[]; } normals;
layout(push_constant) uniform Constants {
  uint baseVertexCount; uint baseTriangleCount; uint workVertexCount; uint workTriangleCount;
  float bodyRadius; float cameraDistance; float tanHalfFov; float aspect;
  uint topologyLevel; uint frameIndex; uint outputIndexCapacity; uint reserved;
  uint coordinateEncoding; uint latticeScale;
} pc;
layout(location=0) out vec3 outNormal;
void main(){
  vec3 p=positions.values[gl_VertexIndex].xyz; float z=max(-p.z,pc.bodyRadius*1e-5);
  gl_Position=vec4(p.x/(pc.aspect*pc.tanHalfFov),p.y/pc.tanHalfFov,z-pc.bodyRadius*1e-5,z);
  outNormal=normals.values[gl_VertexIndex].xyz;
}
