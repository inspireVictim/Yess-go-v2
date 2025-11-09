# Исправление БД для отображения партнёров на карте

## Проблема
При попытке показать партнёров на карте возникает ошибка из-за несоответствия данных в БД.

## ✅ БЫСТРОЕ РЕШЕНИЕ

**Готовый SQL скрипт создан:** `Yess-Go-App-Backend/Yess-Money---app-master/yess-backend/fill_partner_locations.sql`

**Запустите скрипт:**
```bash
# Через Docker
docker-compose exec postgres psql -U yess_user -d yess_db -f /path/to/fill_partner_locations.sql

# Или через PgAdmin - просто скопируйте содержимое файла и выполните
```

Скрипт автоматически создаст локации для всех партнёров из БД с:
- ✅ Координатами (latitude, longitude)
- ✅ Адресами
- ✅ Телефонами
- ✅ Часами работы (working_hours в формате JSON)
- ✅ Флагом is_active = true

**Партнёры, для которых создаются локации:**
- SIERRA Coffee
- Ants
- Bublik Cafe
- Flask Coffee
- Supara
- Нават
- Faiza
- Chicken Star
- Save The Ales
- Promzona
- Teplo Bar
- Глобус
- Фрунзе
- Дордой
- Бишкек Парк
- Айчурек

---

## Что нужно проверить и исправить в БД (если скрипт не помог)

### 1. Таблица `partner_locations`

**Проверьте, есть ли записи в таблице:**
```sql
SELECT COUNT(*) FROM partner_locations;
```

**Если записей нет, создайте их для каждого партнёра:**
```sql
-- Пример: создание локации для партнёра с id=1
INSERT INTO partner_locations (
    partner_id,
    address,
    latitude,
    longitude,
    phone_number,
    working_hours,
    is_active,
    is_main_location,
    created_at,
    updated_at
) VALUES (
    1,  -- partner_id (замените на реальный ID партнёра)
    'ул. Чуй 123, Бишкек',  -- address
    42.8746,  -- latitude (широта Бишкека)
    74.5698,  -- longitude (долгота Бишкека)
    '+996312123456',  -- phone_number
    '{"mon": "09:00-22:00", "tue": "09:00-22:00", "wed": "09:00-22:00", "thu": "09:00-22:00", "fri": "09:00-22:00", "sat": "10:00-23:00", "sun": "10:00-21:00"}'::jsonb,  -- working_hours (JSON формат)
    true,  -- is_active
    true,  -- is_main_location
    NOW(),  -- created_at
    NOW()   -- updated_at
);
```

### 2. Обязательные поля для отображения на карте

**Для каждой локации партнёра должны быть заполнены:**

#### ✅ `partner_id` (обязательно)
- Должен существовать в таблице `partners`
- Не может быть NULL

#### ✅ `latitude` (обязательно для карты)
- Тип: `numeric(10, 8)`
- Формат: десятичное число (например: `42.8746`)
- **НЕ может быть NULL** - без координат маркер не отобразится на карте

#### ✅ `longitude` (обязательно для карты)
- Тип: `numeric(11, 8)`
- Формат: десятичное число (например: `74.5698`)
- **НЕ может быть NULL** - без координат маркер не отобразится на карте

#### ⚠️ `address` (рекомендуется)
- Тип: `varchar(500)`
- Может быть NULL, но лучше заполнить

#### ⚠️ `phone_number` (рекомендуется)
- Тип: `varchar(50)`
- Может быть NULL

#### ⚠️ `working_hours` (рекомендуется)
- Тип: `jsonb`
- Формат: JSON объект с днями недели
- Пример правильного формата:
```json
{
  "mon": "09:00-22:00",
  "tue": "09:00-22:00",
  "wed": "09:00-22:00",
  "thu": "09:00-22:00",
  "fri": "09:00-22:00",
  "sat": "10:00-23:00",
  "sun": "10:00-21:00"
}
```
- Может быть NULL, но лучше заполнить

#### ✅ `is_active` (обязательно)
- Тип: `boolean`
- Должно быть `true` для отображения на карте
- По умолчанию: `true`

