@echo off
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b 1
if "%2"=="Debug" (set "PROBE_FLAGS=/Od /Zi /MDd") else (set "PROBE_FLAGS=/O2 /MD")
cl /nologo /std:c++20 /EHsc /DNOMINMAX /DWIN32_LEAN_AND_MEAN %PROBE_FLAGS% /I native/NovaCore.Native /I "%1" /Fo"%1/packing-%2.obj" /Fd"%1/packing-%2.pdb" /Fe"%1/packing-%2.exe" tools/NovaCore.Causal.Observer/offline/FrozenPackingProbe.cpp /link bcrypt.lib
if errorlevel 1 exit /b 1
dumpbin /imports "%1/packing-%2.exe" > "%1/packing-imports-%2.txt"
exit /b %errorlevel%
