# framework/
# ├── base/
# │   ├── __init__.py
# │   ├── base_page.py
# │   └── base_test.py
# ├── config/
# │   ├── __init__.py
# │   └── config.py
# ├── pages/
# │   ├── __init__.py
# │   ├── home_page.py
# │   └── login_page.py
# ├── tests/
# │   ├── __init__.py
# │   └── test_login.py
# ├── utils/
# │   ├── __init__.py
# │   ├── driver_factory.py
# │   └── logger.py
# ├── conftest.py
# └── requirements.txt

# base/base_page.py
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import TimeoutException
import logging

class BasePage:
    """Базовый класс для всех страниц."""
    
    def __init__(self, driver):
        self.driver = driver
        self.logger = logging.getLogger(__name__)
        
    def open(self, url):
        """Открыть указанный URL."""
        self.driver.get(url)
        self.logger.info(f"Открыта страница: {url}")
        
    def find_element(self, locator, timeout=10):
        """Найти элемент по локатору с ожиданием."""
        try:
            element = WebDriverWait(self.driver, timeout).until(
                EC.presence_of_element_located(locator)
            )
            return element
        except TimeoutException:
            self.logger.error(f"Элемент {locator} не найден за {timeout} секунд")
            return None
            
    def click(self, locator, timeout=10):
        """Нажать на элемент."""
        element = self.find_element(locator, timeout)
        if element:
            element.click()
            self.logger.info(f"Клик по элементу: {locator}")
            return True
        return False
        
    def input_text(self, locator, text, timeout=10):
        """Ввести текст в элемент."""
        element = self.find_element(locator, timeout)
        if element:
            element.clear()
            element.send_keys(text)
            self.logger.info(f"Введен текст '{text}' в элемент: {locator}")
            return True
        return False
        
    def get_text(self, locator, timeout=10):
        """Получить текст элемента."""
        element = self.find_element(locator, timeout)
        if element:
            text = element.text
            self.logger.info(f"Получен текст '{text}' из элемента: {locator}")
            return text
        return None
        
    def is_element_visible(self, locator, timeout=10):
        """Проверить видимость элемента."""
        try:
            element = WebDriverWait(self.driver, timeout).until(
                EC.visibility_of_element_located(locator)
            )
            return bool(element)
        except TimeoutException:
            self.logger.info(f"Элемент {locator} не виден за {timeout} секунд")
            return False
            
    def wait_for_page_load(self, timeout=10):
        """Дождаться загрузки страницы."""
        try:
            WebDriverWait(self.driver, timeout).until(
                lambda d: d.execute_script("return document.readyState") == "complete"
            )
            self.logger.info("Страница полностью загружена")
            return True
        except TimeoutException:
            self.logger.error(f"Страница не загрузилась за {timeout} секунд")
            return False

# base/base_test.py
import unittest
import logging
from utils.driver_factory import DriverFactory
from config.config import Config

class BaseTest(unittest.TestCase):
    """Базовый класс для всех тестов."""
    
    def setUp(self):
        """Подготовка к тесту."""
        self.logger = logging.getLogger(__name__)
        self.logger.info("Инициализация теста")
        
        self.config = Config()
        self.driver_factory = DriverFactory()
        self.driver = self.driver_factory.get_driver(self.config.browser)
        self.driver.maximize_window()
        self.driver.implicitly_wait(self.config.implicit_wait)
        
    def tearDown(self):
        """Завершение теста."""
        if self.driver:
            self.driver.quit()
            self.logger.info("Драйвер закрыт")

# config/config.py
import os
import json

class Config:
    """Класс для работы с конфигурацией."""
    
    def __init__(self, config_path="config/config.json"):
        self.config_path = config_path
        self.load_config()
        
    def load_config(self):
        """Загрузить конфигурацию из файла."""
        if os.path.exists(self.config_path):
            with open(self.config_path, 'r') as f:
                config = json.load(f)
                
            # Базовые настройки
            self.base_url = config.get("base_url", "http://localhost")
            self.browser = config.get("browser", "chrome")
            self.implicit_wait = config.get("implicit_wait", 10)
            self.explicit_wait = config.get("explicit_wait", 15)
            
            # Настройки среды
            self.environment = config.get("environment", "dev")
            env_config = config.get("environments", {}).get(self.environment, {})
            
            # URL для разных сред
            if self.environment in config.get("environments", {}):
                self.base_url = env_config.get("base_url", self.base_url)
                
            # Настройки логирования
            self.log_level = config.get("log_level", "INFO")
            
            # Настройки отчётов
            self.reports_dir = config.get("reports_dir", "reports")
            
            # Настройки скриншотов
            self.screenshots_dir = config.get("screenshots_dir", "screenshots")
            self.take_screenshot_on_failure = config.get("take_screenshot_on_failure", True)
        else:
            # Стандартные настройки
            self.base_url = "http://localhost"
            self.browser = "chrome"
            self.implicit_wait = 10
            self.explicit_wait = 15
            self.environment = "dev"
            self.log_level = "INFO"
            self.reports_dir = "reports"
            self.screenshots_dir = "screenshots"
            self.take_screenshot_on_failure = True

