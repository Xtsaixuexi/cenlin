@echo off
chcp 65001 >nul
title 森林冰火人 - 服务器
echo ================================================
echo     森林冰火人网络版 - 游戏服务器
echo ================================================
echo.
cd /d "%~dp0Server"
dotnet run
pause

