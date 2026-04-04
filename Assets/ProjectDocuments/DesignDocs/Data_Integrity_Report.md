# BÁO CÁO SỰ CỐ: Xung Đột Nguồn Dữ Liệu & File SO Rác

**Ngày phát hiện:** 2026-04-01  
**Mức độ:** Nghiêm trọng (Chặn toàn bộ luồng Test UC1/UC2)  
**Trạng thái:** ĐÃ XỬ LÝ ✅

---

## 1. MÔ TẢ SỰ CỐ

Khi chạy Sandbox Test Scene (UC1/UC2), người chơi không thể gieo hạt dù đã chọn đúng Hạt giống và bơm đủ tồn kho. Mọi ô đất đều báo **"Cây chưa chín! Tiến độ: 0%"** ngay khi click.

## 2. NGUYÊN NHÂN GỐC RỄ (Root Cause)

### 2.1. File ScriptableObject Rác — seedID Rỗng

| Vị trí | File | Vấn đề |
|:-------|:-----|:-------|
| `Assets/ScriptableObjects/CropsAndSeeds/Resources/` | `SeedItemSO.asset` | Được tạo **trước khi** thêm trường `seedID` vào class `SeedItemSO.cs`. File chứa `seedName: Carrot` nhưng **không có** trường `seedID` → khi Unity serialize ra, `seedID = ""` (chuỗi rỗng). |
| (cùng thư mục) | `CropDataSO.asset` | File CropData đi kèm, cũng từ phiên bản cũ. |

**Hậu quả:** Tool `UC1UC2TestSceneGenerator` dùng `AssetDatabase.FindAssets("t:SeedItemSO")` quét **toàn bộ** Project → hốt luôn file rác này → auto-chọn nó làm `availableSeeds[0]` → người dùng cầm "Carrot" nhưng `seedID` rỗng → `GridManager.HandleInteraction()` check `!string.IsNullOrEmpty(selectedSeed.seedID)` → **FAIL** → không gieo được.

### 2.2. PlayerPrefs Ghi Đè Trạng Thái Ô Đất Bất Hợp Pháp

**Nguồn gốc:** `OfflineTimeManager.cs` (do Dev Vũ viết từ giai đoạn đầu dự án) sử dụng `PlayerPrefs` để lưu/load trạng thái cây trồng. Điều này **vi phạm** thiết kế kiến trúc đã thống nhất — chỉ dùng SQLite và ScriptableObject.

**Hành vi gây lỗi:**
1. Lần chạy trước: `OnApplicationQuit()` → `SaveFarmState()` → Ghi `PlayerPrefs.SetInt("Plot_0_Occupied", 1)` cho các ô đất.
2. Lần chạy sau: `Start()` → `Invoke(LoadFarmState, 0.1f)` → Đọc PlayerPrefs → Gọi `plot.SetData(true, ...)` → **Đặt `isOccupied = true`** cho tất cả ô đất.
3. Kết quả: Mọi ô đất bị đánh dấu "đã có cây" dù chưa ai gieo gì → Click vào đất → Chui vào nhánh "thử thu hoạch" → Báo "Cây chưa chín 0%".

**Xung đột dữ liệu:** PlayerPrefs và SQLite (bảng `FARM_TILE`) cùng quản lý trạng thái ô đất nhưng không đồng bộ với nhau, tạo ra **hai nguồn sự thật (two sources of truth)** xung đột.

## 3. HÀNH ĐỘNG KHẮC PHỤC

| # | Hành động | File bị ảnh hưởng | Trạng thái |
|:--|:----------|:-------------------|:-----------|
| 1 | Xóa 2 file SO rác (`SeedItemSO.asset`, `CropDataSO.asset`) | `Assets/ScriptableObjects/CropsAndSeeds/Resources/` | ✅ Xóa |
| 2 | Viết lại `OfflineTimeManager.cs` — Loại bỏ toàn bộ `PlayerPrefs`, chuyển sang dùng 100% SQLite (`DataManager.GetFarmTile()` / `UpdateFarmTile()`) | [OfflineTimeManager.cs](file:///d:/UnityProjects/farmpuzzle/Assets/Scripts/FarmSystem/OfflineTimeManager.cs) | ✅ Viết lại |
| 3 | Yêu cầu người dùng chạy **Edit > Clear All PlayerPrefs** để dọn dữ liệu rác còn sót trong Registry Windows | Thao tác thủ công trên Unity Editor | ⏳ Chờ sếp thực hiện |

## 4. BÀI HỌC RÚT RA

> [!WARNING]
> ### Quy tắc bắt buộc từ nay trở đi:
> 1. **Một nguồn sự thật duy nhất (Single Source of Truth):** Mọi dữ liệu động (trạng thái ô đất, kho đồ, tiến trình nhiệm vụ) CHỈ được lưu trong **SQLite** (`FarmPuzzleDB.db`). Cấm dùng `PlayerPrefs`, `JSON file`, hoặc bất kỳ lớp lưu trữ nào khác mà không có sự đồng ý của Trưởng nhóm.
> 2. **Kiểm tra tính toàn vẹn SO:** Khi quét tự động `FindAssets("t:SomeType")`, luôn **lọc bỏ** các file có trường bắt buộc bị rỗng trước khi sử dụng.
> 3. **Không giữ file di sản (legacy):** File SO từ phiên bản cũ (trước khi thêm trường mới) phải được migrate hoặc xóa bỏ, không để lẫn trong Project gây nhầm lẫn.
