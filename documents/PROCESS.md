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
