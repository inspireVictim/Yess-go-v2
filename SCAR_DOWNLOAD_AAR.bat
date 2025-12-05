@echo off
REM Скрипт для автоматического скачивания AAR файлов для Finik SDK
REM Запустите этот файл из корня проекта

echo ========================================
echo Скачивание AAR файлов для Finik SDK
echo ========================================
echo.

set LIBS_DIR=Platforms\Android\libs

REM Создаем папку libs, если её нет
if not exist "%LIBS_DIR%" (
    mkdir "%LIBS_DIR%"
    echo Создана папка: %LIBS_DIR%
)

echo Скачиваю Flutter Engine Runtime AAR файлы...
echo.

REM Flutter Embedding Release
echo [1/3] Скачиваю flutter_embedding_release-1.0.0.aar...
curl -L -o "%LIBS_DIR%\flutter_embedding_release-1.0.0.aar" "https://storage.googleapis.com/download.flutter.io/io/flutter/flutter_embedding_release/1.0.0/flutter_embedding_release-1.0.0.aar"
if %ERRORLEVEL% EQU 0 (
    echo ✓ flutter_embedding_release скачан успешно
) else (
    echo ✗ Ошибка при скачивании flutter_embedding_release
)

REM Flutter Embedding Debug
echo [2/3] Скачиваю flutter_embedding_debug-1.0.0.aar...
curl -L -o "%LIBS_DIR%\flutter_embedding_debug-1.0.0.aar" "https://storage.googleapis.com/download.flutter.io/io/flutter/flutter_embedding_debug/1.0.0/flutter_embedding_debug-1.0.0.aar"
if %ERRORLEVEL% EQU 0 (
    echo ✓ flutter_embedding_debug скачан успешно
) else (
    echo ✗ Ошибка при скачивании flutter_embedding_debug
)

REM Flutter Embedding Profile
echo [3/3] Скачиваю flutter_embedding_profile-1.0.0.aar...
curl -L -o "%LIBS_DIR%\flutter_embedding_profile-1.0.0.aar" "https://storage.googleapis.com/download.flutter.io/io/flutter/flutter_embedding_profile/1.0.0/flutter_embedding_profile-1.0.0.aar"
if %ERRORLEVEL% EQU 0 (
    echo ✓ flutter_embedding_profile скачан успешно
) else (
    echo ✗ Ошибка при скачивании flutter_embedding_profile
)

echo.
echo ========================================
echo Проверка скачанных файлов:
echo ========================================

if exist "%LIBS_DIR%\android-sdk-2.7.1.aar" (
    echo ✓ android-sdk-2.7.1.aar - есть
) else (
    echo ✗ android-sdk-2.7.1.aar - ОТСУТСТВУЕТ (скачайте вручную)
)

if exist "%LIBS_DIR%\flutter_embedding_release-1.0.0.aar" (
    echo ✓ flutter_embedding_release-1.0.0.aar - есть
) else (
    echo ✗ flutter_embedding_release-1.0.0.aar - ОТСУТСТВУЕТ
)

if exist "%LIBS_DIR%\flutter_embedding_debug-1.0.0.aar" (
    echo ✓ flutter_embedding_debug-1.0.0.aar - есть
) else (
    echo ✗ flutter_embedding_debug-1.0.0.aar - ОТСУТСТВУЕТ
)

if exist "%LIBS_DIR%\flutter_embedding_profile-1.0.0.aar" (
    echo ✓ flutter_embedding_profile-1.0.0.aar - есть
) else (
    echo ✗ flutter_embedding_profile-1.0.0.aar - ОТСУТСТВУЕТ
)

echo.
echo ========================================
echo Готово! Проверьте файлы выше.
echo Если все файлы на месте - пересоберите проект.
echo ========================================
pause

