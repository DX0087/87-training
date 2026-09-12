# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

- Grok Build（xAI）/ 練習 1 同時備好 Claude Code 用的 `training-repo/.claude/**` 與 `AGENTS.md` / `CLAUDE.md`

---

## 活動 2 — 練習 0：接 Playwright MCP（先當使用者）

**日期**：2026-07-31（腳本備援）／**2026-08-08 重驗（純 MCP）**  
**Agent**：Grok Build  
**Node**：v18.18.2（初驗）→ **v22.23.2**（重驗通過）

### 做了什麼

1. **註冊 Playwright MCP（Grok）**
   - User 範圍：`~/.grok/config.toml` → `[mcp_servers.playwright]` = `npx -y @playwright/mcp@latest`
   - Claude/相容：`training-repo/.mcp.json` 同樣指向 `@playwright/mcp@latest`
   - 專案級 `.grok/config.toml` 曾加過，但 Grok 對 **untrusted folder** 不啟動 repo-local MCP；改以 user 範圍為主（若要用 project 範圍，需在 TUI 執行 `/hooks-trust` 或啟動加 `--trust`）

2. **啟動網站**
   - 在 `training-repo/`：`dotnet run --project src/OrderHub.Web --urls http://localhost:5150` → 正常 listening

3. **任務：建立一筆新訂單 + 截圖結果頁**

#### 初驗（2026-07-31）— 腳本備援

- 當時 Node 18：`grok mcp doctor playwright` handshake 失敗：
  > `You are running Node.js 18.18.2. Playwright requires Node.js 20 or higher.`
- **改以同等 Playwright 腳本驗收**（`playwright@1.49.1` 支援 Node 18）：
  - 腳本：`documents/activities/activity-2-artifacts/create-order-screenshot.mjs`
  - 結果：訂單 **#204**；截圖 `01-create-form.png`、`02-order-details.png`

#### 重驗（2026-08-08）— 純 Playwright MCP（通過）

- `node -v` → **v22.23.2**
- `grok mcp doctor playwright` → **handshake OK**，protocol `2025-06-18`，**24 tools discovered**
- Agent 直接呼叫 MCP（無需腳本）：
  1. `browser_navigate` → `/Orders/Create`
  2. `browser_select_option` 客戶「蔡承翰」、商品 SKU-1001
  3. `browser_take_screenshot` → `01-create-form.png`
  4. `browser_click`「送出訂單」
  5. `browser_take_screenshot` → `02-order-details.png`
- **設定無需改動**：既有 `~/.grok/config.toml` + `training-repo/.mcp.json` 即可；只缺 Node ≥20

### 具體結果數字（可覆核）

| 項目 | 初驗（腳本） | 重驗（MCP） |
|------|-------------|-------------|
| 客戶 | 蔡承翰（一般會員） | 蔡承翰（一般會員） |
| 商品 | SKU-1001 極光 無線滑鼠 × 1 | SKU-1001 極光 無線滑鼠 × 1 |
| 結果 URL | `/Orders/Details/204` | `/Orders/Details/205` |
| 狀態 | 待處理 | 待處理 |
| 成功訊息 | 訂單 #204 建立成功 | 訂單 #205 建立成功 |
| 應付總額 | NT$ 1,420.00 | NT$ 1,420.00 |
| 路徑 | `create-order-screenshot.mjs` | Playwright MCP tools |
| 證據 | `result.json`（已覆寫為 MCP 重驗） | 同左；截圖已更新 |

### 與活動 1 練習 2 的對比（指南要求寫進 PROCESS）

| | 活動 1 練習 2（修 bug） | 活動 2 練習 0（有瀏覽器工具） |
|--|------------------------|--------------------------------|
| 重現方式 | **人**開瀏覽器：建單、翻分頁、對金額、取消後看庫存 | **Agent + Playwright MCP** 驅動 Chromium：進 `/Orders/Create` → 選客戶/商品 → 送出 → 截明細頁 |
| 觀察產出 | 人眼記「第幾頁」「金額多少」「庫存數字」再貼給 agent | 直接有 **截圖檔 + URL + 訂單號**，可當驗收物 |
| 卡點 | 症狀清楚但定位仍要讀 code | 初驗卡在 Node ＜20；升級後 MCP 即可用。另：Grok 專案 MCP 要 folder trust |
| 體感 | 「agent 幫忙讀碼修 bug，但我是手」 | 「操作網頁也可以外包給工具」——活動 1 那種人工重現步驟，可交給 Playwright MCP |

