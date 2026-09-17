#version 460
struct EncodedPosition { vec4 high; vec4 low; };
struct RenderObject { EncodedPosition position; vec4 rotation; vec4 scale; uint mesh; uint p0; uint p1; uint p2; };
layout(std430,set=0,binding=0) readonly buffer Frame { EncodedPosition camera; mat4 viewProjection; RenderObject objects[]; } frame;
layout(location=0) in vec3 proxyPosition;
layout(location=0) out vec3 localPosition;
layout(location=1) flat out vec3 localEye;
layout(location=2) flat out vec4 rotation;
layout(location=3) flat out vec3 scale;
layout(location=4) flat out vec3 profile;
vec3 Rotate(vec4 q,vec3 v){return v+2.0*cross(q.xyz,cross(q.xyz,v)+q.w*v);}
void main(){
    RenderObject object=frame.objects[gl_InstanceIndex];
    vec3 origin=object.position.high.xyz+object.position.low.xyz;
    rotation=object.rotation;scale=object.scale.xyz;
    profile=vec3(uintBitsToFloat(object.p0),uintBitsToFloat(object.p1),float(object.p2));
    localPosition=proxyPosition;
    localEye=Rotate(vec4(-rotation.xyz,rotation.w),-origin)/scale;
    gl_Position=frame.viewProjection*vec4(origin+Rotate(rotation,proxyPosition*scale),1.0);
}
