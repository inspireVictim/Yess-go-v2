# Открытие проекта YESS Go Front в Visual Studio

## ✅ Проект готов к открытию!

Все необходимые настройки выполнены:
- ✅ Исправлены конфликты слияния в .csproj файле
- ✅ Восстановлены NuGet пакеты
- ✅ .NET 9.0.306 SDK установлен
- ✅ Visual Studio 2022 Community обнаружен

---

## 🚀 Способы открытия проекта

### Способ 1: Через Windows Explorer (Самый простой)

1. Откройте папку проекта:
   ```
   E:\YessProject\YessGoFrontV2
   ```

2. Дважды кликните на файл:
   ```
   YESS Go front v 2.0.sln
   ```

3. Проект откроется в Visual Studio 2022

---

### Способ 2: Через PowerShell/командную строку

```powershell
# Открыть solution в Visual Studio
cd E:\YessProject\YessGoFrontV2
start "YESS Go front v 2.0.sln"
```

Или напрямую:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe" "E:\YessProject\YessGoFrontV2\YESS Go front v 2.0.sln"
```

---

### Способ 3: Из Visual Studio

1. Запустите Visual Studio 2022
2. Выберите **File → Open → Project/Solution**
3. Перейдите в `E:\YessProject\YessGoFrontV2`
4. Выберите файл `YESS Go front v 2.0.sln`
5. Нажмите **Open**

---

## 📦 Информация о проекте

### Технологии
- **Платформа:** .NET MAUI 9.0
- **Целевые ОС:** Android, iOS
- **Язык:** C# 12
- **UI Framework:** XAML

### Основные пакеты
- `Microsoft.Maui.Controls` 9.0.100
- `CommunityToolkit.Mvvm` 8.4.0
- `Mapsui.Maui` 4.1.9 (карты)
- `ZXing.Net.Maui.Controls` 0.6.0 (QR коды)
- `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.2
- `Microsoft.EntityFrameworkCore` 9.0.0

### Структура проекта
```
YessGoFrontV2/
├── Pages/              - Страницы приложения
├── Views/              - Дополнительные представления
├── ViewModels/         - Модели представлений (MVVM)
├── Services/           - Сервисы (API, Auth, Location)
├── Models/             - Модели данных
├── Components/         - Переиспользуемые компоненты
├── Resources/          - Ресурсы (изображения, шрифты, стили)
├── Data/               - Работа с базой данных
├── Infrastructure/     - Инфраструктурный код
└── Config/             - Конфигурация приложения
```

---

## 🔧 Настройка перед первой сборкой

### 1. Выбор платформы для запуска

После открытия проекта в Visual Studio:

1. В панели инструментов найдите выпадающий список платформ
2. Выберите целевую платформу:
   - **Android** - для Android эмулятора или устройства
   - **iOS** - для iOS симулятора или устройства (требуется Mac)

### 2. Проверка Android SDK (если запускаете на Android)

1. Откройте **Tools → Android → Android SDK Manager**
2. Убедитесь, что установлены:
   - Android SDK Platform 21 или выше
   - Android SDK Build-Tools
   - Android Emulator

### 3. Настройка подключения к бэкенду

Проект настроен на подключение к локальному бэкенду. Проверьте файл конфигурации:

**Файл:** `Config/ApiEndpoints.cs` или `Config/AppSettings.cs`

Убедитесь, что URL бэкенда указан правильно:
```csharp
BaseUrl = "http://localhost:8000/api/v1"
// Или для Android эмулятора:
BaseUrl = "http://10.0.2.2:8000/api/v1"
```

---

## 🏃‍♂️ Запуск приложения

### Для Android

1. Выберите **Android** в выпадающем списке платформ
2. Выберите эмулятор или подключенное устройство
3. Нажмите **F5** или кнопку **▶ Start**

### Для iOS (требуется Mac с Xcode)

1. Настройте Remote iOS Simulator (Mac)
2. Выберите **iOS** в выпадающем списке платформ
3. Выберите симулятор
4. Нажмите **F5** или кнопку **▶ Start**

---

## 🛠️ Полезные команды CLI

### Сборка проекта

```bash
# Сборка для Android
cd E:\YessProject\YessGoFrontV2
dotnet build -f net9.0-android

# Сборка для iOS
dotnet build -f net9.0-ios
```

### Запуск на эмуляторе

```bash
# Android
dotnet build -f net9.0-android -t:Run

# iOS (на Mac)
dotnet build -f net9.0-ios -t:Run
```

### Очистка проекта

```bash
dotnet clean
# Удалить папки bin и obj вручную, если нужно
Remove-Item -Recurse -Force bin, obj
```

---

## 📱 Подключение к бэкенду

Убедитесь, что бэкенд запущен перед тестированием приложения:

```bash
# В другом окне PowerShell
cd E:\YessProject\Yess-Go-App-Backend\Yess-Money---app-master
docker compose up -d

# Проверить, что бэкенд работает
curl http://localhost:8000/
```

Swagger UI бэкенда: http://localhost:8000/docs

---

## 🐛 Решение проблем

### Проблема: "Project not loaded" или ошибки восстановления пакетов

**Решение:**
```bash
cd E:\YessProject\YessGoFrontV2
dotnet restore "YESS Go front v 2.0.sln"
```

### Проблема: Ошибки сборки Android

**Решение:**
1. Откройте **Tools → Options → Xamarin → Android Settings**
2. Проверьте пути к Android SDK и JDK
3. Переустановите Android SDK через Android SDK Manager

### Проблема: Не видит целевую платформу

**Решение:**
1. В Visual Studio Installer проверьте, что установлена рабочая нагрузка:
   - **.NET Multi-platform App UI development**
2. Если не установлена, добавьте её через Installer

### Проблема: Ошибки NuGet

**Решение:**
```bash
# Очистить кэш NuGet
dotnet nuget locals all --clear

# Восстановить пакеты
cd E:\YessProject\YessGoFrontV2
dotnet restore "YESS Go front v 2.0.sln"
```

---

## 📚 Дополнительные ресурсы

### Документация
- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [MVVM Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Mapsui Documentation](https://mapsui.com/)

### Связанные файлы в проекте
- `BACKEND_INTEGRATION_COMPLETE.md` - Интеграция с бэкендом
- `BACKEND_INTEGRATION.md` - Детали подключения к API
- `README.md` - Общая информация о проекте

---

## ✅ Чек-лист перед началом работы

- [x] .NET 9.0 SDK установлен (версия 9.0.306)
- [x] Visual Studio 2022 установлен
- [x] Конфликты слияния исправлены
- [x] NuGet пакеты восстановлены
- [ ] Android SDK настроен (если нужен Android)
- [ ] Бэкенд запущен и доступен
- [ ] Эмулятор/устройство подключено
- [ ] Целевая платформа выбрана в Visual Studio

---

## 🚀 Готово к запуску!

Проект полностью готов к открытию и разработке в Visual Studio 2022.

**Следующий шаг:** Дважды кликните на файл `YESS Go front v 2.0.sln`

---

**Последнее обновление:** 5 ноября 2025  
**Статус:** ✅ Готов к разработке

