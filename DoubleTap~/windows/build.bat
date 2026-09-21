@echo off
setlocal
rem Run from a "x64 Native Tools Command Prompt for VS".
set OUT=%~dp0..\..\DoubleTap\Plugins
if not exist "%OUT%" mkdir "%OUT%"
cl /nologo /LD /O2 /EHsc /MT "%~dp0DoubleTapHook.cpp" /link /OUT:"%OUT%\DoubleTapHook.dll" user32.lib
del "%OUT%\DoubleTapHook.exp" "%OUT%\DoubleTapHook.lib" 2>nul
del "%~dp0DoubleTapHook.obj" 2>nul
echo built: %OUT%\DoubleTapHook.dll
