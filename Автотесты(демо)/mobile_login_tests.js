const { remote } = require('webdriverio');
const assert = require('assert');

describe('Тесты авторизации в мобильном приложении', function() {
  let driver;

  before(async function() {
    const opts = {
      path: '/wd/hub',
      port: 4723,
      capabilities: {
        platformName: 'Android',
        platformVersion: '11.0',
        deviceName: 'Android Emulator',
        app: '/path/to/app.apk',
        automationName: 'UiAutomator2'
      }
    };
    driver = await remote(opts);
  });

  after(async function() {
    if (driver) {
      await driver.deleteSession();
    }
  });

  it('Успешная авторизация', async function() {
    // Открыть экран авторизации
    const loginButton = await driver.$('~login_button');
    await loginButton.click();
    
    // Заполнить форму
    const usernameField = await driver.$('~username_field');
    const passwordField = await driver.$('~password_field');
    const submitButton = await driver.$('~submit_button');
    
    await usernameField.setValue('validuser');
    await passwordField.setValue('validpassword');
    await submitButton.click();
    
    // Проверить успешную авторизацию
    const welcomeMessage = await driver.$('~welcome_message');
    await welcomeMessage.waitForDisplayed({ timeout: 5000 });
    
    const welcomeText = await welcomeMessage.getText();
    assert.strictEqual(welcomeText.includes('Добро пожаловать'), true);
  });

  it('Проверка валидации полей', async function() {
    // Открыть экран авторизации
    await driver.back();
    const loginButton = await driver.$('~login_button');
    await loginButton.click();
    
    // Проверка валидации пустых полей
    const submitButton = await driver.$('~submit_button');
    await submitButton.click();
    
    const errorMessages = await driver.$$('~error_message');
    assert.strictEqual(errorMessages.length > 0, true);
    
    // Проверка минимальной длины пароля
    const usernameField = await driver.$('~username_field');
    const passwordField = await driver.$('~password_field');
    
    await usernameField.setValue('validuser');
    await passwordField.setValue('123');
    await submitButton.click();
    
    const passwordError = await driver.$('~password_error');
    assert.strictEqual(await passwordError.isDisplayed(), true);
    
    const errorText = await passwordError.getText();
    assert.strictEqual(errorText.includes('не менее 6 символов'), true);
  });
  
  it('Тестирование биометрической авторизации', async function() {
    // Открыть экран авторизации
    await driver.back();
    const loginButton = await driver.$('~login_button');
    await loginButton.click();
    
    // Проверить доступность биометрической авторизации
    const biometricButton = await driver.$('~biometric_login');
    assert.strictEqual(await biometricButton.isDisplayed(), true);
    
    // Выполнить биометрическую авторизацию
    await biometricButton.click();
    
    // Эмулировать успешное биометрическое подтверждение
    await driver.executeScript('mobile: biometricLogin', { authenticated: true });
    
    // Проверить успешную авторизацию
    const welcomeMessage = await driver.$('~welcome_message');
    await welcomeMessage.waitForDisplayed({ timeout: 5000 });
  });
});