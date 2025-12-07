@echo off
echo ========================================
echo Проверка подписи AAB/APK файла
echo ========================================
echo.

cd /d "%~dp0"

REM Поиск AAB файла
set AAB_FILE=
for /r "bin\Release\net9.0-android" %%f in (*.aab) do (
    set AAB_FILE=%%f
    goto :found_aab
)

REM Поиск APK файла
for /r "bin\Release\net9.0-android" %%f in (*.apk) do (
    set AAB_FILE=%%f
    goto :found_aab
)

echo [ОШИБКА] AAB/APK файл не найден!
echo Сначала соберите Release версию: build-release-aab.cmd
pause
exit /b 1

:found_aab
echo Найден файл: %AAB_FILE%
echo.

REM Проверка наличия jarsigner
where jarsigner >nul 2>&1
if errorlevel 1 (
    echo [ОШИБКА] jarsigner не найден в PATH
    echo Убедитесь, что JDK установлен и добавлен в PATH
    pause
    exit /b 1
)

echo Проверка подписи файла...
echo.

jarsigner -verify -verbose -certs "%AAB_FILE%"

if errorlevel 1 (
    echo.
    echo [ОШИБКА] Файл НЕ подписан или подпись неверна!
    echo.
    echo Возможные причины:
    echo   - Файл подписан debug ключом
    echo   - Keystore не использовался при сборке
    echo   - Пароли неверны
    echo.
) else (
    echo.
    echo ========================================
    echo ✓ Файл подписан правильно!
    echo ========================================
    echo.
    echo Проверьте выше информацию о сертификате.
    echo Если видите "CN=Android Debug" - файл подписан debug ключом!
    echo Если видите ваше имя/организацию - файл подписан release ключом.
    echo.
)

pause

