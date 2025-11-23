# Инструкция по запуску из Visual Studio

## ✅ Проект настроен для запуска из Visual Studio

### Быстрый запуск:

1. **Откройте проект в Visual Studio:**
   - Откройте файл `YessGoFront.sln` или `YessGoFront.csproj`

2. **Выберите целевую платформу:**
   - В верхней панели выберите `net9.0-android`
   - Выберите эмулятор или устройство (например, "Pixel 7 - API 35")

3. **Запустите проект:**
   - Нажмите `F5` или кнопку "Start Debugging"
   - Или `Ctrl+F5` для запуска без отладки

## 🔧 Что было исправлено:

1. ✅ **MSBuild Targets** - автоматически создают папку `assets` при сборке
2. ✅ **launchSettings.json** - настроен для запуска Android приложения
3. ✅ **YessGoFront.csproj.user** - настроены параметры для Visual Studio
4. ✅ **Папка assets** - создается автоматически при каждой сборке

## ⚠️ Если проблема с assets все еще возникает:

### Вариант 1: Через Visual Studio
1. В Visual Studio: `Build` → `Clean Solution`
2. В Visual Studio: `Build` → `Rebuild Solution`
3. Запустите проект: `F5`

### Вариант 2: Через командную строку
Запустите файл `FIX_AND_BUILD.cmd` в папке проекта:
```cmd
cd "E:\YessProject — копия\YessGoFrontV2"
FIX_AND_BUILD.cmd
```

Затем откройте проект в Visual Studio и запустите.

### Вариант 3: Полная очистка
```cmd
cd "E:\YessProject — копия\YessGoFrontV2"
if exist obj rmdir /s /q obj
if exist bin rmdir /s /q bin
dotnet restore
dotnet build -f net9.0-android
```

## 📝 Примечания:

- **Папка assets** теперь создается автоматически через MSBuild Target
- Проект полностью совместим с Visual Studio
- Все настройки сохранены в `.csproj.user` файле

