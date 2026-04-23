const http = require('http');

const url = "http://127.0.0.1:8080/mcp";

async function listToolsAutonomous() {
    console.log("🚀 [AUTONOMOUS-DEV] Đang quét toàn bộ danh sách Tool...");

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
            // Bước 1: Initialize (Bắt buộc)
            await sendMcpPost({
                jsonrpc: "2.0", id: 1, method: "initialize",
                params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "Antigravity", version: "1.0.0" } }
            });

            // Bước 2: List Tools
            const result = await sendMcpPost({
                jsonrpc: "2.0", id: 2, method: "tools/list",
                params: {}
            });

            console.log("📡 MCP TOOLS LIST JSON:");
            console.log(result.body);
            process.exit(0);
        })();
    });

    req.end();
}

listToolsAutonomous();
