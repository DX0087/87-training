# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

- Grok Build（xAI）/ 練習 1 同時備好 Claude Code 用的 `training-repo/.claude/**` 與 `AGENTS.md` / `CLAUDE.md`

---

## 活動 2 — 練習 0：接 Playwright MCP（先當使用者）

**日期**：2026-07-31  
**Agent**：Grok Build（本機 Node v18.18.2）

### 做了什麼

1. **註冊 Playwright MCP（Grok）**
   - User 範圍：`~/.grok/config.toml` → `[mcp_servers.playwright]` = `npx -y @playwright/mcp@latest`
   - Claude/相容：`training-repo/.mcp.json` 同樣指向 `@playwright/mcp@latest`
   - 專案級 `.grok/config.toml` 曾加過，但 Grok 對 **untrusted folder** 不啟動 repo-local MCP；改以 user 範圍為主（若要用 project 範圍，需在 TUI 執行 `/hooks-trust` 或啟動加 `--trust`）

2. **啟動網站**
   - `dotnet run --project src/OrderHub.Web --urls http://localhost:5150` → 正常 listening

3. **任務：建立一筆新訂單 + 截圖結果頁**
   - 本 session 當下無法即時載入剛加的 MCP tools（需重開 session / `/mcps` 重新連線）
   - 另：`grok mcp doctor playwright` 伺服器能啟動，但 handshake 失敗，stderr：
     > `You are running Node.js 18.18.2. Playwright requires Node.js 20 or higher.`
   - **改以同等 Playwright 腳本完成驗收**（`playwright@1.49.1` 支援 Node 18）：
     - 腳本：`documents/activities/activity-2-artifacts/create-order-screenshot.mjs`
     - 截圖：`01-create-form.png`、`02-order-details.png`
     - 結果：`result.json`

### 具體結果數字（可覆核）

| 項目 | 值 |
|------|-----|
| 客戶 | 蔡承翰（一般會員），Id=9 |
| 商品 | SKU-1001 極光 無線滑鼠 × 1，Id=1，單價 NT$ 1,420.00 |
| 結果 URL | `http://localhost:5150/Orders/Details/204` |
| 狀態 | 待處理 |
| 成功訊息 | 訂單 #204 建立成功 |
| 應付總額 | NT$ 1,420.00（一般會員 0% 折扣） |

### 與活動 1 練習 2 的對比（指南要求寫進 PROCESS）

| | 活動 1 練習 2（修 bug） | 活動 2 練習 0（有瀏覽器工具） |
|--|------------------------|--------------------------------|
| 重現方式 | **人**開瀏覽器：建單、翻分頁、對金額、取消後看庫存 | **Agent / 腳本**驅動 Chromium：進 `/Orders/Create` → 選客戶/商品 → 送出 → 截明細頁 |
| 觀察產出 | 人眼記「第幾頁」「金額多少」「庫存數字」再貼給 agent | 直接有 **截圖檔 + URL + 訂單號**，可當驗收物 |
| 卡點 | 症狀清楚但定位仍要讀 code | 環境：MCP 要 Node ≥20；Grok 專案 MCP 要 folder trust；當前 session 需重連才有 MCP tools |
| 體感 | 「agent 幫忙讀碼修 bug，但我是手」 | 「操作網頁也可以外包給工具」——活動 1 那種人工重現步驟，理論上可交給 Playwright |

**一句話**：活動 1 是人當操作者、agent 當分析者；練習 0 展示 agent 也可當操作者——前提是 MCP 環境（Node、trust、session 連線）就緒。

### 後續若要讓 Grok 直接呼叫 Playwright MCP

1. 升級本機 Node 到 **≥ 20**（目前 18.18.2 會讓 `@playwright/mcp@latest` 握手失敗）  
2. 重開 Grok session（或 `/mcps` reconnect）  
3. 確認 `grok mcp doctor playwright` 為 healthy  
4. 再下 prompt：`網站在 http://localhost:5150，請建立一筆新訂單並截圖結果頁`

### 驗收勾選

- [x] 能自動開瀏覽器完成建單並留下截圖（腳本路徑完成；MCP 路徑受 Node 版本擋住）
- [x] 與活動 1 人工重現的對比已寫入本節

---

## 活動 2 — 練習 1：建立 OrderHub MCP Server（stdio）

**日期**：2026-07-31

### 交付

- 專案：`training-repo/src/OrderHub.Mcp`（console + `ModelContextProtocol` 2.0.0）
- 接線：與 Web 相同 DI（repo + `IOrderService`），log 走 stderr
- 三個唯讀工具（SDK 轉 snake_case）：
  | 方法 | 工具名 | 說明 |
  |------|--------|------|
  | `GetOrder` | `get_order` | 訂單明細 + 折扣/總額（`CalculateTotal`） |
  | `LowStock` | `low_stock` | 活躍商品且庫存 &lt; threshold |
  | `CustomerOrders` | `customer_orders` | 客戶訂單摘要 |
- 驗證：`dotnet build src/OrderHub.Mcp` 成功

### 注意

