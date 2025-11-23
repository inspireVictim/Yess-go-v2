# Решение проблемы с папкой assets

## Проблема
Ошибка: "Не удается найти указанный файл. (2): E:\YessProject — копия\YessGoFrontV2\obj\Debug\net9.0-android\assets."

## Решение

### Вариант 1: Использовать скрипт (рекомендуется)

1. Запустите файл `fix_assets_folder.bat` в этой папке
2. Скрипт автоматически:
   - Создаст недостающую папку assets
   - Очистит проект
   - Восстановит зависимости

### Вариант 2: Вручную через командную строку

```cmd
cd "E:\YessProject — копия\YessGoFrontV2"

REM Создаем папку assets
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets"

REM Очищаем проект
dotnet clean

REM Восстанавливаем зависимости
dotnet restore

REM Пересобираем проект
dotnet build -f net9.0-android
```

### Вариант 3: Полная очистка и пересборка

```cmd
cd "E:\YessProject — копия\YessGoFrontV2"

REM Полная очистка
dotnet clean
if exist "obj" rmdir /s /q "obj"
if exist "bin" rmdir /s /q "bin"

REM Восстановление и сборка
dotnet restore
dotnet build -f net9.0-android
```

## Что было исправлено

1. ✅ Добавлена настройка `MauiAsset` для `Resources\Raw\**` в `.csproj` файле
2. ✅ Создан скрипт `fix_assets_folder.bat` для автоматического исправления
3. ✅ Папка assets теперь будет создаваться автоматически при сборке

## Примечание

Папка `obj\Debug\net9.0-android\assets` создается автоматически при сборке MAUI проекта. Если она отсутствует, это обычно означает, что:
- Проект еще не был собран для Android
- Произошла ошибка при предыдущей сборке
- Папка obj была удалена вручную

После запуска скрипта `fix_assets_folder.bat` проблема должна быть решена.

