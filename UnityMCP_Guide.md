# Hướng dẫn sử dụng Unity MCP (Model Context Protocol)

Tài liệu này giúp bạn nắm bắt các tính năng chính và cách kết nối Unity với các hệ thống AI (LLM) thông qua giao thức MCP.

## 1. Thiết lập phía Unity (Server)
Để các tính năng hoạt động, bạn cần khởi chạy server trong Unity Editor:
- Vào menu **Window > Coplay > MCP Setup**.
- Kiểm tra các phụ thuộc (**Python** và **UV**) phải có màu xanh.
- Nhấn **Start Server**. 
- Mặc định server sẽ chạy tại: `http://localhost:8080/mcp`

## 2. Các chức năng chính (Khi đã kết nối với AI)

### 🌿 Quản lý Scene & Hierarchy
AI có thể giúp bạn thao tác với các đối tượng trong màn hình:
- **Liệt kê đối tượng:** "Liệt kê tất cả GameObjects trong Scene hiện tại."
- **Tìm kiếm:** "Tìm đối tượng có tên 'Player' và kiểm tra tag của nó."
- **Thao tác:** "Tạo một Cube mới tại vị trí (0, 0, 0)" hoặc "Xóa tất cả các đối tượng có tên 'Temp'."

### 🛠 Chỉnh sửa Component & Inspector
Không cần nhập thủ công các thông số:
- **Đọc dữ liệu:** "Kiểm tra Component Transform của LandPlot và cho tôi biết vị trí của nó."
- **Thay đổi giá trị:** "Đặt lại Scale của tất cả các cây trồng về 1."
- **Thêm/Xóa Component:** "Thêm Rigidbody vào Box nhưng tắt Gravity đi."

### 📂 Quản lý Project & Asset
- **Tìm Asset:** "Tìm tất cả các Prefab cây trồng trong thư mục Assets/Prefabs."
- **Tổ chức file:** "Tạo thư mục mới tên là 'Sprites' và di chuyển các hình ảnh vào đó."

### 🕹 Điều khiển Unity Editor
AI có thể điều khiển trực tiếp các nút bấm của Unity:
- **Play/Stop:** "Bắt đầu chạy game để test thử" hoặc "Dừng game lại."
- **Console:** "Đọc lỗi cuối cùng trong Console và giải thích tại sao nó bị lỗi."
- **Menu:** "Mở cửa sổ Build Settings."

## 3. Cách kết nối với các công cụ AI (Client)

Để AI của bạn (Claude, Cursor, Windsurf) có thể hiểu được Unity, bạn cần thêm cấu hình MCP:

### Đối với Cursor / Claude Desktop / Windsurf
Thêm vào file cấu hình (thường là `mcp_config.json` hoặc trong cài đặt MCP của phần mềm):

```json
{
  "mcpServers": {
    "Unity-Bridge": {
      "command": "uvx",
      "args": ["unity-mcp-bridge", "proxy", "--url", "http://localhost:8080/mcp"]
    }
  }
}
```

## 💡 Mẹo nhỏ
- **Giữ Unity luôn mở:** Server chỉ hoạt động khi Unity Editor đang chạy.
- **Refresh:** Nếu bạn thay đổi Scene hoặc thêm Script mới, hãy bảo AI "Refresh lại context" để nó cập nhật dữ liệu mới nhất.
- **An toàn:** Các lệnh thay đổi Scene (Xóa, Sửa) thường có thể Undo (Ctrl+Z), nên bạn cứ yên tâm thử nghiệm!
