@echo off
setlocal EnableExtensions
cd /d "%~dp0"
call "SETUP-AND-BUILD.cmd"
exit /b %ERRORLEVEL%
