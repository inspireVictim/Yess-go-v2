# Решение проблем с подписью Android приложения

## Проблема 1: "подписано в режиме отладки"
Google Play Console показывает ошибку: "Загруженный APK-файл или набор Android App Bundle был подписан в режиме отладки"

## Проблема 2: "несколько цепочек сертификатов"
Google Play Console показывает ошибку: "У набора Android App Bundle несколько цепочек сертификатов. Выберите одну и повторите попытку."

**Это означает, что AAB файл был подписан несколько раз разными ключами или содержит несколько сертификатов.**

### Причины множественных подписей:
1. **MSBuild автоматически подписывает** файл при сборке, если в `.csproj` настроены параметры подписи
2. **Затем файл подписывается вручную** через `sign-aab-manually.cmd`, что добавляет вторую подпись
3. **Использование нескольких скриптов подписи** подряд без проверки существующих подписей

## Пошаговое решение

### Шаг 1: Проверка файлов

Убедитесь, что существуют:
- ✅ `Platforms\Android\yessgo-release.keystore` - файл keystore
- ✅ `Platforms\Android\keystore.props` - файл с паролями

Если файлов нет, создайте их:
```bash
cd Platforms\Android
create-keystore.cmd
create-keystore-props.cmd
```

### Шаг 2: Полная очистка проекта

**КРИТИЧЕСКИ ВАЖНО:** Полностью удалите папки `bin` и `obj`:

```bash
rd /s /q bin
rd /s /q obj
dotnet clean -f net9.0-android -c Release
```

### Шаг 3: Выбор метода сборки

**⚠️ КРИТИЧЕСКИ ВАЖНО:** Выберите ОДИН метод подписи и используйте только его!

#### Вариант A: Безопасная сборка (РЕКОМЕНДУЕТСЯ для предотвращения множественных подписей)

Используйте скрипт, который гарантирует одну подпись:

```bash
build-release-aab-safe.cmd
```

Этот скрипт:
- Собирает AAB БЕЗ автоматической подписи MSBuild
- Подписывает файл вручную ОДИН раз
- Проверяет количество подписей после подписи
- **Гарантирует одну подпись** - предотвращает проблему "несколько цепочек сертификатов"

#### Вариант B: Автоматическая подпись MSBuild

Используйте скрипт с автоматической подписью:

```bash
build-release-aab.cmd
```

или

```bash
build-release-aab-explicit.cmd
```

**⚠️ ВАЖНО:** 
- Эти скрипты автоматически подписывают файл через MSBuild
- **НЕ запускайте** `sign-aab-manually.cmd` после этих скриптов!
- Это создаст множественные подписи и приведет к ошибке

#### Вариант C: Ручная подпись (если автоматическая не работает)

1. Соберите AAB БЕЗ подписи:
   ```bash
   rebuild-clean-aab.cmd
   ```
   Этот скрипт собирает AAB без подписи и затем подписывает его один раз.

2. Или вручную:
   ```bash
   dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab -p:AndroidSigningKeyStore="" -p:AndroidSigningStorePass="" -p:AndroidSigningKeyAlias="" -p:AndroidSigningKeyPass=""
   sign-aab-manually.cmd
   ```


### Шаг 4: Проверка подписи

Проверьте, что файл подписан правильно и только один раз:

```bash
check-signatures.cmd
```

**Правильный результат:**
- Должен быть только **ОДИН** "Signer #1"
- В сертификате НЕ должно быть `CN=Android Debug`
- Должно быть ваше имя/организация

**Неправильный результат:**
- Несколько "Signer #" (например, "Signer #1" и "Signer #2")
- Это означает множественные подписи - используйте `rebuild-clean-aab.cmd` для пересборки

### Шаг 5: Если все еще не работает

**Используйте команду напрямую с абсолютными путями:**

```bash
dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab ^
  -p:AndroidSigningKeyStore="C:\Users\cinem\Desktop\Yess\Yess Front\Platforms\Android\yessgo-release.keystore" ^
  -p:AndroidSigningStorePass="yessgo-key" ^
  -p:AndroidSigningKeyAlias="yessgo-key" ^
  -p:AndroidSigningKeyPass="yessgo-key"
```

**Или используйте apksigner (если доступен):**

1. Соберите AAB без подписи
2. Распакуйте AAB (это ZIP файл)
3. Подпишите содержимое с помощью `apksigner`
4. Упакуйте обратно в AAB

## Проверка настроек в Visual Studio

