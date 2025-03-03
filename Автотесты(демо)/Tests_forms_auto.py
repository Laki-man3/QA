import unittest
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.keys import Keys
import time

class FormTests(unittest.TestCase):
    def setUp(self):
        self.driver = webdriver.Chrome()
        self.driver.maximize_window()
        self.driver.get("https://example.com/register")
        self.wait = WebDriverWait(self.driver, 10)
    
    def tearDown(self):
        self.driver.quit()
    
    def test_registration_form_validation(self):
        # Проверка валидации пустых полей
        submit_button = self.driver.find_element(By.ID, "submit-btn")
        submit_button.click()
        
        # Проверка появления сообщений об ошибке
        error_messages = self.driver.find_elements(By.CLASS_NAME, "error-message")
        self.assertTrue(len(error_messages) > 0)
        
        # Проверка валидации email
        email_field = self.driver.find_element(By.ID, "email")
        email_field.send_keys("invalid-email")
        submit_button.click()
        
        email_error = self.driver.find_element(By.ID, "email-error")
        self.assertTrue(email_error.is_displayed())
        self.assertIn("валидный email", email_error.text)
        
        # Проверка валидации пароля
        email_field.clear()
        email_field.send_keys("test@example.com")
        
        password_field = self.driver.find_element(By.ID, "password")
        password_field.send_keys("123")
        submit_button.click()
        
        password_error = self.driver.find_element(By.ID, "password-error")
        self.assertTrue(password_error.is_displayed())
        self.assertIn("не менее 8 символов", password_error.text)

    def test_successful_form_submission(self):
        # Заполнение формы корректными данными
        self.driver.find_element(By.ID, "name").send_keys("Иван Иванов")
        self.driver.find_element(By.ID, "email").send_keys("ivan@example.com")
        self.driver.find_element(By.ID, "password").send_keys("Password123!")
        self.driver.find_element(By.ID, "confirm-password").send_keys("Password123!")
        
        # Принять условия использования
        self.driver.find_element(By.ID, "terms").click()
        
        # Отправить форму
        self.driver.find_element(By.ID, "submit-btn").click()
        
        # Проверка успешной регистрации - редирект или сообщение
        success_message = self.wait.until(
            EC.visibility_of_element_located((By.CLASS_NAME, "success-message"))
        )
        self.assertTrue(success_message.is_displayed())
        self.assertIn("Регистрация успешно завершена", success_message.text)
    
    def test_dynamic_form_behavior(self):
        # Тест динамического поведения формы
        account_type = self.driver.find_element(By.ID, "account-type")
        account_type.click()
        account_type.find_element(By.XPATH, "//option[@value='business']").click()
        
        # Проверка появления дополнительных полей для бизнес-аккаунта
        company_field = self.wait.until(
            EC.visibility_of_element_located((By.ID, "company"))
        )
        self.assertTrue(company_field.is_displayed())
        
        # Заполнение дополнительных полей
        company_field.send_keys("ООО Тест")
        self.driver.find_element(By.ID, "business-type").click()
        self.driver.find_element(By.XPATH, "//option[@value='retail']").click()
        
        # Проверка условной валидации
        self.driver.find_element(By.ID, "tax-number").send_keys("123")
        self.driver.find_element(By.ID, "submit-btn").click()
        
        tax_error = self.driver.find_element(By.ID, "tax-number-error")
        self.assertTrue(tax_error.is_displayed())
        self.assertIn("некорректный ИНН", tax_error.text)