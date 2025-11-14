# Скрипт для очистки кэша сборки MAUI приложения
# Используйте этот скрипт при ошибке: XABBA7000: Xamarin.Tools.Zip.ZipException

Write-Host "🧹 Очистка кэша сборки MAUI приложения..." -ForegroundColor Cyan

# 1. Остановка процессов Visual Studio и MSBuild
Write-Host "`n1️⃣ Завершение процессов Visual Studio и MSBuild..." -ForegroundColor Yellow
Get-Process | Where-Object {
    $_.ProcessName -like "*devenv*" -or 
    $_.ProcessName -like "*MSBuild*" -or 
    $_.ProcessName -like "*VBCSCompiler*"
} | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep -Seconds 2

# 2. Удаление папок кэша
Write-Host "`n2️⃣ Удаление папок bin и obj..." -ForegroundColor Yellow
Remove-Item -Path "bin", "obj" -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
Write-Host "✅ Папки удалены"

# 3. Очистка NuGet кэша
Write-Host "`n3️⃣ Очистка NuGet кэша..." -ForegroundColor Yellow
dotnet nuget locals all --clear | Out-Null
Write-Host "✅ NuGet кэш очищен"

# 4. Удаление Xamarin кэша
Write-Host "`n4️⃣ Удаление Xamarin кэша..." -ForegroundColor Yellow
$xamarinCache = "$env:USERPROFILE\AppData\Local\Xamarin"
if (Test-Path $xamarinCache) {
    Remove-Item -Path $xamarinCache -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Xamarin кэш удален"
} else {
    Write-Host "ℹ️ Xamarin кэш не найден"
}

# 5. Удаление HotRestart кэша
Write-Host "`n5️⃣ Удаление HotRestart кэша..." -ForegroundColor Yellow
$hotRestartCache = "$env:USERPROFILE\AppData\Local\Temp\Xamarin"
if (Test-Path $hotRestartCache) {
    Remove-Item -Path $hotRestartCache -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ HotRestart кэш удален"
} else {
    Write-Host "ℹ️ HotRestart кэш не найден"
}

Write-Host "`n✅ Очистка завершена!" -ForegroundColor Green
Write-Host "`n📝 Теперь выполните:`n   dotnet restore YessGoFront.csproj`n   dotnet build YessGoFront.csproj -c Debug" -ForegroundColor Cyan













