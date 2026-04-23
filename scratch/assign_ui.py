import requests
import json

url = 'http://127.0.0.1:8080/mcp'
headers = {
    'Content-Type': 'application/json',
    'Accept': 'application/json, text/event-stream'
}

code = """
using UnityEngine;
using FarmPuzzle.UI;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        MainMenuUI menu = Object.FindAnyObjectByType<MainMenuUI>();
        if (menu == null) {
            result.LogError("MainMenuUI not found");
            return;
        }

        GameObject decor = GameObject.Find("DecorShopPanel");
        GameObject seed = GameObject.Load("SeedShopPanel"); // Fallback if find fails
        GameObject topBar = GameObject.Find("Canvas_FarmTopBar");

        // Better search for inactive
        if (decor == null) decor = FindInactive("DecorShopPanel");
        if (seed == null) seed = FindInactive("SeedShopPanel");
        if (topBar == null) topBar = FindInactive("Canvas_FarmTopBar");

        result.RegisterObjectModification(menu);
        menu.decorShopPanel = decor;
        menu.seedShopPanel = seed;
        menu.farmTopBar = topBar;

        result.Log("UI references assigned successfully!");
    }

    private GameObject FindInactive(string name) {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>()) {
            if (go.name == name) return go;
        }
        return null;
    }
}
"""

def run():
    # Init
    r = requests.post(url, json={
        'jsonrpc': '2.0', 'id': 1, 'method': 'initialize',
        'params': {'protocolVersion': '2024-11-05', 'capabilities': {},
                   'clientInfo': {'name': 'antigravity', 'version': '1.0'}}
    }, headers=headers)
    sid = r.headers.get('mcp-session-id', '')
    
    # Call tool
    r = requests.post(url, json={
        'jsonrpc': '2.0', 'id': 2, 'method': 'tools/call',
        'params': {
            'name': 'execute_code',
            'arguments': {'action': 'execute', 'code': code, 'compiler': 'codedom'}
        }
    }, headers={'Mcp-Session-Id': sid, **headers})
    print(r.text)

if __name__ == "__main__":
    run()
