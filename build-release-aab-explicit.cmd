@echo off
setlocal enabledelayedexpansion
echo ========================================
echo Сборка Release AAB с явными параметрами подписи
echo ========================================
echo.
echo ⚠️ ВАЖНО: Этот скрипт использует явные параметры подписи
echo    MSBuild автоматически подпишет файл при сборке
echo    НЕ запускайте sign-aab-manually.cmd после этого скрипта!
echo    Это приведет к множественным подписям!
echo.

cd /d "%~dp0"

REM Проверка наличия keystore
if not exist "Platforms\Android\yessgo-release.keystore" (
    echo [ОШИБКА] Keystore файл не найден!
    pause
    exit /b 1
)

echo Параметры подписи:
echo   Keystore: Platforms\Android\yessgo-release.keystore
echo   Alias: yessgo-key
echo   Пароль: yessgo-key
echo.

echo [1/3] Очистка проекта...
dotnet clean -f net9.0-android -c Release
if errorlevel 1 (
    echo [ОШИБКА] Не удалось очистить проект
    pause
    exit /b 1
)
echo ✓ Проект очищен
echo.

echo [2/3] Восстановление зависимостей...
dotnet restore -f net9.0-android
if errorlevel 1 (
    echo [ОШИБКА] Не удалось восстановить зависимости
    pause
    exit /b 1
)
echo ✓ Зависимости восстановлены
echo.

echo [3/3] Сборка Release AAB с явными параметрами подписи...
dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab ^
  -p:AndroidSigningKeyStore="%CD%\Platforms\Android\yessgo-release.keystore" ^
  -p:AndroidSigningStorePass="yessgo-key" ^
  -p:AndroidSigningKeyAlias="yessgo-key" ^
  -p:AndroidSigningKeyPass="yessgo-key" ^
  -v minimal

if errorlevel 1 (
    echo.
    echo [ОШИБКА] Не удалось собрать AAB файл
    pause
    exit /b 1
)

echo.
echo ========================================
echo ✓ AAB файл успешно собран и подписан!
echo ========================================
echo.

REM Поиск созданного AAB файла
for /r "bin\Release\net9.0-android" %%f in (*.aab) do (
    echo Файл: %%f
    echo.
    echo Проверка подписи файла...
    jarsigner -verify -verbose "%%f" 2>&1 | findstr /C:"Signer #" >nul
    if not errorlevel 1 (
        echo ✓ Файл подписан
        jarsigner -verify -verbose "%%f" 2>&1 | findstr /C:"Signer #" > temp_signers.txt
        for /f %%i in ('type temp_signers.txt ^| find /c /v ""') do set SIGNER_COUNT=%%i
        del temp_signers.txt 2>nul
        if "!SIGNER_COUNT!"=="1" (
            echo ✓ Файл подписан ОДИН раз (правильно)
        ) else (
            echo ⚠️ ВНИМАНИЕ: Файл подписан несколько раз! (!SIGNER_COUNT! подписей)
            echo    Используйте rebuild-clean-aab.cmd для пересборки с одной подписью
        )
    ) else (
        echo ⚠️ Файл НЕ подписан или подписан debug ключом
    )
    echo.
    echo Проверьте подпись детально:
    echo   check-signatures.cmd
    echo.
    echo ⚠️ НЕ запускайте sign-aab-manually.cmd - файл уже подписан!
    echo.
    goto :found
)

echo [ПРЕДУПРЕЖДЕНИЕ] AAB файл не найден

:found
pause