**一句話**：活動 1 是人當操作者、agent 當分析者；練習 0 展示 agent 也可當操作者——Node ≥20 + MCP 連線就緒後，無需手寫腳本即可完成建單與截圖。

### 驗收勾選

- [x] 能自動開瀏覽器完成建單並留下截圖（**2026-08-08 純 MCP 路徑通過**；初驗曾用腳本備援）
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

## 活動 2 — 練習 4：cancel_order（會改資料的工具）

**日期**：2026-07-31

### 實作

`OrderHubTools.cs`：

| 工具 | 標註 | 行為 |
|------|------|------|
| `get_order` / `low_stock` / `customer_orders` | `ReadOnly = true` | 唯讀 |
| `cancel_order` | `Destructive = true`, `Idempotent = false` | 轉接 `OrderService.CancelOrderAsync`（不重寫狀態/庫存規則） |

### Inspector annotations（`tools/list`）

| name | readOnlyHint | destructiveHint | idempotentHint |
|------|--------------|-----------------|----------------|
| get_order | true | — | — |
| low_stock | true | — | — |
| customer_orders | true | — | — |
| cancel_order | — | true | false |

### 行為驗證（官方 Inspector CLI）

| 步驟 | 結果 |
|------|------|
| 取消待處理 **#204** | `訂單 204 已取消,庫存已回補` |
| 庫存 SKU-1001 | 取消前 **24** → 取消後 **25**（+1，回補成功） |
| 再取消 #204 | `取消失敗:狀態為 Cancelled 的訂單不可取消`（清楚訊息，非 exception） |
| 取消 #999999 | `取消失敗:找不到指定的訂單` |

證據：`documents/activities/activity-2-artifacts/practice4-verify.json`、`practice4-tools-list.json`

### 心得（地雷）

- **標註是 hint，不是強制**：client 可依 `destructiveHint` 跳確認；真正規則在 `CancelOrderAsync`。  
- 唯讀工具若不標 `ReadOnly`，預設可能被當成有破壞性 → 多餘確認。  
- 錯誤訊息要可讀，agent 才不會瞎重試。

### Agent 確認提示

Grok 本 session 未必自動掛新工具；重開 session 後對 agent 說「取消訂單 X」時，應看到破壞性工具的權限確認（依 client 政策）。本練習行為面已用 Inspector 直接 call 驗證。

### 驗收勾選

- [x] annotations 正確（3 唯讀 + cancel destructive/non-idempotent）  
- [x] 取消成功 + 庫存回補  
- [x] 重複取消 / 不存在訂單 → 清楚拒絕  
- [x] PROCESS + 獨立 commit  

---

## 活動 2 — 練習 5：Resources 與 Prompts

**日期**：2026-07-31

### 交付

| 檔案 | 原語 | 內容 |
|------|------|------|
| `OrderHubResources.cs` | Resource | `orderhub://discount-rules`（markdown 會員折扣） |
| `OrderHubPrompts.cs` | Prompt | `low_stock_report`（threshold 可調，引導用 `low_stock` 出採購表） |
| `Program.cs` | 註冊 | `.WithResources<>()` + `.WithPrompts<>()` |

### Inspector 驗收

| 方法 | 結果 |
|------|------|
| `resources/list` | `會員折扣規則` @ `orderhub://discount-rules`，mime `text/markdown` |
| `resources/read` | 含 Standard / Silver 95 折 / Gold 9 折、總額折一次、snapshot 原價 |
| `prompts/list` | `low_stock_report`，參數 `threshold`（非必填） |
| `prompts/get` threshold=5 | 展開 user 訊息，明確要求 `low_stock(threshold=5)` 與採購建議表 |

證據：`documents/activities/activity-2-artifacts/practice5-verify.json`

### 5c 思考題

**1. 折扣規則用 Resource 給，vs 讓 agent 自己讀 `OrderService.cs`，差在哪？**

