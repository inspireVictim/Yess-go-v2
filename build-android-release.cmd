@echo off
REM ============================================
REM Скрипт для сборки Android Release App Bundle (.aab)
REM для публикации в Google Play
REM ============================================

echo.
echo ============================================
echo Сборка Android Release App Bundle (.aab)
echo ============================================
echo.

REM Проверяем наличие .NET SDK
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ОШИБКА] .NET SDK не найден. Установите .NET 9 SDK.
    pause
    exit /b 1
)

echo [1/3] Очистка предыдущих сборок...
dotnet clean -f net9.0-android -c Release

echo.
echo [2/3] Восстановление пакетов...
dotnet restore

echo.
echo [3/3] Сборка Release App Bundle...
echo.
echo ВАЖНО: Убедитесь, что в YessGoFront.csproj настроен keystore для подписи!
echo.

dotnet publish -f net9.0-android -c Release /p:AndroidPackageFormat=aab

if errorlevel 1 (
    echo.
    echo [ОШИБКА] Сборка не удалась!
    pause
    exit /b 1
)

echo.
echo ============================================
echo ✅ Сборка завершена успешно!
echo ============================================
echo.
echo Файл .aab находится в:
echo bin\Release\net9.0-android\publish\
echo.
echo Следующие шаги:
echo 1. Найдите файл com.yessgo.front-Signed.aab
echo 2. Загрузите его в Google Play Console
echo 3. Заполните все необходимые данные в Play Console
echo.
pause

