using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

public class OrderServicePricingTests
{
    [Theory]
    [InlineData(CustomerTier.Standard, 0)]
    [InlineData(CustomerTier.Silver, 0.05)]
    [InlineData(CustomerTier.Gold, 0.10)]
    public void GetDiscountRate_ReturnsExpectedRate(CustomerTier tier, decimal expected)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        Assert.Equal(expected, service.GetDiscountRate(tier));
    }

    [Fact]
    public void CalculateSubtotal_SumsQuantityTimesSnapshotPrice()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var order = new Order
        {
            Items =
            {
                new OrderItem { Quantity = 2, UnitPriceSnapshot = 150m },
                new OrderItem { Quantity = 3, UnitPriceSnapshot = 40m }
            }
        };

        Assert.Equal(420m, service.CalculateSubtotal(order));
    }

    [Theory]
    [InlineData(CustomerTier.Standard, 1000, 1000)]
    [InlineData(CustomerTier.Silver, 1000, 950)]
    [InlineData(CustomerTier.Gold, 1000, 900)]
    public void CalculateTotal_AppliesTierDiscountOnSubtotal(CustomerTier tier, decimal unitPrice, decimal expectedTotal)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var order = new Order
        {
            Customer = new Customer { Tier = tier },
            Items = { new OrderItem { Quantity = 1, UnitPriceSnapshot = unitPrice } }
        };

        Assert.Equal(expectedTotal, service.CalculateTotal(order));
    }

    [Fact]
    public void CalculateTotal_WithoutCustomer_UsesStandardRate()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var order = new Order
        {
            Items = { new OrderItem { Quantity = 2, UnitPriceSnapshot = 250m } }
        };

        Assert.Equal(500m, service.CalculateTotal(order));
    }

    /// <summary>
    /// 回歸：Gold 建單時 snapshot 必須是原價，總額只在 CalculateTotal 打 9 折一次
    /// （先前 CreateOrder 對 Gold 先寫折後價，再算總額又折一次 → 實際 0.81 折）。
    /// </summary>
    [Fact]
    public async Task CreateOrder_Gold_DoesNotDoubleDiscount()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db, CustomerTier.Gold);
        var product = TestSetup.AddProduct(db, unitPrice: 100m);

        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 1) });

        Assert.True(result.Success);
        Assert.Equal(100m, result.Value!.Items.Single().UnitPriceSnapshot);

        var order = await service.GetOrderAsync(result.Value.Id);
        Assert.NotNull(order);
        Assert.Equal(100m, service.CalculateSubtotal(order!));
        Assert.Equal(90m, service.CalculateTotal(order!));
    }

    /// <summary>
    /// 對照：Silver 仍只折一次（95 折），且 snapshot 為原價。
    /// </summary>
    [Fact]
    public async Task CreateOrder_Silver_AppliesDiscountOnceOnTotal()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db, CustomerTier.Silver);
        var product = TestSetup.AddProduct(db, unitPrice: 100m);

        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 1) });

        Assert.True(result.Success);
        Assert.Equal(100m, result.Value!.Items.Single().UnitPriceSnapshot);

        var order = await service.GetOrderAsync(result.Value.Id);
        Assert.Equal(95m, service.CalculateTotal(order!));
    }
}
