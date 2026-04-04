# TỔNG HỢP TÀI LIỆU DỰ ÁN TỪ GOOGLE DOCS
*(Nội dung được trích xuất nguyên bản từ các link Google Docs cung cấp)*

---

# 1. GAME DESIGN DOCUMENT (GDD)

**Tên dự án:** FarmPuzzle
**Thể loại:** Nông trại (Farming Simulator) kết hợp Giải đố (Puzzle)
**Nền tảng:** Mobile (Phát triển trên Unity Engine)
**Phong cách đồ họa:** 2.5D Isometric (Nông trại) & 2D Phẳng (Minigame)

**1. TỔNG QUAN LỐI CHƠI (CORE LOOP)**
FarmPuzzle mang đến trải nghiệm quản lý tài nguyên kết hợp tư duy logic. Vòng lặp gameplay cốt lõi diễn ra theo 4 bước:
1. **Trồng trọt (Farming):** Gieo hạt, chờ đợi thời gian thực (hỗ trợ Offline Progress) và thu hoạch nông sản.
2. **Giải đố (Puzzle):** Tham gia các minigame để tương tác với thế giới:
    * **Xếp gạch mở đất:** Phá hủy chướng ngại vật để mở rộng diện tích Nông trại.
    * **Tetris xuất hàng:** Xử lý các khối nông sản rơi tự do để hoàn thành Đơn hàng.
3. **Kinh tế (Economy):** Thu thập Vàng từ việc bán nông sản và hoàn thành Nhiệm vụ.
4. **Trang trí (Decorating):** Sử dụng Vàng để mua sắm vật phẩm, tùy biến và cá nhân hóa không gian Nông trại.

**2. CHI TIẾT CƠ CHẾ GAMEPLAY (MECHANICS)**

**2.1. Hệ thống Nông trại Isometric (Farm System)**
* **Môi trường:** Thiết kế trên lưới Isometric Z as Y Tilemap. Các vật thể tuân thủ quy tắc đề hình Custom Axis Sort (Y=1) và tâm tọa độ (Pivot) đặt ở Bottom.
* **Vòng đời cây trồng:** Hạt giống (SEED_ITEM) => Thời gian sinh trưởng => Nông sản (PRODUCT_ITEM).
* **Cơ chế Offline:** Hệ thống ghi nhận mốc LastExitTime. Khi người chơi mở lại game, hệ thống tự động bù trừ thời gian ngoại tuyến vào bộ đếm sinh trưởng của cây trồng.

**2.2. Minigame Giải đố Mở đất (Land Puzzle)**
* **Luồng chơi:** Người chơi kéo thả các khối gạch hình học (Block) vào một ma trận 2D. Khi lấp đầy một hàng ngang hoặc dọc, hàng đó sẽ bị xóa.
* Mỗi lượt chơi sẽ tốn 1 năng lượng, mỗi năng lượng sẽ hồi trong thời gian thực một khoảng nhất định.
* **Tương tác Isometric:** Cơ chế Sát thương lân cận (Adjacent). Nếu khối gạch bị xóa nằm ngay sát cạnh một chướng ngại vật (đá tảng, cây khô) trên bản đồ Nông trại, chướng ngại vật đó sẽ bị trừ điểm độ bền (Durability). Khi độ bền về 0, ô đất được mở khóa (IsUnlocked = true).

**2.3. Minigame Xuất hàng (Tetris Puzzle)**
* **Luồng chơi:** Hoạt động trên giao diện UI 2D phẳng độc lập. Các khối nông sản rơi tự do từ trên xuống. Người chơi điều hướng (trái/phải), xoay khối và thả rơi nhanh.
* **Tích hợp Kinh tế:** Số lượng hàng ngang bị xóa sẽ được hệ thống ghi nhận và đối chiếu trực tiếp với yêu cầu của bảng ACTIVE_ORDER (Đơn hàng). Hoàn thành chỉ chỉ tiêu sẽ nhận được Vàng.

