import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import SearchForm from '../SearchForm';
import { searchProducts } from '../../api/productService';

// Моки внешних зависимостей
jest.mock('../../api/productService');

describe('Компонент SearchForm', () => {
  beforeEach(() => {
    searchProducts.mockClear();
  });

  test('Рендеринг компонента поиска', () => {
    render(<SearchForm onSearchResults={() => {}} />);
    
    // Проверка наличия необходимых элементов
    expect(screen.getByPlaceholderText('Поиск товаров...')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /искать/i })).toBeInTheDocument();
    expect(screen.getByLabelText('Категория')).toBeInTheDocument();
  });

  test('Валидация формы поиска при отправке', async () => {
    render(<SearchForm onSearchResults={() => {}} />);
    
    // Отправка пустой формы
    const searchButton = screen.getByRole('button', { name: /искать/i });
    fireEvent.click(searchButton);
    
    // Проверка отображения ошибки
    expect(screen.getByText('Введите поисковый запрос')).toBeInTheDocument();
    
    // API не должен вызываться при ошибке валидации
    expect(searchProducts).not.toHaveBeenCalled();
  });

  test('Отправка поискового запроса', async () => {
    const mockResults = [
      { id: 1, name: 'Тестовый продукт 1', price: 1000 },
      { id: 2, name: 'Тестовый продукт 2', price: 2000 }
    ];
    
    searchProducts.mockResolvedValue(mockResults);
    
    const handleSearchResults = jest.fn();
    render(<SearchForm onSearchResults={handleSearchResults} />);
    
    // Заполнение формы
    const searchInput = screen.getByPlaceholderText('Поиск товаров...');
    fireEvent.change(searchInput, { target: { value: 'тестовый запрос' } });
    
    // Выбор категории
    const categorySelect = screen.getByLabelText('Категория');
    fireEvent.change(categorySelect, { target: { value: 'electronics' } });
    
    // Отправка формы
    const searchButton = screen.getByRole('button', { name: /искать/i });
    fireEvent.click(searchButton);
    
    // Проверка вызова API с правильными параметрами
    expect(searchProducts).toHaveBeenCalledWith('тестовый запрос', 'electronics');
    
    // Проверка обработки результатов
    await waitFor(() => {
      expect(handleSearchResults).toHaveBeenCalledWith(mockResults);
    });
  });

  test('Отображение состояния загрузки', async () => {
    // Эмуляция задержки ответа API
    searchProducts.mockImplementation(() => {
      return new Promise(resolve => {
        setTimeout(() => resolve([]), 1000);
      });
    });
    
    render(<SearchForm onSearchResults={() => {}} />);
    
    // Заполнение и отправка формы
    const searchInput = screen.getByPlaceholderText('Поиск товаров...');
    fireEvent.change(searchInput, { target: { value: 'тестовый запрос' } });
    
    const searchButton = screen.getByRole('button', { name: /искать/i });
    fireEvent.click(searchButton);
    
    // Проверка состояния загрузки
    expect(screen.getByText('Выполняется поиск...')).toBeInTheDocument();
    expect(searchButton).toBeDisabled();
    
    // Ожидание завершения запроса
    await waitFor(() => {
      expect(screen.queryByText('Выполняется поиск...')).not.toBeInTheDocument();
      expect(searchButton).not.toBeDisabled();
    });
  });

  test('Обработка ошибки API', async () => {
    // Эмуляция ошибки API
    const errorMessage = 'Ошибка сервера';
    searchProducts.mockRejectedValue(new Error(errorMessage));
    
    render(<SearchForm onSearchResults={() => {}} />);
    
    // Заполнение и отправка формы
    const searchInput = screen.getByPlaceholderText('Поиск товаров...');
    fireEvent.change(searchInput, { target: { value: 'тестовый запрос' } });
    
    const searchButton = screen.getByRole('button', { name: /искать/i });
    fireEvent.click(searchButton);
    
    // Проверка отображения ошибки
    await waitFor(() => {
      expect(screen.getByText(`Ошибка при поиске: ${errorMessage}`)).toBeInTheDocument();
    });
  });
});