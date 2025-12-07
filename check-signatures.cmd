@echo off
echo ========================================
echo Проверка всех подписей в AAB/APK файле
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

echo Проверка всех подписей в файле...
echo.
echo ========================================
echo Детальная информация о подписях:
echo ========================================
echo.

jarsigner -verify -verbose -certs "%AAB_FILE%" 2>&1 | findstr /C:"jar verified" /C:"Certificate" /C:"CN=" /C:"Alias name:" /C:"Signer #"

echo.
echo ========================================
echo Подсчет количества подписей:
echo ========================================
echo.

jarsigner -verify -verbose "%AAB_FILE%" 2>&1 | findstr /C:"Signer #" | find /C "Signer"

echo.
echo Если вы видите несколько "Signer #" - файл подписан несколько раз!
echo Файл должен быть подписан только ОДИН раз одним ключом.
echo.
pause

