-- Script tạo bảng home_page_settings nếu chưa tồn tại
CREATE TABLE IF NOT EXISTS home_page_settings (
    id                INT PRIMARY KEY DEFAULT 1,
    featured_image_url VARCHAR(500) NULL,
    updated_at        DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Insert dữ liệu mặc định nếu chưa có
INSERT INTO home_page_settings (id, featured_image_url) 
VALUES (1, '/assets/images/featured.jpg')
ON DUPLICATE KEY UPDATE featured_image_url = featured_image_url;

