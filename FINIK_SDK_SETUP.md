# Инструкция по настройке Finik SDK (Backend Proxy подход)

## ✅ Решение: Backend Proxy

**Finik SDK теперь работает через Backend Proxy** - все вызовы Finik SDK выполняются на backend, а мобильное приложение использует REST API.

### Почему Backend Proxy?

**Проблема:** Finik SDK 2.7.1 - это Flutter модуль, который невозможно корректно интегрировать в .NET MAUI Android проект:
- Finik SDK требует Flutter Engine, который сложно интегрировать в MAUI
- Стандартные Java bindings не работают с Flutter модулями
- Возникают конфликты классов и ошибки `ClassNotFoundException`

**Решение:** Перенести работу с Finik SDK на backend, а мобильное приложение получает результат через REST API.

### Преимущества Backend Proxy подхода:

✅ Не требует Flutter Engine в мобильном приложении  
✅ Централизованная логика платежей на backend  
✅ Безопасность (API ключи на сервере)  
✅ Проще обновления и поддержка  
✅ Кроссплатформенность (один API для Android и iOS)  
✅ Нет проблем с Java bindings и Flutter Engine  

## 🔍 Архитектура решения

```
┌─────────────────────────────────────────┐
│  MAUI C# код                            │
│  (FinikPaymentService.cs)               │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │ PaymentApiService                  │  │
│  │ - CreateFinikPaymentAsync()        │  │
│  │ - GetPaymentStatusAsync()          │  │
│  └───────────────────────────────────┘  │
└──────────────┬──────────────────────────┘
               │ HTTP REST API
               ▼
┌─────────────────────────────────────────┐
│  Backend API                             │
│  POST /api/v1/payment/finik/create     │
│  GET  /api/v1/payments/{id}/status      │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │ Finik SDK (на backend)            │  │
│  │ - FinikActivity                   │  │
│  │ - CreateItemHandlerWidget         │  │
│  │ - Flutter Engine                  │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## 📱 Настройка мобильного приложения

### 1️⃣ API Endpoints

Endpoints уже настроены в `Config/ApiEndpoints.cs`:

```csharp
public static class PaymentEndpoints
{
    public const string CreateFinikPayment = "/api/v1/payment/finik/create";
    public static string GetPaymentStatus(string paymentId) => $"/api/v1/payments/{paymentId}/status";
}
```

### 2️⃣ API Service

`IPaymentApiService` и `PaymentApiService` уже реализованы в:
- `Services/Api/IPaymentApiService.cs`
- `Services/Api/PaymentApiService.cs`

### 3️⃣ FinikPaymentService

`FinikPaymentService` для Android и iOS уже обновлены для использования `IPaymentApiService`:
- `Platforms/Android/FinikPaymentService.cs`
- `Platforms/iOS/FinikPaymentService.cs`

### 4️⃣ Dependency Injection

Сервисы уже зарегистрированы в `MauiProgram.cs`:

```csharp
// Регистрация PaymentApiService
services.AddHttpClient<IPaymentApiService, PaymentApiService>("ApiClient");

