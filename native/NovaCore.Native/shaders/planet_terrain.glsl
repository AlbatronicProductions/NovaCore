#ifndef NOVACORE_PLANET_TERRAIN_GLSL
#define NOVACORE_PLANET_TERRAIN_GLSL

double PlanetTerrainSinD(double angle){const double tau=6.283185307179586476925286766559;return double(sin(float(angle-double(int(angle/tau))*tau)));}
double PlanetTerrainHeightD(dvec3 direction,uint patchLevel,double maximumHeight){
  if(maximumHeight<=0.0)return 0.0;
  dvec3 d=normalize(direction);
  double continental=.46*PlanetTerrainSinD(dot(d,normalize(dvec3(.8017837257372732,.2672612419124244,.5345224838248488)))*3.1+.7)
    +.31*PlanetTerrainSinD(dot(d,normalize(dvec3(-.4082482904638631,.8164965809277261,.4082482904638631)))*5.3-1.2)
    +.23*PlanetTerrainSinD(dot(d,normalize(dvec3(.1825741858350554,-.3651483716701107,.9128709291752769)))*8.7+.35);
  double land=max(0.0,continental-.02);double height=land*land*5200.0;
  int octaveCount=clamp((int(patchLevel)-7)/2,0,7);
  double amplitude=900.0;
  for(int octave=0;octave<octaveCount;octave++){
    double frequency=double(64u<<uint(octave*2));
    double waveA=PlanetTerrainSinD(dot(d,normalize(dvec3(.8728715609439696,.4364357804719848,-.2182178902359924)))*frequency
      +PlanetTerrainSinD(dot(d,normalize(dvec3(-.1690308509457033,.50709255283711,.8451542547285166)))*frequency*.73)*.65);
    double waveB=PlanetTerrainSinD(dot(d,normalize(dvec3(.3903600291794133,-.6506000486323555,.6506000486323555)))*frequency*1.31+double(octave)*1.7);
    double detail=.5+.3*waveA+.2*waveB;height+=detail*amplitude*(.2+.8*land);
    amplitude*=.52;
  }
  return clamp(height,0.0,maximumHeight);
}

#endif
