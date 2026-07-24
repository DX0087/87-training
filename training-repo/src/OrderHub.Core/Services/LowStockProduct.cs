using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

/// <summary>
/// 低庫存查詢結果：商品本體 + 近 30 天售出數量（排除 Cancelled）。
/// </summary>
public sealed class LowStockProduct
{
    public required Product Product { get; init; }
    public int SoldLast30Days { get; init; }
}
