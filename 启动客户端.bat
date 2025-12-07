@echo off
chcp 65001 >nul
title 森林冰火人 - 客户端
echo ================================================
echo     森林冰火人网络版 - 游戏客户端
echo ================================================
echo.
cd /d "%~dp0GameClient"
dotnet run
pause

