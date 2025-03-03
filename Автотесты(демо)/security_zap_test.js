const ZapClient = require('zaproxy');
const { generateReport } = require('./reportGenerator');

// Конфигурация тестов безопасности
const TARGET_URL = 'https://example.com';
const ZAP_API_URL = 'http://localhost:8080';
const ZAP_API_KEY = 'your-api-key';

// Инициализация ZAP клиента
const zapOptions = {
  apiKey: ZAP_API_KEY,
  proxy: ZAP_API_URL
};

const zaproxy = new ZapClient(zapOptions);

async function runSecurityTest() {
  try {
    console.log('Запуск теста безопасности для:', TARGET_URL);
    
    // Очистка предыдущей сессии
    await zaproxy.core.newSession('', '');
    
    // Настройка контекста и области тестирования
    console.log('Настройка контекста тестирования...');
    const contextId = await zaproxy.context.newContext('security-test');
    await zaproxy.context.includeInContext('security-test', `${TARGET_URL}.*`);
    
    // Исключение путей, которые могут вызывать проблемы
    await zaproxy.context.excludeFromContext('security-test', `${TARGET_URL}/logout.*`);
    
    // Настройка аутентификации (если требуется)
    console.log('Настройка аутентификации...');
    await zaproxy.authentication.setAuthenticationMethod(
      contextId,
      'formBasedAuthentication',
      `loginUrl=${TARGET_URL}/login&loginRequestData=username%3D%7B%25username%25%7D%26password%3D%7B%25password%25%7D`
    );
    
    // Создание учетных данных пользователя
    const userId = await zaproxy.users.newUser(contextId, 'test-user');
    await zaproxy.users.setUserCredentials(
      contextId,
      userId,
      'username=test-user&password=Password123!'
    );
    await zaproxy.users.setUserEnabled(contextId, userId, true);
    
    // Spider-сканирование для обнаружения URL
    console.log('Запуск spider-сканирования...');
    const scanId = await zaproxy.spider.scan(TARGET_URL, '', '', '', true);
    
    // Ожидание завершения сканирования
    let status = 0;
    while (status < 100) {
      await new Promise(resolve => setTimeout(resolve, 2000));
      status = parseInt(await zaproxy.spider.status(scanId));
      console.log(`Spider-сканирование: ${status}%`);
    }
    
    // Запуск активного сканирования
    console.log('Запуск активного сканирования...');
    const ascanId = await zaproxy.ascan.scan(TARGET_URL, '', true, false, '', 'Default Policy');
    
    // Ожидание завершения активного сканирования
    status = 0;
    while (status < 100) {
      await new Promise(resolve => setTimeout(resolve, 5000));
      status = parseInt(await zaproxy.ascan.status(ascanId));
      console.log(`Активное сканирование: ${status}%`);
    }
    
    // Получение и анализ результатов
    console.log('Сбор результатов тестирования...');
    const alerts = await zaproxy.core.alerts(TARGET_URL, '', '');
    
    // Анализ уровней угроз
    let highRisks = 0;
    let mediumRisks = 0;
    let lowRisks = 0;
    
    alerts.forEach(alert => {
      if (alert.risk === 'High') highRisks++;
      else if (alert.risk === 'Medium') mediumRisks++;
      else if (alert.risk === 'Low') lowRisks++;
    });
    
    console.log('Результаты тестирования безопасности:');
    console.log(`- Высокий риск: ${highRisks}`);
    console.log(`- Средний риск: ${mediumRisks}`);
    console.log(`- Низкий риск: ${lowRisks}`);
    
    // Генерация HTML-отчета
    await zaproxy.reports.generate(
      'security-report.html',
      'traditional-html',
      TARGET_URL,
      'Security Test Report',
      '',
      ''
    );
    
    // Создание собственного отчета с рекомендациями
    generateReport(alerts, 'security-report-with-recommendations.html');
    
    // Проверка на критические уязвимости
    if (highRisks > 0) {
      console.error('ТЕСТ НЕ ПРОЙДЕН: Обнаружены уязвимости высокого риска!');
      process.exit(1);
    } else {
      console.log('Тест пройден успешно!');
    }
    
  } catch (error) {
    console.error('Ошибка при выполнении теста безопасности:', error);
    process.exit(1);
  }
}

runSecurityTest();