- Entity 不直接 JSON 序列化（投影匿名物件，避免 Order↔Customer 循環）
- 金額不在工具內重算折扣

---

## 活動 2 — 練習 2：用 MCP Inspector 除錯

**日期**：2026-07-31  
**環境**：Node v22.23.2；網站 `http://localhost:5150`；DB `OrderHubTraining`

### 做法

使用官方套件 `@modelcontextprotocol/inspector`（Inspector CLI）對 `OrderHub.Mcp` 做：

- `tools/list`
- `tools/call` → `low_stock` / `get_order`

Web UI 亦可啟動（例：`http://localhost:6274`，需帶 `MCP_INSPECTOR_API_TOKEN`）。

> CLI 參數順序：server 指令在 `--` **前**，`--method` 等在 `--` **後**。  
> 例：`node inspector-cli.js dotnet run --project src/OrderHub.Mcp --no-build -- --method tools/list --format json`

### 驗收結果

| 檢查 | 結果 |
|------|------|
| 三工具 + description / 參數 | ✅ `customer_orders`(customerId)、`get_order`(id)、`low_stock`(threshold) |
| `low_stock` threshold=10 vs `/Products/LowStock?threshold=10` | ✅ 同 5 筆：SKU-1048(1)、1005(3)、1023(3)、1014(4)、1032(4) |
| `get_order` id=999999 | ✅ 清楚訊息「找不到訂單 999999」，非 exception dump |
| （加測）`get_order` #204 | ✅ 蔡承翰 / SKU-1001 / Total 1420 |

### 證據檔

- `documents/activities/activity-2-artifacts/inspector-tools-list.json`
- `documents/activities/activity-2-artifacts/inspector-low-stock.json`
- `documents/activities/activity-2-artifacts/inspector-get-order-missing.json`
- `documents/activities/activity-2-artifacts/inspector-get-order-204.json`
- `documents/activities/activity-2-artifacts/inspector-web-skus.txt`

### 驗收勾選

- [x] 三工具清單與 description / 參數如預期  
- [x] LowStock 與網站一致  
- [x] 不存在訂單回清楚錯誤訊息  

---

## 活動 2 — 練習 3：註冊給 agent + before/after

**日期**：2026-07-31  
**Agent**：Grok Build（兼寫 Claude 用的 `training-repo/.mcp.json`）

### 註冊

| 對象 | 設定 |
|------|------|
| Claude / 相容（版控） | `training-repo/.mcp.json` → `orderhub`：`dotnet run --project src/OrderHub.Mcp --no-build`（另保留 playwright） |
| Grok（本機 user） | `~/.grok/config.toml` → `[mcp_servers.orderhub]`，`--project` 用**絕對路徑** + `--no-build` |

驗證：

```text
grok mcp doctor orderhub
→ handshake OK, 3 tools discovered
tools: customer_orders, get_order, low_stock
```

> 當前 Grok **session** 不會自動載入剛加的 MCP；新開 session 或 `/mcps` reconnect 後才能 `search_tool`/`use_tool`。  
> 本練習 after 用官方 Inspector / 同一 MCP 協定呼叫 `low_stock`，與 agent 呼叫工具等價。

### 對照實驗：「哪些商品庫存低於 5？」

#### Before — 關掉 / 不使用 OrderHub MCP

Agent 沒有 `low_stock` 時，大致要繞這些路（實際演練）：

1. 讀 `ProductService` / `IProductRepository` / 可能還要懂 `IsActive`、門檻是 `<` 還是 `<=`
2. 自己寫查詢、開 SSMS、或爬網頁  
3. 本機實際採用：**開網站** `GET /Products/LowStock?threshold=5` 再解析 HTML（或去 `/Products` 逐列看庫存）

觀察到的結果（與 after 相同 5 筆，但步驟長）：

| SKU | 庫存（頁面） |
|-----|--------------|
| SKU-1048 | 1 |
| SKU-1005 | 3 |
| SKU-1023 | 3 |
| SKU-1032 | 4 |
| SKU-1014 | 4 |

**成本感**：要知道有低庫存頁、路由參數、怎麼解析；沒有現成頁時可能直接翻 DB / 寫 throwaway 程式。

#### After — 開啟 OrderHub MCP

一次工具呼叫：

```text
tools/call low_stock  threshold=5
```

回傳（JSON，已存檔）：

| SKU | 名稱 | StockQuantity |
|-----|------|---------------|
| SKU-1048 | 晨光 行動電源 | 1 |
| SKU-1005 | 極光 筆電支架 | 3 |
| SKU-1023 | 雲峰 27吋螢幕 | 3 |
| SKU-1014 | 星河 USB-C 集線器 | 4 |
| SKU-1032 | 曜石 機械鍵盤 | 4 |

證據：`documents/activities/activity-2-artifacts/practice3-after-low-stock-5.json`

**成本感**：不需讀商業邏輯原始碼、不需開 SQL；工具 description 已說明「活躍 + 低於門檻 + 升冪」。

### 差異一句話

