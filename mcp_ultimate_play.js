const http = require('http');

const url = "http://127.0.0.1:8080/mcp";

async function executeMcpAction() {
    console.log("🚀 [CHATGPT-POWER] Bắt đầu kết nối Unity MCP...");

    // Bước 1: GET để lấy mcp-session-id từ Header
    const getOptions = {
        method: 'GET',
        headers: { 'Accept': 'text/event-stream' }
    };

    const req = http.request(url, getOptions, (res) => {
        const sessionId = res.headers['mcp-session-id'];
        
        if (!sessionId) {
            console.error("❌ LỖI: Không tìm thấy mcp-session-id trong header!");
            process.exit(1);
        }

        console.log(`✅ Đã bắt được Session ID: ${sessionId}`);

        // Duy trì kết nối SSE (Không đóng req này)
        res.on('data', (chunk) => {
            // Đọc log từ Unity nếu có
            // console.log("Unity Log:", chunk.toString());
        });

        // Payload gọi trực tiếp Menu Item "Thần thánh"
        const postData = JSON.stringify({
            jsonrpc: "2.0",
            id: Date.now(),
            method: "tools/call",
            params: {
                name: "execute_menu_item",
                arguments: {
                    menu_path: "FarmPuzzle/🔬 MCP Automated Test/Step 1: Test New Profile Login"
                }
            }
        });

        const postOptions = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json, text/event-stream', // ĐỦ CẢ HAI THEO ĐÚNG Ý UNITY!
                'mcp-session-id': sessionId,
                'Content-Length': Buffer.byteLength(postData)
            }
        };

        console.log("⚡ Đang bắn lệnh Login qua cổng Header...");
        const postReq = http.request(url, postOptions, (postRes) => {
            let body = '';
            postRes.on('data', (d) => body += d);
            postRes.on('end', () => {
                console.log(`📡 Phản hồi từ Unity (${postRes.statusCode}):`, body);
                console.log("🎉 XONG! Sếp nhìn màn hình Unity nhé!");
                process.exit(0);
            });
        });

        postReq.on('error', (e) => console.error("❌ Lỗi POST:", e));
        postReq.write(postData);
        postReq.end();
    });

    req.on('error', (e) => console.error("❌ Lỗi GET:", e));
    req.end();
}

executeMcpAction();
