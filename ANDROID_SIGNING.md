# Настройка подписи Android приложения для публикации

## Проблема

Google Play Console требует, чтобы все APK/AAB файлы были подписаны цифровой подписью в режиме Release. Файлы, подписанные в режиме Debug, не могут быть загружены в Google Play Store.

## Решение

### Шаг 1: Создание keystore файла

Keystore файл содержит ключ для подписи приложения. Этот файл критически важен - если вы его потеряете, вы не сможете обновлять приложение в Google Play Store.

**Вариант 1: Использование скрипта (рекомендуется)**

```bash
cd Platforms\Android
create-keystore.cmd
```

Скрипт проведет вас через процесс создания keystore. Вам нужно будет ввести:
- Пароль keystore (сохраните его в безопасном месте!)
- Пароль ключа (можно использовать тот же)
- Информацию о вашей организации

**Вариант 2: Ручное создание**

```bash
keytool -genkeypair -v -storetype PKCS12 -keystore Platforms\Android\yessgo-release.keystore -alias yessgo-key -keyalg RSA -keysize 2048 -validity 10000
```

### Шаг 2: Создание файла keystore.props

**ВАЖНО:** Используйте файл `keystore.props` (MSBuild формат), а не `keystore.properties`!

Создайте файл `Platforms\Android\keystore.props` на основе шаблона:

```bash
cd Platforms\Android
create-keystore-props.cmd
```

Или скопируйте `keystore.props.template` в `keystore.props` и заполните:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <KeystoreFile>yessgo-release.keystore</KeystoreFile>
    <KeystorePassword>yessgo-key</KeystorePassword>
    <KeyAlias>yessgo-key</KeyAlias>
    <KeyPassword>yessgo-key</KeyPassword>
  </PropertyGroup>
</Project>
```

**ВАЖНО:** 
- Файл `keystore.props` уже добавлен в `.gitignore` и не будет закоммичен в git
- Никогда не публикуйте пароли в репозитории
- Сохраните пароли в менеджере паролей или зашифрованном хранилище

### Шаг 3: Сборка Release версии

**Вариант 1: Автоматическая подпись (рекомендуется)**

Используйте скрипт для сборки подписанного AAB файла:

```bash
build-release-aab.cmd
```

**Вариант 2: Ручная подпись (если автоматическая не работает)**

Если автоматическая подпись не работает, используйте ручную подпись:

1. Соберите AAB без подписи:
   ```bash
   dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab
   ```

2. Подпишите файл вручную:
   ```bash
   sign-aab-manually.cmd
   ```

**Вариант 3: Подпись через командную строку с явными параметрами**

```bash
dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab ^
  -p:AndroidSigningKeyStore="Platforms\Android\yessgo-release.keystore" ^
  -p:AndroidSigningStorePass="yessgo-key" ^
  -p:AndroidSigningKeyAlias="yessgo-key" ^
  -p:AndroidSigningKeyPass="yessgo-key"
