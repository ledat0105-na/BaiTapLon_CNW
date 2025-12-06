# Script test kết nối MySQL từ .NET
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test kết nối MySQL" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Đọc connection string từ appsettings.json
$appsettings = Get-Content "appsettings.json" | ConvertFrom-Json
$connectionString = $appsettings.ConnectionStrings.DefaultConnection

Write-Host "Connection String:" -ForegroundColor Yellow
Write-Host $connectionString -ForegroundColor Gray
Write-Host ""

# Tạo file test C# đơn giản
$testCode = @"
using System;
using MySqlConnector;

class Program
{
    static void Main()
    {
        string connectionString = "$connectionString";
        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("SUCCESS! Kết nối MySQL thành công!");
                Console.WriteLine("Database: " + connection.Database);
                connection.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
            Environment.Exit(1);
        }
    }
}
"@

$testCode | Out-File -FilePath "test_conn.cs" -Encoding UTF8

Write-Host "Đang test kết nối..." -ForegroundColor Yellow
Write-Host ""

# Chạy test
dotnet run --project . --no-build 2>&1 | Select-String -Pattern "SUCCESS|ERROR" 

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Kết nối thành công!" -ForegroundColor Green
    Write-Host "Bạn có thể chạy: dotnet run" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Kết nối thất bại!" -ForegroundColor Red
    Write-Host "Vui lòng kiểm tra:" -ForegroundColor Yellow
    Write-Host "1. Mật khẩu MySQL trong appsettings.json" -ForegroundColor Yellow
    Write-Host "2. Database honey_shop_db đã được tạo chưa" -ForegroundColor Yellow
    Write-Host "3. MySQL Server đang chạy" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Red
}

# Xóa file test
Remove-Item "test_conn.cs" -ErrorAction SilentlyContinue