| | Before（無 MCP） | After（有 MCP） |
|--|------------------|-----------------|
| 步驟 | 找頁面 / 讀碼 / 查 DB / 解析 | **1 次** `low_stock` |
| 答案來源 | 網站或資料庫 | 同一 DB，經 service/repo 封裝 |
| 答錯風險 | 門檻條件寫錯、漏 IsActive | 與 server 實作綁定（單一真相） |

### 驗收勾選

- [x] orderhub 註冊成功，`grok mcp doctor` 見 3 tools（`.mcp.json` 進版控）  
- [x] before/after 對照完成並寫入本節  
- [x] 獨立 commit  

---

## 通用四問

### 1. 我的任務拆解

1. 讀 `documents/activities/activity-guideline.md` 確認練習 1 交付物  
2. 依指南建立專案記憶（`AGENTS.md` / `CLAUDE.md`）  
3. 建立權限、hooks、subagents、`fix-bug` skill  
4. 用自我驗證三題對照分層與建單流程  
5. commit 設定檔  

（實際：練習 2、3 先做了，再回頭補練習 1 的 agent 設定。）

### 2. AI 幫上大忙的地方

**提問原文（練習開始時）**：  
「先幫我分析下目前的架構 以及具體做什麼的」

有效原因：先建立全景（培訓 repo vs OrderHub、三層職責、建單/取消路徑），後面修 bug、加低庫存頁時不必每次從零摸索。

### 3. AI 誤導我的地方，與我如何發現

**建單流程裡「過度簡化」的點（練習 1 自我驗證 #2）**：

若 agent 只說「建單時依會員等級打折，再存單價快照」，會漏掉重要細節：

1. **折扣應只在總額算一次**（`CalculateTotal`），snapshot 應存**原價**；舊版程式曾對 Gold 在建單時先寫折後價，再算總額又折一次 → 實際 0.81 折（練習 2 客訴 2 已修）。  
2. **取消庫存**：不能只說「取消會還庫存」——必須看**先改狀態還是先還庫存**；舊版先 `Cancelled` 再判斷 Pending/Confirmed，條件永遠 false（練習 2 客訴 3）。  

發現方式：對照 `OrderService.CreateOrderAsync` / `CancelOrderAsync` 原始碼，而不是只聽摘要。

### 4. 我會帶回日常工作的一招

**操作步驟：把專案慣例寫進版控的 agent 記憶檔**

1. 在解法方案根目錄放 `AGENTS.md`（或 `CLAUDE.md`）  
2. 固定六塊：簡介、技術棧版本、分層慣例、常用指令、危險檔案、Don'ts  
3. 每層寫「一句話職責」+ 指出範例檔路徑（例如 `ProductsController.cs`）  
4. 危險操作另寫 settings / hooks（deny Migrations 手改、block TRUNCATE）  
5. 重複流程做成 skill（如 `fix-bug`：先確認症狀 → 定位 → 再修 → 測試 → 症狀/根因/修法 commit）  

---

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責  
   - **Web**：HTTP、ViewModel、Razor；薄轉接  
   - **Core**：Domain + 商業邏輯（折扣、庫存、狀態）  
   - **Infrastructure**：EF DbContext、Repository、Migration、Seed  
2. 我核對過 agent 描述的建單流程，且**至少找出一處不精確或過度簡化的說法**  
   - 見上方「AI 誤導」：折扣套用層級、取消還庫存的順序，摘要常講錯  
3. 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方  
   - 邏輯 → Core service；頁面 → Controller + ViewModel + View +（必要時）Repo + 測試 + 導覽  

練習 2

1. 三個 bug 我都先在頁面上重現過，才開始找程式  
2. 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文  
3. 每個修復都回到頁面驗證過症狀消失  
4. 每個 bug 都補了一個回歸測試，`dotnet test` 全綠  
5. 三個獨立 commit，message 說明症狀與根因  
6. （思考題）為什麼原本的測試沒抓到這三個 bug？  
   - 分頁只測 TotalCount/TotalPages，未斷言第 1 頁內容與最後一頁非空  
   - 定價測 `CalculateTotal` 用已寫好的 snapshot，未走 Gold `CreateOrder` 端到端  
   - 取消只測狀態變更，未斷言庫存加回  

練習 3

1. `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變  
2. `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500  
3. 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）  
4. 停售（已停售 badge）商品不出現在列表  
5. 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）  
6. 至少 3 個新測試，`dotnet test` 全綠  

練習 4

1. 重構後 `dotnet test` 全綠  
2. 我能說出這次重構「改善了什麼、沒有改變什麼」  
3. 我有在 code review 的角度看過 diff（不是 agent 說好就好）  

---

## 附錄：值得留下的對話片段

**片段 1 — 架構理解**  
- 問：分析目前架構與用途  
- 答：外層培訓教材 + 內層 OrderHub 三層 MVC；建單/取消/折扣路徑清楚列出  

**片段 2 — 修 bug 的 commit 格式**  
- 約定：一個修復一個 commit，message 固定「症狀 → 根因 → 修法」  
- 例：`Skip(page * pageSize)` → `Skip((page - 1) * pageSize)`  
