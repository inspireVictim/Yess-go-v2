@echo off
REM ============================================
REM Скрипт для сборки iOS Release IPA
REM для публикации в App Store
REM ============================================
REM ВАЖНО: Требуется Mac с установленным Xcode!

echo.
echo ============================================
echo Сборка iOS Release IPA
echo ============================================
echo.

REM Проверяем наличие .NET SDK
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ОШИБКА] .NET SDK не найден. Установите .NET 9 SDK.
    pause
    exit /b 1
)

echo [ПРЕДУПРЕЖДЕНИЕ] Сборка iOS требует:
echo - Mac с установленным Xcode
echo - Активную подписку Apple Developer
echo - Настроенные сертификаты и provisioning profiles
echo.
echo Для сборки на Windows используйте удаленный Mac или CI/CD.
echo.
pause

echo [1/3] Очистка предыдущих сборок...
dotnet clean -f net9.0-ios -c Release

echo.
echo [2/3] Восстановление пакетов...
dotnet restore

echo.
echo [3/3] Сборка Release IPA...
echo.
echo ВАЖНО: Убедитесь, что настроены:
echo - iOS Distribution Certificate
echo - App Store Provisioning Profile
echo - App ID в Apple Developer Portal
echo.

REM Для сборки на Mac используйте:
REM dotnet publish -f net9.0-ios -c Release /p:ArchiveOnBuild=true /p:RuntimeIdentifier=ios-arm64

echo.
echo ============================================
echo Инструкции для сборки на Mac:
echo ============================================
echo.
echo 1. Откройте проект в Visual Studio for Mac или используйте:
echo    dotnet publish -f net9.0-ios -c Release ^
echo        /p:ArchiveOnBuild=true ^
echo        /p:RuntimeIdentifier=ios-arm64
echo.
echo 2. Или используйте Xcode для архивации и экспорта IPA
echo.
echo 3. Загрузите IPA через Transporter в App Store Connect
echo.
pause

