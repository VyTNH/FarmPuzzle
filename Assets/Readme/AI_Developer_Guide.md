# 🤖 AI DEVELOPER GUIDE: FARM PUZZLE ARCHITECTURE

Tài liệu này được biên soạn để các AI Agent (như Gemini) có thể nhanh chóng nắm bắt "linh hồn" kỹ thuật của dự án mà không cần duyệt qua từng file mã nguồn.

---

## 1. TRIẾT LÝ CỐT LÕI (CORE PRINCIPLE)
Dự án được xây dựng theo mô hình **Data-Driven Architecture**. 
- **Source of Truth**: Mọi trạng thái của game (ô đất, kho đồ, năng lượng, tiến độ nhiệm vụ) đều được lưu trữ trong **SQLite Database** (`DatabaseModels.cs`).
- **State vs. View**: Các Script (như `GridManager`, `CropLogic`) chỉ là các "View" hiển thị hoặc xử lý logic dựa trên dữ liệu từ Database. Nếu dữ liệu thay đổi trong DB, View sẽ được cập nhật tương ứng.

---

## 2. KIẾN TRÚC DỮ LIỆU (DATA FLOW)
### 🗄️ Database Layer (`Core/DatabaseModels.cs`)
Sử dụng **SQLite-net** để ánh xạ các Class C# thành bảng. Các bảng quan trọng:
- `PLAYER`: Tiền, EXP, ID người chơi.
- `INVENTORY`: Số lượng vật phẩm theo ItemID.
- `FARM_TILE`: Trạng thái gieo hạt, loại hạt, thời gian gieo (Ticks).
- `DECOR_RECORD`: Vị trí GridX, GridY của các đồ trang trí đã đặt.

### 💾 Persistence Layer (`Core/DataManager.cs`)
Chịu trách nhiệm mở kết nối DB, thực hiện các câu lệnh SQL (Insert/Update/Query).
- **Session Management**: Dữ liệu thường được cache trong RAM và commit xuống SQLite khi có sự kiện quan trọng (Planting, Harvesting, Game Quit).

---

## 3. LUỒNG KHỞI TẠO (INITIALIZATION FLOW)
Khi một Scene được tải (ví dụ `01_MainFarm`):
1. **`GameManager`**: Khởi tạo các Singleton lõi.
2. **`DataManager`**: Mở file `.db` tại `persistentDataPath`.
3. **`GridManager`**: Truy vấn bảng `FARM_TILE`. Duyệt qua 2500 ô đất (50x50).
4. **Render**: Dựa trên dữ liệu trả về, `GridManager` sẽ Instantiate các Prefab tương ứng (Đất trống, Đất có cây, Vật cản).

---

## 4. CÁC MODULE CHÍNH (MAJOR MODULES)
### 🌱 Farm System (`FarmSystem/`)
- **`CropLogic`**: Xử lý logic sinh trưởng. Sử dụng `OfflineTimeManager` để tính toán thời gian bù trừ khi người chơi không mở game.
- **`YSort`**: Thuật toán sắp xếp hiển thị Isometric (Z as Y).

### 🧩 Land Puzzle (`LandPuzzle/`)
- **Adjacent Damage**: Khi một hàng gạch trong Block Puzzle bị xóa, nó sẽ gọi sự kiện `OnLinesCleared`. `GridBoard` sẽ tìm các Obstacle liền kề tọa độ bị xóa và trừ HP của chúng.
- **Energy System**: Quản lý tài nguyên để chơi puzzle, tự động hồi phục theo thời gian thực (Timestamp-based).

### 🏗️ Tetris Puzzle (`TetrisPuzzle/`)
- **Production Logic**: Khối nông sản rơi xuống. Khi hàng bị xóa, số lượng sản phẩm được cộng vào bộ đếm của `ACTIVE_ORDER`. Khi đủ chỉ tiêu, người chơi nhận thưởng Vàng.

---

## 5. HỆ THỐNG GIAO DIỆN (UI SYSTEM)
Sử dụng **`UIDataBinder`**: Một cơ chế tiêm dữ liệu (Injection) giúp tách rời UI khỏi logic. Các UI Panel (Shop, Inventory) sẽ tự động tìm và lắng nghe các sự kiện từ `DataManager` hoặc `EnergySystem`.

---
> **Lời nhắn cho AI:** Hãy chú ý đến class `DataManager.cs`. Đây là cầu nối quan trọng nhất. Nếu muốn thay đổi bất cứ trạng thái nào của thế giới, hãy thực hiện thông qua câu lệnh SQL tại đây trước khi cập nhật Visual.