// Регистрация FinikPaymentService с зависимостью от IPaymentApiService
services.AddSingleton<IFinikPaymentService>(sp =>
{
    var paymentApiService = sp.GetRequiredService<IPaymentApiService>();
    return new Platforms.Android.FinikPaymentService(paymentApiService);
});
```

## 🖥️ Настройка Backend

### Требования для Backend

Backend должен реализовать следующие endpoints:

#### 1. POST /api/v1/payment/finik/create

**Запрос:**
```json
{
  "amount": 1000.00,
  "description": "Пополнение баланса YessGo",
  "nameEn": "Balance Replenishment",
  "requestId": "unique-request-id",
  "requiredFields": {
    "amount": "1000.00",
    "requestId": "unique-request-id"
  },
  "maxAvailableQuantity": 1
}
```

**Ответ:**
```json
{
  "paymentId": "payment-123",
  "paymentUrl": null,
  "status": "pending",
  "transactionId": null,
  "amount": 1000.00,
  "errorMessage": null
}
```

**Статусы:**
- `pending` - платеж создан, ожидает обработки
- `completed` / `succeeded` - платеж успешно выполнен
- `failed` - платеж не выполнен
- `cancelled` - платеж отменен пользователем

#### 2. GET /api/v1/payments/{paymentId}/status (опционально)

**Ответ:**
```json
{
  "paymentId": "payment-123",
  "status": "completed",
  "transactionId": "finik-transaction-456",
  "amount": 1000.00,
  "errorMessage": null
}
```

### Реализация на Backend

Backend должен:

1. **Создать endpoint** `POST /api/v1/payment/finik/create`
2. **Использовать Finik SDK** для создания платежа:
   - Инициализировать Finik SDK с API ключами
   - Создать платеж через Finik SDK
   - Сохранить информацию о платеже в БД
3. **Вернуть результат** в формате `CreateFinikPaymentResponse`
4. **Обработать webhook** от Finik:
   - Создать endpoint для приема webhook от Finik
   - **ВАЖНО:** Валидировать подпись каждого webhook запроса
   - Обновить статус платежа в БД
   - (Опционально) Уведомить мобильное приложение через WebSocket/Push
5. **Предоставить endpoint** для проверки статуса (опционально):
   - `GET /api/v1/payments/{paymentId}/status`

## 🔐 Finik Webhook (Callback)

После каждого успешного платежа Finik отправляет POST запрос на callback endpoint, указанный при настройке Finik SDK.

**Webhook endpoints на backend:**
- `POST /finik/webhook` (основной)
- `POST /api/v1/payment/finik/webhook` (альтернативный)

Используйте один из этих endpoints для приема webhook от Finik.

### Webhook Payload

Finik отправляет следующий JSON payload:

```json
{
  "id": "transaction-id-15423_CREDIT",
  "accountId": "your account id",
  "amount": 100,
  "fields": {
    "amount": "100",
    "fieldId1": "value1",
    "fieldId2": "value2"
  },
  "item": {
    "id": "generated-item-id"
  },
  "net": 100,
  "receiptNumber": "some-number",
  "requestDate": 1737369012345,
  "service": {
    "id": "averspay-items"
  },
  "status": "SUCCEEDED",
  "transactionDate": 1737369012345,
  "transactionId": "transaction-id-241234",
  "transactionType": "DEBIT",
  "data": {
    "amount": 100,
    "fieldId1": "value1"
  }
}
```

**Статусы:**
- `SUCCEEDED` - платеж успешно выполнен
- `FAILED` - платеж не выполнен

### Валидация подписи Webhook

**КРИТИЧЕСКИ ВАЖНО:** Каждый webhook запрос должен быть валидирован с использованием RSA публичного ключа Finik.

#### Публичные ключи Finik

**Production ключ:**
```
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAuF/PUmhMPPidcMxhZBPb
BSGJoSphmCI+h6ru8fG8guAlcPMVlhs+ThTjw2LHABvciwtpj51ebJ4EqhlySPyT
hqSfXI6Jp5dPGJNDguxfocohaz98wvT+WAF86DEglZ8dEsfoumojFUy5sTOBdHEu
g94B4BbrJvjmBa1YIx9Azse4HFlWhzZoYPgyQpArhokeHOHIN2QFzJqeriANO+wV
aUMta2AhRVZHbfyJ36XPhGO6A5FYQWgjzkI65cxZs5LaNFmRx6pjnhjIeVKKgF99
4OoYCzhuR9QmWkPl7tL4Kd68qa/xHLz0Psnuhm0CStWOYUu3J7ZpzRK8GoEXRcr8
tQIDAQAB
-----END PUBLIC KEY-----
```

**Beta ключ:**
```
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwlrlKz/8gLWd1ARWGA/8
o3a3Qy8G+hPifyqiPosiTY6nCHovANMIJXk6DH4qAqqZeLu8pLGxudkPbv8dSyG7
F9PZEAryMPzjoB/9P/F6g0W46K/FHDtwTM3YIVvstbEbL19m8yddv/xCT9JPPJTb
LsSTVZq5zCqvKzpupwlGS3Q3oPyLAYe+ZUn4Bx2J1WQrBu3b08fNaR3E8pAkCK27
JqFnP0eFfa817VCtyVKcFHb5ij/D0eUP519Qr/pgn+gsoG63W4pPHN/pKwQUUiAy
uLSHqL5S2yu1dffyMcMVi9E/Q2HCTcez5OvOllgOtkNYHSv9pnrMRuws3u87+hNT
ZwIDAQAB
-----END PUBLIC KEY-----
```

#### Алгоритм валидации подписи

1. **Собрать данные для валидации:**
   ```
   data = Lowercase(HTTP method) + "\n"
   data += URIAbsolutePath + "\n"
   data += (header["Host"] и headers начинающиеся с x-api-*) + "\n"
   data += queryStringParams + "\n"
   data += json(request.body) // JSON отсортированный по ключам
   ```

2. **Проверить подпись:**
   - Извлечь подпись из HTTP header `signature`
   - Использовать RSA публичный ключ для проверки подписи
   - Алгоритм: `SHA256withRSA`

#### Готовые библиотеки для валидации

- **Node.js:** `@mancho.devs/authorizer` (NPM)
- **Python:** `mancho-devs/python-authorizer` (PyPI)

#### Пример валидации (Java)

```java
public void verifyWebhook(String jsonData, String signature, String publicKeyPem) {
    try {
        Signature sig = Signature.getInstance("SHA256withRSA");
        
        // Загрузить публичный ключ
        RSAKey rsaKey = (RSAKey) JWK.parseFromPEMEncodedObjects(publicKeyPem);
        PublicKey publicKey = rsaKey.toPublicKey();
        
        sig.initVerify(publicKey);
        
        byte[] data = jsonData.getBytes(StandardCharsets.UTF_8);
        sig.update(data);
        
        if (!sig.verify(Base64.decodeBase64(signature))) {
            throw new SecurityException("Invalid webhook signature");
        }
    } catch (Exception e) {
        throw new RuntimeException("Webhook validation failed", e);
    }
}
```

### Пример реализации на Backend

```java
// Java/Spring Boot пример
@PostMapping("/api/v1/payment/finik/create")
public CreateFinikPaymentResponse createFinikPayment(@RequestBody CreateFinikPaymentRequest request) {
    // 1. Создать платеж через Finik SDK
    FinikWidget widget = FinikWidget.create(
        apiKey: FINIK_API_KEY,
        accountId: FINIK_ACCOUNT_ID,
        amount: request.getAmount(),
        callbackUrl: "https://your-backend.com/finik/webhook",
        // ... другие параметры
    );
    
    // 2. Сохранить платеж в БД
    Payment payment = paymentRepository.save(new Payment(
        amount: request.getAmount(),
        status: "pending",
        requestId: request.getRequestId()
    ));
    
    // 3. Вернуть результат
    return new CreateFinikPaymentResponse(
        paymentId: payment.getId(),
        status: "pending",
        amount: request.getAmount()
    );
}

