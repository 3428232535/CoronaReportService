sc stop CoronaReportService
sc delete CoronaReportService
set basePath=%~dp0%
echo %basePath%
cd %basePath%
rd /s /Q %basePath%
pause