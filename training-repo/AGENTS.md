# OrderHub — 專案記憶

## 專案簡介

公司內部訂單管理系統：業務可建立/查詢訂單、管理商品與客戶。
內部使用、單一 SQL Server 資料庫，不需要考慮多租戶或高併發架構。
本目錄（`training-repo/`）為可建置執行的 .NET 解決方案。

## 技術棧

- .NET 8 / ASP.NET Core MVC（Razor Views + Bootstrap 5，靜態資源本地，無 CDN）
- EF Core 8 + SQL Server（本機；啟動時自動 Migrate + Seed）
- 測試：xUnit + EF Core InMemory（`dotnet test` 不需 SQL Server）

## 分層與慣例

- 三層：`OrderHub.Web`（Controller / View / ViewModel）→ `OrderHub.Core`（Domain / Services / Interfaces）→ `OrderHub.Infrastructure`（Repositories / Migrations / Seeder）
- Controller 保持薄，只轉接 service 結果；商業邏輯一律放 Core 的 service
- 只有 repository 碰 `DbContext`；Controller / Service 不可直接用 EF Core
- 寫入類操作（建單、取消）回傳 `ServiceResult<T>` 表達預期內失敗，不要丟例外當業務錯誤
- View 綁 ViewModel，不要把 domain model 直接丟給 View
- 使用者輸入用 DataAnnotations + ModelState 驗證；輸入錯誤絕不能變成 500
- 金額一律用 `decimal`；會員折扣只在 `OrderService.CalculateTotal` 對總額折一次；`UnitPriceSnapshot` 存原價
- 參考檔：Controller 照 `ProductsController.cs`、Service 照 `ProductService.cs` / `OrderService.cs`

### 各層職責（一句話）

| 專案 | 職責 |
|------|------|
| Web | HTTP、表單、ViewModel 映射、TempData 訊息 |
| Core | 領域模型、商業規則（庫存、狀態、折扣） |
| Infrastructure | EF 查詢、migration、種子資料 |

### 新增功能慣例（例如新頁面）

通常要動：Controller action → Service（+ 介面）→ Repository（+ 介面，若需查詢）→ ViewModel → View → 導覽列 → service 層測試。

## 常用指令

在 `training-repo/` 目錄下：

- `dotnet build`：建置
- `dotnet test`：跑全部測試
- `dotnet run --project src/OrderHub.Web`：啟動網站（預設 http://localhost:5150）

## 重要 / 危險檔案

- `src/OrderHub.Infrastructure/Migrations/**`：EF migration 是歷史紀錄，不要手改
- `src/OrderHub.Web/appsettings.json` / `appsettings.Development.json`：連線字串等設定，改動前先問
- 種子資料在 `DbSeeder.cs`，固定 random seed，勿隨意改除非任務要求

## 不要做的事

- 不要未經同意就加新的 NuGet 套件
- 不要在 Controller / Service 直接使用 DbContext
- 不要為了「順手」重構與當前任務無關的程式碼
- 不要讀取或寫入任何機密檔（*.pfx、appsettings.Production.json、user-secrets）
- 不要手改 Migrations；需要 schema 變更時用 `dotnet ef migrations add`
- commit 修復時 message 用「症狀 → 根因 → 修法」；一個修復一個 commit