**2.4. Hệ thống Hợp đồng & Tiến trình (Contract System)**
* Thay thế cho hệ thống "Nhiệm vụ" truyền thống để phù hợp bối cảnh nông nghiệp.
* Quản lý bằng ContractDataSO với biến phân loại reqType (Enum):
    * **Plant:** Yêu cầu số lần gieo hạt.
    * **Harvest:** Yêu cầu số lượng nông sản thu hoạch.
    * **Sell:** Yêu cầu số lượng nông sản xuất khẩu qua minigame Tetris.

**2.5. Hệ thống Cửa hàng & Xây dựng (Shop & Decor)**
* Cung cấp danh mục đồ trang trí (DECOR_ITEM) mua bằng Vàng.
* **Cơ chế Xây dựng (Placement Controller):** Cấp quyền di chuyển đồ trang trí trên lưới Isometric. Sử dụng tia Raycast để kiểm tra tính hợp lệ của ô đất, phản hồi bằng tín hiệu màu (Xanh: Hợp lệ / Đỏ: Bị vướng vật cản).

**3. KIẾN TRÚC DỮ LIỆU & KỸ THUẬT**

**3.1. Quản lý Dữ liệu Toàn cục**
* GameManager duy trì trạng thái xuyên suốt các cảnh (DontDestroyOnLoad).
* DataManager mã hóa và lưu trữ tệp .json tại Application.persistentDataPath, đảm bảo tiến trình của PLAYER (Vàng, Hàng tồn kho, Toạ độ đồ trang trí) không bị mất khi thoát game.

**3.2. Cấu trúc Cơ sở Dữ liệu (Database Architecture)**
* Áp dụng thiết lập bóc tách thực thể vật phẩm để tối ưu truy vấn:
    * **Trung gian sinh học:** Bảng CROP_DATA giữ vai trò cầu nối, nhận đầu vào là SeedID và trả đầu ra là ProductID.

---

# 2. USE CASE VÀ ĐẶC TẢ CHI TIẾT

**Dự án: FarmPuzzle**

## 1. DANH SÁCH TÁC NHÂN (ACTORS)
- **Người chơi (Player):** Tác nhân chính thực hiện các thao tác tương tác trực tiếp với giao diện game (gieo hạt, thu hoạch, giải đố, mua sắm).
- **Hệ thống (System):** Tác nhân phụ chạy ngầm, đảm nhiệm việc tính toán thời gian, xử lý logic va chạm, lưu/tải dữ liệu và theo dõi tiến độ.

## 2. DANH SÁCH USE CASE TỔNG THỂ
1. **Khởi tạo dữ liệu:** Hệ thống tạo mới hoặc tải dữ liệu lưu trữ (Save game) của người chơi.
2. **Trồng cây & Chăm sóc:** Người chơi gieo hạt và hệ thống tính toán thời gian sinh trưởng.
3. **Giải đố Mở đất:** Người chơi kéo thả khối gạch để phá chướng ngại vật trên bản đồ Isometric.
4. **Xuất hàng (Minigame Tetris):** Người chơi điều khiển khối gạch rơi tự do để hoàn thành đơn hàng.
5. **Cửa hàng & Mua sắm:** Người chơi sử dụng Vàng để đổi lấy vật phẩm/hạt giống.
6. **Xây dựng & Trang trí:** Người chơi đặt vật phẩm từ kho xuống bản đồ Nông trại.

## 3. ĐẶC TẢ USE CASE CHI TIẾT (USE CASE SPECIFICATIONS)

### 3.1. Đặc tả UC: Trồng cây & Chăm sóc (Farm Gameplay)
- **Tóm tắt:** Người chơi chọn hạt giống từ kho để gieo trồng. Hệ thống đếm thời gian để cây lớn lên và cho phép thu hoạch.
- **Điều kiện tiên quyết:** Người chơi có hạt giống và có ô đất trống hợp lệ.
- **Luồng sự kiện chính:**
    1. Người chơi kéo hạt giống từ giao diện túi đồ và thả xuống ô đất trống.
    2. Hệ thống kiểm tra vị trí thả. Nếu hợp lệ, ô đất chuyển trạng thái sang "Đã gieo hạt".
    3. Hệ thống bắt đầu đếm ngược thời gian sinh trưởng của loại cây đó.
    4. Khi hết thời gian, hệ thống đổi hình ảnh cây sang trạng thái "Có thể thu hoạch".
    5. Người chơi nhấn vào cây để thu hoạch.
    6. Hệ thống cộng nông sản vào kho đồ và dọn sạch ô đất.
    7. Cây đến giai đoạn trưởng thành thì có nhu cầu cần chăm sóc random.
