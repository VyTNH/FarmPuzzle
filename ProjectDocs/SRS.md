# **BÁO CÁO ĐẶC TẢ YÊU CẦU PHẦN MỀM (SRS)**

**Tên dự án:** FarmPuzzle (Game Nông Trại Kết Hợp Giải Đố) **Đơn vị đào
tạo:** Cao đẳng FPT Polytechnic **Lớp:** ENT2227.06 **Nhóm thực hiện:**
Nhóm \[Điền số nhóm\]

## **Danh sách thành viên:**

1.  Phạm Đức Dương (Dev - Farm & Kinh tế)

2.  Hào Anh (Dev - Minigame Mở Đất)

3.  Quyết (Dev - Minigame Tetris)

4.  Vũ (Dev - Core & Cài đặt)

5.  Vượng (Dev - Cửa hàng & Trang trí)

6.  \[Tên thành viên 6\] (Dev - Meta & Âm thanh)

## **1. GIỚI THIỆU (INTRODUCTION)**

### **1.1. Mục đích tài liệu**

Tài liệu SRS này được lập ra nhằm đặc tả chi tiết các yêu cầu chức năng
và phi chức năng của trò chơi **FarmPuzzle**. Tài liệu đóng vai trò làm
kim chỉ nam cho nhóm 6 lập trình viên (Unity Developer) trong quá trình
thiết kế, lập trình, kiểm thử và đồng nhất giao tiếp trong suốt vòng đời
dự án.

### **1.2. Phạm vi dự án**

FarmPuzzle là một tựa game 2D/Isometric phát triển trên nền tảng Unity.
Game kết hợp giữa mô phỏng quản lý nông trại thời gian thực (farming
simulator) và hai cơ chế giải đố (puzzle): xếp gạch để khai hoang mở
đất, và xếp gạch Tetris rơi tự do để xuất hàng. Mục tiêu cốt lõi là mang
lại trải nghiệm quản lý kinh tế thư giãn xen lẫn tư duy logic.

## **2. MÔ TẢ TỔNG QUAN (OVERALL DESCRIPTION)**

### **2.1. Nền tảng và Công nghệ**

- **Game Engine:** Unity Engine.

- **Ngôn ngữ lập trình:** C#.

- **Cấu trúc dữ liệu:** Lưu trữ cục bộ bằng định dạng JSON (Scriptable
  Objects cho dữ liệu tĩnh).

- **Nền tảng mục tiêu:** PC (Windows) / Android (Tùy chỉnh sau).

### **2.2. Phân hệ chức năng chính (Module)**

Dự án được chia làm 6 module hoạt động độc lập và giao tiếp qua hệ thống
Sự kiện (Event-driven):

1.  **Core & Data:** Quản lý luồng tải game và lưu trữ dữ liệu người
    chơi.

2.  **Farm System:** Logic trồng trọt, sinh trưởng và thu hoạch thời
    gian thực.

3.  **Land Puzzle:** Minigame kéo thả khối (block) để mở khóa ô đất.

4.  **Tetris Puzzle:** Minigame xếp gạch rơi tự do để hoàn thành đơn
    hàng.

5.  **Economy & Decor:** Hệ thống tiền tệ, cửa hàng và xây dựng trang
    trí.

6.  **Meta & Audio:** Hệ thống nhiệm vụ chạy ngầm và quản lý âm thanh
    tổng.

## **3. ĐẶC TẢ YÊU CẦU CHỨC NĂNG (FUNCTIONAL REQUIREMENTS)**

### **3.1. Module Nông trại (Farm System)**

- **FR-1.1 (Gieo hạt):** Hệ thống cho phép người chơi kéo hạt giống từ
  kho xuống một ô đất trống. Trạng thái ô đất thay đổi thành \"Đã gieo
  hạt\".

- **FR-1.2 (Sinh trưởng):** Cây trồng sẽ tự động phát triển qua các giai
  đoạn (Hạt -\> Mầm -\> Cây trưởng thành) dựa trên thông số thời gian
  của CropDataSO. Hệ thống phải tính toán được cả Offline Progress (thời
  gian người chơi tắt game).

