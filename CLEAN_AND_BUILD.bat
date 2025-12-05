@echo off
echo [Finik SDK] Cleaning project...
echo.

REM Удаляем папки obj и bin
if exist "obj" (
    echo Removing obj folder...
    rmdir /s /q "obj"
)

if exist "bin" (
    echo Removing bin folder...
    rmdir /s /q "bin"
)

echo.
echo [Finik SDK] Cleaning complete!
echo.
echo Building Release for Android...
echo.

dotnet clean "YessGoFront.csproj"
dotnet build -c Release -f net9.0-android "YessGoFront.csproj"

echo.
echo Build complete!