- **Luồng ngoại lệ (Ngoại lệ 2a):** Người chơi thả hạt giống vào ô đất đã có cây hoặc ô đất bị khóa. Hệ thống từ chối thao tác, hoàn trả hạt giống về túi đồ và không trừ số lượng.

### 3.2. Đặc tả UC: Giải đố Mở đất (Block Puzzle)
- **Tóm tắt:** Người chơi lắp đầy các hàng/cột bằng khối gạch để phá vỡ chướng ngại vật (đá, cây khô), qua đó thu thập tài nguyên và mở rộng đất.
- **Điều kiện tiên quyết:** Màn chơi Mở đất đã được tải thành công.
- **Luồng sự kiện chính:**
    1. Hệ thống cung cấp ngẫu nhiên các khối gạch ở khay chứa bên dưới màn hình.
    2. Người chơi chọn, kéo và thả một khối gạch vào vị trí trống trên lưới (Grid).
    3. Hệ thống kiểm tra nếu có hàng dọc hoặc hàng ngang nào được lấp đầy thì tiến hành xóa các khối gạch ở hàng đó.
    4. Nếu hàng bị xóa đi qua vị trí có chướng ngại vật, hệ thống phá hủy chướng ngại vật đó.
    5. Hệ thống kích hoạt sự kiện rơi tài nguyên (Gỗ, Đá) và cộng vào túi đồ của người chơi.
- **Luồng ngoại lệ (Ngoại lệ 2a):** Người chơi kéo khối gạch ra ngoài phạm vi lưới hoặc đè lên khối gạch khác. Khối gạch sẽ tự động nảy (snap) trở lại khay chứa ban đầu.

### 3.3. Đặc tả UC: Xuất hàng (Tetris Minigame)
- **Tóm tắt:** Người chơi sắp xếp các khối nông sản rơi từ trên xuống để hoàn thành yêu cầu của Đơn hàng và nhận Vàng.
- **Điều kiện tiên quyết:** Người chơi kích hoạt tính năng Giao hàng.
- **Luồng sự kiện chính:**
    1. Hệ thống hiển thị Đơn hàng (ví dụ: Cần thu thập 10 khối Cà chua).
    2. Khối gạch tự động rơi từ trên xuống màn hình 2D.
    3. Người chơi thao tác di chuyển khối gạch sang Trái, Phải, Xoay hoặc Rơi nhanh.
    4. Khi khối gạch chạm đáy, hệ thống cố định khối gạch.
    5. Nếu 1 hàng ngang được xếp kín, hệ thống xóa hàng đó.
    6. Hệ thống đối chiếu các ô bị xóa với Đơn hàng để tăng tiến độ.
    7. Khi tiến độ đạt 100%, hệ thống cộng Vàng cho người chơi và kết thúc minigame.
- **Luồng ngoại lệ (Ngoại lệ 4a):** Các khối gạch bị xếp chồng quá cao và chạm đến trần màn hình. Trò chơi kết thúc, người chơi không nhận được phần thưởng của đơn hàng đang làm dở.

### 3.4. Đặc tả UC: Xây dựng & Trang trí (Decor)
- **Tóm tắt:** Người chơi đặt các vật phẩm trang trí đã mua lên bản đồ Nông trại.
- **Điều kiện tiên quyết:** Trong kho đồ của người chơi phải có ít nhất 1 vật phẩm trang trí.
- **Luồng sự kiện chính:**
    1. Người chơi mở chế độ Xây dựng và chọn một vật phẩm từ kho.
    2. Người chơi di chuyển vật phẩm trên lưới Isometric của Nông trại.
    3. Hệ thống liên tục kiểm tra va chạm (Collision). Nếu vị trí trống, hiển thị vùng sáng màu Xanh.
    4. Người chơi nhấn xác nhận để đặt vật phẩm xuống.
    5. Hệ thống trừ vật phẩm trong kho và lưu lại tọa độ của vật phẩm đó lên bản đồ.
