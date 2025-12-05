# Инструкция по настройке Finik SDK

## ⚠️ ВАЖНО: Проблема с Finik SDK 2.7.1

**Finik SDK 2.7.1 - это Flutter модуль**, который **НЕ МОЖЕТ** быть подключен как стандартный Android AAR в .NET MAUI через автоматическую генерацию Java bindings.

### Почему возникает ошибка `BG0000: System.NullReferenceException`?

MAUI пытается создать Java bindings для AAR файла, но:
- Finik SDK содержит Flutter код, а не стандартные Android классы
- Структура AAR не поддерживает автоматическую генерацию bindings
- Генератор `JavaTypeResolutionFixups` падает при попытке обработать файл

## ✅ Решение: Использование Reflection + Runtime AAR

Мы используем **reflection подход** - SDK подключается как runtime зависимость (без генерации bindings), а вызов происходит через Java reflection API.

### Шаги настройки:

#### 1️⃣ Скачать AAR файл

**Файл:** `android-sdk-2.7.1.aar`

**URL:** https://repo1.maven.org/maven2/kg/finik/android-sdk/2.7.1/android-sdk-2.7.1.aar

**Сохранить в:** `Platforms/Android/libs/android-sdk-2.7.1.aar`

> ⚠️ **НЕ нужно** скачивать Flutter runtime AAR файлы отдельно - они будут подтянуты автоматически через Gradle во время сборки приложения.

#### 2️⃣ Настройка .csproj

В `YessGoFront.csproj` уже настроено:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net9.0-android'">
    <!-- Finik SDK AAR - подключен как runtime, но БЕЗ генерации bindings -->
    <AndroidLibrary Include="Platforms/Android/libs/android-sdk-2.7.1.aar">
        <Bind>false</Bind>
    </AndroidLibrary>
</ItemGroup>
```

**Ключевой параметр:** `<Bind>false</Bind>` - отключает генерацию Java bindings, но включает AAR в APK.

#### 3️⃣ Убедиться, что установлены NuGet пакеты

**Kotlin библиотеки:**
- `Xamarin.Kotlin.StdLib` (2.2.21)
- `Xamarin.Kotlin.StdLib.Common` (2.0.21.5)
- `Xamarin.Kotlin.StdLib.Jdk7` (2.2.21)
- `Xamarin.Kotlin.StdLib.Jdk8` (2.2.21)

**AndroidX библиотеки:**
- `Xamarin.AndroidX.Core.Core.Ktx` (1.17.0)
- `Xamarin.AndroidX.AppCompat` (1.7.1.1)
- `Xamarin.AndroidX.Activity` (1.11.0)
- `Xamarin.AndroidX.Lifecycle.Runtime` (2.9.4)
- `Xamarin.AndroidX.Lifecycle.Common` (2.9.4)
- `Xamarin.Google.Android.Material` (1.13.0)

Все эти пакеты уже должны быть в `.csproj`.

#### 4️⃣ Настройка build.gradle (для Flutter зависимостей)

Создайте или обновите файл `Platforms/Android/build.gradle`:

```gradle
allprojects {
    repositories {
        google()
        mavenCentral()
        maven {
            url "https://storage.googleapis.com/download.flutter.io"
        }
    }
}
```

Это нужно для того, чтобы Gradle мог скачать Flutter Engine зависимости во время сборки APK.

#### 5️⃣ Пересобрать проект

1. **Очистите решение** (Clean Solution)
2. **Удалите папки** `bin` и `obj`
3. **Восстановите NuGet пакеты** (Restore NuGet Packages)
4. **Пересоберите проект** (Rebuild Solution)

## 🔍 Как это работает?

### Архитектура решения:

```
┌─────────────────────────────────────────┐
│  MAUI C# код                            │
│  (FinikPaymentService.cs)               │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │ Java Reflection API               │  │
│  │ - Class.ForName()                 │  │
│  │ - GetConstructor()                │  │
│  │ - NewInstance()                   │  │
│  └───────────────────────────────────┘  │
└──────────────┬──────────────────────────┘
               │ Intent
               ▼
┌─────────────────────────────────────────┐
│  Finik SDK (Runtime в APK)              │
│  - FinikActivity                        │
│  - CreateItemHandlerWidget              │
│  - Flutter Engine                       │
└─────────────────────────────────────────┘
```

### Преимущества:

✅ Не требуется генерация Java bindings  
✅ SDK работает на устройстве через reflection  
✅ Все классы доступны во время выполнения  
✅ Поддерживается обновление SDK без перекомпиляции bindings  

### Ограничения:

⚠️ Нет IntelliSense для классов Finik SDK  
⚠️ Ошибки reflection видны только во время выполнения  
⚠️ Требуется тщательное тестирование на реальном устройстве  

## 🧪 Тестирование

После сборки проекта:

1. Запустите приложение на **реальном Android устройстве** (не эмулятор - Flutter может не работать)
2. Перейдите на страницу `Acquiring.xaml`
3. Нажмите кнопку "Оплатить через Finik"
4. Должно открыться окно Finik SDK

## 📝 Настройка параметров

Параметры SDK настраиваются в `Config/FinikConfig.cs`:

```csharp
public const string ApiKey = "YOUR_FINIK_API_KEY";
public const string AccountId = "YOUR_FINIK_ACCOUNT_ID";
public const string CallbackUrl = "https://your-backend.com/finik/callback";
```

**ВАЖНО:** Замените значения на реальные, полученные от Finik.

## ❌ Устранение неполадок

### Ошибка: "ClassNotFoundException: kg.finik.android.sdk.FinikActivity"

**Причина:** AAR файл не включен в APK.

**Решение:**
1. Убедитесь, что файл `android-sdk-2.7.1.aar` находится в `Platforms/Android/libs/`
2. Проверьте, что в `.csproj` есть `<AndroidLibrary Include="...">` (даже с `<Bind>false</Bind>`)
3. Пересоберите проект

### Ошибка: "NoSuchMethodException" при вызове конструктора

**Причина:** Сигнатура конструктора в SDK изменилась или не совпадает.

**Решение:**
1. Проверьте документацию Finik SDK на актуальную версию
2. Обновите метод `CreateFinikWidget()` в `FinikPaymentService.cs`
3. Проверьте параметры конструктора через Android Studio или декомпилятор

### Ошибка: Flutter Engine не найден

**Причина:** Flutter зависимости не загружены.

**Решение:**
1. Убедитесь, что `build.gradle` содержит Flutter Maven репозиторий
2. Очистите кеш Gradle: удалите папку `.gradle` в проекте
3. Пересоберите проект

---

## Решение для iOS

1. Откройте терминал
2. Перейдите в папку `Platforms/iOS/`
3. Выполните: `pod install`
4. Пересоберите проект

**Примечание**: Для полной интеграции iOS SDK потребуется создание Objective-C биндингов, так как SDK написан на Swift/Objective-C.
