@echo off

cd /d %~dp0

dotnet publish WordFinder\WordFinder.csproj -c Release -r win-x64 --sc -o publish