- **Luồng ngoại lệ (Ngoại lệ 3a):** Vị trí đặt bị vướng cây trồng hoặc vật thể khác, hệ thống hiển thị vùng sáng màu Đỏ. Người chơi không thể nhấn nút xác nhận đặt.

---

# 3. BÁO CÁO ĐẶC TẢ YÊU CẦU PHẦN MỀM (SRS)

**Tên dự án:** FarmPuzzle (Game Nông Trại Kết Hợp Giải Đố)  
**Đơn vị đào tạo:** Cao đẳng FPT Polytechnic  
**Lớp:** ENT2227.06  
**Nhóm thực hiện:** Nhóm [Điền số nhóm]  

**Danh sách thành viên:**
1. Phạm Đức Dương (Dev - Farm & Kinh tế)
2. Hào Anh (Dev - Minigame Mở Đất)
3. Quyết (Dev - Minigame Tetris)
4. Vũ (Dev - Core & Cài đặt)
5. Vượng (Dev - Cửa hàng & Trang trí)
6. [Tên thành viên 6] (Dev - Meta & Âm thanh)

## 1. GIỚI THIỆU (INTRODUCTION)

### 1.1. Mục đích tài liệu
Tài liệu SRS này được lập ra nhằm đặc tả chi tiết các yêu cầu chức năng và phi chức năng của trò chơi FarmPuzzle. Tài liệu đóng vai trò làm kim chỉ nam cho nhóm 6 lập trình viên (Unity Developer) trong quá trình thiết kế, lập trình, kiểm thử và đồng nhất giao tiếp trong suốt vòng đời dự án.

### 1.2. Phạm vi dự án
FarmPuzzle là một tựa game 2D/Isometric phát triển trên nền tảng Unity. Game kết hợp giữa mô phỏng quản lý nông trại thời gian thực (farming simulator) và hai cơ chế giải đố (Block Puzzle & Tetris).

**Các mục tiêu chính:**
*   Hệ thống nông trại (trồng trọt, thu hoạch).
*   Minigame giải đố mở rộng diện tích đất (Land Puzzle).
*   Minigame giải đố để hoàn thành đơn hàng xuất nông sản (Tetris Puzzle).
*   Hệ thống kinh tế (Vàng, Kho đồ, Cửa hàng).
*   Hệ thống Core (Lưu trữ, Nhiệm vụ, Âm thanh).

## 2. MÔ TẢ TỔNG QUAN (OVERALL DESCRIPTION)

### 2.1. Nền tảng và Công nghệ
*   **Engine:** Unity 2022.3.x (hoặc LTS mới nhất).
*   **Ngôn ngữ:** C#.
*   **Nền tảng hướng tới:** PC (Windows) và WebGL (tương lai có thể lên Mobile).
*   **Quản lý dữ liệu:** SQLite (cho các bảng tĩnh: Item, Level, Quest) và JSON/PlayerPrefs (cho Save Game).

### 2.2. Phân hệ chức năng chính (Module)
1.  **Core & Data:** Quản lý luồng tải game và lưu trữ dữ liệu người chơi.
2.  **Farm System:** Logic trồng trọt, sinh trưởng và thu hoạch thời gian thực.
3.  **Land Puzzle:** Minigame kéo thả khối (block) để mở khóa ô đất.
4.  **Tetris Puzzle:** Minigame xếp gạch rơi tự do để hoàn thành đơn hàng.
5.  **Economy & Decor:** Quản lý tiền tệ, cửa hàng và hệ thống đặt vật phẩm trang trí.
6.  **Meta & Audio:** Hệ thống nhiệm vụ chạy ngầm và quản lý âm thanh tổng.

## 3. ĐẶC TẢ YÊU CẦU CHỨC NĂNG (FUNCTIONAL REQUIREMENTS)

