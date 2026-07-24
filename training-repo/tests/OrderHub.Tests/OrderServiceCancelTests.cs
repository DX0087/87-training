using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

public class OrderServiceCancelTests
{
    private static async Task<Order> CreateOrderWithStatusAsync(
        Core.Services.OrderService service,
        Infrastructure.Data.OrderHubDbContext db,
        OrderStatus status)
    {
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db);
        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 1) });
        var order = result.Value!;
        order.Status = status;
        await db.SaveChangesAsync();
        return order;
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    public async Task CancelOrder_ActiveOrder_SetsStatusCancelled(OrderStatus initialStatus)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var order = await CreateOrderWithStatusAsync(service, db, initialStatus);

        var result = await service.CancelOrderAsync(order.Id);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Cancelled, db.Orders.Single(o => o.Id == order.Id).Status);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task CancelOrder_NotCancellableStatus_Fails(OrderStatus initialStatus)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var order = await CreateOrderWithStatusAsync(service, db, initialStatus);

        var result = await service.CancelOrderAsync(order.Id);

        Assert.False(result.Success);
        Assert.Equal(initialStatus, db.Orders.Single(o => o.Id == order.Id).Status);
    }

    [Fact]
    public async Task CancelOrder_NotFound_Fails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var result = await service.CancelOrderAsync(12345);

        Assert.False(result.Success);
        Assert.Contains("找不到", result.ErrorMessage);
    }

    /// <summary>
    /// 回歸：取消訂單必須把建單時扣掉的庫存加回
    /// （先前先設 Status=Cancelled，再判斷 Pending/Confirmed 才還庫存，條件永遠為 false）。
    /// </summary>
    [Fact]
    public async Task CancelOrder_RestoresProductStock()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 10);

        var create = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 3) });
        Assert.True(create.Success);
        Assert.Equal(7, db.Products.Single(p => p.Id == product.Id).StockQuantity);

        var cancel = await service.CancelOrderAsync(create.Value!.Id);

        Assert.True(cancel.Success);
        Assert.Equal(OrderStatus.Cancelled, db.Orders.Single(o => o.Id == create.Value.Id).Status);
        Assert.Equal(10, db.Products.Single(p => p.Id == product.Id).StockQuantity);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    public async Task CancelOrder_CancellableStatuses_RestoreStock(OrderStatus initialStatus)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 20);

        var create = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 5) });
        var order = create.Value!;
        order.Status = initialStatus;
        await db.SaveChangesAsync();
        Assert.Equal(15, db.Products.Single(p => p.Id == product.Id).StockQuantity);

        var cancel = await service.CancelOrderAsync(order.Id);

        Assert.True(cancel.Success);
        Assert.Equal(20, db.Products.Single(p => p.Id == product.Id).StockQuantity);
    }

    [Fact]
    public async Task CancelOrder_Shipped_DoesNotRestoreStock()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 10);

        var create = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 2) });
        var order = create.Value!;
        order.Status = OrderStatus.Shipped;
        await db.SaveChangesAsync();
        Assert.Equal(8, db.Products.Single(p => p.Id == product.Id).StockQuantity);

        var cancel = await service.CancelOrderAsync(order.Id);

        Assert.False(cancel.Success);
        Assert.Equal(8, db.Products.Single(p => p.Id == product.Id).StockQuantity);
        Assert.Equal(OrderStatus.Shipped, db.Orders.Single(o => o.Id == order.Id).Status);
    }
}
