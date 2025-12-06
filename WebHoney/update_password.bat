@echo off
echo ========================================
echo Cập nhật mật khẩu MySQL trong appsettings.json
echo ========================================
echo.
set /p new_password="Nhập mật khẩu MySQL của bạn (để trống nếu không có mật khẩu): "
echo.
echo Đang cập nhật...
echo.

powershell -Command "$content = Get-Content 'appsettings.json' -Raw; $content = $content -replace 'Password=[^;]*;', 'Password=%new_password%;'; Set-Content -Path 'appsettings.json' -Value $content -NoNewline"

echo.
echo ========================================
echo Đã cập nhật xong!
echo ========================================
echo.
echo Mật khẩu đã được cập nhật trong appsettings.json
echo.
echo Bây giờ chạy lại: dotnet run
echo.
pause

