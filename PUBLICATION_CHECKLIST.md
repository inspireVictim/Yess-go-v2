# ✅ ЧЕК-ЛИСТ ПУБЛИКАЦИИ YESS GO FRONT

## 📋 Сводка выполненной работы

### ✅ Автоматически настроено:
- Версии приложения (Android и iOS)
- Настройки разрешений в манифестах
- Скрипты сборки (`build-android-release.cmd`, `build-ios-release.cmd`)
- `.gitignore` обновлен для защиты keystore файлов
- Описания разрешений в `Info.plist` улучшены

### ⚠️ Требует ручных действий:
- Создание Android keystore
- Настройка iOS сертификатов (на Mac)
- Подготовка материалов для магазинов
- Финальное тестирование

---

## 📦 1. Подготовка проекта (общая)

### ✅ Выполнено:
- [x] **ApplicationId / Bundle Identifier**: `com.yessgo.front`
- [x] **Версия приложения**:
  - Android: `ApplicationVersion=1`, `ApplicationDisplayVersion=1.0.0`
  - iOS: `CFBundleVersion=1`, `CFBundleShortVersionString=1.0.0`
- [x] **Название приложения**: `YESS Go Front`
- [x] **Иконки / splash screen**: Настроены в `Resources/AppIcon/` и `Resources/Splash/`
- [x] **Разрешения**: Настроены в `AndroidManifest.xml` и `Info.plist`
  - INTERNET ✅
  - CAMERA ✅
  - ACCESS_FINE_LOCATION ✅
  - ACCESS_COARSE_LOCATION ✅
- [x] **URLs**: Настроен прод-сервер `http://5.59.232.211:8000` в `Config/ApiConfiguration.cs`
- [x] **Debug-логи**: Большинство логов обернуты в `#if DEBUG`, что безопасно для релиза
- [x] **`.gitignore`**: Настроен для исключения keystore файлов

### ⚠️ Требует внимания:

#### Тестовые данные (проверить перед релизом):
- [ ] **`Data/DatabaseInitializer.cs`**: 
  - Метод `SeedTestUserAsync()` создает тестового пользователя (`+996504876087`)
  - Метод `SeedTransactionsAsync()` создает тестовые транзакции
  - **Решение**: Эти методы используются только для локальной разработки, но убедитесь, что они не вызываются в продакшене
- [ ] **`ViewModels/MainPageViewModel.cs`**:
  - Метод `LoadPartnerInfo()` содержит тестовые данные партнера (CoffeeTime)
  - **Решение**: Проверить, используется ли это в продакшене или только для разработки
- [ ] **`Services/Domain/NotificationService.cs`**:
  - Метод `CreateSampleNotificationsAsync()` создает тестовые уведомления
  - Метод `DeleteSampleNotificationsAsync()` удаляет тестовые уведомления
  - **Решение**: Убедиться, что эти методы не вызываются в продакшене

#### Дополнительные проверки:
- [ ] Проверить, что все тестовые функции удалены или отключены
- [ ] Убедиться, что нет хардкодных тестовых данных в продакшн-коде
- [ ] Проверить, что все API endpoints указывают на прод-сервер
- [ ] Убедиться, что нет ссылок на `10.0.2.2` (эмулятор Android) в продакшн-коде

---

## 🤖 2. Android (Google Play)

### 🔐 Подпись (КРИТИЧЕСКИ ВАЖНО!)

#### ⚠️ НЕ ВЫПОЛНЕНО (требуется действие):

#### Шаг 1: Создание Keystore

**Что такое Keystore?**  
Keystore - это файл, который содержит ключ для подписи Android приложения. Этот ключ **критически важен** для публикации в Google Play. Если вы потеряете keystore, вы не сможете обновлять приложение!

**⚠️ ВАЖНО:**
1. **Сохраните keystore в безопасном месте** (не в репозиторий!)
2. **Запомните пароли** - их нельзя восстановить
3. **Сделайте резервную копию** keystore файла
4. **Не делитесь** keystore с другими без необходимости

**Создание keystore:**

1. Убедитесь, что установлен Java JDK (для команды `keytool`)

2. Откройте командную строку (PowerShell или CMD) и выполните:
   ```bash
   keytool -genkeypair -v -keystore yessgo-release.keystore -alias yessgo-key -keyalg RSA -keysize 2048 -validity 10000
   ```

   **Параметры:**
   - `-keystore yessgo-release.keystore` - имя файла keystore
   - `-alias yessgo-key` - алиас ключа (можно изменить)
   - `-keyalg RSA` - алгоритм шифрования
   - `-keysize 2048` - размер ключа
   - `-validity 10000` - срок действия в днях (~27 лет)

