# Скрипт для исправления проблем с кэшем NuGet

Write-Host "🔍 Проверка процессов, блокирующих файлы..." -ForegroundColor Yellow

# Проверяем процессы
$processes = Get-Process | Where-Object {
    $_.ProcessName -like "*msbuild*" -or 
    $_.ProcessName -like "*devenv*" -or 
    $_.ProcessName -like "*dotnet*" -and $_.Id -ne $PID
}

if ($processes) {
    Write-Host "⚠️  Найдены процессы, которые могут блокировать файлы:" -ForegroundColor Yellow
    $processes | Format-Table ProcessName, Id -AutoSize
    Write-Host "💡 Закройте Visual Studio и все процессы MSBuild перед продолжением" -ForegroundColor Cyan
    Write-Host "   Нажмите любую клавишу после закрытия процессов..." -ForegroundColor Cyan
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

Write-Host "`n🧹 Очистка папок сборки..." -ForegroundColor Yellow
if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Папка obj удалена" -ForegroundColor Green
}
if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Папка bin удалена" -ForegroundColor Green
}

Write-Host "`n📦 Очистка кэша NuGet (только http-cache и temp)..." -ForegroundColor Yellow
dotnet nuget locals http-cache --clear
dotnet nuget locals temp --clear
Write-Host "✅ Кэш очищен" -ForegroundColor Green

Write-Host "`n🔄 Восстановление пакетов..." -ForegroundColor Yellow
$restoreResult = dotnet restore YessGoFront.csproj --no-cache 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Ошибка при восстановлении пакетов" -ForegroundColor Red
    Write-Host $restoreResult
    
    Write-Host "`n💡 Попробуйте следующее:" -ForegroundColor Yellow
    Write-Host "   1. Закройте Visual Studio полностью" -ForegroundColor Cyan
    Write-Host "   2. Откройте Диспетчер задач (Ctrl+Shift+Esc)" -ForegroundColor Cyan
    Write-Host "   3. Завершите все процессы:" -ForegroundColor Cyan
    Write-Host "      - MSBuild.exe" -ForegroundColor White
    Write-Host "      - devenv.exe (Visual Studio)" -ForegroundColor White
    Write-Host "      - dotnet.exe (если не нужны)" -ForegroundColor White
    Write-Host "   4. Запустите этот скрипт снова" -ForegroundColor Cyan
    Write-Host "`n   ИЛИ попробуйте восстановить пакеты через Visual Studio:" -ForegroundColor Yellow
    Write-Host "   Правой кнопкой на проект → Restore NuGet Packages" -ForegroundColor Cyan
    
    exit 1
}

Write-Host "✅ Пакеты успешно восстановлены" -ForegroundColor Green

Write-Host "`n🔨 Сборка проекта..." -ForegroundColor Yellow
$buildResult = dotnet build YessGoFront.csproj -f net9.0-android --no-incremental 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Проект успешно собран!" -ForegroundColor Green
} else {
    Write-Host "`n❌ Ошибки при сборке:" -ForegroundColor Red
    $buildResult | Select-String -Pattern "error|Error" | Select-Object -First 10
    
    Write-Host "`n💡 Если видите ошибки 'file not found' или 'не удалось найти тип':" -ForegroundColor Yellow
    Write-Host "   1. Убедитесь, что Visual Studio закрыт" -ForegroundColor Cyan
    Write-Host "   2. Попробуйте восстановить пакеты через Visual Studio:" -ForegroundColor Cyan
    Write-Host "      Правой кнопкой на проект → Restore NuGet Packages" -ForegroundColor White
    Write-Host "   3. Затем: Build → Rebuild Solution" -ForegroundColor Cyan
}