| | Resource | 讀原始碼 |
|--|----------|----------|
| 對象 | 給 agent 的**產品知識**（規則摘要） | 實作細節（switch、方法名） |
| 成本 | 固定短文，進 context 便宜 | 要搜檔、可能讀錯/讀過期片段 |
| 共用 | 全隊同一 URI、可版控 | 每人自己找，答案容易不一致 |
| 風險 | 字串與程式**兩份真相**（規則改了要同步 resource，或改成動態從 service 組字） | 與執行中行為可能脫節（看舊碼） |

→ Resource 適合「穩定、要反覆引用的背景」；真正算錢仍應走 tool/`CalculateTotal`，不要在 resource 裡發明第二套公式。

**2. Prompt 範本放 server，vs 每人自己打一段話，差在哪？**

| | Server Prompt | 各自口頭 prompt |
|--|---------------|-----------------|
| 一致性 | 採購報告格式/步驟統一 | 有人忘 threshold、有人漏排除 Cancelled |
| 改版 | 改一處 `OrderHubPrompts.cs` + 部署 | 改一堆聊天記錄/文件 |
| 與 tool 合體 | 範本**點名** `low_stock`，引導正確工具 | 可能改去爬頁或亂讀 DB |
| 入口 | Claude 可變成 `/mcp__orderhub__low_stock_report` | 無標準 slash |

### 三者分工（記住）

- **Tool** = 動作（查 / 改）  
- **Resource** = 資料（放進 context）  
- **Prompt** = 範本（替使用者說話，常再驅動 tool）  

### 驗收勾選

- [x] Inspector 讀得到 resource 與 prompt（含 threshold 展開）  
- [x] 5c 思考寫入本節  
- [x] 獨立 commit  

---

## 活動 3 — 練習 1：自然語言查訂單 API

**日期**：commit `0cee04a` 實作／**2026-09-12 補記與復驗**  
**環境**：網站 `http://localhost:5150`、`gemini-3.5-flash`

### 交付

| 層 | 檔案 | 職責 |
|----|------|------|
| Core | `Ai/OrderSearchQuery.cs` | **白名單參數**：`Status` / `MemberTier` / `DateFrom` / `DateTo`，外加 `HasAnyFilter` |
| Core | `Ai/IOrderQueryTranslator.cs`、`Services/OrderSearchService.cs` | 翻譯介面 + 查詢 service（回 `ServiceResult`） |
| Core | `Ai/AiServiceUnavailableException.cs` | 上游不可用的專用例外 |
| Infrastructure | `Gemini/GeminiInteractionsClient.cs`、`GeminiOrderQueryTranslator.cs`、`GeminiOptions.cs` | 裸 `HttpClient` 打 Interactions API、structured output、重試 |
| Web | `Controllers/Api/OrdersApiController.cs` | `POST /api/orders/search`，只轉接 service |

### 關鍵設計：模型只能產參數，不能產 SQL

`OrderSearchQuery` 只有四個欄位，LLM 回來的 JSON 就算亂寫也只能填進這四格，SQL 一律由 EF Core 從參數生成。「刪除意圖」在翻譯層就變成 `intent: unsupported`，走不到 repository。

### 驗收結果（2026-09-12 實打）

| 查詢文字 | HTTP | 回應 |
|----------|------|------|
| 過去 30 天取消的訂單 | 200 | 1 筆：#212 陳志明 / Gold / Cancelled / NT$2,088 |
| 過去 90 天金卡會員取消的訂單 | 200 | 6+ 筆，全部 `tier=Gold` + `status=Cancelled`（#212、#210、#207、#137、#155、#181…）→ 三個白名單維度都生效 |
| 上個月金卡會員取消的訂單 | 200 | `[]`——**不是 bug**：種子資料在上個月沒有金卡取消單，改問「過去 90 天」就有 |
| 幫我把所有訂單刪掉 | **422** | `{"error":"無法理解的查詢"}`，資料毫髮無傷 |
| 「蕃茄炒蛋的做法：先打三顆蛋」 | **422** | `{"error":"無法理解的查詢"}`，不會炸 |

### 地雷：API key 曾被寫死在原始碼裡（2026-09-12 發現）

跑活動 4 時 `/api/orders/search` 一直回「Gemini API key 未設定」，後來是在 `GeminiInteractionsClient.cs` 把 key 直接寫成字串常值才「會動」。三個問題，記著別再犯：

