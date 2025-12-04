-- Script để sửa đường dẫn hình ảnh sản phẩm
-- Nếu bạn đã có file ảnh trong thư mục wwwroot/uploads/products/, 
-- hãy cập nhật đường dẫn trong database cho đúng với tên file thực tế

-- Cách 1: Cập nhật để sử dụng ảnh placeholder từ assets
UPDATE products 
SET image_url = '/assets/images/property-01.jpg' 
WHERE image_url IS NULL OR image_url = '';

-- Cách 2: Nếu bạn muốn giữ nguyên đường dẫn từ SQL script ban đầu,
-- bạn cần đảm bảo các file ảnh sau tồn tại trong wwwroot/uploads/products/:
-- - honey-forest-500ml.jpg
-- - honey-forest-1l.jpg
-- - honey-forest-premium-250ml.jpg
-- - honey-longan-500ml.jpg
-- - honey-longan-1l.jpg
-- - honey-coffee-500ml.jpg
-- - honey-coffee-1l.jpg
-- - honey-lychee-500ml.jpg
-- - honey-lychee-1l.jpg
-- - honey-ginger-candy-200g.jpg
-- - royal-jelly-10g.jpg
-- - bee-pollen-250g.jpg

-- Cách 3: Cập nhật tất cả sản phẩm để sử dụng ảnh placeholder mặc định
-- UPDATE products SET image_url = '/assets/images/property-01.jpg';

-- Kiểm tra các sản phẩm không có ảnh hoặc ảnh không tồn tại
SELECT id, name, image_url 
FROM products 
WHERE image_url IS NULL OR image_url = '';

