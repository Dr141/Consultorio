import { sufixoExecucao } from '../support/dados-teste';

describe('11 - Financeiro, Indicadores e Pendências', () => {
  const sufixo = sufixoExecucao();
  const descricao = `Taxa de material E2E ${sufixo}`;

  beforeEach(() => {
    cy.loginApi();
    cy.garantirPacienteTeste();
  });

  it('Deve lançar uma cobrança, receber com Pix e abrir o recibo', () => {
    cy.request('GET', '/api/paciente').then((res) => {
      const idPaciente = (res.body as any[])[0].id;
      cy.request('POST', '/api/financeiro/cobrancas', { idPaciente, descricao, valor: 150.5 });
    });

    cy.intercept('GET', '/api/financeiro/cobrancas/*/recibo').as('recibo');
    cy.visit('/financeiro');
    cy.aguardarCarregamento();
    cy.contains('ion-title', 'Financeiro').should('exist');

    cy.contains('ion-card.cobranca-card', descricao).within(() => {
      cy.contains('ion-badge', 'Pendente').should('be.visible');
      cy.contains('ion-button', 'Receber').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-modal .forma-opcao', 'Pix').click();
    cy.contains('ion-modal ion-button', 'Confirmar recebimento').click();

    // Pergunta se deseja o recibo
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();

    cy.url().should('include', '/financeiro/recibo/');
    // Em caso de falha, a mensagem do servidor aparece no relatório do Cypress.
    cy.wait('@recibo').then(({ response }) => {
      expect(response?.statusCode, JSON.stringify(response?.body)).to.eq(200);
    });
    cy.contains('cento e cinquenta reais e cinquenta centavos').should('exist');
    cy.contains(descricao).should('exist');
  });

  it('Deve cadastrar convênio e definir valor na tabela de preços', () => {
    const nomeConvenio = `Plano E2E ${sufixo}`;
    cy.request({ method: 'POST', url: '/api/convenio', body: { nome: nomeConvenio }, failOnStatusCode: false }).then((res) => {
      expect(res.status, JSON.stringify(res.body)).to.eq(201);
    });
    cy.request({ url: '/api/convenio?incluirInativos=true', failOnStatusCode: false }).then((res) => {
      expect(res.status, JSON.stringify(res.body)).to.eq(200);
    });
    cy.request({ url: '/api/convenio/precos', failOnStatusCode: false }).then((res) => {
      expect(res.status, JSON.stringify(res.body)).to.eq(200);
    });

    cy.intercept('PUT', '/api/convenio/precos').as('salvarPreco');
    cy.visit('/financeiro?aba=precos');
    cy.aguardarCarregamento();
    cy.contains('ion-card.tabela-card', 'Particular').should('exist');
    cy.contains('ion-card.tabela-card', nomeConvenio).within(() => {
      cy.get('ion-input input').first().clear({ force: true }).type('220', { force: true }).blur({ force: true });
    });
    cy.wait('@salvarPreco').its('response.statusCode').should('eq', 200);
    cy.request('GET', '/api/convenio/precos').its('body').should((precos: any[]) => {
      expect(precos.some(p => p.nomeConvenio === nomeConvenio && p.tipo === 'Consulta' && p.valor === 220)).to.eq(true);
    });
  });

  it('Deve exibir indicadores e pendências', () => {
    cy.visit('/indicadores');
    cy.contains('ion-title', 'Indicadores').should('exist');
    cy.contains('.kpi', 'Atendimentos realizados').should('exist');
    cy.contains('.kpi', 'Recebido').should('exist');

    cy.visit('/indicadores/pendencias');
    cy.contains('ion-title', 'Pendências').should('exist');
    cy.get('.loading-state').should('not.exist');
  });
});