Если используете Visual Studio:

1. Откройте **Project Properties** (правый клик на проект → Properties)
2. Перейдите в **Android Package Signing**
3. Убедитесь, что:
   - Выбран **Release** конфигурация
   - Указан путь к `yessgo-release.keystore`
   - Указаны правильные пароли и alias

## Важные замечания

### Предотвращение множественных подписей

1. **Используйте только ОДИН метод подписи:**
   - Либо автоматическая подпись MSBuild (`build-release-aab.cmd` или `build-release-aab-explicit.cmd`)
   - Либо ручная подпись (`build-release-aab-safe.cmd` или `rebuild-clean-aab.cmd`)
   - **НЕ используйте оба метода подряд!**

2. **Проверяйте существующие подписи перед ручной подписью:**
   - Скрипт `sign-aab-manually.cmd` автоматически проверяет и предупреждает
   - Если файл уже подписан, используйте `rebuild-clean-aab.cmd` вместо ручной подписи

3. **Используйте безопасный скрипт для гарантии одной подписи:**
   ```bash
   build-release-aab-safe.cmd
   ```
   Это самый безопасный способ избежать множественных подписей.

### Общие рекомендации

1. **Всегда используйте Release конфигурацию** - не Debug!
2. **Проверяйте подпись после сборки** - используйте `check-signatures.cmd`
3. **Если видите "CN=Android Debug"** - файл все еще подписан debug ключом
4. **Если видите несколько "Signer #"** - файл подписан несколько раз, используйте `rebuild-clean-aab.cmd`
5. **Очищайте проект полностью** перед каждой сборкой Release

## Решение проблемы "несколько цепочек сертификатов"

### Быстрое решение

**Используйте безопасный скрипт сборки:**

```bash
build-release-aab-safe.cmd
```

Этот скрипт гарантирует одну подпись и предотвращает проблему множественных подписей.

### Альтернативное решение: Полная пересборка

Если у вас уже есть AAB с множественными подписями:

1. **Проверьте количество подписей:**
   ```bash
   check-signatures.cmd
   ```

2. **Полная пересборка с одной подписью:**
   ```bash
   rebuild-clean-aab.cmd
   ```

   Этот скрипт:
   - Полностью очистит проект (удалит `bin` и `obj`)
   - Удалит старые AAB/APK файлы
   - Соберет новый AAB БЕЗ подписи
   - Подпишет его ОДИН раз вашим release ключом

3. **Проверка результата:**
   ```bash
   check-signatures.cmd
   ```

   Должен быть только **ОДИН** "Signer #1" с вашим сертификатом.

### Если проблема сохраняется

**Вариант 1: Ручная пересборка**

1. Полностью очистите проект:
   ```bash
   rd /s /q bin obj
   dotnet clean -f net9.0-android -c Release
   ```

2. Соберите AAB БЕЗ подписи:
   ```bash
   dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab -p:AndroidSigningKeyStore="" -p:AndroidSigningStorePass="" -p:AndroidSigningKeyAlias="" -p:AndroidSigningKeyPass=""
   ```

3. Найдите созданный AAB в `bin\Release\net9.0-android\`

4. Подпишите его ОДИН раз:
   ```bash
   jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 -keystore "Platforms\Android\yessgo-release.keystore" -storepass "yessgo-key" -keypass "yessgo-key" "bin\Release\net9.0-android\<имя_файла>.aab" "yessgo-key"
   ```

5. Проверьте подпись:
   ```bash
   jarsigner -verify -verbose -certs "bin\Release\net9.0-android\<имя_файла>.aab"
   ```

**Вариант 2: Удаление лишних подписей (если файл уже подписан несколько раз)**

Если у вас уже есть AAB с несколькими подписями, можно попробовать удалить все подписи и подписать заново:

1. Распакуйте AAB (это ZIP файл):
   ```bash
   ren "your-app.aab" "your-app.zip"
   ```

2. Удалите папку `META-INF` (содержит подписи)

3. Упакуйте обратно:
   ```bash
   ren "your-app.zip" "your-app.aab"
   ```

4. Подпишите ОДИН раз (см. Вариант 1, шаг 4)

## Контакты для помощи

Если проблема сохраняется:
- Проверьте логи сборки на наличие ошибок подписи
- Убедитесь, что keystore файл не поврежден
- Попробуйте создать новый keystore и использовать его
- Убедитесь, что не запускаете несколько скриптов подписи подряд

