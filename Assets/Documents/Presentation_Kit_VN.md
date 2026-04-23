# 🎤 KỊCH BẢN THUYẾT TRÌNH PHÂN VAI: FARM PUZZLE (NHÓM 6 NGƯỜI)

Kịch bản này được thiết kế để mỗi thành viên trực tiếp thuyết trình về module mình đảm nhiệm, thể hiện rõ vai trò cá nhân và sự phối hợp tổng thể.

---

## PHẦN 1: TỔNG QUAN & CONCEPT (Project Lead thuyết trình)
- **Lời dẫn**: "Chào hội đồng, dự án Farm Puzzle của nhóm chúng em không chỉ là nông trại, mà là một trải nghiệm giải đố đa tầng..."
- **Nội dung**: Trình bày **The Pitch** (Mục I) và **Quy trình Git Workflow** (Mục II). Nhấn mạnh tính Module hóa giúp 6 người làm việc song song.

---

## PHẦN 2: CHI TIẾT CÁC MODULE CHỨC NĂNG (Từng thành viên thuyết trình)

### 1. Hệ thống Lõi & Dữ liệu (Thuyết trình bởi: DƯƠNG)
- **Nhiệm vụ**: Quản lý "Trái tim" của game - Database.
- **Deep Dive**: 
    - Giải thích triết lý **Data-Driven Architecture**: "Everything starts from Database". 
    - Tại sao dùng **SQLite** thay vì PlayerPrefs? (Quản lý hàng nghìn ô đất, tính toàn vẹn dữ liệu).
    - Kỹ thuật **Dictionary O(1)**: Cách cache dữ liệu kho đồ để truy xuất tức thời.

### 2. Hệ thống Nông trại Isometric (Thuyết trình bởi: VŨ)
- **Nhiệm vụ**: Quản lý Grid 50x50 và logic sinh trưởng.
- **Deep Dive**:
    - Thuật toán **YSort**: Xử lý đè hình trong không gian 2.5D.
    - Cơ chế **OfflineTimeManager**: Cách tính toán bù trừ tiến độ cây trồng dựa trên Timestamp khi người chơi quay lại game.
    - Tối ưu hóa **Viewport Culling**: Giúp bản đồ 2500 ô đất vẫn chạy mượt trên Mobile.

### 3. Minigame Khai hoang mở đất (Thuyết trình bởi: QUYẾT)
- **Nhiệm vụ**: Logic Block Puzzle 10x10.
- **Deep Dive**:
    - Thuật toán **Check & Clear Lines**: Xử lý nổ hàng/cột trên mảng 2 chiều.
    - Cơ chế **Adjacent Damage (Sát thương lân cận)**: Giải thích cách vụ nổ trong Minigame tác động lên vật cản trên Farm thực tế.
    - Quản lý **Energy System**: Cơ chế hồi năng lượng thời gian thực.

### 4. Minigame Sản xuất & Đơn hàng (Thuyết trình bởi: VƯƠNG)
- **Nhiệm vụ**: Logic Tetris và xuất xưởng sản phẩm.
- **Deep Dive**:
    - Xây dựng bộ khung vật lý cho các khối nông sản rơi tự do.
    - Hệ thống **Order Matching**: Cách ánh xạ số hàng Tetris bị xóa vào yêu cầu của đơn hàng (`ACTIVE_ORDER`) để quy đổi ra Vàng và EXP.

### 5. Cửa hàng & Xây dựng trang trí (Thuyết trình bởi: HÀO ANH)
- **Nhiệm vụ**: Kinh tế game và cá nhân hóa trang trại.
- **Deep Dive**:
    - Cơ chế **Placement Controller**: Cách dùng Raycast để xác định tọa độ Grid từ vị trí chạm màn hình.
    - Thuật toán kiểm tra vị trí đặt vật phẩm (Xanh/Đỏ) dựa trên trạng thái ô đất trong Database.

### 6. Nhiệm vụ & Hệ thống Âm thanh (Thuyết trình bởi: VY)
- **Nhiệm vụ**: Tạo động lực chơi game và cảm xúc âm nhạc.
- **Deep Dive**:
    - Thiết kế **Quest System**: Sử dụng `ScriptableObjects` để dễ dàng thêm hàng trăm nhiệm vụ mà không cần sửa code.
    - Quản lý **Audio Mixer**: Cách xử lý chồng lấp âm thanh (SFX) khi có nhiều sự kiện thu hoạch/nổ gạch xảy ra cùng lúc.

---

## PHẦN 3: TẦM NHÌN & KẾT LUẬN (Project Lead thuyết trình)
- **Nội dung**: Trình bày **Roadmap** tương lai (Social, Livestock, Weather).
- **Lời kết**: "Dự án là thành quả của sự phối hợp chặt chẽ giữa 6 thành viên và nền tảng kỹ thuật vững chắc. Cảm ơn hội đồng đã lắng nghe."

---
> **Gợi ý**: Khi chuyển mic, hãy nói câu nối như: "Tiếp theo, bạn Vũ sẽ trình bày về cách chúng em tối ưu hóa hiển thị cho bản đồ 50x50..." giúp buổi thuyết trình chuyên nghiệp hơn.
