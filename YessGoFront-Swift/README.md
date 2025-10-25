# YessGo iOS Swift Version

Нативная iOS версия приложения YessGo, написанная на Swift с использованием SwiftUI.

## Структура проекта

```
YessGoFront-Swift/
├── Models/                 # Модели данных
│   ├── UserDto.swift
│   ├── PartnerDto.swift
│   ├── StoryModel.swift
│   ├── BannerModel.swift
│   ├── CategoryModel.swift
│   └── PartnerLogoModel.swift
├── Services/              # Сервисы и бизнес-логика
│   ├── BalanceStore.swift
│   ├── AccountStore.swift
│   ├── AppClient.swift
│   └── AuthService.swift
├── ViewModels/           # ViewModels с Combine
│   └── MainPageViewModel.swift
├── Views/                # SwiftUI Views
│   ├── MainPageView.swift
│   ├── ContentView.swift
│   └── Components/
│       └── BottomNavBar.swift
├── Resources/            # Ресурсы
│   ├── Images/
│   └── Fonts/
├── Assets/               # Xcode Assets
├── YessGoFrontApp.swift  # Точка входа приложения
├── Info.plist           # iOS настройки
└── README.md
```

## Особенности реализации

### Архитектура
- **MVVM** с использованием Combine для реактивного программирования
- **SwiftUI** для пользовательского интерфейса
- **Singleton** паттерн для сервисов (BalanceStore, AccountStore, AuthService)

### Основные компоненты

#### Модели данных
- Все модели реализуют протокол `Codable` для JSON сериализации
- Используют `Identifiable` для работы с SwiftUI списками
- Поддержка опциональных полей для гибкости API

#### Сервисы
- **BalanceStore**: Управление балансом пользователя с реактивными обновлениями
- **AccountStore**: Управление профилем пользователя с персистентностью через UserDefaults
- **AuthService**: Аутентификация с поддержкой автоматического входа
- **AppClient**: HTTP клиент для API запросов с использованием Combine

#### ViewModels
- **MainPageViewModel**: Основная логика главной страницы
- Поддержка Stories (как в Instagram)
- Управление баннерами и партнерами
- Реактивные обновления UI через `@Published` свойства

#### Views
- **MainPageView**: Главная страница с полным функционалом
- **ContentView**: Корневой view с навигацией
- **BottomNavBar**: Нижняя навигационная панель
- Поддержка Story overlay с анимациями
- Banner overlay для рекламных баннеров

### Технические особенности

#### Цветовая схема
- Основной цвет: `#0F6B53` (зеленый)
- Акцентный цвет: `#DAA520` (золотой)
- Фон: `#F4F6F8` (светло-серый)

#### Навигация
- TabView для основной навигации
- Поддержка модальных окон для Stories и Banners
- Автоматическое управление состоянием

#### Анимации
- Плавные переходы между страницами Stories
- Прогресс-бары с анимацией
- Автоматическое закрытие баннеров

## Требования

- iOS 15.0+
- Xcode 14.0+
- Swift 5.7+

## Установка и запуск

1. Откройте проект в Xcode
2. Выберите симулятор или подключенное устройство
3. Нажмите Run (⌘+R)

## API Integration

Приложение готово для интеграции с реальным API:
- Базовый URL настраивается в `AppClient`
- Все модели поддерживают JSON сериализацию
- HTTP клиент использует современные Combine паттерны

## Дальнейшее развитие

- Интеграция с реальным API
- Добавление push-уведомлений
- Реализация QR-сканера
- Добавление карт для поиска партнеров
- Интеграция с Apple Pay
