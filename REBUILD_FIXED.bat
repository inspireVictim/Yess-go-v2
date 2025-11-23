@echo off
setlocal enabledelayedexpansion

echo ========================================
echo ПОЛНАЯ ПЕРЕСБОРКА С ИСПРАВЛЕНИЯМИ
echo ========================================
echo.

cd /d "%~dp0"

echo [1/7] Удаление всех файлов .csproj.user...
for /r %%f in (*.csproj.user) do (
    if exist "%%f" (
        del /f /q "%%f" 2>nul
        echo   Удален: %%f
    )
)
echo   ✓ Все файлы .csproj.user удалены
echo.

echo [2/7] Полная очистка папок obj и bin...
if exist "obj" (
    rmdir /s /q "obj" 2>nul
    echo   ✓ Папка obj удалена
)
if exist "bin" (
    rmdir /s /q "bin" 2>nul
    echo   ✓ Папка bin удалена
)
echo   ✓ Очистка завершена
echo.

echo [3/7] Очистка через dotnet clean...
dotnet clean -f net9.0-android >nul 2>&1
if errorlevel 1 (
    echo   ⚠ Предупреждение: dotnet clean завершился с ошибкой (может быть нормально)
) else (
    echo   ✓ dotnet clean выполнен
)
echo.

echo [4/7] Создание структуры папок assets...
if not exist "obj" mkdir "obj"
if not exist "obj\Debug" mkdir "obj\Debug"
if not exist "obj\Debug\net9.0-android" mkdir "obj\Debug\net9.0-android"
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets"
if not exist "obj\Debug\net9.0-android\android" mkdir "obj\Debug\net9.0-android\android"
if not exist "obj\Debug\net9.0-android\android\assets" mkdir "obj\Debug\net9.0-android\android\assets"
echo   ✓ Структура папок создана
echo.

echo [5/7] Восстановление зависимостей...
dotnet restore -f net9.0-android
if errorlevel 1 (
    echo.
    echo   ✗ ОШИБКА при восстановлении зависимостей
    echo.
    pause
    exit /b 1
)
echo   ✓ Зависимости восстановлены
echo.

echo [6/7] Повторное создание папки assets после restore...
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets"
if not exist "obj\Debug\net9.0-android\android\assets" mkdir "obj\Debug\net9.0-android\android\assets"
echo   ✓ Папка assets готова
echo.

echo [7/7] Сборка проекта для Android (arm64)...
echo   Выполняю: dotnet build -f net9.0-android -c Debug -r android-arm64
echo.
dotnet build -f net9.0-android -c Debug -r android-arm64 -v minimal
set BUILD_RESULT=%ERRORLEVEL%

if %BUILD_RESULT% NEQ 0 (
    echo.
    echo   ✗ ОШИБКА при сборке (код: %BUILD_RESULT%)
    echo.
    echo Попытка сборки без указания RuntimeIdentifier...
    echo.
    dotnet build -f net9.0-android -c Debug -v minimal
    set BUILD_RESULT2=%ERRORLEVEL%
    
    if %BUILD_RESULT2% NEQ 0 (
        echo.
        echo   ✗ ОШИБКА при сборке без RuntimeIdentifier (код: %BUILD_RESULT2%)
        echo.
        echo Создание подробного лога ошибок...
        dotnet build -f net9.0-android -c Debug -v detailed > build_log.txt 2>&1
        echo.
        echo Подробные логи сохранены в build_log.txt
        echo Проверьте файл build_log.txt для деталей ошибки.
        echo.
        echo Последние строки с ошибками:
        type build_log.txt | findstr /C:"error" /C:"Error" /C:"ERROR" /C:"failed" /C:"RuntimeIdentifier" /C:"PlatformTarget" | more
        echo.
        pause
        exit /b %BUILD_RESULT2%
    ) else (
        echo.
        echo   ✓ Сборка без RuntimeIdentifier завершена успешно
    )
) else (
    echo.
    echo   ✓ Сборка с RuntimeIdentifier завершена успешно
)

echo.
echo ========================================
echo ✓ ПЕРЕСБОРКА ЗАВЕРШЕНА!
echo ========================================
echo.
echo Теперь можно запустить проект в Visual Studio:
echo   1. Откройте YessGoFront.sln в Visual Studio
echo   2. Выберите net9.0-android в панели инструментов
echo   3. Выберите эмулятор/устройство
echo   4. Нажмите F5 для запуска
echo.
pause
exit /b 0
