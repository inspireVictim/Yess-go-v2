# 🚨 Быстрое исправление ошибок компиляции

## Проблема
Файлы в кэше NuGet заблокированы процессами Visual Studio/MSBuild, из-за чего не создаётся `project.assets.json` и компилятор не может найти базовые типы MAUI.

## ✅ Решение (выберите один вариант)

### Вариант 1: Через Visual Studio (РЕКОМЕНДУЕТСЯ)

1. **Откройте проект в Visual Studio**
2. **Правой кнопкой на проект** `YessGoFront.csproj` в Solution Explorer
3. Выберите **"Restore NuGet Packages"**
4. Дождитесь завершения (может занять 1-2 минуты)
5. Затем: **Build** → **Rebuild Solution**

### Вариант 2: Закрыть процессы и восстановить

1. **Закройте Visual Studio полностью**
2. Откройте **Диспетчер задач** (Ctrl+Shift+Esc)
3. Завершите процессы:
   - `MSBuild.exe` (все экземпляры)
   - `devenv.exe` (Visual Studio)
4. В PowerShell выполните:
   ```powershell
   cd E:\YessProject\YessGoFrontV2
   dotnet restore YessGoFront.csproj
   dotnet build YessGoFront.csproj -f net9.0-android
   ```

### Вариант 3: Использовать скрипт

Запустите скрипт `fix_nuget_cache.ps1`:
```powershell
cd E:\YessProject\YessGoFrontV2
.\fix_nuget_cache.ps1
```

## 🔍 Проверка успешности

После восстановления проверьте:
- Файл `obj\project.assets.json` должен существовать
- В Visual Studio не должно быть ошибок "file not found" или "не удалось найти тип"

## ⚠️ Если проблема сохраняется

1. Перезагрузите компьютер
2. Откройте Visual Studio от имени администратора
3. Попробуйте восстановить пакеты снова

