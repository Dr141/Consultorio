/// <reference types="cypress" />

export interface PacienteSeed {
  id: number;
  idPessoa: number;
  nome: string;
  cpf: string;
}

export interface MedicoSeed {
  id: number;
  idPessoa: number;
  nome: string;
  crm: string;
}

declare global {
  namespace Cypress {
    interface Chainable {
      /**
       * Realiza login via API e armazena a sessão (cookies AccessToken, RefreshToken e Roles).
       */
      loginApi(email?: string, senha?: string): Chainable<void>;

      /**
       * Garante a existência de um paciente de teste no banco via API e retorna seus dados.
       */
      garantirPacienteTeste(): Chainable<PacienteSeed>;

      /**
       * Garante a existência de um médico de teste no banco via API e retorna seus dados.
       */
      garantirMedicoTeste(): Chainable<MedicoSeed>;

      /**
       * Verifica que o indicador de carregamento (.loading-state) não está preso na tela.
       */
      aguardarCarregamento(): Chainable<void>;
    }
  }
}

Cypress.Commands.add('loginApi', (email = 'admin@domain.com', senha = 'Admin@123') => {
  cy.session(
    [email],
    () => {
      cy.request({
        method: 'POST',
        url: '/autenticacao',
        body: { Email: email, Senha: senha }
      }).then((resp) => {
        expect(resp.status).to.eq(200);
      });
    },
    {
      validate() {
        cy.getCookie('Roles').should('exist');
      }
    }
  );
});

Cypress.Commands.add('garantirPacienteTeste', () => {
  return cy.request('GET', '/api/paciente').then((res) => {
    const lista = res.body as PacienteSeed[];
    const existente = lista.find((p) => p.cpf === '390.533.447-05' || p.cpf === '39053344705');
    if (existente) {
      return cy.wrap(existente);
    }

    return cy
      .request('POST', '/api/paciente', {
        nome: 'Roberto Almeida E2E',
        cpf: '39053344705',
        dtNasc: '1985-03-10T00:00:00Z',
        telefone: '11977665544',
        observacao: 'Paciente base para testes E2E'
      })
      .its('body');
  });
});

Cypress.Commands.add('garantirMedicoTeste', () => {
  return cy.request('GET', '/api/medico').then((res) => {
    const lista = res.body as MedicoSeed[];
    const existente = lista.find((m) => m.crm === '123456-SP');
    if (existente) {
      return cy.wrap(existente);
    }

    return cy
      .request('POST', '/api/medico', {
        nome: 'Dr. Henrique Souza E2E',
        cpf: '71428793860',
        dtNasc: '1978-11-12T00:00:00Z',
        telefone: '11966554433',
        crm: '123456-SP',
        especialidades: [1, 2]
      })
      .its('body');
  });
});

Cypress.Commands.add('aguardarCarregamento', () => {
  cy.get('.loading-state', { timeout: 10000 }).should('not.exist');
});
