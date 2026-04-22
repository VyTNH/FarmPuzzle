const http = require('http');

const url = "http://127.0.0.1:8080/mcp";

async function getToolSchema() {
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
            await sendMcpPost({ jsonrpc: "2.0", id: 1, method: "initialize", params: { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "Antigravity", version: "1.0.0" } } });

            const result = await sendMcpPost({
                jsonrpc: "2.0", id: 2, method: "tools/list",
                params: {}
            });

            const cleanBody = result.body.replace(/^event: message\ndata: /m, "").trim();
            const data = JSON.parse(cleanBody);
            const tool = data.result.tools.find(t => t.name === "read_console");
            console.log("🔍 READ_CONSOLE SCHEMA:");
            console.log(JSON.stringify(tool.inputSchema, null, 2));
            process.exit(0);
        })();
    });

    req.end();
}

getToolSchema();
