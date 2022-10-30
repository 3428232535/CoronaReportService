chcp 65001
set basePath=%~dp0%
cd /D %basePath%
for /f "delims=" %%i in ("%cd%") do set folder=%%~ni

sc stop %folder%
sc delete %folder%

cd /D %basePath%
rd /s /Q %basePath%
pause