# Инструкция по добавлению Flutter Engine для Finik SDK

## Проблема

Finik SDK требует Flutter Engine для работы, но Flutter Engine классы не включены в APK, что приводит к ошибке:
```
NoClassDefFoundError: io.flutter.embedding.engine.loader.FlutterLoader
```

## Решение

Необходимо добавить Flutter Engine AAR файлы версии **3.16.5** в проект вручную.

## Шаги

### 1. Скачать Flutter Engine AAR файлы (версия 3.16.5)

Finik SDK 2.7.1 использует **Flutter Engine версии 3.16.5**.

**Обязательные AAR файлы:**
1. **flutter_embedding_release-3.16.5.aar**
   - URL: https://storage.googleapis.com/download.flutter.io/io/flutter/flutter_embedding_release/3.16.5/flutter_embedding_release-3.16.5.aar
   - Содержит: `io.flutter.embedding.engine.loader.FlutterLoader`

2. **flutter_engine_release-3.16.5.aar**
   - URL: https://storage.googleapis.com/download.flutter.io/io/flutter/flutter_engine_release/3.16.5/flutter_engine_release-3.16.5.aar
   - Содержит: Flutter Engine core

**Нативные библиотеки (.so файлы):**
3. **arm64_v8a-3.16.5.zip** (для 64-битных устройств)
   - URL: https://storage.googleapis.com/download.flutter.io/io/flutter/arm64_v8a/3.16.5/arm64_v8a-3.16.5.zip
   - После распаковки: `arm64_v8a/libflutter.so`

4. **armeabi_v7a-3.16.5.zip** (для 32-битных устройств, опционально)
   - URL: https://storage.googleapis.com/download.flutter.io/io/flutter/armeabi_v7a/3.16.5/armeabi_v7a-3.16.5.zip
   - После распаковки: `armeabi_v7a/libflutter.so`

### 2. Сохранить файлы в проект

1. Убедиться, что каталог существует: `Platforms/Android/libs/`
2. Скопировать скачанные AAR файлы в этот каталог
3. Распаковать ZIP файлы с нативными библиотеками
4. Итоговая структура должна быть:
   ```
   Platforms/Android/libs/
   ├── android-sdk-2.7.1.aar (уже есть)
   ├── flutter_embedding_release-3.16.5.aar (добавить)
   ├── flutter_engine_release-3.16.5.aar (добавить)
   ├── arm64_v8a/
   │   └── libflutter.so (из распакованного ZIP)
   └── armeabi_v7a/
       └── libflutter.so (из распакованного ZIP, опционально)
   ```

### 3. Добавить файлы в .csproj

Открыть `YessGoFront.csproj` и раскомментировать секции Flutter Engine:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net9.0-android'">
    <!-- Finik SDK -->
    <AndroidLibrary Include="Platforms/Android/libs/android-sdk-2.7.1.aar">
        <Bind>false</Bind>
    </AndroidLibrary>
    
    <!-- Flutter Engine AAR файлы - раскомментировать после добавления -->
    <AndroidLibrary Include="Platforms/Android/libs/flutter_embedding_release-3.16.5.aar">
        <Bind>false</Bind>
    </AndroidLibrary>
    
    <AndroidLibrary Include="Platforms/Android/libs/flutter_engine_release-3.16.5.aar">
        <Bind>false</Bind>
    </AndroidLibrary>
    
    <!-- Flutter Engine нативные библиотеки - раскомментировать после распаковки -->
    <AndroidNativeLibrary Include="Platforms/Android/libs/arm64_v8a/libflutter.so" />
    <AndroidNativeLibrary Include="Platforms/Android/libs/armeabi_v7a/libflutter.so" />
</ItemGroup>
```

**Важно:** Использовать `AndroidLibrary` для AAR файлов и `AndroidNativeLibrary` для .so файлов.

### 5. Пересобрать проект

После добавления AAR файлов:
1. Очистить проект: `dotnet clean`
2. Пересобрать: `dotnet build`
3. Проверить, что Flutter Engine классы включены в APK

### 6. Проверить работу

После сборки приложение должно:
1. Успешно инициализировать Flutter Engine (см. логи)
2. Запускать Finik SDK без ошибок `NoClassDefFoundError`

## Альтернативное решение

Если добавление Flutter Engine AAR файлов не решает проблему или вызывает конфликты:

1. **Создать отдельный нативный Android модуль** с Flutter
2. Этот модуль будет содержать Flutter Engine и Finik SDK
3. Экспортировать простой API из модуля
4. Подключить модуль как AAR к MAUI проекту

Это более сложный, но более надежный подход для долгосрочного решения.

## Проверка

После добавления Flutter Engine, в логах должно появиться:
```
[MainActivity] ✅ Flutter Engine initialized successfully
```

Если все еще видите ошибки:
```
[MainActivity] ❌ Flutter Engine NoClassDefFoundError
```

Это означает, что:
- AAR файлы не добавлены в проект
- Неправильная версия Flutter Engine
- AAR файлы не включены в сборку

## Дополнительная информация

- Flutter Engine репозиторий: https://storage.googleapis.com/download.flutter.io
- Finik SDK документация: обратитесь к поставщику SDK
- MAUI Android зависимости: https://learn.microsoft.com/en-us/dotnet/maui/android/deployment

