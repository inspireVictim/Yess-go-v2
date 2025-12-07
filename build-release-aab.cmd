@echo off
setlocal enabledelayedexpansion
echo ========================================
echo Сборка Release AAB для Google Play Store
echo ========================================
echo.

cd /d "%~dp0"

REM Проверка наличия keystore
if not exist "Platforms\Android\yessgo-release.keystore" (
    echo [ОШИБКА] Keystore файл не найден!
    echo.
    echo Создайте keystore файл:
    echo   1. Запустите: Platforms\Android\create-keystore.cmd
    echo   2. Или создайте вручную с помощью keytool
    echo.
    pause
    exit /b 1
)

REM Проверка наличия keystore.props
if not exist "Platforms\Android\keystore.props" (
    echo [ПРЕДУПРЕЖДЕНИЕ] Файл keystore.props не найден!
    echo.
    echo Создайте файл keystore.props:
    echo   1. Запустите: Platforms\Android\create-keystore-props.cmd
    echo   2. Или скопируйте keystore.props.template в keystore.props
    echo   3. Заполните пароли в keystore.props
    echo.
    echo БЕЗ keystore.props приложение будет подписано debug ключом!
    echo.
    timeout /t 5 >nul
)

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

echo [3/3] Сборка Release AAB...
echo.
echo ⚠️ ВАЖНО: MSBuild автоматически подпишет файл, если keystore.props существует
echo    НЕ запускайте sign-aab-manually.cmd после этого скрипта!
echo    Это приведет к множественным подписям!
echo.
echo Проверка настроек подписи:
if exist "Platforms\Android\keystore.props" (
    echo ✓ keystore.props найден - файл будет автоматически подписан MSBuild
) else (
    echo ✗ keystore.props НЕ найден - будет использован debug ключ!
)
if exist "Platforms\Android\yessgo-release.keystore" (
    echo ✓ yessgo-release.keystore найден
) else (
    echo ✗ yessgo-release.keystore НЕ найден - будет использован debug ключ!
)
echo.
dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab -v minimal
if errorlevel 1 (
    echo.
    echo [ОШИБКА] Не удалось собрать AAB файл
    echo.
    echo Проверьте:
    echo   - Наличие keystore файла
    echo   - Правильность паролей в keystore.properties
    echo   - Отсутствие ошибок компиляции
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo ✓ AAB файл успешно собран!
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
    echo Следующие шаги:
    echo   1. Проверьте подпись: check-signatures.cmd
    echo   2. Загрузите AAB файл в Google Play Console
    echo   3. Завершите процесс публикации
    echo.
    echo ⚠️ НЕ запускайте sign-aab-manually.cmd - файл уже подписан!
    echo.
    goto :found
)

echo [ПРЕДУПРЕЖДЕНИЕ] AAB файл не найден в ожидаемой папке
echo Проверьте папку: bin\Release\net9.0-android\

:found
pause

