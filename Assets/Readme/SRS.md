# BÁO CÁO ĐẶC TẢ YÊU CẦU PHẦN MỀM (SRS) - FARM PUZZLE 📑

**Dự án**: Farm Puzzle  
**Đơn vị**: Cao đẳng FPT Polytechnic  
**Nhóm**: 06 Thành viên  

---

## 1. THÀNH VIÊN & PHÂN VAI (TEAM ROLES)

Dựa trên thực tế các nhánh phát triển (Git Branches), nhóm được phân chia như sau:

1.  **Phạm Đức Dương (feat/Duong-CoreData)**: Backend & Database. Chịu trách nhiệm SQLite, DataManager và Persistence.
2.  **Hào Anh (feat/HaoAnh-ShopSystem)**: Dev - Shop & Decor. Xây dựng hệ thống cửa hàng và trang trí.
3.  **Quyết (feat/Quyet-LandPuzzle)**: Dev - Minigame Mở đất. Chịu trách nhiệm logic Block Puzzle và Sát thương vật cản.
4.  **Vũ (feat/Vu-FarmSystem)**: Dev - Farm & Grid. Xây dựng Grid 50x50, logic Cây trồng và Năng lượng.
5.  **Vương (feat/Vuong-Tertris)**: Dev - Minigame Tetris. Chịu trách nhiệm logic sản xuất và đơn hàng.
6.  **Vy (feat/Vy-QuestSystem)**: Dev - Meta & Audio. Xây dựng hệ thống Nhiệm vụ và quản lý Âm thanh.

---

## 2. PHÂN HỆ CHỨC NĂNG (MODULES)

Dự án được chia làm 6 module hoạt động độc lập, giao tiếp qua Event System:

- **Module 1: Core & Data**: Quản lý vòng đời ứng dụng và lưu trữ SQLite.
- **Module 2: Farm System**: Logic trồng trọt Isometric, Y-Sorting, và tính toán Offline Progress.
- **Module 3: Land Puzzle**: Cơ chế kéo thả khối, Check hàng/cột và phá hủy Obstacle.
- **Module 4: Tetris Puzzle**: Cơ chế Tetris cổ điển tích hợp hệ thống hoàn thành đơn hàng.
- **Module 5: Economy & Decor**: Hệ thống mua bán, kho đồ (Inventory) và trình điều khiển xây dựng (Placement).
- **Module 6: Meta & Audio**: Hệ thống nhiệm vụ (Quests) và Mixer âm thanh.

---

## 3. ĐẶC TẢ YÊU CẦU CHỨC NĂNG (FR)

### 3.1. Module Nông trại
- **FR-1.1**: Gieo hạt và thu hoạch.
- **FR-1.2**: Tự động tính toán tăng trưởng khi người chơi Offline.
- **FR-1.3**: Hiển thị thanh tiến trình (ProgressBar) theo thời gian thực cho cây.

### 3.2. Module Mở đất (Land Puzzle)
- **FR-2.1**: Sinh khối gạch ngẫu nhiên từ thư viện `ShapeData`.
- **FR-2.2**: Gây sát thương vật cản dựa trên tọa độ hàng/cột bị xóa.
- **FR-2.3**: Quản lý Năng lượng tiêu hao mỗi lượt chơi.

### 3.3. Module Xuất hàng (Tetris)
- **FR-3.1**: Điều khiển khối rơi (L-R-Rotate-SoftDrop).
- **FR-3.2**: Đối chiếu hàng bị xóa với danh sách `ACTIVE_ORDER`.
- **FR-3.3**: Cộng tiền thưởng khi hoàn thành mục tiêu đơn hàng.

---

## 4. YÊU CẦU PHI CHỨC NĂNG (NFR)

- **NFR-1 (Hiệu năng)**: Đảm bảo FPS ổn định trên Mobile thông qua kỹ thuật **Viewport Culling** (chỉ render ô đất trong tầm nhìn).
- **NFR-2 (Bảo mật)**: Mã hóa tệp Save JSON hoặc DB bằng Base64 cơ bản để hạn chế can thiệp thủ công.
- **NFR-3 (Tính ổn định)**: Hệ thống SQLite phải tự động Commit dữ liệu sau các hành động quan trọng để tránh mất mát khi Crash.
- **NFR-4 (Quy trình)**: Tuân thủ nghiêm ngặt **Git Flow** (Feature branching -> Merge Request).

---