# utils/driver_factory.py
from selenium import webdriver
from selenium.webdriver.chrome.options import Options as ChromeOptions
from selenium.webdriver.firefox.options import Options as FirefoxOptions
from selenium.webdriver.edge.options import Options as EdgeOptions
import logging

class DriverFactory:
    """Фабрика драйверов для разных браузеров."""
    
    def __init__(self):
        self.logger = logging.getLogger(__name__)
        
    def get_driver(self, browser_name):
        """Получить драйвер для указанного браузера."""
        browser_name = browser_name.lower()
        
        if browser_name == "chrome":
            options = ChromeOptions()
            options.add_argument("--start-maximized")
            options.add_argument("--disable-extensions")
            driver = webdriver.Chrome(options=options)
            self.logger.info("Инициализирован Chrome драйвер")
            
        elif browser_name == "firefox":
            options = FirefoxOptions()
            driver = webdriver.Firefox(options=options)
            self.logger.info("Инициализирован Firefox драйвер")
            
        elif browser_name == "edge":
            options = EdgeOptions()
            driver = webdriver.Edge(options=options)
            self.logger.info("Инициализирован Edge драйвер")
            
        elif browser_name == "safari":
            driver = webdriver.Safari()
            self.logger.info("Инициализирован Safari драйвер")
            
        elif browser_name == "remote":
            options = ChromeOptions()
            driver = webdriver.Remote(
                command_executor="http://localhost:4444/wd/hub",
                options=options
            )
            self.logger.info("Инициализирован Remote драйвер")
            
        else:
            self.logger.warning(f"Неизвестный браузер: {browser_name}, используем Chrome")
            options = ChromeOptions()
            driver = webdriver.Chrome(options=options)
            
        return driver

# utils/logger.py
import logging
import os
from datetime import datetime

class Logger:
    """Настройка логирования."""
    
    @staticmethod
    def setup_logger(log_level="INFO", log_file=None):
        """Настроить логгер."""
        # Создать каталог логов, если он не существует
        if log_file:
            log_dir = os.path.dirname(log_file)
            if not os.path.exists(log_dir):
                os.makedirs(log_dir)
                
        # Настройка уровня логирования
        numeric_level = getattr(logging, log_level.upper(), None)
        if not isinstance(numeric_level, int):
            numeric_level = logging.INFO
            
        # Формат логов
        log_format = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
        date_format = "%Y-%m-%d %H:%M:%S"
        
        # Конфигурация логгера
        logging.basicConfig(
            level=numeric_level,
            format=log_format,
            datefmt=date_format,
            filename=log_file
        )
        
        # Добавить обработчик для вывода в консоль
        console = logging.StreamHandler()
        console.setLevel(numeric_level)
        console.setFormatter(logging.Formatter(log_format))
        logging.getLogger('').addHandler(console)
        
        return logging.getLogger()

# pages/login_page.py
from selenium.webdriver.common.by import By
from base.base_page import BasePage

class LoginPage(BasePage):
    """Класс для работы со страницей логина."""
    
    # Локаторы
    USERNAME_INPUT = (By.ID, "username")
    PASSWORD_INPUT = (By.ID, "password")
    LOGIN_BUTTON = (By.ID, "login-button")
    ERROR_MESSAGE = (By.CLASS_NAME, "error-message")
    
    def __init__(self, driver):
        super().__init__(driver)
        
    def open_login_page(self, url):
        """Открыть страницу логина."""
        self.open(url)
        
    def login(self, username, password):
        """Войти в систему с указанными учетными данными."""
        self.input_text(self.USERNAME_INPUT, username)
        self.input_text(self.PASSWORD_INPUT, password)
        self.click(self.LOGIN_BUTTON)
        
    def get_error_message(self):
        """Получить сообщение об ошибке."""
        return self.get_text(self.ERROR_MESSAGE)
        
    def is_login_successful(self):
        """Проверить, успешен ли логин."""
        return not self.is_element_visible(self.ERROR_MESSAGE, timeout=2)

# pages/home_page.py
from selenium.webdriver.common.by import By
from base.base_page import BasePage

class HomePage(BasePage):
    """Класс для работы с домашней страницей."""
    
    # Локаторы
    WELCOME_MESSAGE = (By.ID, "welcome-message")
    USER_PROFILE = (By.ID, "user-profile")
    LOGOUT_BUTTON = (By.ID, "logout")
    
    def __init__(self, driver):
        super().__init__(driver)
        
    def get_welcome_message(self):
        """Получить приветственное сообщение."""
        return self.get_text(self.WELCOME_MESSAGE)
        
    def open_user_profile(self):
        """Открыть профиль пользователя."""
        self.click(self.USER_PROFILE)
        
    def logout(self):
        """Выйти из системы."""
        self.click(self.LOGOUT_BUTTON)
        
    def is_user_logged_in(self):
        """Проверить, вошел ли пользователь в систему."""
        return self.is_element_visible(self.LOGOUT_BUTTON)

