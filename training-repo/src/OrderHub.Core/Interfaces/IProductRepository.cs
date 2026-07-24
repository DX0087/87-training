using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();
    Task<Product?> GetByIdAsync(int id);
    Task SaveChangesAsync();

    /// <summary>
    /// 活躍且庫存嚴格小於 threshold 的商品，依庫存升冪；
    /// 附帶 soldSinceUtc 起、非 Cancelled 訂單的售出數量彙總。
    /// </summary>
    Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold, DateTime soldSinceUtc);
}
