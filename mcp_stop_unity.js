const http = require('http');

const url = "http://127.0.0.1:8080/mcp";

async function stopUnityPlayMode() {
    console.log("🚀 [STOP-UNITY-CMD] Đang bắt tay để bấm nút STOP...");

    const sseHeaders = { 'Accept': 'text/event-stream' };

    // Bước 1: GET Lấy Session
    const req = http.request(url, { method: 'GET', headers: sseHeaders }, (res) => {
        const sessionId = res.headers['mcp-session-id'];
        if (!sessionId) { console.error("❌ Lỗi Session!"); process.exit(1); }
        console.log(`✅ Session ID: ${sessionId}`);

        res.on('data', () => {}); // Duy trì luồng SSE

        const commonHeaders = {
            'Content-Type': 'application/json',
            'Accept': 'application/json, text/event-stream',
            'mcp-session-id': sessionId
        };

        const sendMcpPost = (payload) => {
            return new Promise((resolve) => {
                const pData = JSON.stringify(payload);
                const pReq = http.request(url, {
                    method: 'POST',
                    headers: { ...commonHeaders, 'Content-Length': Buffer.byteLength(pData) }
                }, (pRes) => {
                    let body = '';
                    pRes.on('data', (d) => body += d);
                    pRes.on('end', () => resolve({ code: pRes.statusCode, body }));
                });
                pReq.write(pData);
                pReq.end();
            });
        };

        (async () => {
            // Bước 2: Initialize
            console.log("🛠️ Bước 2: Khởi tạo MCP...");
            await sendMcpPost({
                jsonrpc: "2.0", id: 101, method: "initialize",
                params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "Antigravity", version: "1.0.0" } }
            });

            // Bước 3: Bấm Edit/Play (vì nó là toggle, bấm lúc đang Play sẽ là STOP)
            console.log("⚡ Bước 3: Đang nhấn nút STOP (Edit/Play toggle)...");
            const result = await sendMcpPost({
                jsonrpc: "2.0", id: 102, method: "tools/call",
                params: {
                    name: "execute_menu_item",
                    arguments: { menu_path: "Edit/Play" }
                }
            });

            console.log(`📡 Phản hồi từ Unity (${result.code}):`, result.body);
            console.log("🛑 STOP! Sếp thấy Unity thoát Play Mode chưa ạ?");
            process.exit(0);
        })();
    });

    req.end();
}

stopUnityPlayMode();
