const http = require('http');

const url = "http://127.0.0.1:8080/mcp";

async function autonomousStep2Test() {
    console.log("🚀 [AUTONOMOUS-STEP2] Bắt đầu kiểm thử Input Blocking & Phân bón...");

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

            // Bước 2: Bấm PLAY (Nếu chưa bấm)
            console.log("🎮 Đang bấm PLAY...");
            await sendMcpPost({
                jsonrpc: "2.0", id: 2, method: "tools/call",
                params: { name: "manage_editor", arguments: { action: "play" } }
            });

            // Tạm dừng 2s để Unity ổn định Scene
            await new Promise(r => setTimeout(r, 2000));

            // Bước 3: Lấy tọa độ LandPlot_0_0
            console.log("📍 Đang lấy tọa độ LandPlot_0_0...");
            const posRes = await sendMcpPost({
                jsonrpc: "2.0", id: 3, method: "tools/call",
                params: {
                    name: "manage_components",
                    arguments: {
                        gameobject_path: "Grid/LandPlot_0_0",
                        component_name: "Transform",
                        action: "get_property",
                        property: "position"
                    }
                }
            });
            console.log("Tọa độ:", posRes.body);

            // Bước 4: Mở SHOP UI để Test Blocking
            console.log("🛒 Mở SHOP UI...");
            await sendMcpPost({
                jsonrpc: "2.0", id: 4, method: "tools/call",
                params: { name: "execute_menu_item", arguments: { menu_path: "FarmPuzzle/🔬 MCP Automated Test/Step 2: Simulate Click-Through" } }
            });

            console.log("🎉 Xong Kịch bản! Sếp hãy nhìn Console để thấy sự sai biệt giữa Click Đất và Click UI nhé!");
            process.exit(0);
        })();
    });

    req.end();
}

autonomousStep2Test();
