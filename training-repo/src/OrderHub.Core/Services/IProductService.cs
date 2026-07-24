using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();

    /// <summary>
    /// 活躍且庫存嚴格小於 threshold 的商品（升冪），含近 30 天售出（排除 Cancelled）。
    /// threshold 必須 &gt; 0。
    /// </summary>
    Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold);
}
