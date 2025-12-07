@echo off
echo ========================================
echo Полная пересборка AAB с нуля
echo ========================================
echo.
echo Этот скрипт полностью очистит проект и пересоберет AAB
echo с гарантией одной подписи
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
echo Найден неподписанный AAB: %UNSIGNED_AAB%
echo.

echo [5/5] Подпись AAB ОДИН раз...
jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 ^
  -keystore "Platforms\Android\yessgo-release.keystore" ^
  -storepass "yessgo-key" ^
  -keypass "yessgo-key" ^
  "%UNSIGNED_AAB%" "yessgo-key"

if errorlevel 1 (
    echo.
    echo [ОШИБКА] Не удалось подписать файл!
    pause
    exit /b 1
)

echo.
echo ========================================
echo ✓ AAB успешно подписан ОДИН раз!
echo ========================================
echo.
echo Файл: %UNSIGNED_AAB%
echo.
echo Проверьте подпись:
echo   check-signatures.cmd
echo.
echo Убедитесь, что видите только ОДИН "Signer #1"
echo.
pause

