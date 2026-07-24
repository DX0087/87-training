using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceLowStockTests
{
    [Fact]
    public async Task GetLowStock_FiltersByThreshold_AndSortsByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        TestSetup.AddProduct(db, stock: 3, sku: "LOW-003");
        TestSetup.AddProduct(db, stock: 1, sku: "LOW-001");
        TestSetup.AddProduct(db, stock: 10, sku: "EQ-010"); // 剛好等於門檻，應排除
        TestSetup.AddProduct(db, stock: 15, sku: "HI-015");

        var result = await service.GetLowStockAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "LOW-001", "LOW-003" }, result.Select(x => x.Product.Sku).ToArray());
        Assert.Equal(new[] { 1, 3 }, result.Select(x => x.Product.StockQuantity).ToArray());
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        TestSetup.AddProduct(db, stock: 2, sku: "ACT-001", isActive: true);
        TestSetup.AddProduct(db, stock: 1, sku: "INA-001", isActive: false);

        var result = await service.GetLowStockAsync(10);

        Assert.Single(result);
        Assert.Equal("ACT-001", result[0].Product.Sku);
        Assert.True(result[0].Product.IsActive);
    }

    [Fact]
    public async Task GetLowStock_SoldLast30Days_ExcludesCancelledAndOldOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 5, sku: "SOLD-001");

        // 近 30 天、非取消：應計入 4
        db.Orders.Add(new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            Items =
            {
                new OrderItem { ProductId = product.Id, Quantity = 2, UnitPriceSnapshot = product.UnitPrice },
                new OrderItem { ProductId = product.Id, Quantity = 2, UnitPriceSnapshot = product.UnitPrice }
            }
        });

        // Cancelled：不計入
        db.Orders.Add(new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Items =
            {
                new OrderItem { ProductId = product.Id, Quantity = 10, UnitPriceSnapshot = product.UnitPrice }
            }
        });

        // 超過 30 天：不計入
        db.Orders.Add(new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Shipped,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            Items =
            {
                new OrderItem { ProductId = product.Id, Quantity = 7, UnitPriceSnapshot = product.UnitPrice }
            }
        });

        db.SaveChanges();

        var result = await service.GetLowStockAsync(10);

        Assert.Single(result);
        Assert.Equal("SOLD-001", result[0].Product.Sku);
        Assert.Equal(4, result[0].SoldLast30Days);
    }
}
