import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Order;
import org.junit.jupiter.api.TestMethodOrder;
import org.junit.jupiter.api.MethodOrderer.OrderAnnotation;

import static io.restassured.RestAssured.given;
import static org.hamcrest.Matchers.*;

@TestMethodOrder(OrderAnnotation.class)
public class ApiTests {
    
    private static String accessToken;
    private static int createdUserId;
    
    @BeforeAll
    public static void setup() {
        RestAssured.baseURI = "https://api.example.com";
    }
    
    @Test
    @Order(1)
    @DisplayName("Тест аутентификации API")
    public void testAuthentication() {
        // Получение токена авторизации
        Response response = given()
            .contentType(ContentType.JSON)
            .body("{ \"username\": \"testuser\", \"password\": \"password123\" }")
            .when()
            .post("/auth/login")
            .then()
            .statusCode(200)
            .body("success", equalTo(true))
            .body("token", not(emptyOrNullString()))
            .extract()
            .response();
            
        accessToken = response.jsonPath().getString("token");
    }
    
    @Test
    @Order(2)
    @DisplayName("Получение списка пользователей")
    public void testGetUsers() {
        given()
            .header("Authorization", "Bearer " + accessToken)
            .when()
            .get("/users")
            .then()
            .statusCode(200)
            .body("users", not(empty()))
            .body("users[0]", hasKey("id"))
            .body("users[0]", hasKey("name"))
            .body("users[0]", hasKey("email"));
    }
    
    @Test
    @Order(3)
    @DisplayName("Создание нового пользователя")
    public void testCreateUser() {
        String requestBody = "{ \"name\": \"Новый Пользователь\", " +
                            "\"email\": \"new.user@example.com\", " +
                            "\"role\": \"user\" }";
                            
        Response response = given()
            .header("Authorization", "Bearer " + accessToken)
            .contentType(ContentType.JSON)
            .body(requestBody)
            .when()
            .post("/users")
            .then()
            .statusCode(201)
            .body("success", equalTo(true))
            .body("user.name", equalTo("Новый Пользователь"))
            .body("user.email", equalTo("new.user@example.com"))
            .body("user.id", not(emptyOrNullString()))
            .extract()
            .response();
            
        createdUserId = response.jsonPath().getInt("user.id");
    }
    
    @Test
    @Order(4)
    @DisplayName("Обновление пользователя")
    public void testUpdateUser() {
        String requestBody = "{ \"name\": \"Обновленное Имя\" }";
        
        given()
            .header("Authorization", "Bearer " + accessToken)
            .contentType(ContentType.JSON)
            .body(requestBody)
            .when()
            .put("/users/" + createdUserId)
            .then()
            .statusCode(200)
            .body("success", equalTo(true))
            .body("user.name", equalTo("Обновленное Имя"))
            .body("user.id", equalTo(createdUserId));
    }
    
    @Test
    @Order(5)
    @DisplayName("Валидация API данных")
    public void testApiValidation() {
        String invalidRequestBody = "{ \"email\": \"invalid-email\" }";
        
        given()
            .header("Authorization", "Bearer " + accessToken)
            .contentType(ContentType.JSON)
            .body(invalidRequestBody)
            .when()
            .post("/users")
            .then()
            .statusCode(400)
            .body("success", equalTo(false))
            .body("errors", hasKey("email"));
    }
    
    @Test
    @Order(6)
    @DisplayName("Тестирование производительности API")
    public void testApiPerformance() {
        given()
            .header("Authorization", "Bearer " + accessToken)
            .when()
            .get("/users")
            .then()
            .time(lessThan(1000L));  // Ответ должен прийти быстрее 1 секунды
    }
    
    @Test
    @Order(7)
    @DisplayName("Удаление пользователя")
    public void testDeleteUser() {
        given()
            .header("Authorization", "Bearer " + accessToken)
            .when()
            .delete("/users/" + createdUserId)
            .then()
            .statusCode(200)
            .body("success", equalTo(true));
        
        // Проверка что пользователь действительно удален
        given()
            .header("Authorization", "Bearer " + accessToken)
            .when()
            .get("/users/" + createdUserId)
            .then()
            .statusCode(404);
    }
}
