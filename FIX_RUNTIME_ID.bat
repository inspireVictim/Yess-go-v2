@echo off
echo ========================================
echo ИСПРАВЛЕНИЕ ПРОБЛЕМЫ С RuntimeIdentifier
echo ========================================
echo.

cd /d "%~dp0"

echo Удаление всех файлов .csproj.user...
for /r %%f in (*.csproj.user) do (
    if exist "%%f" (
        del /f /q "%%f" 2>nul
        echo   Удален: %%f
    )
)

echo.
echo Удаление папок obj и bin...
if exist "obj" rmdir /s /q "obj" 2>nul
if exist "bin" rmdir /s /q "bin" 2>nul

echo.
echo ========================================
echo ✓ ИСПРАВЛЕНИЕ ЗАВЕРШЕНО
echo ========================================
echo.
echo Теперь:
echo   1. Откройте YessGoFront.sln в Visual Studio
echo   2. Visual Studio создаст новый .csproj.user автоматически
echo   3. Выберите правильный эмулятор в панели инструментов
echo   4. RuntimeIdentifier будет выбран автоматически
echo.
pause
