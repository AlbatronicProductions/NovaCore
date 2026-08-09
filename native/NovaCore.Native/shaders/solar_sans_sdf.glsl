float SansSegmentDistance(vec2 point,vec2 start,vec2 end){vec2 segment=end-start;float denominator=max(dot(segment,segment),1e-6);float amount=clamp(dot(point-start,segment)/denominator,0.0,1.0);return length(point-(start+segment*amount));}
float SansAdd(float distance,vec2 point,vec2 start,vec2 end){return min(distance,SansSegmentDistance(point,start,end));}

float SansGlyphDistance(int characterCode,vec2 glyphUv){
  int code=characterCode;vec2 p=vec2(glyphUv.x,1.0-glyphUv.y);
  if(code>=97&&code<=122){code-=32;p.y=(p.y-.14)/.72;}
  float d=10.0;
  if(code==32)return d;
  if(code==65){d=SansAdd(d,p,vec2(.12,.08),vec2(.50,.92));d=SansAdd(d,p,vec2(.50,.92),vec2(.88,.08));return SansAdd(d,p,vec2(.27,.43),vec2(.73,.43));}
  if(code==67){d=SansAdd(d,p,vec2(.84,.80),vec2(.68,.92));d=SansAdd(d,p,vec2(.68,.92),vec2(.30,.92));d=SansAdd(d,p,vec2(.30,.92),vec2(.13,.70));d=SansAdd(d,p,vec2(.13,.70),vec2(.13,.30));d=SansAdd(d,p,vec2(.13,.30),vec2(.30,.08));d=SansAdd(d,p,vec2(.30,.08),vec2(.68,.08));return SansAdd(d,p,vec2(.68,.08),vec2(.84,.20));}
  if(code==68){d=SansAdd(d,p,vec2(.16,.08),vec2(.16,.92));d=SansAdd(d,p,vec2(.16,.92),vec2(.58,.92));d=SansAdd(d,p,vec2(.58,.92),vec2(.82,.70));d=SansAdd(d,p,vec2(.82,.70),vec2(.82,.30));d=SansAdd(d,p,vec2(.82,.30),vec2(.58,.08));return SansAdd(d,p,vec2(.58,.08),vec2(.16,.08));}
  if(code==69){d=SansAdd(d,p,vec2(.18,.08),vec2(.18,.92));d=SansAdd(d,p,vec2(.18,.92),vec2(.84,.92));d=SansAdd(d,p,vec2(.18,.51),vec2(.72,.51));return SansAdd(d,p,vec2(.18,.08),vec2(.84,.08));}
  if(code==72){d=SansAdd(d,p,vec2(.16,.08),vec2(.16,.92));d=SansAdd(d,p,vec2(.84,.08),vec2(.84,.92));return SansAdd(d,p,vec2(.16,.50),vec2(.84,.50));}
  if(code==73){d=SansAdd(d,p,vec2(.18,.92),vec2(.82,.92));d=SansAdd(d,p,vec2(.50,.92),vec2(.50,.08));return SansAdd(d,p,vec2(.18,.08),vec2(.82,.08));}
  if(code==74){d=SansAdd(d,p,vec2(.22,.92),vec2(.84,.92));d=SansAdd(d,p,vec2(.68,.92),vec2(.68,.25));d=SansAdd(d,p,vec2(.68,.25),vec2(.55,.08));return SansAdd(d,p,vec2(.55,.08),vec2(.25,.08));}
  if(code==76){d=SansAdd(d,p,vec2(.18,.92),vec2(.18,.08));return SansAdd(d,p,vec2(.18,.08),vec2(.84,.08));}
  if(code==77){d=SansAdd(d,p,vec2(.10,.08),vec2(.10,.92));d=SansAdd(d,p,vec2(.10,.92),vec2(.50,.46));d=SansAdd(d,p,vec2(.50,.46),vec2(.90,.92));return SansAdd(d,p,vec2(.90,.92),vec2(.90,.08));}
  if(code==78){d=SansAdd(d,p,vec2(.14,.08),vec2(.14,.92));d=SansAdd(d,p,vec2(.14,.92),vec2(.86,.08));return SansAdd(d,p,vec2(.86,.08),vec2(.86,.92));}
  if(code==79||code==48){d=SansAdd(d,p,vec2(.31,.92),vec2(.69,.92));d=SansAdd(d,p,vec2(.69,.92),vec2(.86,.72));d=SansAdd(d,p,vec2(.86,.72),vec2(.86,.28));d=SansAdd(d,p,vec2(.86,.28),vec2(.69,.08));d=SansAdd(d,p,vec2(.69,.08),vec2(.31,.08));d=SansAdd(d,p,vec2(.31,.08),vec2(.14,.28));d=SansAdd(d,p,vec2(.14,.28),vec2(.14,.72));return SansAdd(d,p,vec2(.14,.72),vec2(.31,.92));}
  if(code==80||code==82){d=SansAdd(d,p,vec2(.16,.08),vec2(.16,.92));d=SansAdd(d,p,vec2(.16,.92),vec2(.62,.92));d=SansAdd(d,p,vec2(.62,.92),vec2(.82,.76));d=SansAdd(d,p,vec2(.82,.76),vec2(.82,.58));d=SansAdd(d,p,vec2(.82,.58),vec2(.62,.45));d=SansAdd(d,p,vec2(.62,.45),vec2(.16,.45));if(code==82)d=SansAdd(d,p,vec2(.55,.45),vec2(.86,.08));return d;}
  if(code==83){d=SansAdd(d,p,vec2(.82,.80),vec2(.66,.92));d=SansAdd(d,p,vec2(.66,.92),vec2(.30,.92));d=SansAdd(d,p,vec2(.30,.92),vec2(.14,.75));d=SansAdd(d,p,vec2(.14,.75),vec2(.27,.56));d=SansAdd(d,p,vec2(.27,.56),vec2(.70,.44));d=SansAdd(d,p,vec2(.70,.44),vec2(.84,.25));d=SansAdd(d,p,vec2(.84,.25),vec2(.68,.08));d=SansAdd(d,p,vec2(.68,.08),vec2(.28,.08));return SansAdd(d,p,vec2(.28,.08),vec2(.12,.20));}
  if(code==84){d=SansAdd(d,p,vec2(.10,.92),vec2(.90,.92));return SansAdd(d,p,vec2(.50,.92),vec2(.50,.08));}
  if(code==85){d=SansAdd(d,p,vec2(.14,.92),vec2(.14,.28));d=SansAdd(d,p,vec2(.14,.28),vec2(.31,.08));d=SansAdd(d,p,vec2(.31,.08),vec2(.69,.08));d=SansAdd(d,p,vec2(.69,.08),vec2(.86,.28));return SansAdd(d,p,vec2(.86,.28),vec2(.86,.92));}
  if(code==86){d=SansAdd(d,p,vec2(.10,.92),vec2(.50,.08));return SansAdd(d,p,vec2(.50,.08),vec2(.90,.92));}
  if(code==87){d=SansAdd(d,p,vec2(.08,.92),vec2(.27,.08));d=SansAdd(d,p,vec2(.27,.08),vec2(.50,.55));d=SansAdd(d,p,vec2(.50,.55),vec2(.73,.08));return SansAdd(d,p,vec2(.73,.08),vec2(.92,.92));}
  if(code==88){d=SansAdd(d,p,vec2(.12,.92),vec2(.88,.08));return SansAdd(d,p,vec2(.88,.92),vec2(.12,.08));}
  if(code==89){d=SansAdd(d,p,vec2(.10,.92),vec2(.50,.52));d=SansAdd(d,p,vec2(.90,.92),vec2(.50,.52));return SansAdd(d,p,vec2(.50,.52),vec2(.50,.08));}
  if(code==49){d=SansAdd(d,p,vec2(.31,.76),vec2(.50,.92));d=SansAdd(d,p,vec2(.50,.92),vec2(.50,.08));return SansAdd(d,p,vec2(.27,.08),vec2(.75,.08));}
  if(code==50){d=SansAdd(d,p,vec2(.16,.75),vec2(.31,.92));d=SansAdd(d,p,vec2(.31,.92),vec2(.69,.92));d=SansAdd(d,p,vec2(.69,.92),vec2(.84,.72));d=SansAdd(d,p,vec2(.84,.72),vec2(.16,.08));return SansAdd(d,p,vec2(.16,.08),vec2(.86,.08));}
  if(code==51){d=SansAdd(d,p,vec2(.18,.84),vec2(.34,.92));d=SansAdd(d,p,vec2(.34,.92),vec2(.70,.92));d=SansAdd(d,p,vec2(.70,.92),vec2(.84,.70));d=SansAdd(d,p,vec2(.84,.70),vec2(.66,.51));d=SansAdd(d,p,vec2(.66,.51),vec2(.84,.30));d=SansAdd(d,p,vec2(.84,.30),vec2(.69,.08));d=SansAdd(d,p,vec2(.69,.08),vec2(.31,.08));return SansAdd(d,p,vec2(.31,.08),vec2(.14,.18));}
  if(code==52){d=SansAdd(d,p,vec2(.70,.08),vec2(.70,.92));d=SansAdd(d,p,vec2(.70,.92),vec2(.14,.35));return SansAdd(d,p,vec2(.14,.35),vec2(.88,.35));}
  if(code==53){d=SansAdd(d,p,vec2(.82,.92),vec2(.22,.92));d=SansAdd(d,p,vec2(.22,.92),vec2(.18,.52));d=SansAdd(d,p,vec2(.18,.52),vec2(.68,.52));d=SansAdd(d,p,vec2(.68,.52),vec2(.84,.32));d=SansAdd(d,p,vec2(.84,.32),vec2(.70,.08));d=SansAdd(d,p,vec2(.70,.08),vec2(.28,.08));return SansAdd(d,p,vec2(.28,.08),vec2(.14,.20));}
  if(code==54){d=SansAdd(d,p,vec2(.78,.84),vec2(.64,.92));d=SansAdd(d,p,vec2(.64,.92),vec2(.30,.92));d=SansAdd(d,p,vec2(.30,.92),vec2(.14,.60));d=SansAdd(d,p,vec2(.14,.60),vec2(.14,.28));d=SansAdd(d,p,vec2(.14,.28),vec2(.31,.08));d=SansAdd(d,p,vec2(.31,.08),vec2(.68,.08));d=SansAdd(d,p,vec2(.68,.08),vec2(.84,.30));d=SansAdd(d,p,vec2(.84,.30),vec2(.68,.52));return SansAdd(d,p,vec2(.68,.52),vec2(.14,.52));}
  if(code==55){d=SansAdd(d,p,vec2(.14,.92),vec2(.86,.92));return SansAdd(d,p,vec2(.86,.92),vec2(.36,.08));}
  if(code==56){d=SansAdd(d,p,vec2(.30,.92),vec2(.70,.92));d=SansAdd(d,p,vec2(.70,.92),vec2(.84,.72));d=SansAdd(d,p,vec2(.84,.72),vec2(.68,.52));d=SansAdd(d,p,vec2(.68,.52),vec2(.30,.52));d=SansAdd(d,p,vec2(.30,.52),vec2(.14,.72));d=SansAdd(d,p,vec2(.14,.72),vec2(.30,.92));d=SansAdd(d,p,vec2(.30,.52),vec2(.14,.28));d=SansAdd(d,p,vec2(.14,.28),vec2(.30,.08));d=SansAdd(d,p,vec2(.30,.08),vec2(.70,.08));d=SansAdd(d,p,vec2(.70,.08),vec2(.84,.28));return SansAdd(d,p,vec2(.84,.28),vec2(.68,.52));}
  if(code==57){d=SansAdd(d,p,vec2(.82,.48),vec2(.30,.48));d=SansAdd(d,p,vec2(.30,.48),vec2(.14,.70));d=SansAdd(d,p,vec2(.14,.70),vec2(.30,.92));d=SansAdd(d,p,vec2(.30,.92),vec2(.68,.92));d=SansAdd(d,p,vec2(.68,.92),vec2(.84,.72));d=SansAdd(d,p,vec2(.84,.72),vec2(.84,.28));d=SansAdd(d,p,vec2(.84,.28),vec2(.68,.08));return SansAdd(d,p,vec2(.68,.08),vec2(.32,.08));}
  if(code==46)return length(p-vec2(.50,.10));
  if(code==44){d=SansAdd(d,p,vec2(.52,.12),vec2(.42,-.04));return min(d,length(p-vec2(.52,.12)));}
  if(code==40){d=SansAdd(d,p,vec2(.64,.94),vec2(.42,.70));d=SansAdd(d,p,vec2(.42,.70),vec2(.42,.30));return SansAdd(d,p,vec2(.42,.30),vec2(.64,.06));}
  if(code==41){d=SansAdd(d,p,vec2(.36,.94),vec2(.58,.70));d=SansAdd(d,p,vec2(.58,.70),vec2(.58,.30));return SansAdd(d,p,vec2(.58,.30),vec2(.36,.06));}
  return d;
}

float SansGlyphCoverage(int characterCode,vec2 glyphUv,float weight){float distance=SansGlyphDistance(characterCode,glyphUv);float antialias=max(fwidth(distance),.008);return 1.0-smoothstep(weight-antialias,weight+antialias,distance);}
