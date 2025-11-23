@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Сборка Android проекта YessGoFront
echo ========================================
echo.

cd /d "%~dp0"

echo [1/5] Проверка структуры папок...

REM Создаем все необходимые папки
if not exist "obj" mkdir "obj"
if not exist "obj\Debug" mkdir "obj\Debug"
if not exist "obj\Debug\net9.0-android" mkdir "obj\Debug\net9.0-android"
if not exist "obj\Debug\net9.0-android\assets" (
    echo   Создание папки assets...
    mkdir "obj\Debug\net9.0-android\assets"
    echo   ✓ Папка assets создана
) else (
    echo   ✓ Папка assets уже существует
)

echo   ✓ Структура папок проверена
echo.

echo [2/5] Очистка проекта...
dotnet clean -f net9.0-android >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   ✓ Проект очищен
) else (
    echo   ⚠ Очистка завершилась с предупреждениями
)
echo.

echo [3/5] Восстановление зависимостей...
dotnet restore -f net9.0-android
if %ERRORLEVEL% NEQ 0 (
    echo   ✗ Ошибка при восстановлении зависимостей
    pause
    exit /b 1
)
echo   ✓ Зависимости восстановлены
echo.

echo [4/5] Проверка наличия папки assets после restore...
REM Создаем папку в нескольких возможных местах
if not exist "obj\Debug\net9.0-android\assets" (
    echo   ⚠ Папка assets не найдена, создаем...
    mkdir "obj\Debug\net9.0-android\assets"
)
if not exist "obj\Debug\net9.0-android\android\assets" (
    if not exist "obj\Debug\net9.0-android\android" mkdir "obj\Debug\net9.0-android\android"
    mkdir "obj\Debug\net9.0-android\android\assets"
)
echo   ✓ Папка assets готова
echo.

echo [5/5] Сборка проекта для Android...
echo   Выполняю: dotnet build -f net9.0-android -c Debug
echo.
dotnet build -f net9.0-android -c Debug
set BUILD_RESULT=%ERRORLEVEL%
if %BUILD_RESULT% NEQ 0 (
    echo.
    echo   ✗ Ошибка при сборке проекта (код: %BUILD_RESULT%)
    echo.
    echo Попробуйте:
    echo   1. Проверить наличие всех зависимостей
    echo   2. Выполнить полную очистку:
    echo      dotnet clean -f net9.0-android
    echo      if exist obj rmdir /s /q obj
    echo      if exist bin rmdir /s /q bin
    echo   3. Пересобрать с подробными логами:
    echo      dotnet build -f net9.0-android -c Debug -v detailed
    echo.
    pause
    exit /b %BUILD_RESULT%
)

echo.
echo ========================================
echo ✓ Сборка завершена успешно!
echo ========================================
echo.
pause
