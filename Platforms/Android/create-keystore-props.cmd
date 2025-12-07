@echo off
echo ========================================
echo Создание keystore.props файла
echo ========================================
echo.

cd /d "%~dp0"

if exist "keystore.props" (
    echo [ОШИБКА] Файл keystore.props уже существует!
    echo Если вы хотите создать новый, сначала удалите существующий файл.
    pause
    exit /b 1
)

echo Создание keystore.props на основе шаблона...
copy "keystore.props.template" "keystore.props" >nul

if errorlevel 1 (
    echo [ОШИБКА] Не удалось создать keystore.props
    pause
    exit /b 1
)

echo.
echo ========================================
echo Файл keystore.props создан!
echo ========================================
echo.
echo ВАЖНО: Откройте файл Platforms\Android\keystore.props
echo и заполните реальными значениями:
echo   - KeystorePassword - пароль keystore
echo   - KeyPassword - пароль ключа (можно использовать тот же)
echo   - KeyAlias - alias ключа (обычно: yessgo-key)
echo.
echo Файл keystore.props НЕ будет закоммичен в git.
echo.
pause

