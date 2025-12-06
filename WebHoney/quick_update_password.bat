@echo off
chcp 65001 >nul
echo ========================================
echo Cap nhat mat khau MySQL
echo ========================================
echo.
echo Mat khau hien tai trong appsettings.json: dat1234
echo.
set /p new_password="Nhap mat khau MySQL cua ban (de trong neu khong co): "
echo.
echo Dang cap nhat appsettings.json...
echo.

powershell -Command "$file = 'appsettings.json'; $content = Get-Content $file -Raw -Encoding UTF8; $content = $content -replace 'Password=dat1234;', 'Password=%new_password%;'; [System.IO.File]::WriteAllText((Resolve-Path $file), $content, [System.Text.Encoding]::UTF8)"

echo.
echo ========================================
echo Da cap nhat xong!
echo ========================================
echo.
echo Bay gio chay lai: dotnet run
echo.
pause

