import { formatarCpf, gerarCpfValido } from '../support/dados-teste';

describe('12 - Portal do Paciente', () => {
  it('Deve abrir a entrada do portal a partir da tela de login da equipe', () => {
    cy.visit('/login');
    cy.contains('a', 'Acesse o portal do paciente').click();
    cy.url().should('include', '/portal');
    cy.contains('ion-title', 'Portal do Paciente').should('exist');
    cy.contains('ion-card-title', 'Acesse seus dados').should('be.visible');
  });

  it('Deve responder igual para qualquer CPF (não revela quem é paciente) e pedir o código', () => {
    cy.visit('/portal');
    cy.get('ion-input[formControlName="cpf"] input').type(formatarCpf(gerarCpfValido()), { force: true });
    cy.get('ion-input[formControlName="dtNasc"] input').type('1990-05-20', { force: true });
    cy.contains('ion-button', 'Receber código por e-mail').click();

    cy.contains('ion-card-title', 'Digite o código').should('be.visible');
    cy.contains('enviamos um código de acesso').should('exist');
  });

  it('Deve recusar código inválido sem abrir a área do paciente', () => {
    cy.visit('/portal');
    cy.get('ion-input[formControlName="cpf"] input').type(formatarCpf(gerarCpfValido()), { force: true });
    cy.get('ion-input[formControlName="dtNasc"] input').type('1985-01-10', { force: true });
    cy.contains('ion-button', 'Receber código por e-mail').click();

    cy.get('ion-input.campo-codigo input').type('123456', { force: true });
    cy.contains('ion-button', 'Entrar').click();
    cy.contains('.aviso-erro', 'Código inválido').should('be.visible');
    cy.url().should('not.include', '/portal/inicio');
  });

  it('Deve bloquear a API do portal sem sessão', () => {
    cy.request({ url: '/api/portal/resumo', failOnStatusCode: false }).its('status').should('eq', 401);
    cy.request({ url: '/api/portal/resumo', headers: { 'X-Portal-Token': 'token-invalido' }, failOnStatusCode: false })
      .its('status').should('eq', 401);
  });

  it('Deve redirecionar para a entrada quando a área do paciente é aberta sem sessão', () => {
    cy.visit('/portal/inicio');
    cy.url().should('match', /\/portal(\?.*)?$/);
    cy.contains('ion-card-title', 'Acesse seus dados').should('exist');
  });
});
