# 🔧 Решение ошибок при сборке MAUI

## ❌ Ошибка: XABBA7000 - Permission Denied

```
XABBA7000: Xamarin.Tools.Zip.ZipException: Renaming temporary file failed: Permission denied
```

### 🔍 Причины:
- Файлы заблокированы Visual Studio или MSBuild
- Проблемы с кэшем сборки
- Поврежденные временные файлы
- Недостаточно прав доступа

### ✅ Решение:

#### **Вариант 1: Быстрая очистка (рекомендуется)**

Запустите скрипт очистки:

```powershell
cd YessGoFrontV2
.\cleanup_build.ps1
```

Затем пересоберите:

```powershell
dotnet restore YessGoFront.csproj
dotnet build YessGoFront.csproj -c Debug
```

#### **Вариант 2: Ручная очистка**

1. **Закройте Visual Studio полностью**
   ```powershell
   Get-Process devenv | Stop-Process -Force
   ```

2. **Удалите папки сборки**
   ```powershell
   cd E:\YessProject\YessGoFrontV2
   Remove-Item -Path bin, obj -Recurse -Force -ErrorAction SilentlyContinue
   ```

3. **Очистите NuGet кэш**
   ```powershell
   dotnet nuget locals all --clear
   ```

4. **Очистите Xamarin кэш**
   ```powershell
   Remove-Item -Path "$env:USERPROFILE\AppData\Local\Xamarin" -Recurse -Force -ErrorAction SilentlyContinue
   Remove-Item -Path "$env:USERPROFILE\AppData\Local\Temp\Xamarin" -Recurse -Force -ErrorAction SilentlyContinue
   ```

5. **Восстановите зависимости**
   ```powershell
   dotnet restore YessGoFront.csproj
   ```

6. **Пересоберите проект**
   ```powershell
   dotnet build YessGoFront.csproj -c Debug
   ```

#### **Вариант 3: Полная очистка (если выше не сработало)**

```powershell
# 1. Закрыть все процессы
Get-Process | Where-Object {$_.ProcessName -like "*MSBuild*" -or $_.ProcessName -like "*devenv*"} | Stop-Process -Force

# 2. Перезагрузка
Restart-Computer

# 3. После перезагрузки - повторить Вариант 1
```

---

## 📊 Целевые платформы при сборке

Используйте правильный флаг для нужной платформы:

```powershell
# Android
dotnet build YessGoFront.csproj -f net9.0-android

# iOS симулятор
dotnet build YessGoFront.csproj -f net9.0-ios

# iOS реальное устройство
dotnet build YessGoFront.csproj -f net9.0-ios -r ios-arm64

# Windows
dotnet build YessGoFront.csproj -f net9.0-windows10.0.19041.0
```

---

## 🚀 Запуск после сборки

```powershell
# Android эмулятор
dotnet build -t:Run -f net9.0-android

# iOS симулятор
dotnet build -t:Run -f net9.0-ios

# Windows
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

---

## 🐛 Дополнительные советы

1. **Если ошибка повторяется постоянно:**
   - Переустановите .NET MAUI workload:
     ```powershell
     dotnet workload uninstall maui
     dotnet workload install maui
     ```

2. **Проверьте версию .NET SDK:**
   ```powershell
   dotnet --version
   # Должна быть 9.0.x (соответствует версии в global.json)
   ```

3. **Убедитесь, что достаточно места на диске:**
   ```powershell
   (Get-Volume C).SizeRemaining / 1GB  # Минимум 5-10 GB
   ```

4. **Если ошибка в Android эмуляторе:**
   - Закройте эмулятор
   - Запустите заново
   - Или используйте реальное устройство

5. **Логирование при сборке:**
   ```powershell
   dotnet build YessGoFront.csproj -c Debug -v diag
   ```

---

## 📝 Профилактика

Регулярно выполняйте очистку:

```powershell
# Еженедельно
dotnet nuget locals all --clear

# При любых проблемах со сборкой
.\cleanup_build.ps1
```

---

## 🎯 Если ничего не помогло

1. Создайте Issue с полным логом ошибки
2. Приложите содержимое `bin/Debug` папки
3. Укажите версию .NET SDK и Windows
4. Упомяните целевую платформу (Android/iOS)

**Вы обновили .NET MAUI последнюю версию?**
```powershell
dotnet workload update
```