// Обработка webhook от Finik
// Можно использовать любой из двух endpoints:
// - @PostMapping("/finik/webhook") - основной
// - @PostMapping("/api/v1/payment/finik/webhook") - альтернативный
@PostMapping("/finik/webhook")
public ResponseEntity<?> handleFinikWebhook(
    @RequestHeader("signature") String signature,
    @RequestBody FinikWebhookPayload payload) {
    
    // 1. ВАЛИДАЦИЯ ПОДПИСИ (ОБЯЗАТЕЛЬНО!)
    if (!validateWebhookSignature(payload, signature)) {
        return ResponseEntity.status(HttpStatus.FORBIDDEN).build();
    }
    
    // 2. Найти платеж по transactionId или полям из requiredFields
    Payment payment = paymentRepository.findByTransactionId(payload.getTransactionId());
    if (payment == null && payload.getFields() != null) {
        // Попробовать найти по requestId из fields
        String requestId = payload.getFields().get("requestId");
        payment = paymentRepository.findByRequestId(requestId);
    }
    
    if (payment == null) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND).build();
    }
    
    // 3. Обновить статус платежа
    if ("SUCCEEDED".equals(payload.getStatus())) {
        payment.setStatus("completed");
        payment.setTransactionId(payload.getTransactionId());
        payment.setCompletedAt(new Date(payload.getTransactionDate()));
    } else if ("FAILED".equals(payload.getStatus())) {
        payment.setStatus("failed");
    }
    
    paymentRepository.save(payment);
    
    // 4. (Опционально) Уведомить мобильное приложение
    // notificationService.notifyPaymentStatus(payment);
    
    return ResponseEntity.ok().build();
}

