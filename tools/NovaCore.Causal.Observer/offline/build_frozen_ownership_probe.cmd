@echo off
rem CPU-only tooling: no Vulkan library, native runtime or application linkage.
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b 1
if "%2"=="Debug" (set "PROBE_FLAGS=/Od /Zi /MDd") else (set "PROBE_FLAGS=/O2 /MD")
cl /nologo /std:c++20 /EHsc /DNOMINMAX /DWIN32_LEAN_AND_MEAN %PROBE_FLAGS% /I native/NovaCore.Native /I "%1" /I C:/VulkanSDK/1.4.357.0/Include /Fo"%1/ownership-%2.obj" /Fd"%1/ownership-%2.pdb" /Fe"%1/ownership-%2.exe" tools/NovaCore.Causal.Observer/offline/FrozenOwnershipProbe.cpp /link bcrypt.lib
if errorlevel 1 exit /b 1
dumpbin /imports "%1/ownership-%2.exe" > "%1/imports-%2.txt"
exit /b %errorlevel%
