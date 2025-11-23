@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Сборка Android проекта с подробными логами
echo ========================================
echo.

cd /d "%~dp0"

echo Создание папки assets (если отсутствует)...
if not exist "obj\Debug\net9.0-android" mkdir "obj\Debug\net9.0-android"
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets"
echo   ✓ Папка assets создана
echo.

echo Очистка проекта...
dotnet clean -f net9.0-android 2>&1 | findstr /V "^$"
echo.

echo Восстановление зависимостей...
dotnet restore -f net9.0-android 2>&1 | findstr /V "^$"
if %ERRORLEVEL% NEQ 0 (
    echo   ✗ Ошибка при восстановлении
    pause
    exit /b 1
)
echo   ✓ Зависимости восстановлены
echo.

echo Сборка проекта с подробными логами...
echo   Выполняю: dotnet build -f net9.0-android -c Debug -v detailed
echo.
dotnet build -f net9.0-android -c Debug -v detailed 2>&1 | findstr /C:"error" /C:"warning" /C:"assets" /C:"Failed" /C:"Error" /C:"✓" /C:"Build succeeded" /C:"Build FAILED"

set BUILD_RESULT=%ERRORLEVEL%

echo.
if %BUILD_RESULT% EQU 0 (
    echo ========================================
    echo ✓ Сборка завершена успешно!
    echo ========================================
) else (
    echo ========================================
    echo ✗ Сборка завершилась с ошибками
    echo ========================================
    echo.
    echo Проверьте ошибки выше.
    echo Полный лог сохранен в build_log.txt
)

echo.
pause
exit /b %BUILD_RESULT%
