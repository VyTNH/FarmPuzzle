import requests
import json
import re

url = 'http://127.0.0.1:8080/mcp'
headers = {
    'Content-Type': 'application/json',
    'Accept': 'application/json, text/event-stream'
}

# Init session
init_payload = {
    'jsonrpc': '2.0', 'id': 1, 'method': 'initialize',
    'params': {
        'protocolVersion': '2024-11-05',
        'capabilities': {},
        'clientInfo': {'name': 'antigravity', 'version': '1.0'}
    }
}
r = requests.post(url, json=init_payload, headers=headers, timeout=5)
session_id = r.headers.get('mcp-session-id', '')
print('Session ID:', session_id)

headers['Mcp-Session-Id'] = session_id

# List tools
list_payload = {'jsonrpc': '2.0', 'id': 2, 'method': 'tools/list', 'params': {}}
r2 = requests.post(url, json=list_payload, headers=headers, timeout=10)
data = r2.text

# Extract tool names
names = re.findall(r'"name":"([^"]+)"', data)
print('Tools:')
for n in sorted(set(names)):
    print(' -', n)
