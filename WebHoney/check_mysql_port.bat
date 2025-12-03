@echo off
echo ========================================
echo Kiểm tra MySQL Port
echo ========================================
echo.
echo Đang kiểm tra port 3306 (mặc định)...
netstat -an | findstr :3306
echo.
echo Đang kiểm tra port 3307 (theo config)...
netstat -an | findstr :3307
echo.
echo ========================================
echo Nếu thấy LISTENING nghĩa là MySQL đang chạy ở port đó
echo ========================================
pause

