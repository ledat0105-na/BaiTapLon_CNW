-- Script kiểm tra và sửa đường dẫn ảnh sản phẩm
-- Chạy script này trong MySQL Workbench để kiểm tra và sửa tất cả ảnh sản phẩm

USE honey_shop_db;

-- 1. Kiểm tra các sản phẩm không có ảnh hoặc ảnh rỗng
SELECT id, name, image_url, 
       CASE 
           WHEN image_url IS NULL OR image_url = '' THEN 'KHONG CO ANH'
           WHEN image_url NOT LIKE '/uploads/%' AND image_url NOT LIKE '/assets/%' AND image_url NOT LIKE 'http%' THEN 'DUONG DAN SAI'
           ELSE 'OK'
       END AS trang_thai
FROM products
ORDER BY id;

-- 2. Cập nhật tất cả sản phẩm không có ảnh hoặc ảnh rỗng sang dùng ảnh placeholder
UPDATE products 
SET image_url = '/assets/images/property-01.jpg' 
WHERE image_url IS NULL OR image_url = '';

-- 3. Cập nhật các sản phẩm có đường dẫn sai (không bắt đầu bằng / hoặc http)
UPDATE products 
SET image_url = '/assets/images/property-01.jpg' 
WHERE image_url IS NOT NULL 
  AND image_url != '' 
  AND image_url NOT LIKE '/%' 
  AND image_url NOT LIKE 'http%';

-- 4. Kiểm tra lại sau khi cập nhật
SELECT id, name, image_url 
FROM products 
ORDER BY id;

-- 5. Xem tổng số sản phẩm và số sản phẩm có ảnh
SELECT 
    COUNT(*) AS tong_so_san_pham,
    SUM(CASE WHEN image_url IS NOT NULL AND image_url != '' THEN 1 ELSE 0 END) AS co_anh,
    SUM(CASE WHEN image_url IS NULL OR image_url = '' THEN 1 ELSE 0 END) AS khong_co_anh
FROM products;

