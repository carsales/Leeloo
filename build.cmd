@echo off

echo "Building nuget package"

packages\FAKE.3.17.12\tools\fake.exe build.fsx

for %%x in (%cmdcmdline%) do if /i "%%~x"=="/c" set DOUBLECLICKED=1
if defined DOUBLECLICKED pause