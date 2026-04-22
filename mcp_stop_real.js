const http = require('http');

const url = "http://127.0.0.1:8080/mcp";

async function stopUnityReal() {
    console.log("🚀 [REAL-STOP-CMD] Đang dùng Công tắc tổng manage_editor...");

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
                jsonrpc: "2.0", id: 201, method: "initialize",
                params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "Antigravity", version: "1.0.0" } }
            });

            // Bước 2: DÙNG LỆNH manage_editor - ACTION: STOP
            console.log("⚡ Đang thực thi: manage_editor -> stop");
            const result = await sendMcpPost({
                jsonrpc: "2.0", id: 202, method: "tools/call",
                params: {
                    name: "manage_editor",
                    arguments: {
                        action: "stop"
                    }
                }
            });

            console.log(`📡 Phản hồi:`, result.body);
            console.log("🛑 ĐÃ DỪNG! Sếp thấy nút Play tắt chưa ạ?");
            process.exit(0);
        })();
    });

    req.end();
}

stopUnityReal();
