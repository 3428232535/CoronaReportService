chcp 65001
cd /D %~dp0
set path_dp0=%~dp0%
sc create CoronaReportService start="auto" binPath="%path_dp0%\CoronaReportService.exe" DisplayName="重庆大学自动打卡"
sc start CoronaReportService
pause