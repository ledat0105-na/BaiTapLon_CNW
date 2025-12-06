@echo off
echo ========================================
echo Test đăng nhập MySQL
echo ========================================
echo.
echo Đang thử đăng nhập với mật khẩu: dat1234
echo.
mysql -u root -pdat1234 -e "SELECT 'Connection successful!' AS Status;" 2>nul
if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo SUCCESS! Mật khẩu dat1234 là ĐÚNG
    echo ========================================
    echo.
    echo Bạn có thể chạy lại ứng dụng: dotnet run
) else (
    echo.
    echo ========================================
    echo ERROR! Mật khẩu dat1234 KHÔNG ĐÚNG
    echo ========================================
    echo.
    echo Hãy thử các cách sau:
    echo.
    echo CÁCH 1: Đặt lại mật khẩu MySQL thành dat1234
    echo   1. Mở MySQL Workbench
    echo   2. Kết nối với MySQL (nếu có thể)
    echo   3. Chạy script: reset_mysql_password.sql
    echo.
    echo CÁCH 2: Cập nhật mật khẩu trong appsettings.json
    echo   1. Mở file: WebHoney\appsettings.json
    echo   2. Tìm dòng: "Password=dat1234;"
    echo   3. Thay dat1234 bằng mật khẩu MySQL thực tế của bạn
    echo.
    echo CÁCH 3: Thử các mật khẩu phổ biến
    echo   - root
    echo   - password
    echo   - 123456
    echo   - (để trống)
    echo.
)
echo.
pause