1. 那是**版控追蹤中的檔案**，一 commit 就外流（本次發現時尚未 commit，來得及）
2. 寫死之後 `_options.ApiKey ?? Environment.GetEnvironmentVariable(...)` 這條 fallback 永遠不會執行，**驗收項「拔掉 key 應回 503」等於被架空**
3. CLAUDE.md 明列機密不進設定檔／原始碼

正解只有兩個位置：`dotnet user-secrets set "Gemini:ApiKey" ...`（存在 repo 外的 `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`），或啟動 shell 的環境變數 `GEMINI_API_KEY`。

### 驗收勾選

- [x] 正常查詢查得出結果，白名單三維度（狀態／等級／日期）都生效
- [x] 「幫我把所有訂單刪掉」→ 422，資料無損
- [x] 無關文字 → `unsupported` → 422，不會炸
- [ ] 拔掉 API key → 503：**待復驗**（key 寫死期間這條測不準，還原成讀 config 後要重測）

---

## 活動 3 — 練習 2：同一個 service 接上網站頁面

**日期**：commit `6e039ee`

### 交付

| 檔案 | 內容 |
|------|------|
| `Controllers/OrdersController.cs:121` | `Search(string? q, CancellationToken)`，呼叫同一個 `IOrderSearchService` |
| `ViewModels/OrderSearchViewModel.cs` | 頁面綁的 ViewModel（不把 domain model 丟給 View） |
| `Views/Orders/Search.cshtml` | 查詢框 + 結果表 + 錯誤提示 |
| `Views/Shared/_Layout.cshtml` | 導覽列加入口 |

### 心得：API 與頁面共用一個 service，差別只在「怎麼呈現失敗」

同一個 `OrderSearchService`，API 把 `ServiceResult` 的失敗翻成 422 / 503 JSON，頁面翻成警示區塊。**商業規則沒有兩份**，這正是慣例說的「Controller 保持薄」。

### 驗收勾選

- [x] 頁面查詢與 API 走同一條路徑，結果一致
- [x] 刪除意圖 → 頁面顯示「無法理解的查詢」警示，不是錯誤頁
- [x] Controller 裡沒有任何 Gemini / HttpClient 細節（對兩個 controller `grep Gemini|HttpClient` 皆 0 命中，全封裝在 Infrastructure）
- [ ] 拔掉 API key → 頁面顯示清楚錯誤：同練習 1，待 key 還原後復驗

---

## 活動 4 — 補齊：MCP server 加開 HTTP transport

**日期**：2026-09-12（commit `1ce65de`）

### 做了什麼

`Program.cs` 改成雙 transport：帶 `--http` 走 `WebApplication` + `MapMcp()`（`http://localhost:3001`，`Stateless = true`），不帶就照舊 stdio。工具／Resource／Prompt **一行沒改**——換的只有 transport。csproj 加 `ModelContextProtocol.AspNetCore` + `FrameworkReference Microsoft.AspNetCore.App`。

### 驗證結果

| 檢查 | 結果 |
|------|------|
| `POST http://localhost:3001`（`tools/list`） | **200**，`content-type: text/event-stream`，332ms |
| 工具清單 | `customer_orders`、`get_order`、`low_stock`、`cancel_order` 四個都在，annotations 保留 |
| stdio 照舊 | `.mcp.json` 與 Codex 設定完全沒動，agent 端連線正常 |

### 地雷：stdio 版還開著時，`dotnet run` 會 build 失敗

agent 把 stdio 版當子行程掛著，DLL 被鎖：

```
error MSB3027: Could not copy "OrderHub.Infrastructure.dll" ... The file is locked by: "OrderHub.Mcp (36240)"
error MSB3021: Unable to copy file "OrderHub.Core.dll" ...
```

繞法：直接跑已編譯的 `src/OrderHub.Mcp/bin/Debug/net8.0/OrderHub.Mcp.exe --http`（不重 build），或先關掉 agent 的 MCP 連線。**同一個專案同時被 agent 掛著又要 rebuild，這個坑之後還會遇到。**

### 驗收勾選

- [x] HTTP transport 列得出四工具、resource、prompt
- [x] 不帶 `--http` 照舊走 stdio
- [x] 獨立 commit（`1ce65de`）

---

## 活動 4 — 練習 1：Hello Webhook

**日期**：2026-09-12  
**Workflow**：`活動4 練習1 — Hello Webhook`（id `HHqRySktnc4xV0WY`）

