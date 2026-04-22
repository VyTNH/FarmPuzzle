# Test Plan – MCP Integration for Farm Puzzle

## Goal
Create a reproducible, semi‑automated manual test workflow that validates **all MCP‑related features** described in the design documents (`DesignDocs/*.md`). The workflow is split into **6 incremental steps**. After each step the assistant will report what was done, what remains, and wait for your confirmation before moving to the next step.

---

## 1. Preliminary Setup & Validation
### Test Cases
1. **Manifest & Package Validation** – Verify `manifest.json` is valid JSON (no BOM) and contains `com.unity.nuget.newtonsoft-json`.
2. **Assembly Definition Check** – Ensure `MCPForUnity.Editor.asmdef` and `MCPForUnity.Runtime.asmdef` reference `Newtonsoft.Json`.
3. **Clean Cache** – Delete `Library/PackageCache/com.coplaydev.unity-mcp*` and `packages-lock.json`.
### Expected Result
- Unity opens without entering Safe Mode.
- No CS0246 errors appear.
- MCP source files compile successfully.

---

## 2. Server Startup & Handshake
### Test Cases
1. **Start MCP Server** – Run the provided `uvicorn` command (or `python -m mcp_server`).
2. **GET /mcp (SSE)** – Client sends a GET request with `Accept: text/event-stream` and receives a `200` response, header `mcp-session-id` and a live SSE stream.
3. **Ping Command** – Send `POST /mcp?sessionId=...` with body `{ "method": "ping" }` and expect `{ "result": "pong" }`.
### Expected Result
- Server logs “Application startup complete”.
- Client receives a valid session ID.
- Ping returns `pong`.

---

## 3. Core Tool Registration
### Test Cases
1. **Tool Discovery** – Call `tools/list` and verify that the custom tools (`read_console`, `clear_console`, `execute_menu_item`) are present.
2. **Tool Execution – read_console** – Issue `POST` with `method: "tools/call", params: { "name": "read_console" }` and verify a JSON array of log entries is returned.
3. **Tool Execution – clear_console** – Issue `clear_console` and then call `read_console` again; expect an empty array.
### Expected Result
- All custom tools appear in the registry.
- `read_console` returns the current Unity log buffer (or the Ring‑Buffer content).
- After `clear_console` the buffer is empty.

---

## 4. UI Interaction Tests
### Test Cases
1. **Play Mode Toggle** – Use `execute_menu_item` with argument `{ "path": "Edit/Play" }` to start Play mode. Verify Unity’s `EditorApplication.isPlaying` becomes `true`.
2. **Pause/Resume** – Send `execute_menu_item` for `Edit/Pause` and `Edit/Resume`; verify the pause state toggles.
3. **Stop Play** – Send `execute_menu_item` for `Edit/Stop`; verify Play mode ends.
### Expected Result
- Unity transitions correctly between Edit, Play, Pause, and Stop states.
- No unexpected exceptions appear in the Unity console.

---

## 5. Data‑Integrity & Logging Validation
### Test Cases
1. **Generate Log Entries** – Trigger a known Unity log (e.g., call `Debug.Log("TEST_LOG")` from a temporary script).
2. **Read Log via MCP** – Call `read_console` and confirm the entry `TEST_LOG` is present with correct timestamp and log type.
3. **Ring‑Buffer Overflow** – Generate >1000 logs quickly and verify that the buffer rolls over, keeping only the most recent 1000 entries.
### Expected Result
- All generated logs appear in the MCP‑provided log list.
- Buffer size never exceeds 1000 entries.
- Oldest entries are discarded as expected.

---

## 6. End‑to‑End Scenario & Cleanup
### Test Cases
1. **Full Session** – Run steps 2‑5 sequentially in a single script to simulate a typical developer workflow.
2. **Graceful Shutdown** – Stop the MCP server and ensure Unity does not retain dangling connections.
3. **Project Re‑import** – Close Unity, delete the `Library` folder, reopen the project and verify that the MCP integration still works without manual re‑configuration.
### Expected Result
- The entire workflow completes without errors.
- Unity can be restarted cleanly and MCP functions remain operational.

---

## Open Questions (User Review Required)
- **Server launch command** – Do you prefer using the existing `uvicorn` command from `README.md` or a custom Python script (`call_login_mcp.py`)?
- **Log buffer size** – The current implementation caps at 1000 entries; should this be configurable?
- **Test automation** – Should the test cases be executed via a CI script (e.g., using Unity Test Runner) or manually via the provided Python/JS helpers?

## Verification Plan
- **Automated Tests** – Run the Python scripts in `Assets/Editor/Tools` that invoke MCP endpoints and assert JSON responses.
- **Manual Verification** – After each step the assistant will report the observed Unity console output and ask for your confirmation before moving to the next step.

---

*Please review the above 6‑step test plan, confirm the open questions, or suggest modifications. Once approved, I will start executing **Step 1** and report back.*
