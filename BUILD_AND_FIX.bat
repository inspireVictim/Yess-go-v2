@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Полное исправление и сборка проекта
echo ========================================
echo.

cd /d "%~dp0"

echo [1/6] Полная очистка проекта...
if exist "obj" (
    echo   Удаление папки obj...
    rmdir /s /q "obj" 2>nul
)
if exist "bin" (
    echo   Удаление папки bin...
    rmdir /s /q "bin" 2>nul
)
dotnet clean -f net9.0-android >nul 2>&1
echo   ✓ Проект очищен
echo.

echo [2/6] Создание структуры папок...
if not exist "obj" mkdir "obj"
if not exist "obj\Debug" mkdir "obj\Debug"
if not exist "obj\Debug\net9.0-android" mkdir "obj\Debug\net9.0-android"
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets"
if not exist "obj\Debug\net9.0-android\android" mkdir "obj\Debug\net9.0-android\android"
if not exist "obj\Debug\net9.0-android\android\assets" mkdir "obj\Debug\net9.0-android\android\assets"
echo   ✓ Структура папок создана
echo.

echo [3/6] Восстановление зависимостей...
dotnet restore -f net9.0-android
if %ERRORLEVEL% NEQ 0 (
    echo   ✗ Ошибка при восстановлении зависимостей
    pause
    exit /b 1
)
echo   ✓ Зависимости восстановлены
echo.

echo [4/6] Повторная проверка папки assets после restore...
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets"
if not exist "obj\Debug\net9.0-android\android\assets" mkdir "obj\Debug\net9.0-android\android\assets"
echo   ✓ Папка assets готова
echo.

echo [5/6] Сборка проекта...
echo   Выполняю: dotnet build -f net9.0-android -c Debug
echo.
dotnet build -f net9.0-android -c Debug
set BUILD_RESULT=%ERRORLEVEL%

if %BUILD_RESULT% NEQ 0 (
    echo.
    echo   ✗ Ошибка при сборке (код: %BUILD_RESULT%)
    echo.
    echo [6/6] Попытка сборки с подробными логами...
    echo   Выполняю: dotnet build -f net9.0-android -c Debug -v detailed ^> build_errors.txt
    dotnet build -f net9.0-android -c Debug -v detailed > build_errors.txt 2>&1
    echo   ✓ Подробные логи сохранены в build_errors.txt
    echo.
    echo Проверьте файл build_errors.txt для деталей ошибки.
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