### 3.1. Module Nông trại (Farm System)
*   **FR-1.1 (Gieo hạt):** Hệ thống cho phép người chơi kéo hạt giống từ kho xuống một ô đất trống. Trạng thái ô đất thay đổi thành 'Đã gieo hạt'.
*   **FR-1.2 (Sinh trưởng):** Cây trồng sẽ tự động phát triển qua các giai đoạn (Hạt -> Mầm -> Cây trưởng thành) dựa trên thông số thời gian của CropDataSO. Hệ thống phải tính toán được cả Offline Progress (thời gian người chơi tắt game).
*   **FR-1.3 (Thu hoạch):** Khi cây chín, người chơi tương tác để thu hoạch nông sản và hạt giống. Trạng thái ô đất quay về 'Ô đất trống'.

### 3.2. Module Giải đố Mở Đất (Land Puzzle)
*   **FR-2.1 (Sinh khối):** Hệ thống tạo ngẫu nhiên các khối gạch hình thù khác nhau để người chơi kéo thả vào lưới (Grid).
*   **FR-2.2 (Phá hủy và Rơi đồ):** Khi xếp đầy một hàng/cột, hệ thống tự động xóa các khối đó, đồng thời kích hoạt sự kiện phá vỡ các chướng ngại vật (đá, cây khô) để rơi ra tài nguyên (Gỗ, Đá).

### 3.3. Module Giải đố Xuất Hàng (Tetris Puzzle)
*   **FR-3.1 (Rơi tự do):** Các khối nông sản tự động rơi từ trên xuống. Người chơi có thể điều hướng Trái/Phải/Xoay/Rơi nhanh.
*   **FR-3.2 (Thanh toán đơn hàng):** Khi xếp kín hàng ngang, hàng đó biến mất. Hệ thống đối chiếu số lượng khối bị xóa với yêu cầu của OrderDataSO để quy đổi ra Vàng và cộng vào tài khoản người chơi.

### 3.4. Module Kinh tế & Trang trí (Economy & Decor)
*   **FR-4.1 (Cửa hàng):** Hiển thị danh sách hạt giống và đồ trang trí. Cho phép mua nếu số Vàng hiện có lớn hơn hoặc bằng giá vật phẩm.
*   **FR-4.2 (Xây dựng):** Người chơi lấy đồ trang trí từ kho, hệ thống hiển thị lưới (Grid) xanh/đỏ để báo hiệu vị trí hợp lệ. Chấp nhận đặt vật phẩm xuống nếu vị trí không bị cản trở.

### 3.5. Module Cốt lõi & Tiến trình (Core, Meta & Audio)
*   **FR-5.1 (Lưu dữ liệu):** Toàn bộ số lượng Vàng, vật phẩm trong kho, trạng thái ô đất và tiến trình nhiệm vụ phải được lưu xuống file cục bộ sau mỗi thay đổi lớn.
*   **FR-5.2 (Nhiệm vụ):** Hệ thống ngầm lắng nghe các thao tác của người chơi (VD: 'Thu hoạch 10 củ cải'). Kích hoạt nút 'Nhận thưởng' khi tiến độ đạt 100%.
*   **FR-5.3 (Âm thanh & Cài đặt):** Hệ thống tự động phát BGM. Người chơi có thể Bật/Tắt âm thanh trong màn hình Cài đặt, trạng thái Bật/Tắt được lưu lại cho lần mở game sau.

## 4. YÊU CẦU PHI CHỨC NĂNG (NON-FUNCTIONAL REQUIREMENTS)
*   **Hiệu năng:** Game duy trì ở mức tối thiểu 30FPS trên các thiết bị cấu hình thấp. Tối ưu hóa việc khởi tạo (Instantiate) và hủy (Destroy) object bằng Object Pooling cho hệ thống Tetris.
*   **Giao diện (UI/UX):** Các nút bấm phải to, rõ ràng, phù hợp với thao tác chạm (Touch) trên màn hình mobile. Phản hồi hình ảnh/âm thanh ngay lập tức khi click.
*   **Bảo mật:** Dữ liệu file Save (JSON) cần được mã hóa cơ bản (Base64) để tránh người chơi can thiệp sửa số Vàng một cách quá dễ dàng.
*   **Kiểm soát phiên bản:** Bắt buộc tuân thủ Git Flow. Không được commit file .meta bị lỗi hoặc các thư mục rác (như Library/, Temp/) lên repository.
