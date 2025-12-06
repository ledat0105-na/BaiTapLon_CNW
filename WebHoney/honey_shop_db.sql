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
    lock_reason  VARCHAR(500) NULL COMMENT 'Lý do khóa tài khoản',
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
    best_seller_product_id BIGINT NULL, -- ID sản phẩm bán chạy
    best_seller_image_url VARCHAR(500) NULL, -- Ảnh sản phẩm bán chạy
    best_seller_title VARCHAR(200) NULL, -- Tiêu đề sản phẩm bán chạy
    best_seller_description VARCHAR(500) NULL, -- Mô tả sản phẩm bán chạy
    new_arrival_product_id BIGINT NULL, -- ID sản phẩm mới
    new_arrival_image_url VARCHAR(500) NULL, -- Ảnh sản phẩm mới
    new_arrival_title VARCHAR(200) NULL, -- Tiêu đề sản phẩm mới
    new_arrival_description VARCHAR(500) NULL, -- Mô tả sản phẩm mới
    special_offer_product_id BIGINT NULL, -- ID sản phẩm khuyến mãi
    special_offer_image_url VARCHAR(500) NULL, -- Ảnh khuyến mãi
    special_offer_title VARCHAR(200) NULL, -- Tiêu đề khuyến mãi
    special_offer_description VARCHAR(500) NULL, -- Mô tả khuyến mãi
    banner_slide_interval INT NOT NULL DEFAULT 5000, -- Thời gian chuyển slide banner (mili giây), 0 = không tự động chuyển
    updated_at        DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_home_page_settings_best_seller_product
        FOREIGN KEY (best_seller_product_id) REFERENCES products(id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_home_page_settings_new_arrival_product
        FOREIGN KEY (new_arrival_product_id) REFERENCES products(id)
        ON UPDATE CASCADE ON DELETE SET NULL,
    CONSTRAINT fk_home_page_settings_special_offer_product
        FOREIGN KEY (special_offer_product_id) REFERENCES products(id)
        ON UPDATE CASCADE ON DELETE SET NULL
) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Insert dữ liệu mặc định
INSERT INTO home_page_settings (id, featured_image_url, best_seller_image_url, best_seller_title, best_seller_description, new_arrival_image_url, new_arrival_title, new_arrival_description, special_offer_image_url, special_offer_title, special_offer_description, banner_slide_interval) VALUES
(1, '/assets/images/featured.jpg', '/assets/images/deal-01.jpg', 'Mật Ong Hoa Nhãn', 'Mật ong hoa nhãn với hương vị đặc trưng, ngọt thanh tự nhiên. Sản phẩm được thu hoạch từ các vườn nhãn tại Tây Nguyên, đảm bảo chất lượng và độ tinh khiết cao nhất.', '/assets/images/deal-02.jpg', 'Mật Ong Rừng', 'Mật ong rừng nguyên chất từ các khu rừng nguyên sinh, mang hương vị đậm đà và nhiều dưỡng chất quý giá. Sản phẩm được thu hoạch thủ công, đảm bảo chất lượng cao nhất.', '/assets/images/deal-03.jpg', 'Combo Đặc Biệt', 'Combo đặc biệt với nhiều ưu đãi hấp dẫn. Mua ngay để nhận được giá tốt nhất và quà tặng kèm theo.', 5000);

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

-- 13. BẢNG GIỎ HÀNG CHO KHÁCH HÀNG
CREATE TABLE user_cart_items (
    id            BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id       BIGINT NOT NULL,
    product_id    BIGINT NOT NULL,
    quantity      INT NOT NULL,
    created_at    DATETIME NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_cart_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_cart_product
        FOREIGN KEY (product_id) REFERENCES products(id)
        ON UPDATE CASCADE ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_product_id (product_id)
) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Thêm cột last_login_at vào bảng users (nếu chưa có)
-- Lưu ý: Nếu cột đã tồn tại, sẽ báo lỗi. Bỏ qua lỗi nếu cần.
ALTER TABLE users
ADD COLUMN last_login_at DATETIME NULL AFTER updated_at;

-- Thêm cột avatar_url vào bảng users (nếu chưa có)
ALTER TABLE users
ADD COLUMN avatar_url VARCHAR(255) NULL AFTER last_login_at;

