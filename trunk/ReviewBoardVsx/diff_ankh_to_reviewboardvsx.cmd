@ECHO off

set WINDIFF="C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\x64\WinDiff.Exe"
set OUTFILE=%~n0.txt
if EXIST %OUTFILE% del %OUTFILE%


:ankh

set ANKH_ROOT=C:\srcs\ankhsvn\src

set ROOT_LEFT=%ANKH_ROOT%
set ROOT_RIGHT=.

echo %ROOT_LEFT%\Ankh.Package\Ankh.Package.csproj %ROOT_RIGHT%\ReviewBoardVsx.Package\ReviewBoardVsx.Package.csproj >>%OUTFILE%


set ROOT_LEFT=%ANKH_ROOT%\Ankh.UI\VSSelectionControls
set ROOT_RIGHT=.\ReviewBoardVsx.Package\Ankh\Ankh.UI\VSSelectionControls
for %%i in (%ROOT_RIGHT%\*.*) do (
  rem echo i=%%i
  echo %ROOT_LEFT%\%%~nxi %%i>>%OUTFILE%
)


set ROOT_LEFT=%ANKH_ROOT%\Ankh.Services
set ROOT_RIGHT=.\ReviewBoardVsx.Package\Ankh\Ankh.Services
for %%i in (%ROOT_RIGHT%\*.*) do (
  rem echo i=%%i
  echo %ROOT_LEFT%\%%~nxi %%i>>%OUTFILE%
)


set ROOT_LEFT=%ANKH_ROOT%\tools\Ankh.GenerateVSIXManifest
set ROOT_RIGHT=.\tools\ReviewBoardVsx.GenerateVSIXManifest
echo %ROOT_LEFT%\Program.cs                       %ROOT_RIGHT%\Program.cs                                 >>%OUTFILE%
echo %ROOT_LEFT%\Ankh.GenerateVSIXManifest.csproj %ROOT_RIGHT%\ReviewBoardVsx.GenerateVSIXManifest.csproj >>%OUTFILE%
echo %ROOT_LEFT%\Properties\AssemblyInfo.cs       %ROOT_RIGHT%\Properties\AssemblyInfo.cs                 >>%OUTFILE%


set ROOT_LEFT=%ANKH_ROOT%\tools\Ankh.GenPkgDef
set ROOT_RIGHT=.\tools\Ankh.GenPkgDef
for %%i in (%ROOT_RIGHT%\*.*) do (
  rem echo i=%%i
  echo %ROOT_LEFT%\%%~nxi %%i>>%OUTFILE%
)
for %%i in (%ROOT_RIGHT%\Properties\*.*) do (
  rem echo i=%%i
  echo %ROOT_LEFT%\Properties\%%~nxi %%i>>%OUTFILE%
)


:ankh_votive
set ROOT_LEFT=%ANKH_ROOT%
set ROOT_RIGHT=.
echo %ROOT_LEFT%\Ankh.Votive.wixproj           %ROOT_RIGHT%\ReviewBoardVsx.Setup.wixproj                      >>%OUTFILE%
echo %ROOT_LEFT%\Ankh.Ids\Ankh.Ids.wxs         %ROOT_RIGHT%\ReviewBoardVsx.Ids\ReviewBoardVsx.Ids.wxs         >>%OUTFILE%
echo %ROOT_LEFT%\Ankh.Package\Ankh.Package.wxs %ROOT_RIGHT%\ReviewBoardVsx.Package\ReviewBoardVsx.Package.wxs >>%OUTFILE%
echo %ROOT_LEFT%\Ankh.Votive\Ankh.Votive.wxs   %ROOT_RIGHT%\ReviewBoardVsx.Setup\ReviewBoardVsx.Setup.wxs     >>%OUTFILE%
echo %ROOT_LEFT%\Ankh.Votive\VSExtension.wxs   %ROOT_RIGHT%\ReviewBoardVsx.Setup\VSExtension.wxs              >>%OUTFILE%


:windiff

rem start "notepad" notepad %OUTFILE%
start "windiff" %WINDIFF% -I %OUTFILE%

