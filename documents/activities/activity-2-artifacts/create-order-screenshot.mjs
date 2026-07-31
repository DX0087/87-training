/**
 * 活動 2 練習 0：用 Playwright 自動建立訂單並截圖
 * 網站需已在 http://localhost:5150 執行
 */
import { chromium } from 'playwright';
import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const BASE = process.env.ORDERHUB_URL || 'http://localhost:5150';
const outDir = __dirname;

async function main() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });

  console.log('Open create order page...');
  await page.goto(`${BASE}/Orders/Create`, { waitUntil: 'networkidle' });

  // 選第一個非空白客戶
  const customerSelect = page.locator('select[name="CustomerId"]');
  await customerSelect.waitFor();
  const customerOptions = await customerSelect.locator('option').all();
  let customerValue = null;
  let customerLabel = null;
  for (const opt of customerOptions) {
    const value = await opt.getAttribute('value');
    if (value) {
      customerValue = value;
      customerLabel = (await opt.textContent())?.trim();
      break;
    }
  }
  if (!customerValue) throw new Error('No customers found in dropdown');
  await customerSelect.selectOption(customerValue);
  console.log(`Customer: ${customerLabel} (id=${customerValue})`);

  // 選第一個非空白商品，數量 1
  const productSelect = page.locator('select[name="Lines[0].ProductId"]');
  const productOptions = await productSelect.locator('option').all();
  let productValue = null;
  let productLabel = null;
  for (const opt of productOptions) {
    const value = await opt.getAttribute('value');
    if (value) {
      productValue = value;
      productLabel = (await opt.textContent())?.trim();
      break;
    }
  }
  if (!productValue) throw new Error('No products found in dropdown');
  await productSelect.selectOption(productValue);
  await page.locator('input[name="Lines[0].Quantity"]').fill('1');
  console.log(`Product: ${productLabel} (id=${productValue}) x1`);

  await page.screenshot({ path: path.join(outDir, '01-create-form.png'), fullPage: true });
  console.log('Saved 01-create-form.png');

  await Promise.all([
    page.waitForURL(/\/Orders\/Details\/\d+/, { timeout: 15000 }),
    page.click('button[type="submit"]'),
  ]);

  await page.waitForLoadState('networkidle');
  const url = page.url();
  const title = await page.locator('h1, .h3').first().textContent();
  const success = await page.locator('.alert-success').textContent().catch(() => null);
  console.log(`Result URL: ${url}`);
  console.log(`Title: ${title?.trim()}`);
  if (success) console.log(`Success: ${success.trim()}`);

  await page.screenshot({ path: path.join(outDir, '02-order-details.png'), fullPage: true });
  console.log('Saved 02-order-details.png');

  const meta = {
    customer: customerLabel,
    product: productLabel,
    resultUrl: url,
    title: title?.trim(),
    success: success?.trim() ?? null,
    at: new Date().toISOString(),
  };
  fs.writeFileSync(path.join(outDir, 'result.json'), JSON.stringify(meta, null, 2), 'utf8');
  console.log('Done.');

  await browser.close();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
