chcp 65001
cd /D %~dp0
set path_dp0=%~dp0%
for /f "delims=" %%i in ("%cd%") do set folder=%%~ni
sc create %folder% start="auto" binPath="%path_dp0%\%folder%.exe" DisplayName="重庆大学自动打卡"
sc start %folder%
pause
