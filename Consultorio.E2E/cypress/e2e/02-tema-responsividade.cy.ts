describe('02 - Tema Dark/Light, Botões em Português e Responsividade', () => {
  beforeEach(() => {
    cy.loginApi();
  });

  it('Deve alternar entre Modo Claro e Modo Escuro pelo seletor da navbar e persistir no localStorage', () => {
    cy.visit('/home');
    cy.get('app-theme-toggle ion-button').should('be.visible');

    cy.get('html').then(($html) => {
      const iniciouDark = $html.hasClass('ion-palette-dark');

      cy.get('app-theme-toggle ion-button').first().click();

      if (iniciouDark) {
        cy.get('html').should('not.have.class', 'ion-palette-dark');
        cy.window().its('localStorage').invoke('getItem', 'consultorio_theme').should('eq', 'light');
      } else {
        cy.get('html').should('have.class', 'ion-palette-dark');
        cy.window().its('localStorage').invoke('getItem', 'consultorio_theme').should('eq', 'dark');
      }

      // Alterna novamente e valida reversão
      cy.get('app-theme-toggle ion-button').first().click();
      if (iniciouDark) {
        cy.get('html').should('have.class', 'ion-palette-dark');
      } else {
        cy.get('html').should('not.have.class', 'ion-palette-dark');
      }
    });
  });

  it('Deve exibir o botão "Voltar" em português (e nunca "Back") em todas as páginas internas', () => {
    const paginasInternas = [
      { rota: '/pacientes', titulo: 'Pacientes' },
      { rota: '/medicos', titulo: 'Corpo Clínico' },
      { rota: '/agenda', titulo: 'Agenda' },
      { rota: '/consultas', titulo: 'Consultas' },
      { rota: '/exames', titulo: 'Exames e Laudos' },
      { rota: '/users', titulo: 'Gerenciamento de Usuários' }
    ];

    paginasInternas.forEach((pagina) => {
      cy.visit(pagina.rota);
      cy.aguardarCarregamento();
      cy.contains('ion-title', pagina.titulo).should('exist');
      cy.get('ion-back-button')
        .should('exist')
        .and('have.attr', 'text', 'Voltar')
        .and('not.have.attr', 'text', 'Back');
    });
  });

  it('Deve adaptar o layout corretamente em Mobile (375x812), Tablet (768x1024) e Desktop (1440x900)', () => {
    // Mobile
    cy.viewport(375, 812);
    cy.visit('/pacientes');
    cy.aguardarCarregamento();
    cy.get('ion-fab ion-fab-button').should('be.visible');

    // Tablet
    cy.viewport(768, 1024);
    cy.visit('/home');
    cy.get('.cardHome').should('have.length', 9);
    cy.get('.cardHome').first().should('be.visible');

    // Desktop Full
    cy.viewport(1440, 900);
    cy.visit('/users');
    cy.aguardarCarregamento();
    cy.get('.btn-novo-desk').should('be.visible');
  });
});