3. Заполните данные:
   - **Пароль keystore** - придумайте надежный пароль (запомните его!)
   - **Повтор пароля keystore**
   - **Ваше имя и фамилия**
   - **Название организации** (например: "YESS")
   - **Подразделение** (опционально)
   - **Город**
   - **Регион/Область**
   - **Код страны** (например: RU, KZ)
   - **Подтверждение** (yes/no)
   - **Пароль для ключа** - обычно такой же, как пароль keystore

4. Сохраните keystore в безопасное место:
   - `C:\Android\keystores\yessgo-release.keystore`
   - Или в защищенную папку с резервным копированием

#### Шаг 2: Настройка в проекте

- [ ] **Откройте `YessGoFront.csproj`**

- [ ] **Найдите секцию Android и раскомментируйте:**
   ```xml
   <PropertyGroup Condition="$(TargetFramework.Contains('-android'))">
       <RuntimeIdentifiers>android-arm64;android-x64</RuntimeIdentifiers>
       
       <!-- 🔐 Настройки подписи для Google Play -->
       <AndroidKeyStore>true</AndroidKeyStore>
       <AndroidSigningKeyStore>C:\Android\keystores\yessgo-release.keystore</AndroidSigningKeyStore>
       <AndroidSigningStorePass>YOUR_KEYSTORE_PASSWORD</AndroidSigningStorePass>
       <AndroidSigningKeyAlias>yessgo-key</AndroidSigningKeyAlias>
       <AndroidSigningKeyPass>YOUR_KEY_PASSWORD</AndroidSigningKeyPass>
   </PropertyGroup>
   ```

- [ ] **Замените значения:**
   - `AndroidSigningKeyStore` - **полный путь** к вашему keystore файлу
   - `AndroidSigningStorePass` - **пароль keystore**
   - `AndroidSigningKeyAlias` - **алиас** (обычно `yessgo-key`)
   - `AndroidSigningKeyPass` - **пароль ключа** (обычно такой же, как StorePass)

#### ⚠️ Безопасность паролей

**НЕ храните пароли в открытом виде в репозитории!**

**Вариант 1: Переменные окружения (рекомендуется)**

Вместо паролей в `.csproj` используйте переменные окружения:

```xml
<AndroidSigningStorePass>$(ANDROID_KEYSTORE_PASSWORD)</AndroidSigningStorePass>
<AndroidSigningKeyPass>$(ANDROID_KEY_PASSWORD)</AndroidSigningKeyPass>
```

Затем установите переменные перед сборкой:
```bash
set ANDROID_KEYSTORE_PASSWORD=your_password
set ANDROID_KEY_PASSWORD=your_password
```

**Вариант 2: Секреты в CI/CD**

Если используете GitHub Actions, Azure DevOps и т.д., храните пароли в секретах.

**Вариант 3: Локальный файл (не в репозитории)**

Создайте файл `keystore.props` (добавьте в `.gitignore`):

```xml
<Project>
  <PropertyGroup>
    <AndroidSigningStorePass>your_password</AndroidSigningStorePass>
    <AndroidSigningKeyPass>your_password</AndroidSigningKeyPass>
  </PropertyGroup>
</Project>
```

И подключите в `.csproj`:
```xml
<Import Project="keystore.props" Condition="Exists('keystore.props')" />
```

#### Проверка keystore

Проверить информацию о keystore:
```bash
keytool -list -v -keystore yessgo-release.keystore
```

#### Резервное копирование

**Обязательно сделайте резервную копию:**
1. Keystore файл (`yessgo-release.keystore`)
2. Пароли (в безопасном месте, например, менеджер паролей)
3. Алиас ключа

**Рекомендуемые места хранения:**
- Зашифрованный USB-накопитель
- Облачное хранилище с шифрованием (например, 1Password, LastPass)
- Бумажная копия паролей в сейфе

#### ❓ Частые вопросы

**Q: Что делать, если я потерял keystore?**  
A: К сожалению, вы не сможете обновлять существующее приложение. Придется создать новое приложение в Play Console с новым Package Name.

**Q: Можно ли использовать один keystore для нескольких приложений?**  
A: Технически да, но не рекомендуется. Лучше создать отдельный keystore для каждого приложения.

**Q: Как часто нужно менять keystore?**  
A: Никогда, если вы не потеряли его. Keystore используется на протяжении всего жизненного цикла приложения.

**Q: Можно ли использовать debug keystore для релиза?**  
A: Нет! Debug keystore не подходит для публикации в Google Play.

