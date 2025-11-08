# 🌐 Настройка сетевого подключения между фронтендом и бэкендом

## Проблема

Вы пытаетесь подключиться к бэкенду **с другого устройства в одной сети**, но авторизация/регистрация не работает.

## Причины

1. ❌ **CORS был ограничен** - исправлено, теперь разрешены все источники
2. ❌ **Фронтенд использовал неправильный IP** - исправлено, теперь имеет правильные дефолты
3. ⚠️ **Нужно указать правильный IP в коде или переменной окружения**

---

## Решение

### 1️⃣ **Узнайте IP вашего компьютера**

На Windows:
```powershell
# Найти основной IP (обычно 192.168.x.x)
ipconfig

# Или более конкретно:
(Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.PrefixOrigin -eq "Dhcp"}).IPAddress
```

Запомните IP вроде `192.168.X.X`

### 2️⃣ **Способ A: Переменная окружения (рекомендуется)**

Перед запуском приложения установите переменную окружения:

```powershell
# На Windows PowerShell
$env:API_BASE_URL = "http://192.168.X.X:8000/"

# Потом запустите приложение
.\YessGoFront.exe
```

Или для постоянной установки:
```powershell
[System.Environment]::SetEnvironmentVariable("API_BASE_URL", "http://192.168.X.X:8000/", "User")
```

### 3️⃣ **Способ B: Отредактировать в коде**

Если переменная окружения не работает, отредактируйте `ApiClient.cs`:

```csharp
// Строка 42 в ApiClient.cs
?? (isEmulator ? "http://10.0.2.2:8000/" : "http://192.168.X.X:8000/");
//                                           ↑ замените на ваш реальный IP
```

---

## Тестирование

### ✅ Проверить, что бэкенд работает:

```bash
# Из командной строки проверить здоровье бэкенда
curl http://YOUR_IP:8000/

# Должен вернуть:
# {"status":"ok","service":"yess-backend","api":"/api/v1","docs":"/docs"}
```

### ✅ Проверить CORS

```bash
# Попробуйте login запрос
curl -X POST http://YOUR_IP:8000/api/v1/auth/login \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "username=test&password=test"
```

Если видите JSON ответ (даже с ошибкой) - CORS работает! ✅

---

## Специальные случаи

### 🤖 Для Android эмулятора:

```csharp
// Автоматически использует 10.0.2.2 (специальный alias на хост)
API_BASE_URL = "http://10.0.2.2:8000/"
```

### 📱 Для реального Android телефона:

```csharp
// Используйте IP вашего компьютера
API_BASE_URL = "http://192.168.1.100:8000/"  // Примените свой IP вместо 192.168.1.100
```

### 🖥️ Для WinUI (Desktop приложение):

```csharp
// Автоматически использует localhost
API_BASE_URL = "http://localhost:8000/"
```

---

## Отладка проблем

### ❌ Ошибка: "The connection was refused" или "Timeout"

```
✅ Проверьте:
1. Docker контейнер yess-backend работает: docker ps | findstr "backend"
2. Порт 8000 открыт: netstat -ano | findstr ":8000"
3. IP адрес верный: ping 192.168.X.X
4. Попробуйте http://localhost:8000 из того же ПК
```

### ❌ Ошибка: "CORS error" или "preflight failed"

```
✅ Проверьте:
1. Backend запущен с правильным CORS конфигом
2. Переменная CORS_ORIGINS содержит "*" или ваш адрес
3. Попробуйте повторить запрос после перезапуска backend
```

### ❌ Ошибка: "Invalid Credentials" при правильном пароле

```
✅ Это означает:
1. Связь с бэкендом работает
2. Но БД возвращает ошибку аутентификации
3. Проверьте логи бэкенда:
   docker logs yess-money---app-master-backend-1 | tail -50
```

---

## Переменные окружения для разных сценариев

| Сценарий | API_BASE_URL | Где установить |
|----------|--------------|-----------------|
| Desktop (WinUI) | `http://localhost:8000/` | Автоматически |
| Android эмулятор | `http://10.0.2.2:8000/` | Автоматически |
| Android реальный телефон | `http://192.168.X.X:8000/` | Set-EnvironmentVariable или Edit ApiClient.cs |
| iOS | `http://localhost:8000/` (если на одном ПК) | Edit ApiClient.cs |

---

## Быстрая проверка всей системы

```powershell
# 1. Проверить Docker
docker ps --filter "name=yess"

# 2. Проверить, что бэкенд слушает на 8000
netstat -ano | findstr ":8000"

# 3. Проверить CORS
curl -H "Origin: http://192.168.1.100:3000" -v http://localhost:8000/api/v1/banners

# 4. Проверить вашу сеть
ipconfig | findstr "IPv4"
```

---

**✅ После этих шагов регистрация/вход должны работать!**

