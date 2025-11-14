# Скрипт для очистки и пересборки проекта MAUI

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Очистка и пересборка проекта MAUI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Остановка всех процессов dotnet
Write-Host "`nОстановка процессов dotnet..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Очистка папок bin и obj
Write-Host "`nОчистка папок bin и obj..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Папка bin удалена" -ForegroundColor Green
}
if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Папка obj удалена" -ForegroundColor Green
}

# Очистка кеша NuGet
Write-Host "`nОчистка кеша NuGet..." -ForegroundColor Yellow
dotnet nuget locals all --clear
Write-Host "Кеш NuGet очищен" -ForegroundColor Green

# Восстановление пакетов
Write-Host "`nВосстановление пакетов..." -ForegroundColor Yellow
dotnet restore YessGoFront.csproj
if ($LASTEXITCODE -eq 0) {
    Write-Host "Пакеты восстановлены" -ForegroundColor Green
} else {
    Write-Host "Ошибка при восстановлении пакетов" -ForegroundColor Red
    exit 1
}

# Очистка проекта
Write-Host "`nОчистка проекта..." -ForegroundColor Yellow
dotnet clean YessGoFront.csproj
if ($LASTEXITCODE -eq 0) {
    Write-Host "Проект очищен" -ForegroundColor Green
} else {
    Write-Host "Ошибка при очистке проекта" -ForegroundColor Red
    exit 1
}

# Сборка проекта
Write-Host "`nСборка проекта..." -ForegroundColor Yellow
dotnet build YessGoFront.csproj -f net9.0-android -c Debug
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "Проект успешно собран!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
} else {
    Write-Host "`n========================================" -ForegroundColor Red
    Write-Host "Ошибка при сборке проекта" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}

