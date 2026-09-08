"""Recompile restored source into existing build outputs; preserve deployment."""
import json,subprocess
from lifecycle import ROOT,HERE,HOST,assess
before=assess.verify_deployment()
assert not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()
native=ROOT/'build/native-ninja-release/NovaCore.Native.dll';saved=native.read_bytes()
try:
    subprocess.run(['pwsh','-NoProfile','-Command',"& 'C:/Program Files/Microsoft Visual Studio/18/Community/Common7/Tools/Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation\ncmake --build build/native-ninja-release --target NovaCore.Native --parallel 4\nexit $LASTEXITCODE"],cwd=ROOT,check=True)
    rebuilt=assess.sha(native)
    subprocess.run(['dotnet','build','samples/NovaCore.Triangle/NovaCore.Triangle.csproj','-c','Release','--no-restore','-o',str(HOST),'-v','quiet'],cwd=ROOT,check=True)
finally:native.write_bytes(saved)
after=assess.verify_deployment()
assert before['deployment']==after['deployment'] and before['assets']==after['assets']
(HERE/'restoration.json').write_text(json.dumps({'nativeProductionRebuild':'PASS','managedProductionRebuild':'PASS',
 'rebuiltNativeHash':rebuilt,'nativeBuildDllRestoredHash':assess.sha(native),'normalDeployedArtifactsUnchanged':True,
 'trackedDiffEmpty':not subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).strip()},indent=2)+'\n')
