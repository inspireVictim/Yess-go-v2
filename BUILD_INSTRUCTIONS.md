# Инструкция по сборке Android проекта

## Проблема с папкой assets

Если возникает ошибка: "Не удается найти указанный файл. (2): E:\YessProject — копия\YessGoFrontV2\obj\Debug\net9.0-android\assets."

## Решение

### Способ 1: Использовать скрипт сборки (рекомендуется)

Запустите файл `build_android.bat`:

```cmd
cd "E:\YessProject — копия\YessGoFrontV2"
build_android.bat
```

Скрипт автоматически:
1. Создаст недостающие папки
2. Очистит проект
3. Восстановит зависимости
4. Соберет проект

### Способ 2: Ручная сборка

```cmd
cd "E:\YessProject — копия\YessGoFrontV2"

REM Создаем папки assets
if not exist "obj\Debug\net9.0-android\assets" mkdir "obj\Debug\net9.0-android\assets"
if not exist "obj\Debug\net9.0-android\android\assets" mkdir "obj\Debug\net9.0-android\android\assets"

REM Очистка
dotnet clean -f net9.0-android

REM Восстановление
dotnet restore -f net9.0-android

REM Сборка
dotnet build -f net9.0-android -c Debug
```

### Способ 3: Полная очистка и пересборка

Если проблема не решается, выполните полную очистку:

```cmd
cd "E:\YessProject — копия\YessGoFrontV2"

REM Полная очистка
if exist "obj" rmdir /s /q "obj"
if exist "bin" rmdir /s /q "bin"

REM Восстановление и сборка
dotnet restore -f net9.0-android
dotnet build -f net9.0-android -c Debug
```

## Что было исправлено

1. ✅ Добавлен MSBuild Target `EnsureAssetsFolder` в `.csproj` - автоматически создает папку assets перед сборкой
2. ✅ Создан скрипт `build_android.bat` для автоматической сборки
3. ✅ Добавлена проверка папок assets в нескольких местах

## Примечание

Папка assets создается автоматически через MSBuild Target, который выполняется перед каждой сборкой. Если проблема повторяется, используйте полную очистку (способ 3).

