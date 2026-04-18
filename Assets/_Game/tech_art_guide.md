# 🎨 Tech-Art Guide: Hướng dẫn Quản lý & Ghép nối Đồ họa

Tài liệu này hướng dẫn Tech-Artist cách tìm kiếm, gán (assign) và tùy chỉnh các thuộc tính cơ bản (Transform, Scale, Offset) cho toàn bộ tài nguyên 2D trong game.

## 📁 Cấu trúc Thư mục Mới (_Game)
Toàn bộ tài nguyên chính hiện đã được gom vào thư mục **`Assets/_Game`**:
- **`_Game/Art/`**: Chứa toàn bộ Sprite, SpriteSheet và Animation.
- **`_Game/Prefabs/`**: Chứa các Prefab UI và GameObjects.
- **`_Game/Scripts/`**: Chứa code logic (Sắp xếp theo Core, UI, Farm, Puzzle).
- **`_Game/Scenes/`**: Chứa các scene của game (Final.unity).
- **`_Game/Data/`**: Chứa các file ScriptableObject (SO) để link Sprite vào code.

> [!IMPORTANT]
> **KHÔNG** di chuyển các file bên trong `_Game` bằng Windows Explorer. Hãy dùng Unity Editor để di chuyển nhằm giữ nguyên các tham chiếu (References).

---

## 🛠️ Hướng dẫn Ghép nối & Chỉnh sửa (Workflow)

### 1. Ghép nối Nông sản & Hạt giống (Crops & Seeds)
Game sử dụng hệ thống **ScriptableObject** để quản lý data. Khi bạn có ảnh mới, hãy gán vào đây:

- **Vị trí Data:** `Assets/Resources/CropData/` và `Assets/Resources/SeedData/`
- **Cách thực hiện:**
  1. Chọn file SO tương ứng (VD: `SO_Crop_product_apple`).
  2. Kéo Sprite từ `_Game/Art` vào ô **Product Icon** hoặc **Inventory Icon**.
  3. **Chỉnh sửa Transform:** Nếu sprite nông sản quá to/nhỏ khi hiển thị trên ô đất, hãy mở Prefab nông sản tương ứng trong `_Game/Prefabs/Prefabs/Farm/` và chỉnh lại **Scale** của SpriteRenderer.

### 2. Hệ thống Trang trí (Decoration System)
Mọi đồ trang trí (Decor) phải được đăng ký thủ công vào Manager.
- **Vị trí Manager:** Trong scene `Final.unity`, tìm GameObject **`DecorationManager`**.
- **Cách gán:** Kéo Sprite vào mảng `allDecorSprites`. 
- **Lưu ý:** Tên Sprite phải khớp chính xác với `DecorID` trong Database để hiển thị đúng.

### 3. Tùy chỉnh UI (Inventory & Shop)
Phần lớn UI được sinh ra tự động bằng code (Procedural UI).
- **Chỉnh sửa Layout:** Mở các Prefab mẫu tại `Assets/_Game/Prefabs/Prefabs/UI/`.
- **Inventory Slot:** Chỉnh sửa `InventorySlot.prefab`. Bạn có thể thay đổi `Width/Height`, `Padding` của Icon tại đây. Transform của các icon sẽ tự động tuân theo thiết lập của Prefab này.

---

## 📊 Thống kê Số lượng Tài nguyên (Inventory)

Dưới đây là bảng thống kê sơ bộ số lượng ảnh cần thiết cho từng giai đoạn/chức năng:

| Chức năng | Phân loại | Số lượng hiện tại | Ghi chú |
| :--- | :--- | :---: | :--- |
| **Trồng trọt (Farm)** | Sprites Nông sản (Crops) | ~300 | 32x32px, bao gồm các giai đoạn mầm, lớn, chín. |
| **Xếp hình (Tetris)** | Vector Icons | ~70 | Dùng làm block trong puzzle Tetris. |
| **Trang trí (Decor)** | SpriteSheet | 1 (Sheet lớn) | Chứa các mảnh lâu đài, rào chắn, cây cối. |
| **Hệ thống (System)** | UI Icons | ~15 | Nút Settings, Inventory, Progress Bar, Gold/Gem. |
| **Nông cụ (Tools)** | Tool Icons | 5 | Cuốc, Bình tưới, Thuốc sâu, Phân bón, Găng tay. |

---

## 📐 Thông số Transform Khuyến nghị

Để game trông nhất quán, Tech-Art nên tuân thủ các thông số sau:

- **Crops (World Space):**
  - **Scale:** Thường là (1, 1, 1) nếu sprite là 32x32. Nếu sprite 64x64, để Scale (0.5, 0.5, 0.5).
  - **Pivot:** Luôn để ở **Bottom** (Chân) để cây mọc từ mặt đất lên.

- **UI Icons (Screen Space):**
  - **Inventory Icon Scale:** (1, 1, 1).
  - **OffsetMin/Max:** Luôn giữ Padding khoảng 5-10 pixel so với khung Slot để tránh bị đè lên viền.

---

## 🆘 Troubleshooting
- **Lỗi Missing Sprite:** Nếu thấy ô màu trắng/hồng, hãy kiểm tra lại ScriptableObject xem đã kéo Sprite vào chưa.
- **Lỗi Sai vị trí:** Kiểm tra **Pivot** của Sprite trong cửa sổ Import Settings (đảm bảo là Center hoặc Bottom tùy loại).
