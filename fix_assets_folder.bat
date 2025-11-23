@echo off
echo ========================================
echo Исправление проблемы с папкой assets
echo ========================================
echo.

cd /d "%~dp0"

echo [1/4] Создание недостающих папок...
if not exist "obj\Debug\net9.0-android" (
    echo   Создание obj\Debug\net9.0-android...
    mkdir "obj\Debug\net9.0-android"
)
if not exist "obj\Debug\net9.0-android\assets" (
    echo   Создание obj\Debug\net9.0-android\assets...
    mkdir "obj\Debug\net9.0-android\assets"
)
echo   ✓ Папки созданы
echo.

echo [2/4] Очистка проекта...
call dotnet clean
echo   ✓ Проект очищен
echo.

echo [3/4] Восстановление зависимостей...
call dotnet restore
echo   ✓ Зависимости восстановлены
echo.

echo [4/4] Готово!
echo.
echo Теперь можно пересобрать проект:
echo   dotnet build -f net9.0-android
echo.
echo Или запустить приложение:
echo   dotnet build -f net9.0-android
echo.
pause
