const { test, expect } = require('@playwright/test');

test.describe('Процесс оформления заказа', () => {
  let page;

  test.beforeEach(async ({ browser }) => {
    page = await browser.newPage();
    await page.goto('https://example.com');
    
    // Авторизация пользователя
    await page.click('.login-button');
    await page.fill('#username', 'testuser');
    await page.fill('#password', 'Password123!');
    await page.click('#login-submit');
    await page.waitForSelector('.welcome-message');
  });

  test('Добавление товара в корзину', async () => {
    // Перейти в каталог продуктов
    await page.click('text=Каталог');
    
    // Выбрать категорию
    await page.click('text=Электроника');
    
    // Добавить товар в корзину
    const productName = await page.textContent('.product-item:first-child .product-name');
    const productPrice = await page.textContent('.product-item:first-child .product-price');
    
    await page.click('.product-item:first-child .add-to-cart');
    
    // Проверить уведомление о добавлении товара
    await expect(page.locator('.notification')).toBeVisible();
    await expect(page.locator('.notification')).toContainText('Товар добавлен в корзину');
    
    // Проверить счетчик товаров в корзине
    await expect(page.locator('.cart-count')).toContainText('1');
  });

  test('Оформление заказа', async () => {
    // Добавить товар и перейти в корзину
    await page.click('text=Каталог');
    await page.click('text=Электроника');
    await page.click('.product-item:first-child .add-to-cart');
    await page.click('.cart-icon');
    
    // Проверить содержимое корзины
    await expect(page.locator('.cart-items')).toBeVisible();
    await expect(page.locator('.cart-items .item')).toHaveCount(1);
    
    // Перейти к оформлению
    await page.click('text=Оформить заказ');
    
    // Заполнить данные доставки
    await page.fill('#delivery-address', 'Тестовая улица, 123');
    await page.fill('#delivery-city', 'Москва');
    await page.fill('#delivery-zip', '123456');
    await page.selectOption('#delivery-method', 'courier');
    
    // Перейти к оплате
    await page.click('#continue-to-payment');
    
    // Заполнить данные оплаты
    await page.fill('#card-number', '4111 1111 1111 1111');
    await page.fill('#card-holder', 'TEST USER');
    await page.fill('#card-expiry', '12/25');
    await page.fill('#card-cvv', '123');
    
    // Подтвердить заказ
    await page.click('#place-order');
    
    // Проверить подтверждение заказа
    await expect(page.locator('.order-confirmation')).toBeVisible();
    await expect(page.locator('.order-number')).toBeTruthy();
  });

  test('Проверка подтверждения заказа по email', async () => {
    // Используем мокированный почтовый сервер для проверки
    const emailAPI = 'https://api.example.com/test-emails/latest';
    
    // Добавить товар и оформить заказ (сокращенно)
    await page.click('text=Каталог');
    await page.click('.product-item:first-child .add-to-cart');
    await page.click('.cart-icon');
    await page.click('text=Оформить заказ');
    
    // Заполнение форм (сокращенно)
    await page.fill('#delivery-address', 'Тестовая улица, 123');
    await page.selectOption('#delivery-method', 'courier');
    await page.click('#continue-to-payment');
    await page.fill('#card-number', '4111 1111 1111 1111');
    await page.click('#place-order');
    
    // Дождаться подтверждения заказа
    const orderNumber = await page.textContent('.order-number');
    
    // Проверка содержимого письма через тестовый API
    const emailResponse = await page.request.get(emailAPI);
    const emailJson = await emailResponse.json();
    
    expect(emailJson.to).toBe('testuser@example.com');
    expect(emailJson.subject).toContain('Подтверждение заказа');
    expect(emailJson.body).toContain(orderNumber);
    expect(emailJson.body).toContain('Спасибо за ваш заказ');
  });
});