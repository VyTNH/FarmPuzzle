# **BÁO CÁO USE CASE VÀ ĐẶC TẢ CHI TIẾT**

**Dự án:** FarmPuzzle

## **1. DANH SÁCH TÁC NHÂN (ACTORS)**

- **Người chơi (Player):** Tác nhân chính thực hiện các thao tác tương
  tác trực tiếp với giao diện game (gieo hạt, thu hoạch, giải đố, mua
  sắm).

- **Hệ thống (System):** Tác nhân phụ chạy ngầm, đảm nhiệm việc tính
  toán thời gian, xử lý logic va chạm, lưu/tải dữ liệu và theo dõi tiến
  độ.

## **2. DANH SÁCH USE CASE TỔNG THỂ**

1.  **Khởi tạo dữ liệu:** Hệ thống tạo mới hoặc tải dữ liệu lưu trữ
    (Save game) của người chơi.

2.  **Trồng cây & Chăm sóc:** Người chơi gieo hạt và hệ thống tính toán
    thời gian sinh trưởng.

3.  **Giải đố Mở đất:** Người chơi kéo thả khối gạch để phá chướng ngại
    vật trên bản đồ Isometric.

4.  **Xuất hàng (Minigame Tetris):** Người chơi điều khiển khối gạch rơi
    tự do để hoàn thành đơn hàng.

5.  **Cửa hàng & Mua sắm:** Người chơi sử dụng Vàng để đổi lấy vật
    phẩm/hạt giống.

6.  **Xây dựng & Trang trí:** Người chơi đặt vật phẩm từ kho xuống bản
    đồ Nông trại.

7.  **Quản lý Cài đặt:** Người chơi bật/tắt âm thanh, hệ thống lưu lại
    tùy chọn.

## 

## 

## 

## **3. ĐẶC TẢ USE CASE CHI TIẾT (USE CASE SPECIFICATIONS)**

### **3.1. Đặc tả UC: Trồng cây & Chăm sóc (Farm Gameplay)**

- **Tóm tắt:** Người chơi chọn hạt giống từ kho để gieo trồng. Hệ thống
  đếm thời gian để cây lớn lên và cho phép thu hoạch.

- **Điều kiện tiên quyết:** Người chơi có hạt giống và có ô đất trống
  hợp lệ.

- **Luồng sự kiện chính:**

  1.  Người chơi kéo hạt giống từ giao diện túi đồ và thả xuống ô đất
      trống.

  2.  Hệ thống kiểm tra vị trí thả. Nếu hợp lệ, ô đất chuyển trạng thái
      sang \"Đã gieo hạt\".

  3.  Hệ thống bắt đầu đếm ngược thời gian sinh trưởng của loại cây đó.

  4.  Khi hết thời gian, hệ thống đổi hình ảnh cây sang trạng thái \"Có
      thể thu hoạch\".

  5.  Người chơi nhấn vào cây để thu hoạch.

  6.  Hệ thống cộng nông sản vào kho đồ và dọn sạch ô đất.

  7.  Cây đến giai đoạn trưởng thành thì có nhu cầu cần chăm sóc random.

  8.  

- **Luồng ngoại lệ (Ngoại lệ 2a):** Người chơi thả hạt giống vào ô đất
  đã có cây hoặc ô đất bị khóa. Hệ thống từ chối thao tác, hoàn trả hạt
  giống về túi đồ và không trừ số lượng.

### **3.2. Đặc tả UC: Giải đố Mở đất (Land Puzzle)**

- **Tóm tắt:** Người chơi lấp đầy các hàng/cột bằng khối gạch để phá vỡ
  chướng ngại vật (đá, cây khô), qua đó thu thập tài nguyên và mở rộng
  đất.

- **Điều kiện tiên quyết:** Màn chơi Mở đất đã được tải thành công.

- **Luồng sự kiện chính:**

  1.  Hệ thống cung cấp ngẫu nhiên các khối gạch ở khay chứa bên dưới
      màn hình.

  2.  Người chơi chọn, kéo và thả một khối gạch vào vị trí trống trên
      lưới (Grid).

  3.  Hệ thống kiểm tra nếu có hàng dọc hoặc hàng ngang nào được lấp đầy
      thì tiến hành xóa các khối gạch ở hàng đó.

  4.  Nếu hàng bị xóa đi qua vị trí có chướng ngại vật, hệ thống phá hủy
      chướng ngại vật đó.

  5.  Hệ thống kích hoạt sự kiện rơi tài nguyên (Gỗ, Đá) và cộng vào túi
      đồ của người chơi.

- **Luồng ngoại lệ (Ngoại lệ 2a):** Người chơi kéo khối gạch ra ngoài
  phạm vi lưới hoặc đè lên khối gạch khác. Khối gạch sẽ tự động nảy
  (snap) trở lại khay chứa ban đầu.

### **3.3. Đặc tả UC: Xuất hàng (Tetris Minigame)**

- **Tóm tắt:** Người chơi sắp xếp các khối nông sản rơi từ trên xuống để
  hoàn thành yêu cầu của Đơn hàng và nhận Vàng.

- **Điều kiện tiên quyết:** Người chơi kích hoạt tính năng Giao hàng.

- **Luồng sự kiện chính:**

  1.  Hệ thống hiển thị Đơn hàng (ví dụ: Cần thu thập 10 khối Cà chua).

  2.  Khối gạch tự động rơi từ trên xuống màn hình 2D.

  3.  Người chơi thao tác di chuyển khối gạch sang Trái, Phải, Xoay hoặc
      Rơi nhanh.

  4.  Khi khối gạch chạm đáy, hệ thống cố định khối gạch.

  5.  Nếu 1 hàng ngang được xếp kín, hệ thống xóa hàng đó.

  6.  Hệ thống đối chiếu các ô bị xóa với Đơn hàng để tăng tiến độ.

  7.  Khi tiến độ đạt 100%, hệ thống cộng Vàng cho người chơi và kết
      thúc minigame.

- **Luồng ngoại lệ (Ngoại lệ 4a):** Các khối gạch bị xếp chồng quá cao
  và chạm đến trần màn hình. Trò chơi kết thúc, người chơi không nhận
  được phần thưởng của đơn hàng đang làm dở.

### **3.4. Đặc tả UC: Xây dựng & Trang trí (Decor)**

- **Tóm tắt:** Người chơi đặt các vật phẩm trang trí đã mua lên bản đồ
  Nông trại.

- **Điều kiện tiên quyết:** Trong kho đồ của người chơi phải có ít nhất
  1 vật phẩm trang trí.

- **Luồng sự kiện chính:**

  1.  Người chơi mở chế độ Xây dựng và chọn một vật phẩm từ kho.

  2.  Người chơi di chuyển vật phẩm trên lưới Isometric của Nông trại.

  3.  Hệ thống liên tục kiểm tra va chạm (Collision). Nếu vị trí trống,
      hiển thị vùng sáng màu Xanh.

  4.  Người chơi nhấn xác nhận để đặt vật phẩm xuống.

  5.  Hệ thống trừ vật phẩm trong kho và lưu lại tọa độ của vật phẩm đó
      lên bản đồ.

- **Luồng ngoại lệ (Ngoại lệ 3a):** Vị trí đặt bị vướng cây trồng hoặc
  vật thể khác, hệ thống hiển thị vùng sáng màu Đỏ. Người chơi không thể
  nhấn nút xác nhận đặt.