### 3. SQL запросы для проверки и исправления

#### Проверка локаций без координат:
```sql
SELECT 
    pl.id,
    pl.partner_id,
    p.name as partner_name,
    pl.latitude,
    pl.longitude,
    pl.address
FROM partner_locations pl
JOIN partners p ON p.id = pl.partner_id
WHERE pl.latitude IS NULL 
   OR pl.longitude IS NULL
   OR pl.is_active = false;
```

#### Обновление координат для существующих локаций:
```sql
-- Пример: обновление координат для локации с id=1
UPDATE partner_locations
SET 
    latitude = 42.8746,  -- замените на реальные координаты
    longitude = 74.5698,  -- замените на реальные координаты
    updated_at = NOW()
WHERE id = 1;
```

#### Создание локаций для всех партнёров, у которых их нет:
```sql
-- Создание основной локации для партнёров без локаций
INSERT INTO partner_locations (
    partner_id,
    address,
    latitude,
    longitude,
    phone_number,
    working_hours,
    is_active,
    is_main_location,
    created_at,
    updated_at
)
SELECT 
    p.id,
    COALESCE(p.address, 'Адрес не указан'),
    COALESCE(p.latitude, 42.8746),  -- координаты Бишкека по умолчанию
    COALESCE(p.longitude, 74.5698),  -- координаты Бишкека по умолчанию
    p.phone,
    '{"mon": "09:00-22:00", "tue": "09:00-22:00", "wed": "09:00-22:00", "thu": "09:00-22:00", "fri": "09:00-22:00", "sat": "10:00-23:00", "sun": "10:00-21:00"}'::jsonb,
    true,
    true,
    NOW(),
    NOW()
FROM partners p
WHERE NOT EXISTS (
    SELECT 1 FROM partner_locations pl WHERE pl.partner_id = p.id
)
AND p.is_active = true;
```

#### Исправление формата working_hours (если он неправильный):
```sql
-- Если working_hours хранится как строка, нужно преобразовать в JSON
-- Сначала проверьте формат:
SELECT id, working_hours, pg_typeof(working_hours) FROM partner_locations LIMIT 5;

-- Если это текст, обновите:
UPDATE partner_locations
SET working_hours = '{"mon": "09:00-22:00", "tue": "09:00-22:00", "wed": "09:00-22:00", "thu": "09:00-22:00", "fri": "09:00-22:00", "sat": "10:00-23:00", "sun": "10:00-21:00"}'::jsonb
WHERE working_hours IS NULL 
   OR pg_typeof(working_hours) != 'jsonb';
```

### 4. Проверка после исправления

```sql
-- Проверка, что все активные локации имеют координаты
SELECT 
    COUNT(*) as total_locations,
    COUNT(CASE WHEN latitude IS NOT NULL AND longitude IS NOT NULL THEN 1 END) as with_coordinates,
    COUNT(CASE WHEN latitude IS NULL OR longitude IS NULL THEN 1 END) as without_coordinates
FROM partner_locations
WHERE is_active = true;
```

**Ожидаемый результат:** `without_coordinates` должно быть `0`

### 5. Координаты для Бишкека (по умолчанию)

Если у партнёра нет точных координат, используйте координаты центра Бишкека:
- **Latitude (широта):** `42.8746`
- **Longitude (долгота):** `74.5698`

### 6. Примеры координат для популярных мест в Бишкеке

- **Центр города (площадь Ала-Тоо):** `42.8746, 74.5698`
- **ТЦ Дордой Плаза:** `42.8800, 74.5800`
- **ТЦ ЦУМ:** `42.8700, 74.5700`
- **Аэропорт Манас:** `43.0611, 74.4772`

## После исправления

1. Перезапустите backend сервер
2. Проверьте API endpoint: `GET /api/v1/partner/locations`
3. Убедитесь, что возвращаются локации с заполненными `latitude` и `longitude`
4. Проверьте отображение на карте в приложении

## Важно!

- **Без координат (`latitude` и `longitude`) маркеры не будут отображаться на карте**
- **Локации с `is_active = false` не будут возвращаться API**
- **Формат `working_hours` должен быть JSON, а не строка**

