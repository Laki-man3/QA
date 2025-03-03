const request = require('supertest');
const app = require('../../app');
const mongoose = require('mongoose');
const { Order } = require('../../models');
const { generateAuthToken } = require('../../utils/auth');
const kafka = require('../../utils/kafka');

// Мокаем Kafka для тестирования
jest.mock('../../utils/kafka');

describe('API заказов', () => {
  let server;
  let authToken;
  let testUserId;
  
  beforeAll(async () => {
    server = app.listen(3001);
    // Установить соединение с тестовой БД
    await mongoose.connect(process.env.TEST_MONGO_URI);
    
    // Создать тестового пользователя для авторизации
    testUserId = new mongoose.Types.ObjectId();
    authToken = generateAuthToken({ id: testUserId, role: 'user' });
  });
  
  beforeEach(async () => {
    // Очистить коллекцию перед каждым тестом
    await Order.deleteMany({});
    
    // Сбросить моки
    kafka.send.mockClear();
  });
  
  afterAll(async () => {
    // Закрыть соединение с сервером и БД
    await mongoose.connection.close();
    await server.close();
  });
  
  describe('POST /api/orders', () => {
    it('должен создать новый заказ', async () => {
      const orderData = {
        products: [
          { productId: new mongoose.Types.ObjectId(), quantity: 2, price: 1000 },
          { productId: new mongoose.Types.ObjectId(), quantity: 1, price: 500 }
        ],
        shippingAddress: {
          street: 'Тестовая улица',
          city: 'Москва',
          country: 'Россия',
          postalCode: '123456'
        },
        paymentMethod: 'card'
      };
      
      const response = await request(app)
        .post('/api/orders')
        .set('Authorization', `Bearer ${authToken}`)
        .send(orderData)
        .expect(201);
      
      // Проверка структуры ответа
      expect(response.body).toHaveProperty('id');
      expect(response.body).toHaveProperty('status', 'pending');
      expect(response.body).toHaveProperty('totalAmount', 2500);
      
      // Проверка сохранения в БД
      const savedOrder = await Order.findById(response.body.id);
      expect(savedOrder).not.toBeNull();
      expect(savedOrder.userId.toString()).toBe(testUserId.toString());
      
      // Проверка отправки события в Kafka
      expect(kafka.send).toHaveBeenCalledWith('order-created', expect.objectContaining({
        orderId: response.body.id,
        userId: testUserId.toString(),
        status: 'pending'
      }));
    });
    
    it('должен вернуть ошибку при некорректных данных', async () => {
      // Отсутствуют обязательные поля
      const incompleteOrder = {
        products: []
      };
      
      const response = await request(app)
        .post('/api/orders')
        .set('Authorization', `Bearer ${authToken}`)
        .send(incompleteOrder)
        .expect(400);
      
      expect(response.body).toHaveProperty('error');
      expect(response.body.error).toContain('products');
      
      // Проверка что заказ не создан
      const orderCount = await Order.countDocuments();
      expect(orderCount).toBe(0);
    });
  });
  
  describe('GET /api/orders', () => {
    it('должен вернуть список заказов пользователя', async () => {
      // Создаем тестовые заказы
      const orders = [
        {
          userId: testUserId,
          products: [{ productId: new mongoose.Types.ObjectId(), quantity: 1, price: 1000 }],
          totalAmount: 1000,
          status: 'completed',
          createdAt: new Date('2023-01-01')
        },
        {
          userId: testUserId,
          products: [{ productId: new mongoose.Types.ObjectId(), quantity: 2, price: 500 }],
          totalAmount: 1000,
          status: 'pending',
          createdAt: new Date('2023-01-02')
        }
      ];
      
      await Order.insertMany(orders);
      
      const response = await request(app)
        .get('/api/orders')
        .set('Authorization', `Bearer ${authToken}`)
        .expect(200);
      
      expect(response.body).toHaveProperty('orders');
      expect(response.body.orders).toHaveLength(2);
      expect(response.body.orders[0].status).toBe('pending'); // Проверка сортировки по дате
    });
    
    it('должен поддерживать фильтрацию и пагинацию', async () => {
      // Создаем 10 тестовых заказов с разными статусами
      const orders = [];
      for (let i = 0; i < 5; i++) {
        orders.push({
          userId: testUserId,
          products: [{ productId: new mongoose.Types.ObjectId(), quantity: 1, price: 1000 }],
          totalAmount: 1000,
          status: 'completed'
        });
      }
      
      for (let i = 0; i < 5; i++) {
        orders.push({
          userId: testUserId,
          products: [{ productId: new mongoose.Types.ObjectId(), quantity: 1, price: 1000 }],
          totalAmount: 1000,
          status: 'pending'
        });
      }
      
      await Order.insertMany(orders);
      
      // Проверка фильтрации по статусу
      const filteredResponse = await request(app)
        .get('/api/orders?status=completed')
        .set('Authorization', `Bearer ${authToken}`)
        .expect(200);
      
      expect(filteredResponse.body.orders).toHaveLength(5);
      expect(filteredResponse.body.orders.every(order => order.status === 'completed')).toBe(true);
      
      // Проверка пагинации
      const paginatedResponse = await request(app)
        .get('/api/orders?page=1&limit=3')
        .set('Authorization', `Bearer ${authToken}`)
        .expect(200);
      
      expect(paginatedResponse.body.orders).toHaveLength(3);
      expect(paginatedResponse.body).toHaveProperty('pagination');
      expect(paginatedResponse.body.pagination).toHaveProperty('total', 10);
      expect(paginatedResponse.body.pagination).toHaveProperty('pages', 4);
    });
  });
  
  describe('PUT /api/orders/:id/cancel', () => {
    it('должен отменить заказ в статусе pending', async () => {
      // Создаем тестовый заказ
      const order = new Order({
        userId: testUserId,
        products: [{ productId: new mongoose.Types.ObjectId(), quantity: 1, price: 1000 }],
        totalAmount: 1000,
        status: 'pending'
      });
      
      await order.save();
      
      const response = await request(app)
        .put(`/api/orders/${order._id}/cancel`)
        .set('Authorization', `Bearer ${authToken}`)
        .expect(200);
      
      expect(response.body).toHaveProperty('status', 'cancelled');
      
      // Проверка обновления в БД
      const updatedOrder = await Order.findById(order._id);
      expect(updatedOrder.status).toBe('cancelled');
      
      // Проверка отправки события в Kafka
      expect(kafka.send).toHaveBeenCalledWith('order-cancelled', expect.objectContaining({
        orderId: order._id.toString(),
        userId: testUserId.toString()
      }));
    });
    
    it('не должен отменить заказ в статусе completed', async () => {
      // Создаем тестовый заказ в статусе completed
      const order = new Order({
        userId: testUserId,
        products: [{ productId: new mongoose.Types.ObjectId(), quantity: 1, price: 1000 }],
        totalAmount: 1000,
        status: 'completed'
      });
      
      await order.save();
      
      const response = await request(app)
        .put(`/api/orders/${order._id}/cancel`)
        .set('Authorization', `Bearer ${authToken}`)
        .expect(400);
      
      expect(response.body).toHaveProperty('error');
      expect(response.body.error).toContain('нельзя отменить');
      
      // Проверка что статус не изменился
      const updatedOrder = await Order.findById(order._id);
      expect(updatedOrder.status).toBe('completed');
    });
  });
});