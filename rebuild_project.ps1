# Скрипт для пересборки проекта после проблем с кэшем NuGet

Write-Host "🧹 Очистка проекта..." -ForegroundColor Yellow

# Очистка папок сборки
if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Папка obj удалена" -ForegroundColor Green
}

if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Папка bin удалена" -ForegroundColor Green
}

Write-Host "`n📦 Восстановление пакетов..." -ForegroundColor Yellow
dotnet restore YessGoFront.csproj --force

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Ошибка при восстановлении пакетов" -ForegroundColor Red
    Write-Host "💡 Попробуйте:" -ForegroundColor Yellow
    Write-Host "   1. Закрыть Visual Studio" -ForegroundColor Cyan
    Write-Host "   2. Закрыть все процессы MSBuild через Диспетчер задач" -ForegroundColor Cyan
    Write-Host "   3. Запустить этот скрипт снова" -ForegroundColor Cyan
    exit 1
}

Write-Host "`n🔨 Сборка проекта..." -ForegroundColor Yellow
dotnet build YessGoFront.csproj -f net9.0-android

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Проект успешно собран!" -ForegroundColor Green
} else {
    Write-Host "`n❌ Ошибка при сборке проекта" -ForegroundColor Red
    Write-Host "💡 Если видите ошибки 'Access denied' или 'file not found':" -ForegroundColor Yellow
    Write-Host "   1. Закройте Visual Studio полностью" -ForegroundColor Cyan
    Write-Host "   2. Закройте все процессы MSBuild и dotnet через Диспетчер задач" -ForegroundColor Cyan
    Write-Host "   3. Запустите скрипт снова" -ForegroundColor Cyan
}

