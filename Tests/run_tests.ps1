# Скрипт для запуска модульных тестов блокировки emoji
# Использование: .\run_tests.ps1

Write-Host "🧪 Запуск модульных тестов для блокировки emoji..." -ForegroundColor Cyan
Write-Host ""

# Получаем путь к тестовому проекту
$testProjectPath = Join-Path $PSScriptRoot "YessGoFront.EmojiFilter.Tests.csproj"

if (-not (Test-Path $testProjectPath)) {
    Write-Host "❌ Тестовый проект не найден: $testProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "📦 Восстановление пакетов..." -ForegroundColor Yellow
dotnet restore $testProjectPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Ошибка при восстановлении пакетов" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🔨 Сборка тестового проекта..." -ForegroundColor Yellow
dotnet build $testProjectPath --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Ошибка при сборке тестового проекта" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "▶️  Запуск тестов..." -ForegroundColor Green
Write-Host ""

# Запускаем тесты с подробным выводом
dotnet test $testProjectPath --no-build --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Все тесты прошли успешно!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ Некоторые тесты не прошли" -ForegroundColor Red
    exit 1
}

