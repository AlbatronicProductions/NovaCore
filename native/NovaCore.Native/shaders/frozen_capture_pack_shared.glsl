// Integer-only diagnostic transport. This exact function also runs in the
// CPU-only qualification probe. It never recreates renderer output.
void FrozenPackLane(uint lane,uint lanes,uint vertices,uint triangles,
                    uint physicalOffset,uint visibilityOffset,uint compactedOffset,
                    uint counterOffset,uint indirectOffset) {
  uint count=PackIndirect(0u);
  bool valid=count<=triangles*3u&&count%3u==0u&&PackIndirect(1u)==1u&&
             PackIndirect(2u)==0u&&PackIndirect(3u)==0u&&PackIndirect(4u)==0u;
  if(lane==0u){if(!valid)PackError(1u);PackMetadata(count,valid);}
  for(uint v=lane;v<vertices;v+=lanes){
    for(uint k=0u;k<12u;++k)PackWrite(physicalOffset+v*12u+k,PackPhysical(v*16u+k));
    for(uint k=0u;k<4u;++k){uint value=PackPhysical(v*16u+12u+k);
      if(v<4u)PackWrite(physicalOffset+vertices*12u+v*4u+k,value);
      else if(value!=0u)PackPaddingError(v,k,value);
    }
  }
  for(uint i=lane;i<triangles;i+=lanes)PackWrite(visibilityOffset+i,PackVisibility(i));
  // Invalid counts fail the entire capture. Never clamp a count into a valid
  // capture, read unwritten tail bytes, or synthesize indices from visibility.
  if(valid)for(uint i=lane;i<count;i+=lanes)PackWrite(compactedOffset+i,PackCompacted(i));
  for(uint i=lane;i<42u;i+=lanes)PackWrite(counterOffset+i,PackCounter(i));
  for(uint i=lane;i<5u;i+=lanes)PackWrite(indirectOffset+i,PackIndirect(i));
}
