import './commands';

Cypress.on('uncaught:exception', (err) => {
  if (err.message.includes('ResizeObserver loop')) {
    return false;
  }
  return true;
});

Cypress.on('window:before:load', (win) => {
  cy.spy(win.console, 'warn').as('consoleWarn');
  cy.spy(win.console, 'error').as('consoleError');
});
