@echo off
echo ========================================
echo Sửa lỗi mật khẩu MySQL
echo ========================================
echo.
echo Bạn có 2 lựa chọn:
echo.
echo [1] Đặt lại mật khẩu MySQL thành "dat1234"
echo [2] Cập nhật appsettings.json với mật khẩu mới
echo.
set /p choice="Chọn (1 hoặc 2): "

if "%choice%"=="1" goto reset_password
if "%choice%"=="2" goto update_config
goto end

:reset_password
echo.
echo ========================================
echo Đặt lại mật khẩu MySQL
echo ========================================
echo.
echo Bước 1: Mở MySQL Workbench hoặc MySQL Command Line
echo Bước 2: Đăng nhập với mật khẩu hiện tại (nếu biết)
echo Bước 3: Chạy các lệnh sau:
echo.
echo   ALTER USER 'root'@'localhost' IDENTIFIED BY 'dat1234';
echo   FLUSH PRIVILEGES;
echo.
echo Hoặc chạy file: reset_mysql_password.sql
echo.
echo Sau đó chạy lại: dotnet run
echo.
pause
goto end

:update_config
echo.
echo ========================================
echo Cập nhật mật khẩu trong appsettings.json
echo ========================================
echo.
set /p new_password="Nhập mật khẩu MySQL của bạn: "
echo.
echo Đang cập nhật appsettings.json...
echo.

powershell -Command "(Get-Content 'appsettings.json') -replace 'Password=dat1234;', 'Password=%new_password%;' | Set-Content 'appsettings.json'"

echo.
echo ========================================
echo Đã cập nhật xong!
echo ========================================
echo.
echo Mật khẩu mới: %new_password%
echo.
echo Bây giờ bạn có thể chạy lại: dotnet run
echo.
pause
goto end

:end

