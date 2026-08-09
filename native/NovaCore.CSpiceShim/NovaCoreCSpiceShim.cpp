#include "NovaCoreCSpiceShim.h"
#include <SpiceUsr.h>
#include <cmath>
#include <cstring>
namespace { int Copy(const char* value,char* out,int cap){if(!out||cap<1)return 0;std::strncpy(out,value,static_cast<size_t>(cap-1));out[cap-1]=0;return 1;} int Result(){return failed_c()?0:1;} }
int NCSP_CALL NcspGetToolkitVersion(char* out,int cap){return Copy(tkvrsn_c("TOOLKIT"),out,cap);}
int NCSP_CALL NcspLoadKernel(const char* path){if(!path)return 0;SpiceChar action[]="RETURN";SpiceChar device[]="NULL";erract_c("SET",0,action);errdev_c("SET",0,device);furnsh_c(path);return Result();}
int NCSP_CALL NcspClearKernels(){kclear_c();return Result();}
int NCSP_CALL NcspQueryGeometricState(int target,double et,NcspState* out){if(!out||!std::isfinite(et))return 0;SpiceDouble state[6];SpiceDouble lt;spkez_c(target,et,"J2000","NONE",0,state,&lt);if(!Result())return 0;for(int i=0;i<3;i++){out->positionKm[i]=state[i];out->velocityKmPerSecond[i]=state[i+3];}return 1;}
int NCSP_CALL NcspQueryFrameTransform(const char* fromFrame,const char* toFrame,double et,NcspMatrix3* out){if(!fromFrame||!toFrame||!out||!std::isfinite(et))return 0;SpiceDouble matrix[3][3];pxform_c(fromFrame,toFrame,et,matrix);if(!Result())return 0;for(int row=0;row<3;row++)for(int column=0;column<3;column++)out->value[row*3+column]=matrix[row][column];return 1;}
int NCSP_CALL NcspHasFailure(){return failed_c()?1:0;}
int NCSP_CALL NcspGetError(int isLong,char* out,int cap){SpiceChar message[1841];getmsg_c(isLong?"LONG":"SHORT",1840,message);return Copy(message,out,cap);}
void NCSP_CALL NcspResetError(){reset_c();}
