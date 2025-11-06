# Скрипт для открытия проекта YESS Go Front в Visual Studio 2022
# Автоматически находит Visual Studio и открывает solution файл

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "  YESS Go Front - Open in VS" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Путь к solution файлу
$solutionFile = Join-Path $PSScriptRoot "YESS Go front v 2.0.sln"

# Проверяем наличие solution файла
if (-not (Test-Path $solutionFile)) {
    Write-Host "❌ Ошибка: Solution файл не найден!" -ForegroundColor Red
    Write-Host "   Ожидаемый путь: $solutionFile" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Нажмите Enter для выхода"
    exit 1
}

Write-Host "✅ Solution файл найден" -ForegroundColor Green
Write-Host "   $solutionFile" -ForegroundColor Gray
Write-Host ""

# Поиск Visual Studio 2022
$vsPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe"
)

$vsPath = $null
foreach ($path in $vsPaths) {
    if (Test-Path $path) {
        $vsPath = $path
        break
    }
}

if ($null -eq $vsPath) {
    Write-Host "❌ Visual Studio 2022 не найден!" -ForegroundColor Red
    Write-Host ""
    Write-Host "📥 Установите Visual Studio 2022 с официального сайта:" -ForegroundColor Yellow
    Write-Host "   https://visualstudio.microsoft.com/downloads/" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "⚠️  Убедитесь, что установлена рабочая нагрузка:" -ForegroundColor Yellow
    Write-Host "   '.NET Multi-platform App UI development'" -ForegroundColor White
    Write-Host ""
    
    # Попытка открыть через ассоциацию файлов
    Write-Host "🔄 Попытка открыть через ассоциацию файлов..." -ForegroundColor Yellow
    try {
        Start-Process $solutionFile
        Write-Host "✅ Файл открыт!" -ForegroundColor Green
    } catch {
        Write-Host "❌ Не удалось открыть файл" -ForegroundColor Red
    }
    
    Write-Host ""
    Read-Host "Нажмите Enter для выхода"
    exit 1
}

Write-Host "✅ Visual Studio 2022 найден" -ForegroundColor Green
Write-Host "   $vsPath" -ForegroundColor Gray
Write-Host ""

# Проверяем .NET SDK
Write-Host "🔍 Проверка .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = & dotnet --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ .NET SDK установлен (версия $dotnetVersion)" -ForegroundColor Green
    } else {
        Write-Host "⚠️  .NET SDK не найден или не в PATH" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Не удалось проверить .NET SDK" -ForegroundColor Yellow
}
Write-Host ""

# Открываем Visual Studio
Write-Host "🚀 Открываю проект в Visual Studio..." -ForegroundColor Cyan
Write-Host ""

try {
    Start-Process $vsPath -ArgumentList "`"$solutionFile`""
    
    Write-Host "✅ Visual Studio запущен!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📝 Следующие шаги:" -ForegroundColor Cyan
    Write-Host "   1. Дождитесь загрузки проекта" -ForegroundColor White
    Write-Host "   2. Выберите целевую платформу (Android/iOS)" -ForegroundColor White
    Write-Host "   3. Выберите эмулятор или устройство" -ForegroundColor White
    Write-Host "   4. Нажмите F5 для запуска" -ForegroundColor White
    Write-Host ""
    Write-Host "📚 Подробная инструкция: OPEN_IN_VISUAL_STUDIO.md" -ForegroundColor Gray
    Write-Host ""
    Write-Host "✨ Удачной разработки!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Ошибка при запуске Visual Studio:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔄 Попытка альтернативного способа..." -ForegroundColor Yellow
    
    try {
        Start-Process $solutionFile
        Write-Host "✅ Файл открыт через ассоциацию" -ForegroundColor Green
    } catch {
        Write-Host "❌ Альтернативный способ также не сработал" -ForegroundColor Red
        Write-Host ""
        Write-Host "💡 Попробуйте открыть файл вручную:" -ForegroundColor Yellow
        Write-Host "   $solutionFile" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Start-Sleep -Seconds 2

