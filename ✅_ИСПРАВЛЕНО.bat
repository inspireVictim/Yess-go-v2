@echo off
chcp 65001 >nul
cls
echo ═══════════════════════════════════════════════════════════════
echo   АВТОМАТИЧЕСКАЯ ПЕРЕСБОРКА ПРОЕКТА (ИСПРАВЛЕНО!)
echo   Выполняю все необходимые действия...
echo ═══════════════════════════════════════════════════════════════
echo.

cd /d "%~dp0"

echo [ШАГ 1/6] Удаление конфликтующих файлов настроек...
for /r %%f in (*.csproj.user) do (
    if exist "%%f" (
        del /f /q "%%f" 2>nul
        echo   ✓ Удален: %%~nxf
    )
)
echo   ✓ Готово
echo.

echo [ШАГ 2/6] Очистка папок сборки...
if exist "obj" (
    rmdir /s /q "obj" 2>nul
    echo   ✓ Папка obj удалена
)
if exist "bin" (
    rmdir /s /q "bin" 2>nul
    echo   ✓ Папка bin удалена
)
dotnet clean YessGoFront.csproj -f net9.0-android >nul 2>&1
echo   ✓ Готово
echo.

echo [ШАГ 3/6] Восстановление зависимостей...
dotnet restore YessGoFront.csproj
if errorlevel 1 (
    echo.
    echo   ✗ ОШИБКА при восстановлении зависимостей
    echo.
    pause
    exit /b 1
)
echo   ✓ Готово
echo.

echo [ШАГ 4/6] Создание необходимых папок...
if not exist "obj" mkdir "obj"
if not exist "obj\Debug" mkdir "obj\Debug"
if not exist "obj\Debug\net9.0-android" mkdir "obj\Debug\net9.0-android"
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets"
if not exist "obj\Debug\net9.0-android\android" mkdir "obj\Debug\net9.0-android\android"
if not exist "obj\Debug\net9.0-android\android\assets" mkdir "obj\Debug\net9.0-android\android\assets"
echo   ✓ Папка assets создана
echo.

echo [ШАГ 5/6] Сборка проекта для Android (arm64)...
echo   Это может занять несколько минут...
echo.
dotnet build YessGoFront.csproj -f net9.0-android -c Debug -r android-arm64
set BUILD_RESULT=%ERRORLEVEL%

if %BUILD_RESULT% NEQ 0 (
    echo.
    echo   ⚠ Попытка сборки без указания RuntimeIdentifier...
    echo.
    dotnet build YessGoFront.csproj -f net9.0-android -c Debug
    set BUILD_RESULT2=%ERRORLEVEL%
    
    if %BUILD_RESULT2% NEQ 0 (
        echo.
        echo   ✗ ОШИБКА при сборке
        echo.
        echo   Сохранение подробного лога ошибок...
        dotnet build YessGoFront.csproj -f net9.0-android -c Debug -v detailed > build_log.txt 2>&1
        echo   ✓ Лог сохранен в build_log.txt
        echo.
        echo   Последние ошибки:
        type build_log.txt | findstr /C:"error" /C:"Error" /C:"ERROR" /C:"failed" /C:"RuntimeIdentifier" /C:"PlatformTarget" | more
        echo.
        pause
        exit /b %BUILD_RESULT2%
    )
    set BUILD_RESULT=%BUILD_RESULT2%
)

if %BUILD_RESULT% EQU 0 (
    echo.
    echo ═══════════════════════════════════════════════════════════════
    echo   ✓✓✓ СБОРКА ЗАВЕРШЕНА УСПЕШНО! ✓✓✓
    echo ═══════════════════════════════════════════════════════════════
    echo.
    echo Теперь вы можете:
    echo   1. Открыть YessGoFront.sln в Visual Studio
    echo   2. Выбрать net9.0-android в панели инструментов
    echo   3. Выбрать эмулятор (например, "Pixel 7 - API 35")
    echo   4. Нажать F5 для запуска
    echo.
) else (
    echo.
    echo ═══════════════════════════════════════════════════════════════
    echo   ✗ ОШИБКА ПРИ СБОРКЕ
    echo ═══════════════════════════════════════════════════════════════
    echo.
    echo Подробные логи сохранены в build_log.txt
    echo Проверьте файл build_log.txt для деталей ошибки.
    echo.
)

pause
exit /b %BUILD_RESULT%

