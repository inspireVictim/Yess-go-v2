# Скрипт для скачивания Flutter Engine файлов
# Flutter Engine обычно распространяется через Maven, поэтому прямые URL могут не работать

$libsPath = "Platforms\Android\libs"
if (-not (Test-Path $libsPath)) {
    New-Item -ItemType Directory -Path $libsPath -Force | Out-Null
}

Write-Host "========================================="
Write-Host "Flutter Engine Download Script"
Write-Host "========================================="
Write-Host ""

Write-Host "ВАЖНО: Flutter Engine файлы обычно НЕ доступны для прямого скачивания."
Write-Host "Они распространяются через Maven репозитории."
Write-Host ""

Write-Host "Альтернативные способы получения файлов:"
Write-Host ""
Write-Host "1. Через Maven/Gradle (рекомендуется):"
Write-Host "   Добавьте зависимости в build.gradle:"
Write-Host "   implementation 'io.flutter:flutter_embedding_release:3.16.5'"
Write-Host "   implementation 'io.flutter:flutter_engine_release:3.16.5'"
Write-Host ""
Write-Host "2. Собрать из исходников Flutter:"
Write-Host "   flutter build aar"
Write-Host ""
Write-Host "3. Скачать из GitHub Releases Flutter:"
Write-Host "   https://github.com/flutter/flutter/releases"
Write-Host ""
Write-Host "4. Использовать Maven Download Plugin:"
Write-Host "   mvn dependency:copy -Dartifact=io.flutter:flutter_embedding_release:3.16.5:aar"
Write-Host ""

# Попробуем скачать через альтернативные URL
Write-Host "Попытка скачать через альтернативные источники..."
Write-Host ""

$baseUrls = @(
    "https://repo1.maven.org/maven2/io/flutter/flutter_embedding_release/3.16.5",
    "https://jcenter.bintray.com/io/flutter/flutter_embedding_release/3.16.5",
    "https://storage.googleapis.com/download.flutter.io"
)

$files = @(
    @{Name="flutter_embedding_release-3.16.5.aar"; Paths=@("/flutter_embedding_release-3.16.5.aar", "/io/flutter/flutter_embedding_release/3.16.5/flutter_embedding_release-3.16.5.aar")},
    @{Name="flutter_engine_release-3.16.5.aar"; Paths=@("/flutter_engine_release-3.16.5.aar", "/io/flutter/flutter_engine_release/3.16.5/flutter_engine_release-3.16.5.aar")}
)

foreach ($file in $files) {
    $downloaded = $false
    Write-Host "Попытка скачать: $($file.Name)"
    
    foreach ($baseUrl in $baseUrls) {
        foreach ($path in $file.Paths) {
            $url = $baseUrl + $path
            try {
                Write-Host "  Пробую: $url"
                $outputPath = Join-Path $libsPath $file.Name
                Invoke-WebRequest -Uri $url -OutFile $outputPath -UseBasicParsing -ErrorAction Stop
                Write-Host "  ✅ Успешно скачан: $($file.Name)" -ForegroundColor Green
                $downloaded = $true
                break
            } catch {
                Write-Host "  ❌ Ошибка: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        if ($downloaded) { break }
    }
    
    if (-not $downloaded) {
        Write-Host "  ⚠️ Не удалось скачать $($file.Name) автоматически" -ForegroundColor Yellow
        Write-Host "  Пожалуйста, скачайте вручную и поместите в $libsPath" -ForegroundColor Yellow
    }
    Write-Host ""
}

Write-Host "========================================="
Write-Host "Скрипт завершен"
Write-Host "========================================="

