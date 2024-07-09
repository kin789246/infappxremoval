@echo off
dir /b /s *.inf > temp.txt
for /f "tokens=*" %%a in (temp.txt) do echo %%~nxa >> %1
del temp.txt