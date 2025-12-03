-- Tạo database
DROP DATABASE IF EXISTS honey_shop_db;
CREATE DATABASE honey_shop_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE honey_shop_db;

-- 1. BẢNG USERS (Tài khoản đăng nhập: Admin / Customer)
CREATE TABLE users (
    id           BIGINT AUTO_INCREMENT PRIMARY KEY,
    username     VARCHAR(100) NULL, -- Tên đăng nhập (cho phép đăng nhập bằng username hoặc email)
    email        VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NULL, -- NULL cho phép user đăng nhập qua OAuth (nếu có)
    external_provider VARCHAR(255) NULL, -- "Google", "Facebook" (nếu có)
    external_id  VARCHAR(255) NULL, -- ID từ Google/Facebook (nếu có)
    full_name    VARCHAR(150) NOT NULL,
    phone        VARCHAR(20),
    role         ENUM('ADMIN', 'CUSTOMER') NOT NULL DEFAULT 'CUSTOMER',
    is_active    TINYINT(1) NOT NULL DEFAULT 1,
    created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at   DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_username (username),
    INDEX idx_email (email)
) ENGINE=InnoDB;

-- 2. BẢNG CUSTOMERS (Hồ sơ khách hàng – có thể tạo nhanh tại quầy/offline)
CREATE TABLE customers (
    id            BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id       BIGINT NULL, -- customer online (có tài khoản)
    full_name     VARCHAR(150) NOT NULL,
    phone         VARCHAR(20) NOT NULL,
    email         VARCHAR(150),
    address       VARCHAR(255),
    is_active     TINYINT(1) NOT NULL DEFAULT 1,
    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_customers_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB;

-- 3. BẢNG DANH MỤC SẢN PHẨM
CREATE TABLE categories (
    id          BIGINT AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    slug        VARCHAR(120) NOT NULL UNIQUE,
    description TEXT,
    is_active   TINYINT(1) NOT NULL DEFAULT 1,
    created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- 4. BẢNG SẢN PHẨM MẬT ONG
CREATE TABLE products (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    category_id     BIGINT NOT NULL,
    name            VARCHAR(150) NOT NULL,
    slug            VARCHAR(180) NOT NULL UNIQUE,
    short_desc      VARCHAR(255),
    description     TEXT,
    origin          VARCHAR(150),      -- nguồn gốc (VD: Mật ong rừng Tây Bắc)
    volume          DECIMAL(10,2),     -- dung tích, ví dụ: 500.00
    unit            VARCHAR(50),       -- ml, l, gram...
    price           DECIMAL(15,2) NOT NULL,
    image_url       VARCHAR(255),
    stock_quantity  INT NOT NULL DEFAULT 0,
    is_active       TINYINT(1) NOT NULL DEFAULT 1,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_products_category
        FOREIGN KEY (category_id) REFERENCES categories(id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

-- 5. BẢNG MÃ KHUYẾN MÃI (COUPONS)
CREATE TABLE coupons (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    code            VARCHAR(50) NOT NULL UNIQUE,
    description     VARCHAR(255),
    discount_type   ENUM('PERCENT', 'AMOUNT') NOT NULL, -- % hoặc số tiền
    discount_value  DECIMAL(15,2) NOT NULL,
    min_order_value DECIMAL(15,2) DEFAULT 0,
    start_date      DATE NOT NULL,
    end_date        DATE NOT NULL,
    max_uses        INT DEFAULT NULL,      -- tổng số lần được dùng (nếu có)
    used_count      INT NOT NULL DEFAULT 0,
    is_active       TINYINT(1) NOT NULL DEFAULT 1,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- 6. BẢNG ĐƠN HÀNG
CREATE TABLE orders (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id         BIGINT NULL,  -- customer có tài khoản
    customer_id     BIGINT NULL,  -- hồ sơ khách hàng (kể cả mua tại quầy / khách vãng lai)
    full_name       VARCHAR(150) NOT NULL, -- snapshot thông tin lúc đặt đơn
    phone           VARCHAR(20) NOT NULL,
    address         VARCHAR(255) NOT NULL,
    status          ENUM('PENDING','PROCESSING','SHIPPING','COMPLETED','CANCELED') 
                        NOT NULL DEFAULT 'PENDING',
    payment_method  ENUM('COD','BANK_TRANSFER') NOT NULL DEFAULT 'COD',
    coupon_id       BIGINT NULL,
    subtotal_amount DECIMAL(15,2) NOT NULL DEFAULT 0, -- tổng trước giảm
    discount_amount DECIMAL(15,2) NOT NULL DEFAULT 0, -- tổng tiền giảm
    total_amount    DECIMAL(15,2) NOT NULL DEFAULT 0, -- số tiền khách phải trả
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_orders_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_orders_customer
        FOREIGN KEY (customer_id) REFERENCES customers(id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_orders_coupon
        FOREIGN KEY (coupon_id) REFERENCES coupons(id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB;

-- 7. BẢNG CHI TIẾT ĐƠN HÀNG
CREATE TABLE order_items (
    id           BIGINT AUTO_INCREMENT PRIMARY KEY,
    order_id     BIGINT NOT NULL,
    product_id   BIGINT NOT NULL,
    product_name VARCHAR(150) NOT NULL, -- snapshot tên SP
    unit_price   DECIMAL(15,2) NOT NULL, -- snapshot giá lúc bán
    quantity     INT NOT NULL,
    line_total   DECIMAL(15,2) NOT NULL, -- unit_price * quantity
    CONSTRAINT fk_order_items_order
        FOREIGN KEY (order_id) REFERENCES orders(id)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_order_items_product
        FOREIGN KEY (product_id) REFERENCES products(id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

-- 8. BẢN TIN / BLOG POSTS
CREATE TABLE blog_posts (
    id           BIGINT AUTO_INCREMENT PRIMARY KEY,
    author_id    BIGINT NULL, -- user (admin) viết bài
    title        VARCHAR(200) NOT NULL,
    slug         VARCHAR(220) NOT NULL UNIQUE,
    thumbnail_url VARCHAR(255),
    summary      VARCHAR(255),
    content      TEXT NOT NULL,
    is_published TINYINT(1) NOT NULL DEFAULT 0,
    published_at DATETIME NULL,
    created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at   DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_blog_author
        FOREIGN KEY (author_id) REFERENCES users(id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB;

-- 9. BẢNG FORM LIÊN HỆ / GÓP Ý
CREATE TABLE contact_messages (
    id          BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id     BIGINT NULL, -- nếu khách đã đăng nhập
    name        VARCHAR(150) NOT NULL,
    email       VARCHAR(150),
    phone       VARCHAR(20),
    subject     VARCHAR(200),
    message     TEXT NOT NULL,
    status      ENUM('NEW','IN_PROGRESS','RESOLVED') NOT NULL DEFAULT 'NEW',
    created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at  DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_contact_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB;

-- 10. BẢNG BANNER HÌNH ẢNH TRANG CHỦ
CREATE TABLE banner_images (
    id            BIGINT AUTO_INCREMENT PRIMARY KEY,
    image_url     VARCHAR(500) NOT NULL,
    title         VARCHAR(200) NULL,
    subtitle      VARCHAR(200) NULL,
    display_order INT NOT NULL DEFAULT 0,
    is_active     TINYINT(1) NOT NULL DEFAULT 1,
    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_display_order (display_order),
    INDEX idx_is_active (is_active)
) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Insert dữ liệu mẫu cho banner_images (3 banner mặc định)
INSERT INTO banner_images (image_url, title, subtitle, display_order, is_active) VALUES
('/assets/images/banner-01.jpg', 'Chất lượng cao!', 'Mật ong thiên nhiên tốt nhất cho bạn', 1, 1),
('/assets/images/banner-02.jpg', 'Nhanh tay!', 'Mật ong nguyên chất tốt nhất', 2, 1),
('/assets/images/banner-03.jpg', 'Đặt ngay!', 'Mật ong hảo hạng chất lượng cao', 3, 1);

-- 11. BẢNG CÀI ĐẶT TRANG CHỦ
CREATE TABLE home_page_settings (
    id                INT PRIMARY KEY DEFAULT 1,
    featured_image_url VARCHAR(500) NULL, -- Ảnh bên trái phần giới thiệu
    updated_at        DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Insert dữ liệu mặc định
INSERT INTO home_page_settings (id, featured_image_url) VALUES
(1, '/assets/images/featured.jpg');

-- 12. BẢNG SẢN PHẨM NỔI BẬT TRANG CHỦ (từ phần "We Provide The Best Property You Like" trở xuống)
CREATE TABLE featured_products (
    id            BIGINT AUTO_INCREMENT PRIMARY KEY,
    product_id    BIGINT NULL, -- Liên kết với sản phẩm (nếu có)
    image_url     VARCHAR(500) NULL, -- Ảnh giới thiệu (có thể upload hoặc lấy từ sản phẩm)
    title         VARCHAR(200) NULL, -- Tiêu đề
    subtitle      VARCHAR(200) NULL, -- Phụ đề
    description   VARCHAR(500) NULL, -- Mô tả
    display_order INT NOT NULL DEFAULT 0, -- Thứ tự hiển thị
    is_active     TINYINT(1) NOT NULL DEFAULT 1, -- Kích hoạt
    created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_featured_product
        FOREIGN KEY (product_id) REFERENCES products(id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    INDEX idx_display_order (display_order),
    INDEX idx_is_active (is_active)
) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- ============================================
-- INSERT DỮ LIỆU MẪU
-- ============================================

-- 1. INSERT USERS (Admin và Customer mẫu)
INSERT INTO users (username, email, password_hash, full_name, phone, role, is_active, created_at) VALUES
('admin', 'admin@honey.com', 'admin123', 'Quản trị viên', '0123456789', 'ADMIN', 1, NOW()),
('customer1', 'customer1@example.com', '123456', 'Nguyễn Văn A', '0987654321', 'CUSTOMER', 1, NOW()),
('customer2', 'customer2@example.com', '123456', 'Trần Thị B', '0912345678', 'CUSTOMER', 1, NOW()),
('customer3', 'customer3@example.com', '123456', 'Lê Văn C', '0923456789', 'CUSTOMER', 1, NOW());

-- 2. INSERT CATEGORIES (Danh mục sản phẩm)
INSERT INTO categories (name, slug, description, is_active, created_at) VALUES
('Mật ong rừng', 'mat-ong-rung', 'Mật ong nguyên chất từ rừng tự nhiên', 1, NOW()),
('Mật ong hoa nhãn', 'mat-ong-hoa-nhan', 'Mật ong từ hoa nhãn thơm ngon', 1, NOW()),
('Mật ong hoa cà phê', 'mat-ong-hoa-ca-phe', 'Mật ong từ hoa cà phê đặc biệt', 1, NOW()),
('Mật ong hoa vải', 'mat-ong-hoa-vai', 'Mật ong từ hoa vải ngọt ngào', 1, NOW()),
('Sản phẩm từ mật ong', 'san-pham-tu-mat-ong', 'Các sản phẩm chế biến từ mật ong', 1, NOW());

-- 3. INSERT PRODUCTS (Sản phẩm mật ong)
INSERT INTO products (category_id, name, slug, short_desc, description, origin, volume, unit, price, image_url, stock_quantity, is_active, created_at) VALUES
-- Mật ong rừng
(1, 'Mật ong rừng nguyên chất 500ml', 'mat-ong-rung-nguyen-chat-500ml', 'Mật ong rừng tự nhiên 100% nguyên chất', 'Mật ong rừng được thu hoạch từ các tổ ong tự nhiên trong rừng, không qua xử lý, giữ nguyên hương vị đặc trưng và các dưỡng chất quý giá.', 'Tây Nguyên', 500.00, 'ml', 250000, '/uploads/products/honey-forest-500ml.jpg', 50, 1, NOW()),
(1, 'Mật ong rừng nguyên chất 1 lít', 'mat-ong-rung-nguyen-chat-1-lit', 'Mật ong rừng tự nhiên 100% nguyên chất', 'Mật ong rừng được thu hoạch từ các tổ ong tự nhiên trong rừng, không qua xử lý, giữ nguyên hương vị đặc trưng và các dưỡng chất quý giá.', 'Tây Nguyên', 1000.00, 'ml', 450000, '/uploads/products/honey-forest-1l.jpg', 30, 1, NOW()),
(1, 'Mật ong rừng cao cấp 250ml', 'mat-ong-rung-cao-cap-250ml', 'Mật ong rừng cao cấp đóng chai thủy tinh', 'Mật ong rừng cao cấp được đóng trong chai thủy tinh, giữ nguyên chất lượng và hương vị tự nhiên.', 'Tây Bắc', 250.00, 'ml', 180000, '/uploads/products/honey-forest-premium-250ml.jpg', 25, 1, NOW()),

-- Mật ong hoa nhãn
(2, 'Mật ong hoa nhãn 500ml', 'mat-ong-hoa-nhan-500ml', 'Mật ong từ hoa nhãn thơm ngon', 'Mật ong hoa nhãn có màu vàng nhạt, vị ngọt thanh, mùi thơm đặc trưng của hoa nhãn, rất tốt cho sức khỏe.', 'Hưng Yên', 500.00, 'ml', 200000, '/uploads/products/honey-longan-500ml.jpg', 40, 1, NOW()),
(2, 'Mật ong hoa nhãn 1 lít', 'mat-ong-hoa-nhan-1-lit', 'Mật ong từ hoa nhãn thơm ngon', 'Mật ong hoa nhãn có màu vàng nhạt, vị ngọt thanh, mùi thơm đặc trưng của hoa nhãn, rất tốt cho sức khỏe.', 'Hưng Yên', 1000.00, 'ml', 380000, '/uploads/products/honey-longan-1l.jpg', 20, 1, NOW()),

-- Mật ong hoa cà phê
(3, 'Mật ong hoa cà phê 500ml', 'mat-ong-hoa-ca-phe-500ml', 'Mật ong từ hoa cà phê đặc biệt', 'Mật ong hoa cà phê có màu vàng đậm, vị ngọt đậm đà, mùi thơm nồng của hoa cà phê, giàu dinh dưỡng.', 'Đắk Lắk', 500.00, 'ml', 220000, '/uploads/products/honey-coffee-500ml.jpg', 35, 1, NOW()),
(3, 'Mật ong hoa cà phê 1 lít', 'mat-ong-hoa-ca-phe-1-lit', 'Mật ong từ hoa cà phê đặc biệt', 'Mật ong hoa cà phê có màu vàng đậm, vị ngọt đậm đà, mùi thơm nồng của hoa cà phê, giàu dinh dưỡng.', 'Đắk Lắk', 1000.00, 'ml', 400000, '/uploads/products/honey-coffee-1l.jpg', 15, 1, NOW()),

-- Mật ong hoa vải
(4, 'Mật ong hoa vải 500ml', 'mat-ong-hoa-vai-500ml', 'Mật ong từ hoa vải ngọt ngào', 'Mật ong hoa vải có màu vàng trong, vị ngọt thanh mát, mùi thơm nhẹ nhàng của hoa vải, rất dễ uống.', 'Bắc Giang', 500.00, 'ml', 210000, '/uploads/products/honey-lychee-500ml.jpg', 30, 1, NOW()),
(4, 'Mật ong hoa vải 1 lít', 'mat-ong-hoa-vai-1-lit', 'Mật ong từ hoa vải ngọt ngào', 'Mật ong hoa vải có màu vàng trong, vị ngọt thanh mát, mùi thơm nhẹ nhàng của hoa vải, rất dễ uống.', 'Bắc Giang', 1000.00, 'ml', 390000, '/uploads/products/honey-lychee-1l.jpg', 18, 1, NOW()),

-- Sản phẩm từ mật ong
(5, 'Kẹo mật ong gừng 200g', 'keo-mat-ong-gung-200g', 'Kẹo mật ong gừng thơm ngon', 'Kẹo mật ong gừng được làm từ mật ong nguyên chất và gừng tươi, có tác dụng giữ ấm cơ thể, tốt cho tiêu hóa.', 'Việt Nam', 200.00, 'g', 85000, '/uploads/products/honey-ginger-candy-200g.jpg', 60, 1, NOW()),
(5, 'Sữa ong chúa tươi 10g', 'sua-ong-chua-tuoi-10g', 'Sữa ong chúa tươi nguyên chất', 'Sữa ong chúa tươi là thực phẩm bổ dưỡng cao cấp, chứa nhiều vitamin và khoáng chất, tốt cho sức khỏe và làm đẹp.', 'Việt Nam', 10.00, 'g', 150000, '/uploads/products/royal-jelly-10g.jpg', 25, 1, NOW()),
(5, 'Phấn hoa ong 250g', 'phan-hoa-ong-250g', 'Phấn hoa ong nguyên chất', 'Phấn hoa ong là nguồn protein tự nhiên, giàu vitamin và khoáng chất, tăng cường sức đề kháng.', 'Việt Nam', 250.00, 'g', 180000, '/uploads/products/bee-pollen-250g.jpg', 20, 1, NOW());

-- 4. INSERT CUSTOMERS (Khách hàng mẫu)
INSERT INTO customers (user_id, full_name, phone, email, address, is_active, created_at) VALUES
(2, 'Nguyễn Văn A', '0987654321', 'customer1@example.com', '123 Đường ABC, Phường XYZ, Quận 1, TP.HCM', 1, NOW()),
(3, 'Trần Thị B', '0912345678', 'customer2@example.com', '456 Đường DEF, Phường UVW, Quận 2, TP.HCM', 1, NOW()),
(4, 'Lê Văn C', '0923456789', 'customer3@example.com', '789 Đường GHI, Phường RST, Quận 3, TP.HCM', 1, NOW()),
(NULL, 'Phạm Thị D', '0934567890', 'customer4@example.com', '321 Đường JKL, Phường MNO, Quận 4, TP.HCM', 1, NOW()),
(NULL, 'Hoàng Văn E', '0945678901', 'customer5@example.com', '654 Đường PQR, Phường STU, Quận 5, TP.HCM', 1, NOW());

-- 5. INSERT COUPONS (Mã giảm giá)
INSERT INTO coupons (code, description, discount_type, discount_value, min_order_value, start_date, end_date, max_uses, used_count, is_active, created_at) VALUES
('WELCOME10', 'Giảm 10% cho khách hàng mới', 'PERCENT', 10.00, 200000, CURDATE(), DATE_ADD(CURDATE(), INTERVAL 30 DAY), 100, 0, 1, NOW()),
('SAVE50K', 'Giảm 50.000đ cho đơn hàng từ 500.000đ', 'AMOUNT', 50000.00, 500000, CURDATE(), DATE_ADD(CURDATE(), INTERVAL 60 DAY), 50, 0, 1, NOW()),
('HONEY20', 'Giảm 20% cho sản phẩm mật ong', 'PERCENT', 20.00, 300000, CURDATE(), DATE_ADD(CURDATE(), INTERVAL 45 DAY), NULL, 0, 1, NOW()),
('VIP100K', 'Giảm 100.000đ cho đơn hàng từ 1.000.000đ', 'AMOUNT', 100000.00, 1000000, CURDATE(), DATE_ADD(CURDATE(), INTERVAL 90 DAY), 20, 0, 1, NOW());

-- 6. INSERT ORDERS (Đơn hàng mẫu)
INSERT INTO orders (user_id, customer_id, full_name, phone, address, status, payment_method, coupon_id, subtotal_amount, discount_amount, total_amount, created_at) VALUES
(2, 1, 'Nguyễn Văn A', '0987654321', '123 Đường ABC, Phường XYZ, Quận 1, TP.HCM', 'COMPLETED', 'COD', 1, 450000, 45000, 405000, DATE_SUB(NOW(), INTERVAL 5 DAY)),
(3, 2, 'Trần Thị B', '0912345678', '456 Đường DEF, Phường UVW, Quận 2, TP.HCM', 'SHIPPING', 'BANK_TRANSFER', NULL, 600000, 0, 600000, DATE_SUB(NOW(), INTERVAL 2 DAY)),
(4, 3, 'Lê Văn C', '0923456789', '789 Đường GHI, Phường RST, Quận 3, TP.HCM', 'PROCESSING', 'COD', NULL, 380000, 0, 380000, DATE_SUB(NOW(), INTERVAL 1 DAY)),
(NULL, 4, 'Phạm Thị D', '0934567890', '321 Đường JKL, Phường MNO, Quận 4, TP.HCM', 'PENDING', 'COD', 2, 550000, 50000, 500000, NOW());

-- 7. INSERT ORDER_ITEMS (Chi tiết đơn hàng)
INSERT INTO order_items (order_id, product_id, product_name, unit_price, quantity, line_total) VALUES
-- Đơn hàng 1
(1, 1, 'Mật ong rừng nguyên chất 500ml', 250000, 1, 250000),
(1, 4, 'Mật ong hoa nhãn 500ml', 200000, 1, 200000),

-- Đơn hàng 2
(2, 2, 'Mật ong rừng nguyên chất 1 lít', 450000, 1, 450000),
(2, 5, 'Mật ong hoa nhãn 1 lít', 380000, 1, 380000),
(2, 11, 'Kẹo mật ong gừng 200g', 85000, 2, 170000),

-- Đơn hàng 3
(3, 6, 'Mật ong hoa cà phê 500ml', 220000, 1, 220000),
(3, 8, 'Mật ong hoa vải 500ml', 210000, 1, 210000),

-- Đơn hàng 4
(4, 3, 'Mật ong rừng cao cấp 250ml', 180000, 1, 180000),
(4, 7, 'Mật ong hoa cà phê 1 lít', 400000, 1, 400000),
(4, 12, 'Sữa ong chúa tươi 10g', 150000, 1, 150000);
