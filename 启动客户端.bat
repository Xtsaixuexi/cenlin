@echo off
chcp 65001 >nul
title Fireboy and Watergirl - 客户端
echo ================================================
echo     Fireboy and Watergirl 网络版 - 游戏客户端
echo ================================================
echo.
cd /d "%~dp0GameClient"
dotnet run
pause