-- Thêm cột rejection_reason vào bảng orders (nếu chưa có)
ALTER TABLE orders
ADD COLUMN rejection_reason VARCHAR(500) NULL AFTER updated_at;

-- BẢNG NOTIFICATIONS (Thông báo)
CREATE TABLE IF NOT EXISTS notifications (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id         BIGINT NULL,  -- NULL = thông báo cho tất cả admin
    title           VARCHAR(255) NOT NULL,
    message         VARCHAR(1000) NULL,
    type            VARCHAR(50) NOT NULL DEFAULT 'INFO', -- INFO, SUCCESS, WARNING, ERROR
    related_id      BIGINT NULL,  -- ID của đơn hàng hoặc entity liên quan
    related_type    VARCHAR(50) NULL, -- ORDER, USER, etc.
    is_read         TINYINT(1) NOT NULL DEFAULT 0,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    read_at         DATETIME NULL,
    CONSTRAINT fk_notifications_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON UPDATE CASCADE ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_is_read (is_read),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- ============================================
-- INSERT DỮ LIỆU MẪU
-- ============================================

-- 1. INSERT USERS (Admin và Customer mẫu)
INSERT INTO users (username, email, password_hash, full_name, phone, role, is_active, created_at) VALUES
('admin', 'admin@honey.com', 'admin123', 'Quản trị viên', '0123456789', 'ADMIN', 1, NOW()),
('admin2', 'admin2@honey.com', '123', 'Quản trị viên 2', '0123456790', 'ADMIN', 1, NOW()),
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

-- 8. INSERT BLOG POSTS (Bài viết/Bản tin)
INSERT INTO blog_posts (author_id, title, slug, thumbnail_url, summary, content, is_published, published_at, created_at) VALUES
(1, 'Lợi ích sức khỏe của mật ong thiên nhiên', 'loi-ich-suc-khoe-cua-mat-ong-thien-nhien', '/assets/images/blog-01.jpg', 'Mật ong thiên nhiên không chỉ là một loại thực phẩm ngọt ngào mà còn mang lại nhiều lợi ích sức khỏe tuyệt vời.', 'Mật ong thiên nhiên là một trong những thực phẩm quý giá từ thiên nhiên, được ong mật tạo ra từ mật hoa của các loài hoa. Mật ong chứa nhiều vitamin, khoáng chất và chất chống oxy hóa, giúp tăng cường hệ miễn dịch, làm đẹp da, hỗ trợ tiêu hóa và cung cấp năng lượng tự nhiên. Sử dụng mật ong đều đặn mỗi ngày sẽ giúp bạn có một sức khỏe tốt hơn.', 1, NOW(), NOW()),

(1, 'Cách phân biệt mật ong thật và mật ong giả', 'cach-phan-biet-mat-ong-that-va-mat-ong-gia', '/assets/images/blog-02.jpg', 'Hướng dẫn các cách đơn giản để nhận biết mật ong nguyên chất và tránh mua phải mật ong pha trộn.', 'Trên thị trường hiện nay có rất nhiều loại mật ong với chất lượng khác nhau. Để phân biệt mật ong thật và mật ong giả, bạn có thể áp dụng một số cách sau: thử nghiệm với nước (mật ong thật sẽ chìm xuống đáy), kiểm tra độ nhớt, mùi thơm tự nhiên, và quan trọng nhất là mua từ các nhà cung cấp uy tín có chứng nhận chất lượng.', 1, DATE_SUB(NOW(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY)),

(1, 'Công thức làm đẹp với mật ong', 'cong-thuc-lam-dep-voi-mat-ong', '/assets/images/blog-03.jpg', 'Mật ong không chỉ tốt cho sức khỏe mà còn là nguyên liệu làm đẹp tự nhiên hiệu quả.', 'Mật ong từ lâu đã được sử dụng trong các công thức làm đẹp tự nhiên. Mật ong có khả năng dưỡng ẩm, làm sáng da, chống lão hóa và kháng khuẩn. Bạn có thể sử dụng mật ong để làm mặt nạ dưỡng da, tẩy tế bào chết, hoặc dưỡng môi. Các công thức đơn giản với mật ong sẽ giúp bạn có làn da khỏe mạnh và tươi trẻ.', 1, DATE_SUB(NOW(), INTERVAL 7 DAY), DATE_SUB(NOW(), INTERVAL 7 DAY)),

(1, 'Mật ong và sức khỏe tim mạch', 'mat-ong-va-suc-khoe-tim-mach', '/assets/images/blog-01.jpg', 'Nghiên cứu cho thấy mật ong có thể hỗ trợ sức khỏe tim mạch một cách tự nhiên.', 'Các nghiên cứu khoa học đã chỉ ra rằng mật ong có thể giúp giảm cholesterol xấu, tăng cholesterol tốt, và cải thiện sức khỏe tim mạch. Mật ong chứa các chất chống oxy hóa giúp bảo vệ tim khỏi các tổn thương do gốc tự do. Tuy nhiên, cần sử dụng mật ong một cách hợp lý và kết hợp với chế độ ăn uống lành mạnh.', 1, DATE_SUB(NOW(), INTERVAL 10 DAY), DATE_SUB(NOW(), INTERVAL 10 DAY)),

(1, 'Bảo quản mật ong đúng cách', 'bao-quan-mat-ong-dung-cach', '/assets/images/blog-02.jpg', 'Hướng dẫn cách bảo quản mật ong để giữ được chất lượng và hương vị tốt nhất.', 'Mật ong có thể bảo quản được rất lâu nếu biết cách. Bạn nên bảo quản mật ong ở nơi khô ráo, thoáng mát, tránh ánh nắng trực tiếp. Không nên để mật ong trong tủ lạnh vì sẽ làm mật ong bị kết tinh. Sử dụng lọ thủy tinh hoặc nhựa chất lượng tốt, đậy kín nắp sau mỗi lần sử dụng. Với cách bảo quản đúng, mật ong có thể giữ được chất lượng trong nhiều năm.', 1, DATE_SUB(NOW(), INTERVAL 14 DAY), DATE_SUB(NOW(), INTERVAL 14 DAY));

-- 9. INSERT CONTACT MESSAGES (Tin nhắn liên hệ)
INSERT INTO contact_messages (user_id, name, email, phone, subject, message, status, created_at) VALUES
(NULL, 'Nguyễn Văn An', 'nguyenvanan@example.com', '0901234567', 'Hỏi về sản phẩm', 'Tôi muốn hỏi về sản phẩm mật ong rừng nguyên chất. Sản phẩm có đảm bảo 100% nguyên chất không?', 'NEW', DATE_SUB(NOW(), INTERVAL 2 DAY)),
(2, 'Nguyễn Văn A', 'customer1@example.com', '0987654321', 'Đặt hàng số lượng lớn', 'Tôi muốn đặt hàng số lượng lớn mật ong hoa nhãn. Có thể có giá ưu đãi không?', 'IN_PROGRESS', DATE_SUB(NOW(), INTERVAL 1 DAY)),
(NULL, 'Trần Thị Bình', 'tranthibinh@example.com', '0912345678', 'Góp ý về dịch vụ', 'Dịch vụ giao hàng của các bạn rất tốt. Tôi rất hài lòng với sản phẩm mật ong. Cảm ơn!', 'RESOLVED', DATE_SUB(NOW(), INTERVAL 5 DAY)),
(NULL, 'Lê Văn Cường', 'levancuong@example.com', '0923456789', 'Hỏi về chính sách đổi trả', 'Tôi muốn biết chính sách đổi trả sản phẩm của cửa hàng như thế nào?', 'NEW', DATE_SUB(NOW(), INTERVAL 3 DAY)),
(3, 'Trần Thị B', 'customer2@example.com', '0912345678', 'Phản hồi tích cực', 'Sản phẩm mật ong của các bạn rất ngon và chất lượng. Tôi sẽ tiếp tục ủng hộ!', 'RESOLVED', DATE_SUB(NOW(), INTERVAL 7 DAY)),
(NULL, 'Phạm Văn Đức', 'phamvanduc@example.com', '0934567890', 'Yêu cầu tư vấn', 'Tôi muốn được tư vấn về cách sử dụng mật ong để tăng cường sức khỏe. Có thể liên hệ lại cho tôi không?', 'IN_PROGRESS', DATE_SUB(NOW(), INTERVAL 1 DAY)),
(NULL, 'Hoàng Thị Lan', 'hoangthilan@example.com', '0945678901', 'Đề xuất sản phẩm mới', 'Tôi đề xuất các bạn nên có thêm sản phẩm mật ong đóng gói nhỏ hơn để tiện mang theo khi đi du lịch.', 'NEW', NOW()),
(NULL, 'Vũ Văn Em', 'vuvanem@example.com', '0956789012', 'Hỏi về xuất xứ', 'Mật ong của các bạn được thu hoạch từ đâu? Có chứng nhận chất lượng không?', 'NEW', DATE_SUB(NOW(), INTERVAL 4 DAY)),
(4, 'Lê Văn C', 'customer3@example.com', '0923456789', 'Cảm ơn', 'Cảm ơn cửa hàng đã cung cấp sản phẩm chất lượng. Tôi rất hài lòng!', 'RESOLVED', DATE_SUB(NOW(), INTERVAL 6 DAY)),
(NULL, 'Đỗ Thị Phương', 'dothiphuong@example.com', '0967890123', 'Hỏi về giá cả', 'Tôi muốn biết giá của các loại mật ong khác nhau. Có thể gửi bảng giá cho tôi không?', 'IN_PROGRESS', DATE_SUB(NOW(), INTERVAL 2 DAY));

-- 10. INSERT FEATURED PRODUCTS (Sản phẩm nổi bật trang chủ)
INSERT INTO featured_products (product_id, image_url, title, subtitle, description, display_order, is_active, created_at) VALUES
(1, '/assets/images/property-01.jpg', 'Mật Ong Rừng Nguyên Chất', 'Mật Ong Rừng', 'Mật ong rừng được thu hoạch từ các tổ ong tự nhiên trong rừng, không qua xử lý, giữ nguyên hương vị đặc trưng và các dưỡng chất quý giá.', 1, 1, NOW()),
(4, '/assets/images/property-01.jpg', 'Mật Ong Hoa Nhãn Thơm Ngọt', 'Mật Ong Hoa Nhãn', 'Mật ong hoa nhãn có màu vàng nhạt, vị ngọt thanh, mùi thơm đặc trưng của hoa nhãn, rất tốt cho sức khỏe.', 2, 1, NOW()),
(6, '/assets/images/property-01.jpg', 'Mật Ong Hoa Cà Phê Đậm Đà', 'Mật Ong Hoa Cà Phê', 'Mật ong hoa cà phê có màu vàng đậm, vị ngọt đậm đà, mùi thơm nồng của hoa cà phê, giàu dinh dưỡng.', 3, 1, NOW()),
(8, '/assets/images/property-01.jpg', 'Mật Ong Hoa Vải Thanh Mát', 'Mật Ong Hoa Vải', 'Mật ong hoa vải có màu vàng trong, vị ngọt thanh mát, mùi thơm nhẹ nhàng của hoa vải, rất dễ uống.', 4, 1, NOW()),
(11, '/assets/images/property-01.jpg', 'Kẹo Mật Ong Gừng', 'Sản Phẩm Từ Mật Ong', 'Kẹo mật ong gừng được làm từ mật ong nguyên chất và gừng tươi, có tác dụng giữ ấm cơ thể, tốt cho tiêu hóa.', 5, 1, NOW()),
(12, '/assets/images/property-01.jpg', 'Sữa Ong Chúa Tươi', 'Sản Phẩm Cao Cấp', 'Sữa ong chúa tươi là thực phẩm bổ dưỡng cao cấp, chứa nhiều vitamin và khoáng chất, tốt cho sức khỏe và làm đẹp.', 6, 1, NOW());

-- 11. INSERT USER CART ITEMS (Giỏ hàng của khách hàng)
INSERT INTO user_cart_items (user_id, product_id, quantity, created_at, updated_at) VALUES
(2, 1, 2, DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY)),
(2, 4, 1, DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY)),
(3, 2, 1, DATE_SUB(NOW(), INTERVAL 2 DAY), DATE_SUB(NOW(), INTERVAL 2 DAY)),
(3, 5, 2, DATE_SUB(NOW(), INTERVAL 2 DAY), DATE_SUB(NOW(), INTERVAL 2 DAY)),
(3, 11, 3, DATE_SUB(NOW(), INTERVAL 2 DAY), DATE_SUB(NOW(), INTERVAL 2 DAY)),
(4, 6, 1, DATE_SUB(NOW(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY)),
(4, 8, 1, DATE_SUB(NOW(), INTERVAL 3 DAY), DATE_SUB(NOW(), INTERVAL 3 DAY)),
(2, 12, 1, NOW(), NOW());


