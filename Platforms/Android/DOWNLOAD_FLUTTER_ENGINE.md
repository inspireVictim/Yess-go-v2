# Инструкция по получению Flutter Engine файлов

## ⚠️ Важная информация

Flutter Engine AAR файлы **НЕ доступны для прямого скачивания** по HTTP URL. Они распространяются через:
- Maven репозитории (через Gradle/Maven)
- Сборка из исходников Flutter
- Официальные релизы Flutter

## Способы получения файлов

### Способ 1: Через Gradle (рекомендуется для MAUI)

Добавьте зависимости в `build.gradle`:

```groovy
dependencies {
    implementation 'io.flutter:flutter_embedding_release:3.16.5'
    implementation 'io.flutter:flutter_engine_release:3.16.5'
}

repositories {
    google()
    mavenCentral()
    maven {
        url 'https://storage.googleapis.com/download.flutter.io'
    }
}
```

Затем Gradle автоматически скачает файлы при сборке.

### Способ 2: Собрать из исходников Flutter

1. Установите Flutter SDK версии 3.16.5
2. Выполните:
   ```bash
   flutter build aar
   ```
3. Файлы будут в `build/host/outputs/repo/io/flutter/`

### Способ 3: Использовать Maven Download Plugin

Если у вас установлен Maven:

```bash
mvn dependency:copy -Dartifact=io.flutter:flutter_embedding_release:3.16.5:aar -DoutputDirectory=Platforms/Android/libs
mvn dependency:copy -Dartifact=io.flutter:flutter_engine_release:3.16.5:aar -DoutputDirectory=Platforms/Android/libs
```

### Способ 4: Скачать вручную из GitHub

1. Перейдите на https://github.com/flutter/flutter
2. Найдите релиз версии 3.16.5
3. Скачайте артефакты сборки (если доступны)

### Способ 5: Использовать готовые AAR из другого проекта

Если у вас есть другой Android проект с Flutter, скопируйте AAR файлы из:
- `~/.gradle/caches/modules-2/files-2.1/io.flutter/flutter_embedding_release/`
- `~/.gradle/caches/modules-2/files-2.1/io.flutter/flutter_engine_release/`

## После получения файлов

1. Поместите файлы в `Platforms/Android/libs/`:
   - `flutter_embedding_release-3.16.5.aar`
   - `flutter_engine_release-3.16.5.aar`
   - `arm64_v8a/libflutter.so` (из ZIP)
   - `armeabi_v7a/libflutter.so` (из ZIP, опционально)

2. Раскомментируйте секции в `YessGoFront.csproj`

3. Пересоберите проект

## Примечание

Если файлы недоступны, возможно:
- Версия 3.16.5 указана в другом формате (например, с commit hash)
- Нужно использовать другую версию Flutter Engine
- Обратитесь к документации Finik SDK для точной версии

