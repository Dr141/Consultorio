describe('01 - Autenticação e Proteção de Rotas', () => {
  beforeEach(() => {
    cy.clearCookies();
    cy.clearLocalStorage();
  });

  it('Deve redirecionar acesso não autenticado em rotas protegidas para /login sem travar em carregando', () => {
    const rotasProtegidas = ['/pacientes', '/medicos', '/agenda', '/consultas', '/exames', '/users'];

    rotasProtegidas.forEach((rota) => {
      cy.clearCookies();
      cy.visit(rota);
      cy.url({ timeout: 10000 }).should('include', '/login');
      cy.get('#cardLogin').should('be.visible');
    });
  });

  it('Deve manter o botão Entrar desabilitado quando o formulário for inválido', () => {
    cy.visit('/login');
    cy.get('ion-card-title').should('contain.text', 'Acesso ao Sistema');
    cy.get('ion-button[type="submit"]').should('have.attr', 'disabled');

    cy.get('ion-input[formControlName="email"] input').type('email-invalido', { force: true });
    cy.get('ion-input[formControlName="senha"] input').type('123', { force: true });
    cy.get('ion-button[type="submit"]').should('have.attr', 'disabled');
  });

  it('Deve exibir mensagem de erro ao tentar autenticar com credenciais inválidas', () => {
    cy.visit('/login');
    cy.get('ion-input[formControlName="email"] input').clear({ force: true }).type('admin@domain.com', { force: true });
    cy.get('ion-input[formControlName="senha"] input').clear({ force: true }).type('SenhaErrada@999', { force: true });
    cy.get('ion-button[type="submit"]').should('not.have.attr', 'disabled');
    cy.get('ion-button[type="submit"]').click();

    cy.get('#contentErro', { timeout: 8000 }).should('be.visible');
    cy.url().should('include', '/login');
  });

  it('Deve realizar login com sucesso como Administrador e acessar o painel principal (/home)', () => {
    cy.visit('/login');
    cy.get('ion-input[formControlName="email"] input').clear({ force: true }).type('admin@domain.com', { force: true });
    cy.get('ion-input[formControlName="senha"] input').clear({ force: true }).type('Admin@123', { force: true });
    cy.get('ion-button[type="submit"]').click();

    cy.url({ timeout: 10000 }).should('include', '/home');
    cy.get('.welcome-banner h2').should('be.visible');
    cy.get('.cardHome').should('have.length', 9);
  });

  it('Deve encerrar a sessão (Logout) através do menu lateral e retornar ao /login', () => {
    cy.loginApi();
    cy.visit('/home');
    cy.get('.welcome-banner h2').should('be.visible');

    cy.get('ion-menu-button').click();
    cy.get('.menu-item-logout').should('be.visible').click({ force: true });

    // Confirmação de saída
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();

    cy.url({ timeout: 10000 }).should('include', '/login');
    cy.get('#cardLogin').should('be.visible');
  });
});
