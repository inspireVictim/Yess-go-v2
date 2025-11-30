-- SQL скрипт для добавления товаров в таблицу partner_products (PostgreSQL)
-- Использование: выполните этот скрипт в вашей базе данных PostgreSQL

-- ВАЖНО: Замените значение PARTNER_ID на ID вашего партнёра
-- Чтобы узнать ID партнёра, выполните: SELECT id, name FROM partners;

-- Пример: если ваш партнёр имеет id = 1, замените все вхождения :PARTNER_ID на 1

-- ============================================
-- ВСТАВКА ТОВАРОВ
-- ============================================

-- Товар 1: Полный набор данных
INSERT INTO partner_products (
    partner_id,
    name,
    description,
    ingredients,
    image_url,
    weight,
    price,
    original_price,
    discount_percent,
    yess_coins,
    is_available,
    category,
    created_at,
    updated_at
) VALUES (
    1,  -- Замените на ваш partner_id
    'Пицца Маргарита',
    'Классическая итальянская пицца с томатами, моцареллой и базиликом. Свежие ингредиенты и традиционный рецепт.',
    'Тесто для пиццы, томатный соус, моцарелла, базилик, оливковое масло, соль',
    'https://example.com/images/pizza-margherita.jpg',
    '350 г',
    450.00,
    550.00,
    18.18,
    22.50,
    true,
    'Пицца',
    NOW(),
    NULL
);

-- Товар 2: С минимальными данными (только обязательные поля)
INSERT INTO partner_products (
    partner_id,
    name,
    price,
    is_available,
    created_at
) VALUES (
    1,  -- Замените на ваш partner_id
    'Кола 0.5л',
    120.00,
    true,
    NOW()
);

-- Товар 3: С описанием и категорией, но без скидки
INSERT INTO partner_products (
    partner_id,
    name,
    description,
    image_url,
    weight,
    price,
    yess_coins,
    is_available,
    category,
    created_at
) VALUES (
    1,  -- Замените на ваш partner_id
    'Салат Цезарь',
    'Свежий салат с курицей, пармезаном и соусом Цезарь',
    'https://example.com/images/caesar-salad.jpg',
    '250 г',
    380.00,
    19.00,
    true,
    'Салаты',
    NOW()
);

-- Товар 4: Со скидкой и всеми полями
INSERT INTO partner_products (
    partner_id,
    name,
    description,
    ingredients,
    image_url,
    weight,
    price,
    original_price,
    discount_percent,
    yess_coins,
    is_available,
    category,
    created_at,
    updated_at
) VALUES (
    1,  -- Замените на ваш partner_id
    'Бургер Классик',
    'Сочный бургер с говяжьей котлетой, овощами и специальным соусом',
    'Булочка, говяжья котлета, салат, помидор, лук, маринованные огурцы, соус',
    'https://example.com/images/classic-burger.jpg',
    '300 г',
    520.00,
    650.00,
    20.00,
    26.00,
    true,
    'Бургеры',
    NOW(),
    NULL
);

-- Товар 5: Недоступный товар (для тестирования)
INSERT INTO partner_products (
    partner_id,
    name,
    description,
    price,
    is_available,
    category,
    created_at
) VALUES (
    1,  -- Замените на ваш partner_id
    'Товар временно недоступен',
    'Этот товар временно отсутствует в наличии',
    0.00,
    false,
    'Прочее',
    NOW()
);

-- Товар 6: С большим описанием и ингредиентами
INSERT INTO partner_products (
    partner_id,
    name,
    description,
    ingredients,
    image_url,
    weight,
    price,
    original_price,
    discount_percent,
    yess_coins,
    is_available,
    category,
    created_at
) VALUES (
    1,  -- Замените на ваш partner_id
    'Паста Карбонара',
    'Традиционная итальянская паста с беконом, яйцами и пармезаном. Готовится по классическому рецепту с использованием качественных ингредиентов.',
    'Спагетти, бекон, яйца, пармезан, черный перец, соль, оливковое масло',
    'https://example.com/images/carbonara.jpg',
    '400 г',
    480.00,
    580.00,
    17.24,
    24.00,
    true,
    'Паста',
    NOW()
);

-- Товар 7: Десерт
INSERT INTO partner_products (
    partner_id,
    name,
    description,
    ingredients,
    image_url,
    weight,
    price,
    yess_coins,
    is_available,
    category,
    created_at
) VALUES (
    1,  -- Замените на ваш partner_id
    'Чизкейк Нью-Йорк',
    'Нежный чизкейк с классическим вкусом. Подается с ягодным соусом.',
    'Творожный сыр, сахар, яйца, сливки, печенье, масло, ваниль',
    'https://example.com/images/cheesecake.jpg',
    '200 г',
    350.00,
    17.50,
    true,
    'Десерты',
    NOW()
);

-- Товар 8: Напиток
INSERT INTO partner_products (
    partner_id,
    name,
    description,
    weight,
    price,
    yess_coins,
    is_available,
    category,
    created_at
) VALUES (
    1,  -- Замените на ваш partner_id
    'Свежевыжатый апельсиновый сок',
    '100% натуральный сок из свежих апельсинов',
    '300 мл',
    180.00,
    9.00,
    true,
    'Напитки',
    NOW()
);

-- ============================================
-- ПРОВЕРКА ВСТАВЛЕННЫХ ДАННЫХ
-- ============================================

-- Выполните этот запрос, чтобы проверить добавленные товары:
-- SELECT 
--     id,
--     partner_id,
--     name,
--     price,
--     original_price,
--     discount_percent,
--     category,
--     is_available,
--     created_at
-- FROM partner_products
-- WHERE partner_id = 1  -- Замените на ваш partner_id
-- ORDER BY id;

-- ============================================
-- ПРИМЕЧАНИЯ
-- ============================================
-- 1. Обязательные поля: partner_id, name, price, is_available, created_at
-- 2. Все остальные поля могут быть NULL
-- 3. discount_percent автоматически вычисляется как: ((original_price - price) / original_price) * 100
-- 4. Если указан original_price, рекомендуется указать discount_percent
-- 5. image_url может быть URL или локальным путем к изображению
-- 6. Для тестирования можно использовать разные значения is_available (true/false)

