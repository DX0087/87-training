# 87-training1 — 培訓 Repo

公司內部 **AI Agentic Coding** 練習專案。可執行的訂單系統在 `training-repo/`。

## 目錄

| 路徑 | 說明 |
|------|------|
| `documents/` | 練習指南、PROCESS 心得、agent 設定參考 |
| `training-repo/` | OrderHub 解法方案（.NET 8 MVC + EF Core） |

練習說明：`documents/README.md`、`documents/activities/activity-guideline.md`。

## OrderHub 專案記憶（精簡）

完整版見 `training-repo/AGENTS.md` 與 `training-repo/CLAUDE.md`。

- 三層：Web（薄 Controller + ViewModel）→ Core（商業邏輯）→ Infrastructure（EF / Repo）
- 商業邏輯放 Core service；只有 repository 碰 DbContext
- View 綁 ViewModel；表單驗證 DataAnnotations + ModelState，不可 500
- 金額 `decimal`；折扣只在 `OrderService.CalculateTotal`；snapshot 存原價
- 指令（在 `training-repo/`）：`dotnet build` / `dotnet test` / `dotnet run --project src/OrderHub.Web`
- 不要未經同意加套件、不要手改 Migrations、不要順手重構無關程式

## Agent 設定（練習 1）

Claude Code 設定位於 `training-repo/.claude/`（settings、hooks、subagents、`/fix-bug` skill）。
