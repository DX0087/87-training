using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly OrderHubDbContext _db;

    public ProductRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync() =>
        await _db.Products.OrderBy(p => p.Sku).ToListAsync();

    public async Task<IReadOnlyList<Product>> GetActiveAsync() =>
        await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Sku).ToListAsync();

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.FirstOrDefaultAsync(p => p.Id == id);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold, DateTime soldSinceUtc)
    {
        var products = await _db.Products
            .Where(p => p.IsActive && p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

        if (products.Count == 0)
            return Array.Empty<LowStockProduct>();

        var productIds = products.Select(p => p.Id).ToList();

        // 明確 join Orders，避免僅靠導覽屬性在 InMemory 下漏資料；一次彙總避免 N+1
        var soldByProduct = await (
            from i in _db.OrderItems
            join o in _db.Orders on i.OrderId equals o.Id
            where productIds.Contains(i.ProductId)
                  && o.Status != OrderStatus.Cancelled
                  && o.CreatedAt >= soldSinceUtc
            group i by i.ProductId into g
            select new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) }
        ).ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        return products
            .Select(p => new LowStockProduct
            {
                Product = p,
                SoldLast30Days = soldByProduct.TryGetValue(p.Id, out var qty) ? qty : 0
            })
            .ToList();
    }
}
