@echo off
chcp 65001 >nul
cls
echo ═══════════════════════════════════════════════════════════════
echo   ФИНАЛЬНОЕ ИСПРАВЛЕНИЕ - СОЗДАНИЕ ПАПКИ ASSETS
echo ═══════════════════════════════════════════════════════════════
echo.

cd /d "%~dp0"

echo [ШАГ 1/5] Удаление конфликтующих файлов настроек...
for /r %%f in (*.csproj.user) do (
    if exist "%%f" del /f /q "%%f" 2>nul
)
echo ✓ Готово
echo.

echo [ШАГ 2/5] Очистка папок сборки...
if exist "obj" rmdir /s /q "obj" 2>nul
if exist "bin" rmdir /s /q "bin" 2>nul
dotnet clean YessGoFront.csproj -f net9.0-android >nul 2>&1
echo ✓ Готово
echo.

echo [ШАГ 3/5] Восстановление зависимостей...
dotnet restore YessGoFront.csproj
if errorlevel 1 (
    echo ✗ ОШИБКА при восстановлении зависимостей
    pause
    exit /b 1
)
echo ✓ Готово
echo.

echo [ШАГ 4/5] Создание ВСЕХ необходимых папок assets...
REM Создаем структуру папок
if not exist "obj" mkdir "obj"
if not exist "obj\Debug" mkdir "obj\Debug"
if not exist "obj\Debug\net9.0-android" mkdir "obj\Debug\net9.0-android"
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets"
if not exist "obj\Debug\net9.0-android\android-arm64" mkdir "obj\Debug\net9.0-android\android-arm64"
if not exist "obj\Debug\net9.0-android\android-arm64\assets" mkdir "obj\Debug\net9.0-android\android-arm64\assets"
if not exist "obj\Debug\net9.0-android\android-x64" mkdir "obj\Debug\net9.0-android\android-x64"
if not exist "obj\Debug\net9.0-android\android-x64\assets" mkdir "obj\Debug\net9.0-android\android-x64\assets"
if not exist "obj\Debug\net9.0-android\android" mkdir "obj\Debug\net9.0-android\android"
if not exist "obj\Debug\net9.0-android\android\assets" mkdir "obj\Debug\net9.0-android\android\assets"
echo ✓ Все папки assets созданы
echo.

echo [ШАГ 5/5] Сборка проекта для Android (arm64)...
echo Это может занять несколько минут...
echo.
dotnet build YessGoFront.csproj -f net9.0-android -c Debug -r android-arm64
set BUILD_RESULT=%ERRORLEVEL%

if %BUILD_RESULT% NEQ 0 (
    echo.
    echo ⚠ Попытка сборки без указания RuntimeIdentifier...
    echo.
    REM Создаем папку assets еще раз перед сборкой
    if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets" 2>nul
    dotnet build YessGoFront.csproj -f net9.0-android -c Debug
    set BUILD_RESULT2=%ERRORLEVEL%
    
    if %BUILD_RESULT2% NEQ 0 (
        echo.
        echo ✗ ОШИБКА при сборке
        echo Подробные логи сохранены в build_log.txt
        dotnet build YessGoFront.csproj -f net9.0-android -c Debug -v detailed > build_log.txt 2>&1
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

