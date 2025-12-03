# Hướng dẫn cấu hình OAuth (Google & Facebook)

## Cấu hình Google OAuth

### Bước 1: Tạo OAuth 2.0 Client ID trong Google Cloud Console

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo một project mới hoặc chọn project hiện có
3. Vào **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth client ID**
5. Nếu chưa có, bạn sẽ cần cấu hình OAuth consent screen trước:
   - Chọn **External** (cho testing) hoặc **Internal** (cho G Suite)
   - Điền thông tin: App name, User support email, Developer contact email
   - Thêm scopes: `email`, `profile`
   - Thêm test users (nếu chọn External)
6. Tạo OAuth client ID:
   - Application type: **Web application**
   - Name: Tên ứng dụng của bạn
   - Authorized redirect URIs: 
     - `https://localhost:5001/Account/GoogleCallback` (cho development)
     - `https://yourdomain.com/Account/GoogleCallback` (cho production)
7. Copy **Client ID** và **Client Secret**

### Bước 2: Cập nhật appsettings.json

Mở file `appsettings.json` và cập nhật:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_ACTUAL_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_ACTUAL_CLIENT_SECRET"
    }
  }
}
```

## Cấu hình Facebook OAuth

### Bước 1: Tạo Facebook App

1. Truy cập [Facebook Developers](https://developers.facebook.com/)
2. Click **My Apps** > **Create App**
3. Chọn **Consumer** hoặc **Business**
4. Điền thông tin app: App Name, Contact Email
5. Vào **Settings** > **Basic**
6. Thêm **Facebook Login** product:
   - Vào **Products** > **Facebook Login** > **Set Up**
   - Chọn **Web**
7. Cấu hình **Valid OAuth Redirect URIs**:
   - `https://localhost:5001/Account/FacebookCallback` (cho development)
   - `https://yourdomain.com/Account/FacebookCallback` (cho production)
8. Copy **App ID** và **App Secret** từ Settings > Basic

### Bước 2: Cập nhật appsettings.json

```json
{
  "Authentication": {
    "Facebook": {
      "AppId": "YOUR_ACTUAL_APP_ID",
      "AppSecret": "YOUR_ACTUAL_APP_SECRET"
    }
  }
}
```

## Lưu ý quan trọng

1. **Development (localhost)**:
   - Google: Sử dụng `https://localhost:5001` hoặc port mà bạn đang chạy
   - Facebook: Cần thêm localhost vào Valid OAuth Redirect URIs

2. **Production**:
   - Thay thế `yourdomain.com` bằng domain thực tế của bạn
   - Đảm bảo domain đã được verify trong Google/Facebook console

3. **Security**:
   - **KHÔNG** commit file `appsettings.json` có chứa Client Secret/App Secret vào Git
   - Sử dụng `appsettings.Development.json` cho development
   - Sử dụng Environment Variables hoặc Azure Key Vault cho production

4. **Testing**:
   - Sau khi cấu hình, restart ứng dụng
   - Thử đăng nhập bằng Google/Facebook
   - Nếu vẫn lỗi, kiểm tra:
     - ClientId/AppId và Secret có đúng không
     - Redirect URI có khớp với cấu hình không
     - OAuth consent screen đã được publish chưa (Google)

## Troubleshooting

### Lỗi "OAuth client was not found" hoặc "invalid_client"
- Kiểm tra ClientId và ClientSecret có đúng không
- Đảm bảo đã copy đầy đủ (không thiếu ký tự)
- Kiểm tra redirect URI có khớp với cấu hình trong console

### Lỗi "redirect_uri_mismatch"
- Kiểm tra redirect URI trong appsettings.json
- Đảm bảo đã thêm đúng URI vào Authorized redirect URIs trong Google/Facebook console
- URI phải khớp chính xác (bao gồm http/https, port, path)

### Lỗi "access_denied"
- Kiểm tra OAuth consent screen đã được cấu hình đúng chưa
- Đảm bảo đã thêm test users (nếu dùng External app type)
