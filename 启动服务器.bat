@echo off
chcp 65001 >nul
title Fireboy and Watergirl - 服务器
echo ================================================
echo     Fireboy and Watergirl 网络版 - 游戏服务器
echo ================================================
echo.
cd /d "%~dp0Server"
dotnet run
pause
