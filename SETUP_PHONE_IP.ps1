# 📱 Скрипт настройки IP для реального Android телефона

Write-Host "================================" -ForegroundColor Cyan
Write-Host "📱 SETUP PHONE IP - YessGoFront" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Шаг 1: Найти IP компьютера
Write-Host "`n📍 Определение IP адреса ПК..." -ForegroundColor Yellow

$ipAddress = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.IPAddress -like "192.168.*" -and $_.PrefixOrigin -eq "Dhcp"
} | Select-Object -First 1).IPAddress

if (-not $ipAddress) {
    Write-Host "⚠️  Не найден DHCP IP адрес!" -ForegroundColor Red
    Write-Host "Пожалуйста, укажите IP вручную:" -ForegroundColor Yellow
    $ipAddress = Read-Host "Введите IP адрес ПК (например, 192.168.0.177)"
}

Write-Host "✅ IP найден: $ipAddress" -ForegroundColor Green

# Шаг 2: Проверить доступность бэкенда
Write-Host "`n🔍 Проверка доступности бэкенда..." -ForegroundColor Yellow
$apiUrl = "http://${ipAddress}:8000/"

try {
    $response = Invoke-WebRequest -Uri $apiUrl -Method Get -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ Бэкенд доступен! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ Бэкенд НЕ доступен по адресу $apiUrl" -ForegroundColor Red
    Write-Host "⚠️  Убедитесь, что:" -ForegroundColor Yellow
    Write-Host "   1. Docker контейнер yess-backend запущен" -ForegroundColor Yellow
    Write-Host "   2. Порт 8000 открыт в брандмауэре" -ForegroundColor Yellow
    Write-Host "   3. Телефон и ПК в одной сети Wi-Fi" -ForegroundColor Yellow
    Write-Host ""
    $continue = Read-Host "Продолжить настройку? (y/n)"
    if ($continue -ne "y") {
        exit
    }
}

# Шаг 3: Установить переменную окружения
Write-Host "`n🔧 Установка переменной окружения..." -ForegroundColor Yellow
$env:API_BASE_URL = "${apiUrl}"
Write-Host "✅ API_BASE_URL установлен: $env:API_BASE_URL" -ForegroundColor Green

# Шаг 4: Сохранить в пользовательские переменные окружения (опционально)
Write-Host "`n💾 Сохранить в пользовательские переменные окружения?" -ForegroundColor Cyan
$save = Read-Host "Сохранить? (y/n)"

if ($save -eq "y") {
    [System.Environment]::SetEnvironmentVariable("API_BASE_URL", "${apiUrl}", "User")
    Write-Host "✅ Переменная сохранена в пользовательские настройки!" -ForegroundColor Green
    Write-Host "⚠️  Перезапустите Visual Studio / Rider для применения изменений" -ForegroundColor Yellow
}

# Шаг 5: Инструкции
Write-Host "`n📋 ИНСТРУКЦИИ:" -ForegroundColor Cyan
Write-Host "1. Убедитесь, что телефон подключен к той же Wi-Fi сети" -ForegroundColor White
Write-Host "2. Запустите приложение на телефоне" -ForegroundColor White
Write-Host "3. Проверьте логи в Visual Studio / Rider" -ForegroundColor White
Write-Host "4. Если не работает, проверьте брандмауэр Windows" -ForegroundColor White
Write-Host ""
Write-Host "🔗 API URL: $apiUrl" -ForegroundColor Green
Write-Host ""
Write-Host "✅ Готово! Теперь можно запускать приложение." -ForegroundColor Green

