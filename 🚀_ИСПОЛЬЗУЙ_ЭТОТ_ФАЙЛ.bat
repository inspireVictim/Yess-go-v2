@echo off
REM ========================================
REM АВТОМАТИЧЕСКАЯ ПЕРЕСБОРКА (ИСПРАВЛЕНО!)
REM Просто запустите этот файл двойным кликом!
REM ========================================

cd /d "%~dp0"

echo.
echo ╔══════════════════════════════════════════════════════════════╗
echo ║  АВТОМАТИЧЕСКАЯ ПЕРЕСБОРКА ПРОЕКТА (ИСПРАВЛЕНО!)            ║
echo ║  Выполняю все необходимые действия...                       ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.

REM ШАГ 1: Удаление конфликтующих файлов
echo [1/6] Удаление конфликтующих файлов настроек...
for /r %%f in (*.csproj.user) do (
    if exist "%%f" del /f /q "%%f" 2>nul
)
echo ✓ Готово
echo.

REM ШАГ 2: Очистка
echo [2/6] Очистка папок сборки...
if exist "obj" rmdir /s /q "obj" 2>nul
if exist "bin" rmdir /s /q "bin" 2>nul
dotnet clean YessGoFront.csproj -f net9.0-android >nul 2>&1
echo ✓ Готово
echo.

REM ШАГ 3: Restore
echo [3/6] Восстановление зависимостей...
dotnet restore YessGoFront.csproj
if errorlevel 1 (
    echo ✗ ОШИБКА при восстановлении зависимостей
    pause
    exit /b 1
)
echo ✓ Готово
echo.

REM ШАГ 4: Создание папок
echo [4/6] Создание необходимых папок...
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets" 2>nul
if not exist "obj\Debug\net9.0-android\android\assets" mkdir "obj\Debug\net9.0-android\android\assets" 2>nul
echo ✓ Готово
echo.

REM ШАГ 5: Build
echo [5/6] Сборка проекта для Android (arm64)...
echo Это может занять несколько минут...
echo.
dotnet build YessGoFront.csproj -f net9.0-android -c Debug -r android-arm64
if errorlevel 1 (
    echo.
    echo Попытка сборки без указания RuntimeIdentifier...
    dotnet build YessGoFront.csproj -f net9.0-android -c Debug
    if errorlevel 1 (
        echo.
        echo ✗ ОШИБКА при сборке
        echo Подробные логи сохранены в build_log.txt
        dotnet build YessGoFront.csproj -f net9.0-android -c Debug -v detailed > build_log.txt 2>&1
        pause
        exit /b 1
    )
)

echo.
echo ╔══════════════════════════════════════════════════════════════╗
echo ║  ✓✓✓ СБОРКА ЗАВЕРШЕНА УСПЕШНО! ✓✓✓                          ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
echo Теперь откройте Visual Studio и запустите проект (F5)
echo.

pause