```

Скрипт:
1. Проверит наличие keystore файла
2. Очистит проект
3. Восстановит зависимости
4. Соберет подписанный AAB файл в Release режиме

**Альтернатива: Сборка через Visual Studio**

1. Откройте проект в Visual Studio
2. Выберите конфигурацию **Release**
3. Выберите платформу **Android**
4. Соберите проект: `Build > Build Solution` (или `Ctrl+Shift+B`)
5. AAB файл будет в папке `bin\Release\net9.0-android\`

**Альтернатива: Сборка через командную строку**

```bash
dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab
```

### Шаг 4: Загрузка в Google Play Console

1. Войдите в [Google Play Console](https://play.google.com/console)
2. Выберите ваше приложение
3. Перейдите в раздел **Production** (или **Internal testing** / **Closed testing**)
4. Нажмите **Create new release**
5. Загрузите собранный AAB файл
6. Заполните информацию о релизе
7. Сохраните и опубликуйте

## Проверка подписи

Чтобы убедиться, что файл подписан правильно, используйте скрипт:

```bash
verify-signing.cmd
```

Или вручную:

```bash
jarsigner -verify -verbose -certs bin\Release\net9.0-android\<имя_файла>.aab
```

**Важно:** Проверьте вывод команды. Если вы видите:
- `CN=Android Debug` - файл подписан **debug** ключом (неправильно!)
- Ваше имя/организация - файл подписан **release** ключом (правильно!)

## Безопасность

⚠️ **КРИТИЧЕСКИ ВАЖНО:**

1. **Keystore файл** (`yessgo-release.keystore`) - храните в безопасном месте:
   - Не коммитьте в git (уже добавлено в `.gitignore`)
   - Делайте резервные копии
   - Храните в зашифрованном хранилище или менеджере паролей

2. **Пароли** - никогда не публикуйте:
   - Не коммитьте `keystore.properties` в git
   - Не отправляйте пароли по email или в мессенджерах
   - Используйте менеджер паролей (1Password, LastPass, Bitwarden и т.д.)

3. **Если потеряли keystore:**
   - Вы не сможете обновлять существующее приложение в Google Play Store
   - Придется создавать новое приложение с новым package name
   - Все пользователи должны будут переустановить приложение

## Настройка в YessGoFront.csproj

Проект уже настроен для автоматической подписи в Release режиме. Настройки находятся в `YessGoFront.csproj`:

- Чтение `keystore.properties` (если файл существует)
- Автоматическая подпись при сборке Release
- Использование keystore из `Platforms\Android\yessgo-release.keystore`

## Устранение проблем

### Ошибка: "Keystore file not found"
- Убедитесь, что файл `Platforms\Android\yessgo-release.keystore` существует
- Проверьте путь к файлу

### Ошибка: "Wrong password"
- Проверьте пароли в `keystore.properties`
- Убедитесь, что пароли совпадают с теми, что использовались при создании keystore

### Ошибка: "Alias not found"
- Проверьте, что alias в `keystore.properties` совпадает с alias, использованным при создании keystore
- По умолчанию используется `yessgo-key`

### Google Play Console: "App signed with debug certificate"

**Если автоматическая подпись не работает, попробуйте:**

1. **Проверьте, что файлы существуют:**
   ```bash
   dir Platforms\Android\yessgo-release.keystore
   dir Platforms\Android\keystore.props
   ```

2. **Используйте ручную подпись:**
   ```bash
   sign-aab-manually.cmd
   ```

3. **Используйте явные параметры в командной строке:**
   ```bash
   dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab ^
     -p:AndroidSigningKeyStore="Platforms\Android\yessgo-release.keystore" ^
     -p:AndroidSigningStorePass="yessgo-key" ^
     -p:AndroidSigningKeyAlias="yessgo-key" ^
     -p:AndroidSigningKeyPass="yessgo-key"
   ```

4. **Проверьте подпись после сборки:**
   ```bash
   verify-signing.cmd
   ```

5. **Полностью очистите проект перед сборкой:**
   ```bash
   dotnet clean -f net9.0-android -c Release
   rd /s /q bin obj
   dotnet build -f net9.0-android -c Release -p:AndroidPackageFormat=aab
   ```

6. **Проверьте, что используется правильное свойство:**
   - В `.csproj` должно быть `AndroidSigningKeyStore` (не `AndroidKeyStore`)
   - Импорт `keystore.props` должен быть ПЕРЕД PropertyGroup с настройками подписи

## Дополнительные ресурсы

- [Официальная документация Android по подписи приложений](https://developer.android.com/studio/publish/app-signing)
- [Google Play App Signing](https://support.google.com/googleplay/android-developer/answer/9842756)
- [Документация .NET MAUI по подписи Android](https://learn.microsoft.com/dotnet/maui/android/deployment/overview)

