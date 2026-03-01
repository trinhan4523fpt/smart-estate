# SmartEstate

## Chạy Nhanh
- Yêu cầu: Docker + Docker Compose.
- Tại thư mục gốc:
  - `docker compose up --build`
  - Swagger: http://localhost:8080/swagger

Kết nối DB (nếu cần xem bằng SSMS):
- Server: `localhost,14333`
- User: `sa`
- Password: `Your_password123`
- Database: `SmartEstate`

## Tài Khoản Mẫu
- Admin
  - Email: `admin@local`, Password: `Admin123!`
- Broker
  - Email: `broker@local`, Password: `Broker123!`
- Seller/User
  - Email: `seller@local`, Password: `Seller123!`
- User 1 (có 50 điểm)
  - Email: `user1@local`, Password: `User123!`
- User 2 (có 5 điểm)
  - Email: `user2@local`, Password: `User123!`

Roles:
- Mặc định đăng ký là `User`. Broker lên vai trò qua duyệt hồ sơ (Admin duyệt, trừ 60 điểm).

## Luồng Chính

### Auth & Profile
- Đăng ký: `POST /api/auth/register`
- Đăng nhập: `POST /api/auth/login` → trả JWT có claim Role.
- Hồ sơ: `GET/PUT /api/users/me`

### Listing & Moderation & Điểm
- Tạo listing: `POST /api/listings` (User/Broker/Admin).
  - Hệ thống trừ **1 điểm** ngay (TxType: `SPEND_POST`). Không đủ điểm → lỗi `INSUFFICIENT_POINTS`.
  - Hệ thống chạy AI moderation, kết quả: AUTO_APPROVE / AUTO_REJECT / NEED_REVIEW.
- Duyệt (Admin):
  - `POST /api/moderation/{id}/approve` → không trừ điểm thêm (đã trừ trước).
  - `POST /api/moderation/{id}/reject` → **hoàn 1 điểm** (TxType: `REFUND_POST`).

### Points & Payment
- Danh sách gói: `GET /api/points/packages`
- Mua gói: `POST /api/points/purchases { pointPackageId }`
  - Trả `purchaseId`, `paymentId`, `provider`, `payUrl` (VNPay giả lập).
  - Xác nhận thanh toán thành công (giả lập): `POST /api/payments/points/{paymentId}/paid`
  - Cộng điểm vĩnh viễn (TxType: `PURCHASE_POINTS`).

### Takeover
- Tạo request (User/Admin): `POST /api/takeovers`
  - **Trừ 30 điểm** ngay (TxType: `SPEND_TAKEOVER`). Không đủ → `INSUFFICIENT_POINTS`.
- Broker quyết định: `POST /api/takeovers/{id}/decide { accept }`
  - `accept=false` → **hoàn 30 điểm** (TxType: `REFUND_TAKEOVER`).
  - `accept=true` → hoàn tất takeover, gán broker quản lý listing.

### Boost & Search
- Boost: `POST /api/listings/{id}/boost` (User/Broker/Admin) → **trừ 10 điểm**/7 ngày, chống trùng boost đang hoạt động.
- Tìm kiếm: `GET /api/search/listings?...`
  - Ưu tiên nhóm listing đang boost lên đầu; trong mỗi nhóm áp sort (mặc định mới nhất/giá).

### Chat
- Tin nhắn theo Conversation; đánh dấu đã đọc theo buyer/responsible.

## Admin
- Báo cáo doanh thu (điểm): `GET /api/admin/payments/point-purchases?from=...&to=...`
- Gói điểm:
  - `GET /api/admin/point-packages`
  - `POST /api/admin/point-packages`
  - `PUT /api/admin/point-packages/{id}`
  - `PATCH /api/admin/point-packages/{id}/active?isActive=true|false`
- Người dùng:
  - `GET /api/admin/users?isActive=`
  - `PATCH /api/admin/users/{id}/active?isActive=`
- Reports:
  - User gửi report: `POST /api/listings/{listingId}/reports { reason, detail? }`
  - Admin xem: `GET /api/admin/reports/listings?isResolved=`
  - Admin xử lý: `POST /api/admin/reports/listings/{id}/resolve { resolutionNote }`

## Payment Provider (VNPay Stub)
- Hệ thống đang dùng VNPay stub cho dev:
  - Khi tạo purchase, server sinh `providerRef` và `payUrl` giả.
  - FE giả lập thanh toán thành công bằng cách gọi `POST /api/payments/points/{paymentId}/paid`.

## Docker
- File: `docker-compose.yml` gồm:
  - `sqlserver`: MSSQL 2022 (cổng 14333)
  - `api`: SmartEstate.Api (cổng 8080/8081), tự migrate & seed.

## Ghi Chú
- Điểm có 2 bucket: Monthly & Permanent; ledger lưu `Bucket/MonthKey/TxType`.
- TxType chính: `SPEND_POST`, `REFUND_POST`, `SPEND_TAKEOVER`, `REFUND_TAKEOVER`, `PURCHASE_POINTS`, `GRANT_MONTHLY`.
