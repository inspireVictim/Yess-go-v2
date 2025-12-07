@echo off
setlocal enabledelayedexpansion
echo ========================================
echo Безопасная сборка Release AAB
echo ========================================
echo.
echo Этот скрипт гарантирует ОДНУ подпись файла:
echo   1. Собирает AAB БЕЗ автоматической подписи MSBuild
echo   2. Подписывает файл вручную ОДИН раз
echo.
echo Это предотвращает проблему "несколько цепочек сертификатов"
echo.

cd /d "%~dp0"

REM Проверка наличия keystore
if not exist "Platforms\Android\yessgo-release.keystore" (
    echo [ОШИБКА] Keystore файл не найден!
    echo Создайте keystore: Platforms\Android\create-keystore.cmd
    pause
    exit /b 1
)

echo [1/5] Полная очистка проекта...
rd /s /q bin 2>nul
rd /s /q obj 2>nul
dotnet clean -f net9.0-android -c Release
echo ✓ Проект полностью очищен
echo.

echo [2/5] Удаление старых AAB/APK файлов...
for /r "bin\Release\net9.0-android" %%f in (*.aab) do del "%%f" 2>nul
for /r "bin\Release\net9.0-android" %%f in (*.apk) do del "%%f" 2>nul
echo ✓ Старые файлы удалены
echo.

echo [3/5] Восстановление зависимостей...
dotnet restore -f net9.0-android
if errorlevel 1 (
    echo [ОШИБКА] Не удалось восстановить зависимости
    pause
    exit /b 1
)
echo ✓ Зависимости восстановлены
echo.

echo [4/5] Сборка AAB БЕЗ подписи...
echo ⚠️ Отключаем автоматическую подпись MSBuild
dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab ^
  -p:AndroidSigningKeyStore="" ^
  -p:AndroidSigningStorePass="" ^
  -p:AndroidSigningKeyAlias="" ^
  -p:AndroidSigningKeyPass=""
if errorlevel 1 (
    echo [ОШИБКА] Не удалось собрать AAB
    pause
    exit /b 1
)
echo ✓ AAB собран без подписи
echo.

REM Поиск созданного AAB файла
set UNSIGNED_AAB=
for /r "bin\Release\net9.0-android" %%f in (*.aab) do (
    set UNSIGNED_AAB=%%f
    goto :found_unsigned
)

echo [ОШИБКА] AAB файл не найден после сборки
pause
exit /b 1

:found_unsigned
echo Найден неподписанный AAB: !UNSIGNED_AAB!
echo.

REM Проверка, что файл действительно не подписан
jarsigner -verify -verbose "!UNSIGNED_AAB!" 2>&1 | findstr /C:"Signer #" >nul
if not errorlevel 1 (
    echo [ПРЕДУПРЕЖДЕНИЕ] Файл уже содержит подписи!
    echo Это не должно происходить. Возможно, MSBuild все еще подписывает файл.
    echo.
    jarsigner -verify -verbose "!UNSIGNED_AAB!" 2>&1 | findstr /C:"Signer #" > temp_signers.txt
    for /f %%i in ('type temp_signers.txt ^| find /c /v ""') do set SIGNER_COUNT=%%i
    del temp_signers.txt 2>nul
    echo Найдено подписей: !SIGNER_COUNT!
    echo.
    echo Продолжить подпись? Это создаст множественные подписи!
    pause
    exit /b 1
)

echo [5/5] Подпись AAB ОДИН раз...
jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 ^
  -keystore "Platforms\Android\yessgo-release.keystore" ^
  -storepass "yessgo-key" ^
  -keypass "yessgo-key" ^
  "!UNSIGNED_AAB!" "yessgo-key"

if errorlevel 1 (
    echo.
    echo [ОШИБКА] Не удалось подписать файл!
    pause
    exit /b 1
)

echo.
echo Проверка результата...
jarsigner -verify -verbose "!UNSIGNED_AAB!" 2>&1 | findstr /C:"Signer #" > temp_signers.txt
for /f %%i in ('type temp_signers.txt ^| find /c /v ""') do set SIGNER_COUNT=%%i
del temp_signers.txt 2>nul

if "!SIGNER_COUNT!"=="1" (
    echo.
    echo ========================================
    echo ✓ AAB успешно подписан ОДИН раз!
    echo ========================================
    echo.
    echo Файл: !UNSIGNED_AAB!
    echo Количество подписей: !SIGNER_COUNT! (правильно)
    echo.
) else (
    echo.
    echo [ОШИБКА] Файл подписан !SIGNER_COUNT! раз(а) вместо одного!
    echo Это не должно происходить. Проверьте файл вручную.
    echo.
)

echo Проверьте подпись детально:
echo   check-signatures.cmd
echo.
echo Теперь можно загрузить файл в Google Play Console.
echo.
pause