### 🛠️ Сборка

#### ✅ Готово:
- [x] Скрипт сборки создан: `build-android-release.cmd`

#### 📝 Инструкция:
1. **Убедитесь, что keystore настроен** (см. выше)
2. **Запустите `build-android-release.cmd`**
3. **Или вручную:**
   ```bash
   dotnet publish -f net9.0-android -c Release /p:AndroidPackageFormat=aab
   ```
4. **Файл `.aab` будет в `bin\Release\net9.0-android\publish\`**
5. **Ищите файл**: `com.yessgo.front-Signed.aab`

### 🎨 Подготовка Play Market

#### ⚠️ Требует заполнения:
- [ ] **Play Console**: Создать аккаунт и настроить приложение
- [ ] **Иконка**: 512×512 пикселей
- [ ] **Скриншоты**: 
  - Телефоны (минимум 2)
  - Планшеты (опционально)
- [ ] **Описание**:
  - Короткое (до 80 символов)
  - Полное (до 4000 символов)
- [ ] **Категория**: Выбрать подходящую
- [ ] **Контакты**: Email, сайт
- [ ] **Политика конфиденциальности**: Ссылка на страницу
- [ ] **Content Rating**: Пройти опрос
- [ ] **Страны распространения**: Настроить

### 🚀 Публикация
- [ ] Загрузить `.aab` в Production или Closed Testing
- [ ] Отправить на проверку Google

---

## 🍏 3. iOS (App Store)

### 🖥 Требования

#### ⚠️ Проверить:
- [ ] Mac с установленным Xcode
- [ ] Активная подписка Apple Developer ($99/год)

### 🔐 Сертификаты

#### ⚠️ НЕ ВЫПОЛНЕНО (требуется действие):

#### Шаг 1: Создание сертификатов в Apple Developer Portal

1. **Войдите в [Apple Developer Portal](https://developer.apple.com/account/)**

2. **Создайте App ID** (если еще не создан):
   - Перейдите в "Certificates, Identifiers & Profiles"
   - Выберите "Identifiers" → "+"
   - Выберите "App IDs" → "Continue"
   - Выберите "App" → "Continue"
   - Введите описание и Bundle ID: `com.yessgo.front`
   - Выберите необходимые capabilities (Push Notifications, если используются)
   - Сохраните

3. **Создайте iOS Distribution Certificate**:
   - Перейдите в "Certificates" → "+"
   - Выберите "iOS App Development" или "Apple Distribution" → "Continue"
   - Следуйте инструкциям для создания CSR (Certificate Signing Request) в Keychain Access
   - Загрузите CSR и скачайте сертификат
   - Установите сертификат на Mac (двойной клик по файлу)

4. **Создайте App Store Provisioning Profile**:
   - Перейдите в "Profiles" → "+"
   - Выберите "App Store" → "Continue"
   - Выберите App ID: `com.yessgo.front` → "Continue"
   - Выберите Distribution Certificate → "Continue"
   - Введите имя профиля → "Generate"
   - Скачайте и установите профиль (двойной клик)

#### Шаг 2: Настройка в Xcode

- [ ] Откройте проект в Xcode
- [ ] Выберите проект в навигаторе
- [ ] Перейдите в "Signing & Capabilities"
- [ ] Выберите команду (Team)
- [ ] Убедитесь, что выбран правильный Provisioning Profile

### 🛠️ Сборка IPA

#### ✅ Готово:
- [x] Скрипт сборки создан: `build-ios-release.cmd`
- [x] Info.plist настроен с описаниями разрешений

#### 📝 Инструкция (на Mac):

**Вариант 1: Через Visual Studio for Mac**
1. Откройте проект в Visual Studio for Mac
2. Выберите конфигурацию "Release"
3. Выберите устройство "Any iOS Device"
4. Build → Archive for Publishing
5. Следуйте мастеру экспорта

**Вариант 2: Через командную строку**
```bash
dotnet publish -f net9.0-ios -c Release /p:ArchiveOnBuild=true /p:RuntimeIdentifier=ios-arm64
```

**Вариант 3: Через Xcode**
1. Откройте проект в Xcode
2. Product → Archive
3. После архивации: Distribute App
4. Выберите "App Store Connect"
5. Следуйте инструкциям

### 🎨 App Store Connect

#### ⚠️ Требует заполнения:
- [ ] **Создать приложение** в https://appstoreconnect.apple.com
- [ ] **Загрузить билд** через Transporter или Xcode
- [ ] **Скриншоты** (обязательно):
  - iPhone 6.7" (iPhone 14 Pro Max, 15 Pro Max) - 1290×2796
  - iPhone 6.1" (iPhone 14 Pro, 15 Pro) - 1179×2556
  - iPad (опционально)
- [ ] **Иконка**: 1024×1024 пикселей
- [ ] **Описание**: Короткое и полное
- [ ] **Ключевые слова**: До 100 символов
- [ ] **Privacy Policy**: Ссылка на страницу
- [ ] **App Privacy**: Указать типы данных
- [ ] **Возрастной рейтинг**: Настроить
- [ ] **TestFlight**: Настроить тестировщиков (опционально)

### 🚀 Публикация
- [ ] Отправить на Review в App Store Connect

---

## 🧪 4. Проверка перед релизом

### ⚠️ Обязательно проверить:
- [ ] Приложение запускается без ошибок
- [ ] Все API работают (прод-сервер `http://5.59.232.211:8000`)
- [ ] Push-уведомления протестированы (если используются)
- [ ] Deep links протестированы (если есть)
- [ ] UI проверен на разных размерах экранов
- [ ] Нет утечек данных / логов / debug-интерфейсов
- [ ] Тестовые данные удалены или отключены
- [ ] Все функции работают корректно
- [ ] Протестировано на реальных устройствах (не только эмуляторах)
- [ ] Проверена работа на разных версиях ОС (Android 21+, iOS 15.0+)

