using Microsoft.AspNetCore.Mvc;
using OrderHub.Core.Services;
using OrderHub.Web.ViewModels;

namespace OrderHub.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();

        var vm = new ProductListViewModel
        {
            Products = products.Select(p => new ProductRowViewModel
            {
                Sku = p.Sku,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive
            }).ToList()
        };

        return View(vm);
    }

    /// <summary>
    /// 低庫存警示。未帶 threshold 時預設 10；&lt;=0 顯示表單錯誤而非 500。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LowStock(int? threshold)
    {
        var vm = new LowStockViewModel
        {
            Threshold = threshold ?? 10
        };

        // 明確帶了非法門檻：走 ModelState，不呼叫 service（避免 500）
        if (threshold.HasValue && threshold.Value <= 0)
        {
            ModelState.AddModelError(nameof(LowStockViewModel.Threshold), "門檻必須大於 0");
            return View(vm);
        }

        var items = await _productService.GetLowStockAsync(vm.Threshold);
        vm.Items = items.Select(x => new LowStockRowViewModel
        {
            Sku = x.Product.Sku,
            Name = x.Product.Name,
            StockQuantity = x.Product.StockQuantity,
            SoldLast30Days = x.SoldLast30Days
        }).ToList();

        return View(vm);
    }
}

