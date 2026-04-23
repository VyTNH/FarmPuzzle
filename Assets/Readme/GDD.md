# GAME DESIGN DOCUMENT (GDD) - FARM PUZZLE 🎮

**Phiên bản**: 2.0 (Cập nhật 22/04/2026)  
**Thể loại**: Farming Simulator & Puzzle Hybrid  
**Phong cách**: 2.5D Isometric Pixel Art  

---

## 1. TỔNG QUAN (VISION)
**Farm Puzzle** mang đến một làn gió mới cho dòng game nông trại bằng cách tích hợp các cơ chế giải đố kinh điển vào mọi hoạt động quản lý. Thay vì chỉ click và chờ đợi, người chơi phải tư duy để mở rộng đất đai và tối ưu hóa sản xuất.

---

## 2. VÒNG LẶP gameplay CỐT LÕI (CORE LOOP)
1. **Trồng trọt (Farm)**: Gieo hạt và thu hoạch nông sản.
2. **Khai hoang (Expand)**: Sử dụng nông sản hoặc năng lượng để chơi **Block Puzzle**. Phá hủy vật cản để mở rộng diện tích.
3. **Sản xuất & Bán hàng (Production)**: Đưa nông sản vào **Tetris Puzzle** để đóng gói và hoàn thành đơn hàng.
4. **Tái đầu tư (Upgrade)**: Dùng tiền kiếm được mua hạt giống cao cấp hơn và đồ trang trí (Decor).

---

## 3. CÁC CƠ CHẾ CHI TIẾT (MECHANICS)

### 3.1. Hệ thống Nông trại (Farm System)
- **Lưới Isometric (50x50)**: Bản đồ rộng lớn được chia thành các ô gạch kim cương.
- **Sinh trưởng thời gian thực**: Cây trồng có 3 giai đoạn: Mầm -> Phát triển -> Chín.
- **Offline Progress**: Khi người chơi thoát game, hệ thống lưu lại `LastExitTime`. Khi quay lại, game tính toán: `DeltaTime = CurrentTime - LastExitTime` để tự động cộng tiến độ sinh trưởng cho cây.

### 3.2. Block Puzzle: Khai hoang mở đất
- **Bàn cờ 10x10**: Người chơi kéo các khối hình vào lưới.
- **Sát thương lân cận (Adjacent Damage)**: Đây là cơ chế độc đáo. Mỗi khi một hàng/cột gạch biến mất, nó sẽ phát ra "sóng xung kích" gây sát thương cho các vật cản (Đá, Cây khô) nằm ở các ô đất thực tế tương ứng trên bản đồ Farm.
- **Phá hủy vật cản**: Khi vật cản hết HP, ô đất đó chính thức được mở khóa và có thể trồng trọt.

### 3.3. Tetris Puzzle: Xuất xưởng đơn hàng
- **Cơ chế rơi tự do**: Các khối nông sản (Cà rốt, Cà chua...) rơi xuống.
- **Hoàn thành đơn hàng (Orders)**: Mỗi đơn hàng yêu cầu xóa một số lượng hàng ngang nhất định của một loại sản phẩm cụ thể. 
- **Phần thưởng**: Hoàn thành đơn hàng mang lại lượng Vàng (Gold) lớn hơn nhiều so với bán lẻ.

---

## 4. HỆ THỐNG KINH TẾ (ECONOMY)
- **Tiền tệ (Gold)**: Dùng để mua hạt giống và đồ trang trí.
- **Năng lượng (Energy)**: Giới hạn số lượt chơi Block Puzzle mỗi ngày. Tự động hồi phục 1 điểm sau mỗi 10 phút.
- **Kinh nghiệm (EXP)**: Lên cấp để mở khóa các loại hạt giống mới và mở rộng giới hạn năng lượng.

---

## 5. THIẾT KẾ ĐỒ HỌA & ÂM THANH
- **Art**: Pixel Art 2D mang lại cảm giác hoài cổ, nhẹ nhàng.
- **Camera**: Cho phép Drag để di chuyển và Zoom để bao quát toàn cảnh trang trại 50x50.
- **Âm thanh**: Nhạc nền (BGM) Lo-fi thư giãn. SFX vui tai khi thu hoạch hoặc xóa hàng puzzle.

---
