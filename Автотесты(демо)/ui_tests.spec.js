// Файл: cypress/integration/ui_tests.spec.js
describe('UI/UX тестирование', () => {
  beforeEach(() => {
    cy.visit('https://example.com')
  })

  context('Адаптивный дизайн', () => {
    it('Корректно отображается на мобильных устройствах', () => {
      cy.viewport('iphone-x')
      cy.get('header').should('be.visible')
      cy.get('nav').should('exist')
      cy.get('.mobile-menu').should('be.visible')
      cy.get('.desktop-menu').should('not.be.visible')
    })

    it('Корректно отображается на планшетах', () => {
      cy.viewport('ipad-2')
      cy.get('header').should('be.visible')
      cy.get('.content').should('have.css', 'width').and('match', /75%|750px/)
    })

    it('Корректно отображается на десктопах', () => {
      cy.viewport(1920, 1080)
      cy.get('.desktop-menu').should('be.visible')
      cy.get('.mobile-menu').should('not.be.visible')
    })
  })

  context('Навигация и юзабилити', () => {
    it('Меню навигации работает корректно', () => {
      cy.get('nav a').first().click()
      cy.url().should('include', '/homepage')
      cy.get('h1').should('contain', 'Домашняя страница')
    })

    it('Кнопки имеют состояние наведения', () => {
      cy.get('.primary-button')
        .should('have.css', 'background-color', 'rgb(0, 123, 255)')
        .trigger('mouseover')
        .should('have.css', 'background-color', 'rgb(0, 105, 217)')
    })

    it('Доступность: элементы имеют правильные атрибуты', () => {
      cy.get('button').should('have.attr', 'aria-label')
      cy.get('img').should('have.attr', 'alt')
      cy.get('input').should('have.attr', 'aria-describedby')
    })

    it('Корректные transition-эффекты', () => {
      cy.get('.dropdown').click()
      cy.get('.dropdown-menu')
        .should('be.visible')
        .and('have.css', 'opacity', '1')
        .and('have.css', 'transition-duration').and('not.eq', '0s')
    })
  })
})