- **FR-1.3 (Thu hoạch):** Khi cây chín, người chơi tương tác để thu
  hoạch nông sản vào kho. Trạng thái ô đất quay về \"Đất trống\".

### **3.2. Module Giải đố Mở Đất (Land Puzzle)**

- **FR-2.1 (Sinh khối):** Hệ thống tạo ngẫu nhiên các khối gạch hình thù
  khác nhau để người chơi kéo thả vào lưới (Grid).

- **FR-2.2 (Phá hủy và Rơi đồ):** Khi xếp đầy một hàng/cột, hệ thống tự
  động xóa các khối đó, đồng thời kích hoạt sự kiện phá vỡ các chướng
  ngại vật (đá, cây khô) để rơi ra tài nguyên (Gỗ, Đá).

### **3.3. Module Giải đố Xuất Hàng (Tetris Puzzle)**

- **FR-3.1 (Rơi tự do):** Các khối nông sản tự động rơi từ trên xuống.
  Người chơi có thể điều hướng Trái/Phải/Xoay/Rơi nhanh.

- **FR-3.2 (Thanh toán đơn hàng):** Khi xếp kín hàng ngang, hàng đó biến
  mất. Hệ thống đối chiếu số lượng khối bị xóa với yêu cầu của
  OrderDataSO để quy đổi ra Vàng và cộng vào tài khoản người chơi.

### **3.4. Module Kinh tế & Trang trí (Economy & Decor)**

- **FR-4.1 (Cửa hàng):** Hiển thị danh sách hạt giống và đồ trang trí.
  Cho phép mua nếu số Vàng hiện có lớn hơn hoặc bằng giá vật phẩm.

- **FR-4.2 (Xây dựng):** Người chơi lấy đồ trang trí từ kho, hệ thống
  hiển thị lưới (Grid) xanh/đỏ để báo hiệu vị trí hợp lệ. Chấp nhận đặt
  vật phẩm xuống nếu vị trí không bị cản trở.

### **3.5. Module Cốt lõi & Tiến trình (Core, Meta & Audio)**

- **FR-5.1 (Lưu dữ liệu):** Toàn bộ số lượng Vàng, vật phẩm trong kho,
  trạng thái ô đất, và tọa độ đồ trang trí phải được mã hóa và lưu vào
  file PlayerData.json.

- **FR-5.2 (Nhiệm vụ):** Hệ thống ngầm lắng nghe các thao tác của người
  chơi (VD: \"Thu hoạch 10 củ cải\"). Kích hoạt nút \"Nhận thưởng\" khi
  tiến độ đạt 100%.

- **FR-5.3 (Âm thanh & Cài đặt):** Hệ thống tự động phát BGM. Người chơi
  có thể Bật/Tắt âm thanh trong màn hình Cài đặt, trạng thái Bật/Tắt
  được lưu lại cho lần mở game sau.

## **4. YÊU CẦU PHI CHỨC NĂNG (NON-FUNCTIONAL REQUIREMENTS)**

- **Hiệu năng:** Game duy trì ở mức tối thiểu 30FPS trên các thiết bị
  cấu hình thấp. Tối ưu hóa việc khởi tạo (Instantiate) và hủy (Destroy)
  object bằng Object Pooling cho hệ thống Tetris.

- **Giao diện (UI/UX):** Các nút bấm phải to, rõ ràng, phù hợp với thao
  tác chạm (Touch) trên màn hình mobile. Phản hồi hình ảnh/âm thanh ngay
  lập tức khi click.

- **Bảo mật:** Dữ liệu file Save (JSON) cần được mã hóa cơ bản (Base64)
  để tránh người chơi can thiệp sửa số Vàng một cách quá dễ dàng.

- **Kiểm soát phiên bản:** Bắt buộc tuân thủ Git Flow. Không được commit
  file .meta bị lỗi hoặc các thư mục rác (như Library/, Temp/) lên
  repository.