### 節點鏈

`Webhook (POST, Respond: Using 'Respond to Webhook' Node)` → `Edit Fields (Set)`（加 `receivedAt = {{ $now.toISO() }}`、Include Other Input Fields = All）→ `Respond to Webhook`（First Incoming Item）

### 兩個必踩的預設值

1. Webhook 的 **Respond 預設是 _Immediately_**，不改的話只會回一句 `Workflow was started`，後面接的 Respond 節點被完全忽略
2. Set 節點的 **Include Other Input Fields 預設關閉**，不開的話送進來的 body 會被丟掉，只剩 `receivedAt`

### Test URL vs Production URL（實際體會到的差別）

| | Test URL | Production URL |
|--|----------|----------------|
| 何時活著 | 在編輯器按 Execute／Listen 之後，**只活 120 秒、收一發就停** | workflow **Activate** 後常駐 |
| 看結果 | 畫布上每個節點亮綠勾，點開看輸入輸出 | 到 **Executions** 分頁看 |

這條後來被練習 2 當成「通知端點」重用：練習 2 的通知節點打的就是這條的 Production URL，執行紀錄 **execution 30** 就是被那一發打起來的（success）——Activate 的意義在這裡才真正具體。

### 驗收勾選

- [x] 回應含送出的內容 + 時間戳
- [x] 理解 Test / Production URL 差別（並在練習 2 實際用到 Production URL）

---

## 活動 4 — 練習 2：退單巡檢日報

**日期**：2026-09-12  
**Workflow**：`活動4 練習2 — 退單巡檢日報`（id `QRR6nKqKupoRthMa`）

### 節點鏈

```
Schedule Trigger（每天 09:00）
  → HTTP Request「查詢取消訂單」POST /api/orders/search  {"text":"過去 30 天取消的訂單"}（Always Output Data 開）
  → Code「整理筆數」（濾掉空 item，輸出單一 item {count, orders}）
  → AI Agent + Google Gemini Chat Model
  → IF count > 0
      ├─ true : GitHub 開 issue → HTTP Request 通知（打練習 1 的 Production URL）
      └─ false: Data Table 插一列「本日無退單」
```

IF 的左值用 `{{ $('整理筆數').first().json.count }}`——AI Agent 的輸出只剩一個 `output` 欄位，`count` 已經不在裡面，只能用 `$('節點名')` 跨節點回頭拿。

### 失敗紀錄（比成功更有參考價值）

| execution | 停在哪 | 錯誤 | 真正原因 |
|-----------|--------|------|----------|
| 21 | AI Agent | `The service is receiving too many requests from you` | Gemini 免費層 429，連續重跑觸發 |
| 22 / 27 / 28 | 查詢取消訂單 | `Service unavailable` | **5150 網站沒開**——n8n 的錯誤訊息只說上游掛了，不會告訴你是自己沒啟動 |
| 23 | Schedule Trigger | — | 手動中斷 |
| **29** | 全綠 | — | 網站 + MCP + key 都就緒後一次過 |

**一句話**：n8n 節點的紅字幾乎都在講「上游」，排錯要先回頭確認自己的本機服務是不是活的（5150 / 3001 / 5678 三個都要）。

### 思考題：如果「查什麼、怎麼查」也交給 AI Agent 自由發揮，會失去什麼？

會一次失去三樣東西，而且剛好是活動 3 花整個練習建起來的：

1. **白名單防線**：現在 LLM 只能填 `Status` / `MemberTier` / `DateFrom` / `DateTo` 四格，SQL 由 EF Core 生成；「幫我把所有訂單刪掉」在翻譯層就被擋成 422。若讓 agent 自由決定查法（自己組 SQL、或掛一個萬用查詢工具），這道牆就沒了——注入與誤刪從「不可能」變成「靠 prompt 祈禱」。
2. **可測試性**：`OrderSearchService` 可以用固定輸入斷言固定輸出；agent 自由發揮的查詢每次都可能飄，測試寫不出斷言，壞掉也沒人知道。
3. **日報數字的可信度**：現在 AI 拿到的是查詢結果 JSON，system message 又限制「只根據提供的資料寫，不要編造數字」。若連查什麼都它決定，日報裡的「本月退單 N 筆」就沒有任何一層可以回頭覆核——你無法分辨那是真的 N 筆，還是它少查了一個條件。

