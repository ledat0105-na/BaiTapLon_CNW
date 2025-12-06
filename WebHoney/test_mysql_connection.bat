@echo off
echo ========================================
echo Kiểm tra kết nối MySQL
echo ========================================
echo.
echo 1. Kiểm tra MySQL có đang chạy không...
netstat -an | findstr :3306
echo.
echo 2. Thử kết nối MySQL với thông tin từ appsettings.json...
echo.
echo Thông tin kết nối:
echo - Server: localhost
echo - Port: 3306
echo - Database: honey_shop_db
echo - User: root
echo - Password: dat1234
echo.
echo ========================================
echo HƯỚNG DẪN SỬA LỖI:
echo ========================================
echo.
echo Nếu MySQL chưa chạy:
echo   1. Mở MySQL Workbench hoặc Services
echo   2. Khởi động MySQL Server
echo.
echo Nếu mật khẩu không đúng:
echo   1. Mở file: WebHoney\appsettings.json
echo   2. Sửa Password trong ConnectionStrings
echo   3. Hoặc đặt lại mật khẩu MySQL:
echo      mysql -u root -p
echo      ALTER USER 'root'@'localhost' IDENTIFIED BY 'dat1234';
echo.
echo ========================================
pause

