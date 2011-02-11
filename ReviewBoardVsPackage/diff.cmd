@ECHO off

set WINDIFF="C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\x64\WinDiff.Exe"
set OUTFILE=%~n0.txt
if EXIST %OUTFILE% del %OUTFILE%


:ankh

set ANKH_ROOT=C:\srcs\ankhsvn\src

set ROOT_LEFT=Ankh\Ankh.UI\VSSelectionControls
set ROOT_RIGHT=%ANKH_ROOT%\Ankh.UI\VSSelectionControls

for %%i in (%ROOT_LEFT%\*.*) do (
  echo i=%%i
  echo %%i %ROOT_RIGHT%\%%~nxi>>%OUTFILE%
)

set ROOT_LEFT=Ankh\Ankh.Services
set ROOT_RIGHT=%ANKH_ROOT%\Ankh.Services

for %%i in (%ROOT_LEFT%\*.*) do (
  echo i=%%i
  echo %%i %ROOT_RIGHT%\%%~nxi>>%OUTFILE%
)


:windiff

rem notepad %OUTFILE%
start "windiff" %WINDIFF% -I %OUTFILE%