所以分工是刻意的：**查詢留在產品程式碼裡（有白名單、有測試、有 code review），n8n 只做編排，AI 只做摘要。**

### 驗收勾選

- [x] 先在系統裡製造素材：#212 陳志明（Gold）取消，NT$2,088
- [x] Execute workflow → 開出 GitHub issue、通知送達（觸發練習 1 execution 30）
- [x] 日報數字與 API 查詢結果一致（都是 #212 這一筆）
- [ ] **false 分支（本日無退單）尚未跑過**：n8n 事件紀錄裡 Data Table 節點只在探索用的 `My workflow` 出現過。把查詢文字改成「昨天取消的訂單」即可觸發——該文字已實測 `POST /api/orders/search` 回 `[]`。另需先確認 Data Table `巡檢紀錄`（欄位 `date`、`note`）已建立，且節點裡重選過該表（匯出的 JSON 裡 `dataTableId` 是空的）
- [x] 思考題已寫入本節

---

## 活動 4 — 練習 3：MCP 合體——讓流程裡的 AI 會用你的工具

**日期**：2026-09-12  
**做法**：直接在練習 2 的同一條 workflow（`QRR6nKqKupoRthMa`）的 AI Agent 下掛 **MCP Client Tool**

### 設定

| 項目 | 值 |
|------|-----|
| Endpoint | `http://localhost:3001` |
| Server Transport | HTTP Streamable |
| Authentication | None |
| Tools to Include | **Selected → 只勾 `get_order`** |
| System Message 追加 | 「對每筆取消的訂單，先用工具查出品項明細與會員等級，日報中引用查到的實際數字」 |

### 只掛唯讀工具，是活動 1 approval 哲學的另一個形狀

`cancel_order` 就在同一台 server 上、同一個 endpoint 列得出來，但**不勾**。無人流程裡沒有人可以按「同意」，所以最強的保護不是「跳確認」而是**根本不給工具**——權限控制往前挪到工具清單這一層。

### 執行證據（execution 29，manual，success）

節點軌跡（來自 `~/.n8n/n8nEventLog.log`）：

```
Schedule Trigger → 查詢取消訂單 → 整理筆數 → AI Agent
    ├─ Google Gemini Chat Model   (第 1 次：決定要呼叫工具)
    ├─ MCP Client                 (nodeId 5f175e99…，掛的工具只有 get_order)
    └─ Google Gemini Chat Model   (第 2 次：拿到工具結果後寫日報)
  → IF 有退單 → GitHub 開 issue → 通知
```

`AI Agent` 中間夾著一次 `MCP Client` 的 started/finished，就是「真的有深挖」的直接證據——不是看日報寫得像不像，而是看執行紀錄裡工具被呼叫過。

### 有深挖 vs 沒深挖的日報差異

| | 練習 2（只有 search API 的六欄） | 練習 3（加掛 `get_order`） |
|--|--------------------------------|---------------------------|
| AI 拿得到的資料 | `id` / `customerName` / `tier` / `status` / `total` / `createdAt` | 再加上**品項明細、單價快照、折扣、應付總額**（`get_order` 的回傳） |
| 日報能寫到什麼粒度 | 「#212 陳志明 Gold，NT$2,088」——只能複述查詢結果 | 「#212 退掉的是哪幾樣商品、各幾件、原價多少、Gold 折後 2,088」 |
| 數字來源 | 一次查詢的摘要欄位 | 每筆訂單回頭跟 server 要真實明細 |
| 失敗模式 | 想講細節只能瞎編 | 工具查不到就查不到，編不出來 |
| 代價 | 一次 LLM 呼叫 | 多一輪 LLM + N 次工具呼叫（退單多時會變慢、也更容易撞 429） |

> **待補**：把 execution 29 開出的 GitHub issue 內文，與練習 2 時期（execution 14 / 16 / 19）的 issue 內文各貼第一行到這裡，做字面對照。

### 驗收勾選

- [x] 執行紀錄看得到 agent 對退單呼叫 MCP 工具（execution 29，`MCP Client` 節點 started/finished，工具清單只含 `get_order`）
- [x] 有深挖 vs 沒深挖的差異已寫入本節
- [ ] 兩份 issue 內文字面對照（待貼）

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
