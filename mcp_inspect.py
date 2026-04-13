import requests
import json

url = 'http://127.0.0.1:8080/mcp'
headers = {
    'Content-Type': 'application/json',
    'Accept': 'application/json, text/event-stream'
}

def init():
    r = requests.post(url, json={
        'jsonrpc': '2.0', 'id': 1, 'method': 'initialize',
        'params': {'protocolVersion': '2024-11-05', 'capabilities': {},
                   'clientInfo': {'name': 'antigravity', 'version': '1.0'}}
    }, headers=headers, timeout=5)
    sid = r.headers.get('mcp-session-id', '')
    headers['Mcp-Session-Id'] = sid
    return sid

def call_tool(tool_name, args, call_id=10):
    r = requests.post(url, json={
        'jsonrpc': '2.0', 'id': call_id, 'method': 'tools/call',
        'params': {'name': tool_name, 'arguments': args}
    }, headers=headers, timeout=20)
    for line in r.text.split('\n'):
        if line.startswith('data:'):
            try:
                obj = json.loads(line[5:].strip())
                if 'result' in obj:
                    for c in obj['result'].get('content', []):
                        print(c.get('text', ''))
                elif 'error' in obj:
                    print("ERROR:", json.dumps(obj['error']))
            except:
                pass

def execute_cs(code, call_id=20):
    call_tool("execute_code", {"action": "execute", "code": code, "compiler": "codedom"}, call_id=call_id)

init()

print("=== INVENTORY INSPECTION ===\n")
code = """
UnityEngine.GameObject go = UnityEngine.GameObject.Find("Canvas_Inventory");
if (go == null) return "Canvas_Inventory NOT FOUND";

UnityEngine.Canvas canvas = go.GetComponent<UnityEngine.Canvas>();
string result = "Canvas_Inventory active=" + go.activeSelf + " canvas.enabled=" + (canvas != null ? canvas.enabled.ToString() : "null") + "\\n";

UnityEngine.Transform sc = null;
System.Collections.Generic.Queue<UnityEngine.Transform> queue = new System.Collections.Generic.Queue<UnityEngine.Transform>();
queue.Enqueue(go.transform);
while (queue.Count > 0) {
    UnityEngine.Transform t = queue.Dequeue();
    if (t.name == "SlotContainer") { sc = t; break; }
    foreach (UnityEngine.Transform ch in t) queue.Enqueue(ch);
}

if (sc == null) return result + "SlotContainer NOT FOUND";

UnityEngine.RectTransform scRect = sc.GetComponent<UnityEngine.RectTransform>();
result += "SlotContainer children=" + sc.childCount + "\\n";
result += "  anchorMin=" + scRect.anchorMin + " anchorMax=" + scRect.anchorMax + "\\n";
result += "  pivot=" + scRect.pivot + " sizeDelta=" + scRect.sizeDelta + "\\n";
result += "  rect=" + scRect.rect + " worldPos=" + scRect.position + "\\n";

UnityEngine.Transform bg = sc.parent;
UnityEngine.RectTransform bgRect = bg != null ? bg.GetComponent<UnityEngine.RectTransform>() : null;
UnityEngine.UI.Mask bgMask = bg != null ? bg.GetComponent<UnityEngine.UI.Mask>() : null;
UnityEngine.UI.RectMask2D bgRM2D = bg != null ? bg.GetComponent<UnityEngine.UI.RectMask2D>() : null;
UnityEngine.UI.Image bgImg = bg != null ? bg.GetComponent<UnityEngine.UI.Image>() : null;
result += "BackGround='" + (bg != null ? bg.name : "null") + "'\\n";
result += "  rect=" + (bgRect != null ? bgRect.rect.ToString() : "null") + " worldPos=" + (bgRect != null ? bgRect.position.ToString() : "null") + "\\n";
result += "  Mask=" + (bgMask != null) + " enabled=" + (bgMask != null ? bgMask.enabled.ToString() : "N/A") + "\\n";
result += "  RectMask2D=" + (bgRM2D != null) + "\\n";
result += "  Image=" + (bgImg != null) + " alpha=" + (bgImg != null ? bgImg.color.a.ToString("F2") : "N/A") + " imgEnabled=" + (bgImg != null ? bgImg.enabled.ToString() : "N/A") + "\\n";
result += "Slots:\\n";
for (int i = 0; i < sc.childCount; i++) {
    UnityEngine.Transform child = sc.GetChild(i);
    UnityEngine.RectTransform cr = child.GetComponent<UnityEngine.RectTransform>();
    result += "  [" + i + "] " + child.name + " active=" + child.gameObject.activeSelf + " worldPos=" + (cr != null ? cr.position.ToString() : "null") + "\\n";
}
return result;
"""
execute_cs(code, call_id=20)
