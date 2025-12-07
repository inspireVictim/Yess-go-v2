@echo off
echo ========================================
echo Создание keystore для подписи приложения
echo ========================================
echo.

cd /d "%~dp0"

if exist "yessgo-release.keystore" (
    echo [ОШИБКА] Файл yessgo-release.keystore уже существует!
    echo Если вы хотите создать новый, сначала удалите существующий файл.
    pause
    exit /b 1
)

echo Создание keystore файла...
echo.
echo ВАЖНО: Запомните или сохраните в безопасном месте:
echo   - Пароль keystore
echo   - Пароль ключа (можно использовать тот же)
echo   - Alias: yessgo-key
echo.
echo Если потеряете эти данные, вы не сможете обновлять приложение в Google Play Store!
echo.
pause

keytool -genkeypair -v -storetype PKCS12 -keystore yessgo-release.keystore -alias yessgo-key -keyalg RSA -keysize 2048 -validity 10000

if errorlevel 1 (
    echo.
    echo [ОШИБКА] Не удалось создать keystore
    pause
    exit /b 1
)

echo.
echo ========================================
echo Keystore успешно создан!
echo ========================================
echo.
echo Файл: %CD%\yessgo-release.keystore
echo.
echo Следующие шаги:
echo   1. Создайте файл keystore.properties с паролями
echo   2. Обновите YessGoFront.csproj (уже сделано)
echo   3. Соберите release версию: build-release-aab.cmd
echo.
pause

