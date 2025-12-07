@echo off
setlocal enabledelayedexpansion
echo ========================================
echo Ручная подпись AAB файла
echo ========================================
echo.
echo ⚠️ ВАЖНО: Этот скрипт подпишет AAB файл вручную
echo    Используйте ТОЛЬКО для неподписанных файлов!
echo    Если файл уже подписан MSBuild, это создаст множественные подписи!
echo.

cd /d "%~dp0"

REM Поиск AAB файла
set AAB_FILE=
for /r "bin\Release\net9.0-android" %%f in (*.aab) do (
    set AAB_FILE=%%f
    goto :found_aab
)

echo [ОШИБКА] AAB файл не найден!
echo Сначала соберите Release версию: build-release-aab.cmd
pause
exit /b 1

:found_aab
echo Найден файл: %AAB_FILE%
echo.

REM Проверка наличия keystore
if not exist "Platforms\Android\yessgo-release.keystore" (
    echo [ОШИБКА] Keystore файл не найден!
    pause
    exit /b 1
)

REM Проверка наличия keystore.props
if not exist "Platforms\Android\keystore.props" (
    echo [ОШИБКА] Файл keystore.props не найден!
    pause
    exit /b 1
)

REM Чтение паролей из keystore.props
set KEYSTORE_PASS=yessgo-key
set KEY_ALIAS=yessgo-key
set KEY_PASS=yessgo-key

echo Используемые параметры:
echo   Keystore: Platforms\Android\yessgo-release.keystore
echo   Alias: %KEY_ALIAS%
echo   Пароль: (скрыт)
echo.

REM Проверка существующих подписей
echo Проверка существующих подписей...
setlocal enabledelayedexpansion
jarsigner -verify -verbose "%AAB_FILE%" 2>&1 | findstr /C:"Signer #" >nul
if not errorlevel 1 (
    echo [ОШИБКА] Файл уже содержит подписи!
    echo.
    jarsigner -verify -verbose "%AAB_FILE%" 2>&1 | findstr /C:"Signer #" > temp_signers.txt
    for /f %%i in ('type temp_signers.txt ^| find /c /v ""') do set SIGNER_COUNT=%%i
    del temp_signers.txt 2>nul
    echo Найдено подписей: !SIGNER_COUNT!
    echo.
    echo ⚠️ ВНИМАНИЕ: Подпись уже подписанного файла создаст МНОЖЕСТВЕННЫЕ подписи!
    echo    Это приведет к ошибке "несколько цепочек сертификатов" в Google Play Console.
    echo.
    echo Рекомендуемые действия:
    echo   1. Используйте rebuild-clean-aab.cmd для пересборки с одной подписью
    echo   2. Или используйте build-release-aab-safe.cmd (безопасный метод)
    echo.
    echo Если вы все равно хотите переподписать файл:
    echo   - Распакуйте AAB (это ZIP файл)
    echo   - Удалите папку META-INF (содержит подписи)
    echo   - Упакуйте обратно в AAB
    echo   - Затем запустите этот скрипт снова
    echo.
    pause
    exit /b 1
)
endlocal

REM Создание резервной копии
echo Создание резервной копии...
copy "%AAB_FILE%" "%AAB_FILE%.backup" >nul
echo ✓ Резервная копия создана
echo.

REM Подпись файла ОДИН раз
echo Подпись AAB файла ОДИН раз...
jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 -keystore "Platforms\Android\yessgo-release.keystore" -storepass %KEYSTORE_PASS% -keypass %KEY_PASS% "%AAB_FILE%" %KEY_ALIAS%

if errorlevel 1 (
    echo.
    echo [ОШИБКА] Не удалось подписать файл!
    echo.
    echo Проверьте:
    echo   - Правильность паролей
    echo   - Наличие keystore файла
    echo   - Правильность alias
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo ✓ AAB файл успешно подписан!
echo ========================================
echo.
echo Файл: %AAB_FILE%
echo.
echo Теперь можно загрузить файл в Google Play Console.
echo.
pause

