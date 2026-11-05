describe('07 - Gerenciamento de Usuários e Permissões', () => {
  beforeEach(() => {
    cy.loginApi();
  });

  it('Deve listar os usuários cadastrados sem travar em carregando e exibir o administrador padrão', () => {
    cy.visit('/users');
    cy.aguardarCarregamento();

    cy.contains('ion-card-title', 'Usuários Cadastrados').should('be.visible');
    cy.contains('.user-email', 'admin@domain.com').should('be.visible');
  });

  it('Deve abrir o modal de Novo Usuário, validar campos e fechar corretamente', () => {
    cy.visit('/users');
    cy.aguardarCarregamento();

    cy.get('.btn-novo-desk').click();
    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Novo Usuário').should('be.visible');

    // Botão deve iniciar desabilitado
    cy.get('ion-modal ion-button[type="submit"]').should('have.attr', 'disabled');

    // Preencher senhas divergentes mantém botão desabilitado ou inválido
    cy.get('ion-modal ion-input[formControlName="email"] input')
      .clear({ force: true })
      .type('recepcao.e2e@consultorio.com', { force: true });
    cy.get('ion-modal ion-input[formControlName="senha"] input')
      .clear({ force: true })
      .type('Teste@123', { force: true });
    cy.get('ion-modal ion-input[formControlName="confirmarSenha"] input')
      .clear({ force: true })
      .type('Teste@123', { force: true });

    cy.get('ion-modal ion-button[type="submit"]').should('not.have.attr', 'disabled');
    cy.contains('ion-modal ion-button', 'Fechar').click();
    cy.get('ion-modal').should('not.exist');
  });

  it('Deve abrir o modal de Permissões (Roles) e o modal de Senha de um usuário', () => {
    cy.visit('/users');
    cy.aguardarCarregamento();

    // 1. Abrir modal de Permissões
    cy.contains('ion-item.user-item', 'admin@domain.com').within(() => {
      cy.contains('ion-button', 'Permissões').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Permissões (Roles)').should('be.visible');
    cy.contains('h4', 'Permissões Atuais do Usuário').should('be.visible');
    cy.contains('ion-modal ion-button', 'Fechar').click();
    cy.get('ion-modal').should('not.exist');

    // 2. Abrir modal de Senha
    cy.contains('ion-item.user-item', 'admin@domain.com').within(() => {
      cy.contains('ion-button', 'Senha').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-modal ion-button', 'Fechar').click();
    cy.get('ion-modal').should('not.exist');
  });
});