# tests/test_login.py
import unittest
from base.base_test import BaseTest
from pages.login_page import LoginPage
from pages.home_page import HomePage
from utils.logger import Logger
import os

class LoginTest(BaseTest):
    """Тесты для проверки функциональности логина."""
    
    def setUp(self):
        """Подготовка к тесту."""
        super().setUp()
        
        # Настройка логирования
        log_file = os.path.join("logs", f"login_test_{self._testMethodName}.log")
        self.logger = Logger.setup_logger(self.config.log_level, log_file)
        
        # Инициализация страниц
        self.login_page = LoginPage(self.driver)
        self.home_page = HomePage(self.driver)
        
    def test_successful_login(self):
        """Тест успешного входа в систему."""
        self.logger.info("Начало теста успешного входа")
        
        # Открыть страницу логина
        self.login_page.open_login_page(f"{self.config.base_url}/login")
        
        # Войти в систему
        self.login_page.login("testuser", "password123")
        
        # Проверить, что вход выполнен успешно
        self.assertTrue(
            self.home_page.is_user_logged_in(),
            "Пользователь не вошел в систему после ввода корректных данных"
        )
        
        # Проверить приветственное сообщение
        welcome_message = self.home_page.get_welcome_message()
        self.assertIn(
            "testuser", 
            welcome_message, 
            f"Приветственное сообщение не содержит имя пользователя. Текст: {welcome_message}"
        )
        
        self.logger.info("Тест успешного входа завершен")
        
    def test_failed_login(self):
        """Тест неудачного входа в систему."""
        self.logger.info("Начало теста неудачного входа")
        
        # Открыть страницу логина
        self.login_page.open_login_page(f"{self.config.base_url}/login")
        
        # Войти с неверными учетными данными
        self.login_page.login("wronguser", "wrongpassword")
        
        # Проверить, что вход не выполнен
        self.assertFalse(
            self.login_page.is_login_successful(),
            "Система позволила войти с неверными данными"
        )
        
        # Проверить сообщение об ошибке
        error_message = self.login_page.get_error_message()
        self.assertIn(
            "неверный", 
            error_message.lower(), 
            f"Неожиданное сообщение об ошибке: {error_message}"
        )
        
        self.logger.info("Тест неудачного входа завершен")

# conftest.py
import pytest
import os
import time
from datetime import datetime
from selenium import webdriver
from utils.driver_factory import DriverFactory
from config.config import Config
from utils.logger import Logger

def pytest_addoption(parser):
    """Добавить опции командной строки."""
    parser.addoption("--browser", action="store", default="chrome", help="Выберите браузер")
    parser.addoption("--env", action="store", default="dev", help="Выберите среду (dev/stage/prod)")

@pytest.fixture(scope="session")
def config(request):
    """Фикстура для конфигурации."""
    # Получить настройки из командной строки
    browser = request.config.getoption("--browser")
    env = request.config.getoption("--env")
    
    # Инициализировать конфигурацию
    cfg = Config()
    
    # Переопределить настройки
    cfg.browser = browser
    cfg.environment = env
    
    return cfg

@pytest.fixture(scope="function")
def driver(config):
    """Фикстура для драйвера."""
    # Инициализировать логгер
    log_dir = "logs"
    if not os.path.exists(log_dir):
        os.makedirs(log_dir)
    log_file = os.path.join(log_dir, f"test_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log")
    logger = Logger.setup_logger(config.log_level, log_file)
    
    # Получить драйвер
    driver_factory = DriverFactory()
    driver = driver_factory.get_driver(config.browser)
    driver.maximize_window()
    driver.implicitly_wait(config.implicit_wait)
    
    # Вернуть драйвер для использования в тесте
    yield driver
    
    # Закрыть драйвер после теста
    driver.quit()

@pytest.fixture(scope="function")
def screenshot_on_failure(request, driver, config):
    """Фикстура для создания скриншота при падении теста."""
    yield
    
    # Проверить, завершился ли тест с ошибкой
    if request.node.rep_call.failed and config.take_screenshot_on_failure:
        # Создать каталог для скриншотов, если он не существует
        if not os.path.exists(config.screenshots_dir):
            os.makedirs(config.screenshots_dir)
            
        # Создать имя файла
        file_name = f"{request.node.name}_{time.time()}.png"
        screenshot_path = os.path.join(config.screenshots_dir, file_name)
        
        # Сохранить скриншот
        driver.save_screenshot(screenshot_path)
        print(f"Скриншот сохранён: {screenshot_path}")

@pytest.hookimpl(tryfirst=True, hookwrapper=True)
def pytest_runtest_makereport(item, call):
    """Хук для определения результата теста."""
    outcome = yield
    rep = outcome.get_result()
    
    # Установить атрибут для последующего использования в фикстуре screenshot_on_failure
    setattr(item, f"rep_{rep.when}", rep)

# requirements.txt
selenium==4.10.0
pytest==7.3.1
pytest-html==3.2.0
webdriver-manager==4.0.0
requests==2.31.0
python-dotenv==1.0.0
allure-pytest==2.13.2