private boolean validateWebhookSignature(FinikWebhookPayload payload, String signature) {
    try {
        // Собрать данные для валидации согласно алгоритму Finik
        String data = buildValidationData(payload);
        
        // Использовать публичный ключ (prod или beta в зависимости от окружения)
        String publicKey = isProduction() ? FINIK_PROD_PUBLIC_KEY : FINIK_BETA_PUBLIC_KEY;
        
        // Проверить подпись
        return verifySignature(data, signature, publicKey);
    } catch (Exception e) {
        logger.error("Webhook signature validation failed", e);
        return false;
    }
}
```

## 🧪 Тестирование

### Мобильное приложение

1. Запустите приложение на Android или iOS устройстве
2. Перейдите на страницу `Acquiring.xaml`
3. Введите сумму и нажмите "Оплатить через Finik"
4. Приложение отправит запрос на backend API
5. Backend создаст платеж через Finik SDK
6. Результат вернется в мобильное приложение

### Backend

1. Убедитесь, что backend endpoint доступен: `POST /api/v1/payment/finik/create`
2. Проверьте, что Finik SDK правильно настроен на backend
3. Проверьте обработку callback от Finik
4. Убедитесь, что статусы платежей обновляются в БД

## 📝 Настройка параметров

Параметры Finik SDK настраиваются на **backend**, а не в мобильном приложении.

Мобильное приложение передает только:
- `amount` - сумма платежа
- `description` - описание
- `nameEn` - название на английском
- `requestId` - уникальный ID запроса
- `requiredFields` - дополнительные поля

API ключи и другие конфигурационные параметры Finik SDK должны быть настроены на backend.

## ❌ Устранение неполадок

### Ошибка: "Ошибка при создании платежа: Connection refused"

**Причина:** Backend endpoint недоступен или неправильно настроен.

**Решение:**
1. Проверьте, что backend запущен и доступен
2. Проверьте URL в `AppSettings.Api.BaseUrl`
3. Проверьте, что endpoint `POST /api/v1/payment/finik/create` существует

### Ошибка: "Payment not found" или "404 Not Found"

**Причина:** Endpoint не найден на backend.

**Решение:**
1. Убедитесь, что backend реализует endpoint `/api/v1/payment/finik/create`
2. Проверьте, что путь соответствует `ApiEndpoints.PaymentEndpoints.CreateFinikPayment`

### Ошибка: "Invalid request" или "400 Bad Request"

**Причина:** Формат запроса не соответствует ожидаемому на backend.

**Решение:**
1. Проверьте формат запроса в `CreateFinikPaymentRequest`
2. Убедитесь, что все обязательные поля заполнены
3. Проверьте логи backend для деталей ошибки

### Платеж создан, но статус остается "pending"

**Причина:** Backend не обрабатывает callback от Finik или не обновляет статус.

**Решение:**
1. Проверьте, что backend правильно обрабатывает callback от Finik
2. Убедитесь, что callback URL настроен в Finik SDK на backend
3. Проверьте логи backend для обработки callback

## 🔄 Миграция с прямого использования SDK

Если вы ранее использовали прямое подключение Finik SDK:

1. ✅ Удалены все зависимости от Flutter Engine из `.csproj`
2. ✅ Удален `FinikWrapper.java`
3. ✅ Удален метод `InitializeFlutterEngine()` из `MainActivity.cs`
4. ✅ Обновлены `FinikPaymentService` для использования API
5. ✅ Добавлен `IPaymentApiService` и `PaymentApiService`

**Важно:** Теперь необходимо настроить backend для работы с Finik SDK.

## 📚 Дополнительные ресурсы

- [Finik SDK Документация](https://finik.kg/docs) - для настройки SDK на backend
- [API Endpoints](./Config/ApiEndpoints.cs) - список всех API endpoints
- [PaymentApiService](./Services/Api/PaymentApiService.cs) - реализация API сервиса
