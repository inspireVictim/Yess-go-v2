# Скрипт для исправления проблемы с папкой assets
Write-Host "Исправление проблемы с папкой assets для Android сборки..." -ForegroundColor Cyan

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$assetsDir = Join-Path $projectDir "obj\Debug\net9.0-android\assets"

Write-Host "Проверка директории: $assetsDir" -ForegroundColor Yellow

# Создаем папку assets если её нет
if (-not (Test-Path $assetsDir)) {
    Write-Host "Создание папки assets..." -ForegroundColor Green
    New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
    Write-Host "Папка создана: $assetsDir" -ForegroundColor Green
} else {
    Write-Host "Папка уже существует: $assetsDir" -ForegroundColor Yellow
}

Write-Host "Очистка проекта..." -ForegroundColor Cyan
Set-Location $projectDir
dotnet clean 2>&1 | Out-Null

Write-Host "Восстановление зависимостей..." -ForegroundColor Cyan
dotnet restore 2>&1 | Out-Null

Write-Host "`nГотово! Теперь можно пересобрать проект:" -ForegroundColor Green
Write-Host "  dotnet build" -ForegroundColor White
Write-Host ""

