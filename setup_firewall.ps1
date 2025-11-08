# Скрипт для настройки Windows Firewall
# Запустите его с правами администратора (ПКМ -> "Запуск от имени администратора")

Write-Host "================================" -ForegroundColor Green
Write-Host "Настройка Firewall для Yess Backend" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

# Проверяем права администратора
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "❌ ОШИБКА: Требуются права администратора!" -ForegroundColor Red
    Write-Host "Запустите скрипт через ПКМ -> 'Запуск от имени администратора'" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "✓ Права администратора подтверждены" -ForegroundColor Green
Write-Host ""

# Удаляем старое правило если существует
Write-Host "Проверяем существующие правила..." -ForegroundColor Cyan
$existingRule = Get-NetFirewallRule -DisplayName "Yess Backend API" -ErrorAction SilentlyContinue

if ($existingRule) {
    Write-Host "Удаляем старое правило..." -ForegroundColor Yellow
    Remove-NetFirewallRule -DisplayName "Yess Backend API"
}

# Создаем новое правило
Write-Host "Создаем правило firewall для порта 8000..." -ForegroundColor Cyan

try {
    New-NetFirewallRule `
        -DisplayName "Yess Backend API" `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort 8000 `
        -Action Allow `
        -Profile Any `
        -Enabled True
    
    Write-Host ""
    Write-Host "✅ Правило firewall успешно создано!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Теперь ваш телефон сможет подключиться к backend на:" -ForegroundColor Cyan
    Write-Host "  http://192.168.2.155:8000/" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host ""
    Write-Host "❌ Ошибка при создании правила:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    pause
    exit 1
}

# Проверяем правило
Write-Host "Проверяем созданное правило..." -ForegroundColor Cyan
Get-NetFirewallRule -DisplayName "Yess Backend API" | Format-List DisplayName, Enabled, Direction, Action, Profile

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "Настройка завершена!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""
Write-Host "Следующие шаги:" -ForegroundColor Yellow
Write-Host "1. Убедитесь, что телефон подключен к той же Wi-Fi сети" -ForegroundColor White
Write-Host "2. Пересоберите приложение MAUI" -ForegroundColor White
Write-Host "3. Установите на телефон и запустите" -ForegroundColor White
Write-Host ""
pause