---

## 📁 5. Что подготовить заранее

### ✅ Готово:
- [x] Иконка приложения: `Resources/AppIcon/appicon.png`
- [x] Splash screen: `Resources/Splash/`

### ⚠️ Требуется подготовить:
- [ ] **Иконка 1024×1024** для App Store
- [ ] **Скриншоты для App Store**:
  - iPhone 6.7" (1290×2796)
  - iPhone 6.1" (1179×2556)
  - iPad (опционально)
- [ ] **Скриншоты для Google Play**:
  - Телефон (минимум 2)
  - Планшет (опционально)
- [ ] **Текст описаний**:
  - Короткое описание (до 80 символов)
  - Полное описание (до 4000 символов)
- [ ] **Privacy Policy**: Обычно файл на сайте или отдельная страница

---

## 📝 Дополнительные заметки

### Версии приложения
При обновлении версии измените в `YessGoFront.csproj`:
- Android: `ApplicationVersion` (номер сборки) и `ApplicationDisplayVersion` (версия для пользователя)
- iOS: `CFBundleVersion` (номер сборки) и `CFBundleShortVersionString` (версия для пользователя)

**Пример обновления до версии 1.1.0:**
```xml
<!-- Android -->
<ApplicationVersion>2</ApplicationVersion>
<ApplicationDisplayVersion>1.1.0</ApplicationDisplayVersion>

<!-- iOS -->
<CFBundleVersion>2</CFBundleVersion>
<CFBundleShortVersionString>1.1.0</CFBundleShortVersionString>
```

### Безопасность
- Keystore для Android должен храниться в безопасном месте
- Пароли keystore не должны быть в репозитории (уже добавлено в `.gitignore`)
- Используйте переменные окружения или секреты для паролей
- Не коммитьте файлы `keystore.props` или другие файлы с паролями

### API Configuration
Текущий прод-сервер: `http://5.59.232.211:8000`  
Файл конфигурации: `Config/ApiConfiguration.cs`

### Обновление приложения
После первой публикации в Google Play, **всегда используйте тот же keystore** для обновлений. Google Play не примет обновление, подписанное другим ключом.

---

## 🎯 Быстрый старт

### Android:
1. Создайте keystore (см. раздел "🔐 Подпись")
2. Настройте подпись в `YessGoFront.csproj`
3. Запустите `build-android-release.cmd`
4. Загрузите `.aab` в Play Console
5. Заполните все необходимые данные
6. Отправьте на проверку

### iOS:
1. Настройте сертификаты на Mac (см. раздел "🔐 Сертификаты")
2. Соберите IPA на Mac
3. Загрузите через Transporter в App Store Connect
4. Заполните все необходимые данные
5. Отправите на Review

---

## 📚 Полезные ссылки

- [Официальная документация Android по подписи](https://developer.android.com/studio/publish/app-signing)
- [Google Play App Signing](https://support.google.com/googleplay/android-developer/answer/9842756)
- [Apple Developer Portal](https://developer.apple.com/account/)
- [App Store Connect](https://appstoreconnect.apple.com)
- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)

---

**Последнее обновление**: 2024  
**Версия проекта**: 1.0.0  
**Статус**: Готов к публикации после настройки keystore и подготовки материалов
