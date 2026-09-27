// Presentation only. Local attachment frames remain shared catalog data.
export const socketMarkerLimit=2048;
export function* sockets(part,definition){
    const r=part.pose.rotation,o=part.pose.position;
    for(const a of definition.attachments){const p=a.frame.position;yield {id:a.id,family:a.family,position:{
        x:o.x+r.a*p.x+r.b*p.y+r.c*p.z,y:o.y+r.d*p.x+r.e*p.y+r.f*p.z,z:o.z+r.g*p.x+r.h*p.y+r.i*p.z}};}
}
export function extents(documents,definitionFor,axes){
    let count=0,minX=Infinity,maxX=-Infinity,minY=Infinity,maxY=-Infinity;
    const add=p=>{const x=p[axes[0]],y=p[axes[1]];minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);count++;};
    for(const d of documents)for(const p of d.parts){add(p.pose.position);add(p.com);for(const s of sockets(p,definitionFor(p)))add(s.position);}
    return count?{count,minX,maxX,minY,maxY}:null;
}
