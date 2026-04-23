# Báo Cáo Security Audit - FarmPuzzle Unity Project

**Ngày:** 2026-04-21  
**Threat Model:** Game Unity single-player với SQLite lưu trữ cục bộ. Mối đe dọa chính: chỉnh sửa save file, chỉnh sửa bộ nhớ.

---

## Tóm Tắt

| Mức độ | Số lượng |
|--------|----------|
| Critical | 2 |
| High | 4 |
| Medium | 3 |
| Low | 3 |

---

## 1. SQL Injection - LOW

**Tình trạng:** ✅ Không bị lỗi

Tất cả queries sử dụng SQLite.NET's typed LINQ API:
```csharp
DB.Table<PlayerModel>().Where(p => p.PlayerID == userID).FirstOrDefault();
```

---

## 2. Player Data Exposure - HIGH

**File:** `Assets/_Game/Scripts/Core/DataManager.cs`  
**Dòng:** 47

```csharp
string dbPath = Path.Combine(Application.persistentDataPath, "FarmPuzzleDB.db");
```

**Rủi ro:**
- Android: `/data/data/<package>/files/FarmPuzzleDB.db` đọc được qua file manager
- Windows: SQLite database dễ dàng truy cập và chỉnh sửa
- Player Money, inventory, quest progress đều trong plain DB

---

## 3. Inventory/Currency Manipulation - CRITICAL ⚠️

**File:** `Assets/_Game/Scripts/Core/DataManager.cs`  
**Dòng:** 232-254

```csharp
public void AddItem(string itemID, int amount) {
    int currentQty = _sessionInventory.ContainsKey(itemID) ? _sessionInventory[itemID] : 0;
    int newQty = currentQty + amount;
    _sessionInventory[itemID] = newQty;
    _sessionChanges.Add(itemID);
}
```

**Exploit:**
1. **Memory editing:** `_sessionInventory` dictionary có thể chỉnh sửa qua memory scanner
2. **Save file editing:** Chỉnh sửa SQLite trực tiếp set số lượng item tùy ý
3. **Không có server validation:** Tất cả client-authoritative

---

## 4. Input Validation - MEDIUM

**File:** `Assets/_Game/Scripts/Core/DataManager.cs`  
**Dòng:** 69+

```csharp
public bool LoginPlayer(string userID, string userName) {
    var player = DB.Table<PlayerModel>().Where(p => p.PlayerID == userID).FirstOrDefault();
```

**Rủi ro:** Không có giới hạn độ dài hay sanitization cho `userID` và `userName`.

---

## 5. Energy System Tampering - HIGH ⚠️

**File:** `Assets/_Game/Scripts/LandPuzzle/EnergySystem.cs`  
**Dòng:** 64-93

```csharp
private void LoadAndCalculateOfflineRegen() {
    if (PlayerPrefs.HasKey(PREF_LAST_TIME)) {
        string lastTimeStr = PlayerPrefs.GetString(PREF_LAST_TIME);
        if (long.TryParse(lastTimeStr, out long lastTicks)) {
            DateTime lastTime = new DateTime(lastTicks);
```

**Exploit:** Chỉnh sửa `Energy_LastRegenTime` trong PlayerPrefs sang quá khứ để nhận đầy energy khi vào game.

---

## 6. Quest Progress Tampering - HIGH ⚠️

**File:** `Assets/_Game/Scripts/Meta/Quest/QuestManager.cs`  
**Dòng:** 344-367

```csharp
private void SaveProgressToDB(string questID, int currentAmount, bool isBanned = false) {
    var rec = table.FirstOrDefault(r => r.PlayerID == pId && r.QuestID == questID);
    if (rec != null) { rec.QuestProgress = currentAmount; rec.IsBanned = isBanned; }
}
```

**Exploit:** Chỉnh sửa DB trực tiếp set `QuestProgress` và `IsBanned` để nhận thưởng quest nhiều lần.

---

## 7. No Anti-Cheat/Integrity Checks - MEDIUM ⚠️

**Tình trạng:** Không có checksums, signatures hay validation cho save file integrity.

---

## 8. Debug Logging Exposes Game State - LOW

**Tình trạng:** Extensive `Debug.Log` statements expose internal game state.

---

## 9. GameSimulatorWindow - DEBUG FEATURE - MEDIUM ⚠️

**File:** `Assets/_Game/Scripts/Editor/GameSimulatorWindow.cs`

**Lưu ý:** Scripts trong `Assets/_Game/Scripts/Editor/` được tự động exclude khỏi Unity builds. An toàn.

---

## Khuyến Nghị Bảo Mật

| Ưu tiên | Khuyến nghị |
|---------|-------------|
| Critical | Thêm integrity validation (hash của critical save data) |
| Critical | Mã hóa SQLite database hoặc implement save file encryption |
| High | Implement item acquisition rate limiting |
| High | Sử dụng giá trị obfuscated/encrypted cho energy timestamp |
| Medium | Thêm input validation (giới hạn độ dài, sanitization) |
| Medium | Xóa debug logging trong production builds |
| Medium | Audit ShopCatalogSO price validation |

---

## Các Cải Tiến Đã Thực Hiện

1. ✅ `DataManager.GetItemAmount()` giờ dùng RAM cache thay vì query DB mỗi lần
2. ✅ `DataManager.MigratePlayerData()` fix N+1 query pattern
3. ✅ Thêm constants cho magic numbers
