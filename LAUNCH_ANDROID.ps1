# 🚀 Скрипт запуска на Android с правильным IP

# ============================================
# НАСТРОЙКА ДЛЯ РЕАЛЬНОГО ANDROID ТЕЛЕФОНА
# ============================================

Write-Host "================================" -ForegroundColor Cyan
Write-Host "🚀 LAUNCH ANDROID - YessGoFront" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Шаг 1: Узнать IP компьютера
Write-Host "`n📍 Определение IP адреса ПК..." -ForegroundColor Yellow

$ipAddress = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {
    $_.PrefixOrigin -eq "Dhcp" -and $_.IPAddress -like "192.168*"
} | Select-Object -First 1).IPAddress

if (-not $ipAddress) {
    Write-Host "⚠️  Не найден DHCP IP адрес!" -ForegroundColor Red
    Write-Host "Пожалуйста, укажите IP вручную:" -ForegroundColor Yellow
    $ipAddress = Read-Host "Введите IP адрес ПК (например, 192.168.1.100)"
}

Write-Host "✅ IP найден: $ipAddress" -ForegroundColor Green

# Шаг 2: Спросить, что запускать
Write-Host "`n🤖 Выберите, что запустить:" -ForegroundColor Cyan
Write-Host "1️⃣  Android эмулятор"
Write-Host "2️⃣  Реальный Android телефон"
Write-Host "3️⃣  Только установить переменную окружения"

$choice = Read-Host "Введите номер (1/2/3)"

# Шаг 3: Установить переменную окружения
if ($choice -eq "1" -or $choice -eq "2" -or $choice -eq "3") {
    if ($choice -eq "1") {
        $apiUrl = "http://10.0.2.2:8000/"
        Write-Host "`n🔷 Будет использован IP эмулятора: $apiUrl" -ForegroundColor Cyan
    } else {
        $apiUrl = "http://$($ipAddress):8000/"
        Write-Host "`n📱 Будет использован IP ПК: $apiUrl" -ForegroundColor Cyan
    }
    
    Write-Host "⚙️  Установка переменной окружения..." -ForegroundColor Yellow
    
    # Временная установка (текущая сессия)
    $env:API_BASE_URL = $apiUrl
    Write-Host "✅ API_BASE_URL = $apiUrl" -ForegroundColor Green
    
    # Предложить постоянную установку
    if ($choice -ne "3") {
        $permanent = Read-Host "`n💾 Установить постоянно? (y/n)"
        if ($permanent -eq "y" -or $permanent -eq "Y") {
            [System.Environment]::SetEnvironmentVariable("API_BASE_URL", $apiUrl, "User")
            Write-Host "✅ Установлено постоянно для пользователя!" -ForegroundColor Green
            Write-Host "⚠️  Перезагрузите PowerShell и компьютер для применения" -ForegroundColor Yellow
        }
    }
}

# Шаг 4: Сборка и запуск
if ($choice -eq "1" -or $choice -eq "2") {
    Write-Host "`n🔨 Сборка и запуск приложения..." -ForegroundColor Yellow
    Write-Host "💡 Убедитесь, что:" -ForegroundColor Cyan
    Write-Host "   - Android SDK установлен"
    Write-Host "   - Эмулятор или телефон подключены (для реального телефона: adb devices)"
    Write-Host "   - Проект открыт в Visual Studio"
    
    # Построить и развернуть
    Write-Host "`n▶️  Запуск: dotnet maui build -f android -c Release" -ForegroundColor Cyan
    
    # Раскомментируйте для реального запуска:
    # dotnet maui build -f android -c Release
    
    Write-Host "✅ После завершения приложение запустится на вашем устройстве!" -ForegroundColor Green
}

Write-Host "`n" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "✨ Готово!" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

