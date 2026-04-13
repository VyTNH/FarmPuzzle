const http = require('http');

const url = "http://127.0.0.1:8080/mcp";

async function ultimateAutonomousTest() {
    console.log("🚀 [ULTIMATE-TEST] BẮT ĐẦU CHIẾN DỊCH TỰ ĐỘNG HÓA...");

    const sseHeaders = { 'Accept': 'text/event-stream' };

    const req = http.request(url, { method: 'GET', headers: sseHeaders }, (res) => {
        const sessionId = res.headers['mcp-session-id'];
        if (!sessionId) { process.exit(1); }

        res.on('data', () => {}); 

        const sendMcpPost = (payload) => {
            return new Promise((resolve) => {
                const pData = JSON.stringify(payload);
                const pReq = http.request(url, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Accept': 'application/json, text/event-stream',
                        'mcp-session-id': sessionId,
                        'Content-Length': Buffer.byteLength(pData)
                    }
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
            // Bước 1: Initialize
            await sendMcpPost({
                jsonrpc: "2.0", id: 1, method: "initialize",
                params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "Antigravity", version: "1.0.0" } }
            });

            const callMcpTool = (name, args) => sendMcpPost({
                jsonrpc: "2.0", id: Math.floor(Math.random() * 1000),
                method: "tools/call",
                params: { name, arguments: args }
            });

            // 1. PLAY UNITY
            console.log("🎬 1. Đang bấm PLAY...");
            await callMcpTool("manage_editor", { action: "play" });
            await new Promise(r => setTimeout(r, 4000)); // Đợi Unity load Scene

            // 2. STEP 1: LOGIN (mcp_tester_001)
            console.log("👤 2. Đang Đăng nhập (Step 1)...");
            await callMcpTool("execute_menu_item", { menu_path: "FarmPuzzle/🔬 MCP Automated Test/Step 1: Test New Profile Login" });
            await new Promise(r => setTimeout(r, 1000));

            // 3. STEP 2a: CLICK TỰ DO (SUCCESS)
            console.log("🟢 3. Test Click Tự do (Kỳ vọng: Success)...");
            await callMcpTool("execute_menu_item", { menu_path: "FarmPuzzle/🔬 MCP Automated Test/Step 2: Test Click Blocking" });
            await new Promise(r => setTimeout(r, 1000));

            // 4. MỞ UI SHOP
            console.log("🛒 4. Đang mở Dashboard/Shop... (Simulate UI Open)");
            // Giả lập mở cửa sổ Dashboard (Sếp có Menu FarmPuzzle/Dashboard ko?)
            await callMcpTool("execute_menu_item", { menu_path: "FarmPuzzle/Open Designer Dashboard" }); 
            await new Promise(r => setTimeout(r, 1000));

            // 5. STEP 2b: CLICK KHI CÓ UI (BLOCK)
            console.log("🔴 5. Test Chặn Click (Kỳ vọng: BLOCK)...");
            await callMcpTool("execute_menu_item", { menu_path: "FarmPuzzle/🔬 MCP Automated Test/Step 2: Test Click Blocking" });
            await new Promise(r => setTimeout(r, 1000));

            // 6. ĐÓNG UI VÀ TEST BÓN PHÂN (STEP 3)
            console.log("🧪 6. Test Bón phân (Auto)...");
            // Gieo hạt trước (Giả lập bằng lệnh script)
            // (Hiện tại tôi chỉ có lệnh bón phân vào ô có sẵn hạt)
            await callMcpTool("execute_menu_item", { menu_path: "FarmPuzzle/🔬 MCP Automated Test/Step 3: Test Fertilizer (Auto)" });

            console.log("🏁 CHIẾN DỊCH HOÀN TẤT! Sếp hãy xem Log để đối chiếu kết quả nhé!");
            process.exit(0);
        })();
    });

    req.end();
}

ultimateAutonomousTest();
