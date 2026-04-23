# **GAME DESIGN DOCUMENT (GDD) - CẬP NHẬT MỚI NHẤT**

**Tên dự án:** FarmPuzzle

**Thể loại:** Nông trại (Farming Simulator) kết hợp Giải đố (Puzzle)

**Nền tảng:** Mobile (Phát triển trên Unity Engine)

**Phong cách đồ họa:** 2.5D Isometric (Nông trại) & 2D Phẳng (Minigame)

## **1. TỔNG QUAN LỐI CHƠI (CORE LOOP)**

FarmPuzzle mang đến trải nghiệm quản lý tài nguyên kết hợp tư duy logic.
Vòng lặp gameplay cốt lõi diễn ra theo 4 bước:

1.  **Trồng trọt (Farming):** Gieo hạt, chờ đợi thời gian thực (hỗ trợ
    Offline Progress) và thu hoạch nông sản.

2.  **Giải đố (Puzzle):** Tham gia các minigame để tương tác với thế
    giới:

    - *Xếp gạch mở đất:* Phá hủy chướng ngại vật để mở rộng diện tích
      Nông trại.

    - *Tetris xuất hàng:* Xử lý các khối nông sản rơi tự do để hoàn
      thành Đơn hàng.

3.  **Kinh tế (Economy):** Thu thập Vàng từ việc bán nông sản và hoàn
    thành Nhiệm vụ.

4.  **Trang trí (Decorating):** Sử dụng Vàng để mua sắm vật phẩm, tùy
    biến và cá nhân hóa không gian Nông trại.

## **2. CHI TIẾT CƠ CHẾ GAMEPLAY (MECHANICS)**

### **2.1. Hệ thống Nông trại Isometric (Farm System)**

- **Môi trường:** Thiết kế trên lưới Isometric Z as Y Tilemap. Các vật
  thể tuân thủ quy tắc đè hình Custom Axis Sort (Y=1) và tâm tọa độ
  (Pivot) đặt ở Bottom.

- **Vòng đời cây trồng:** Hạt giống (SEED_ITEM) =\> Thời gian sinh
  trưởng =\> Nông sản (PRODUCT_ITEM).

- **Cơ chế Offline:** Hệ thống ghi nhận mốc LastExitTime. Khi người chơi
  mở lại game, hệ thống tự động bù trừ thời gian ngoại tuyến vào bộ đếm
  sinh trưởng của cây trồng.

### **2.2. Minigame Giải đố Mở đất (Land Puzzle)**

- **Luồng chơi:** Người chơi kéo thả các khối gạch hình học (Block) vào
  một ma trận 2D. Khi lấp đầy một hàng ngang hoặc dọc, hàng đó sẽ bị
  xóa.

- Mỗi lượt chơi sẽ tốn 1 năng lượng, mỗi năng lượng sẽ hồi trong thời
  gian thực một khoảng nhất định.

- **Tương tác Isometric:** Cơ chế **Sát thương lân cận (Adjacent)**. Nếu
  khối gạch bị xóa nằm ngay sát cạnh một chướng ngại vật (đá tảng, cây
  khô) trên bản đồ Nông trại, chướng ngại vật đó sẽ bị trừ điểm độ bền
  (Durability). Khi độ bền về 0, ô đất được mở khóa (IsUnlocked = true).

### **2.3. Minigame Xuất hàng (Tetris Puzzle)**

- **Luồng chơi:** Hoạt động trên giao diện UI 2D phẳng độc lập. Các khối
  nông sản rơi tự do từ trên xuống. Người chơi điều hướng (trái/phải),
  xoay khối và thả rơi nhanh.

- **Tích hợp Kinh tế:** Số lượng hàng ngang bị xóa sẽ được hệ thống ghi
  nhận và đối chiếu trực tiếp với yêu cầu của bảng ACTIVE_ORDER (Đơn
  hàng). Hoàn thành chỉ tiêu sẽ nhận được Vàng.

### **2.4. Hệ thống Hợp đồng & Tiến trình (Contract System)**

- Thay thế cho hệ thống \"Nhiệm vụ\" truyền thống để phù hợp bối cảnh
  nông nghiệp.

- Quản lý bằng ContractDataSO với biến phân loại reqType (Enum):

  - Plant: Yêu cầu số lần gieo hạt.

  - Harvest: Yêu cầu số lượng nông sản thu hoạch.

  - Sell: Yêu cầu số lượng nông sản xuất khẩu qua minigame Tetris.

### **2.5. Hệ thống Cửa hàng & Xây dựng (Shop & Decor)**

- Cung cấp danh mục đồ trang trí (DECOR_ITEM) mua bằng Vàng.

- **Cơ chế Xây dựng (Placement Controller):** Cấp quyền di chuyển đồ
  trang trí trên lưới Isometric. Sử dụng tia Raycast để kiểm tra tính
  hợp lệ của ô đất, phản hồi bằng tín hiệu màu (Xanh: Hợp lệ / Đỏ: Bị
  vướng vật cản).

## **3. KIẾN TRÚC DỮ LIỆU & KỸ THUẬT**

### **3.1. Quản lý Dữ liệu Toàn cục**

- GameManager duy trì trạng thái xuyên suốt các cảnh
  (DontDestroyOnLoad).

- DataManager mã hóa và lưu trữ tệp .json tại
  Application.persistentDataPath, đảm bảo tiến trình của PLAYER (Vàng,
  Hàng tồn kho, Tọa độ đồ trang trí) không bị mất khi thoát game.

### **3.2. Cấu trúc Cơ sở Dữ liệu (Database Architecture)**

- Áp dụng thiết kế bóc tách thực thể vật phẩm để tối ưu truy vấn:

  - **Từ điển Vật phẩm:** Chia làm 3 bảng độc lập: SEED_ITEM,
    PRODUCT_ITEM, DECOR_ITEM.

  - **Túi đồ (Inventory):** Sử dụng khóa ngoại ReferenceID để liên kết
    đa hình với các bảng danh mục.

  - **Trung gian sinh học:** Bảng CROP_DATA giữ vai trò cầu nối, nhận
    đầu vào là SeedID và trả đầu ra là ProductID.
