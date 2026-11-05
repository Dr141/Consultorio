describe('06 - Prontuário de Consultas e Solicitação/Laudos de Exames', () => {
  const textoEvolucao = 'Paciente estável em avaliação E2E Cypress';
  const textoEvolucaoEditada = 'Conduta ajustada com retorno em 15 dias E2E';
  const descricaoExame = 'Ressonância Magnética Crânio E2E';
  const resultadoLaudo = 'Exame dentro dos padrões de normalidade (Laudo E2E)';

  beforeEach(() => {
    cy.loginApi();
    cy.garantirPacienteTeste();
    cy.garantirMedicoTeste();

    // Limpa consultas e exames de teste anteriores
    cy.request('GET', '/api/consulta').then((res) => {
      (res.body as any[]).forEach((c) => {
        if (c.observacao === textoEvolucao || c.observacao === textoEvolucaoEditada) {
          cy.request('DELETE', `/api/consulta/${c.id}`);
        }
      });
    });

    cy.request('GET', '/api/exame').then((res) => {
      (res.body as any[]).forEach((e) => {
        if (e.descricao === descricaoExame) {
          cy.request('DELETE', `/api/exame/${e.id}`);
        }
      });
    });
  });

  it('Deve registrar uma nova consulta médica, editar o prontuário/evolução e excluir', () => {
    cy.visit('/consultas');
    cy.aguardarCarregamento();

    // 1. Abrir modal de Nova Consulta
    cy.get('ion-button[title="Registrar nova consulta"]').click();
    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Nova Consulta Médica').should('be.visible');

    // Selecionar Paciente
    cy.get('ion-modal ion-select[formControlName="idPaciente"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();

    // Selecionar Médico
    cy.get('ion-modal ion-select[formControlName="idMedico"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();

    // Preencher evolução médica
    cy.get('ion-modal ion-textarea[formControlName="observacao"] textarea')
      .clear({ force: true })
      .type(textoEvolucao, { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // 2. Filtrar e verificar card da consulta
    cy.get('ion-searchbar input').clear({ force: true }).type(textoEvolucao, { force: true });
    cy.contains('ion-card.consulta-card', textoEvolucao).should('be.visible').within(() => {
      cy.contains('ion-button', 'Editar Observação').click();
    });

    // 3. Editar observação da consulta
    cy.get('ion-modal').should('be.visible');
    cy.get('ion-modal ion-textarea[formControlName="observacao"] textarea')
      .clear({ force: true })
      .type(textoEvolucaoEditada, { force: true });
    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    cy.get('ion-searchbar input').clear({ force: true }).type(textoEvolucaoEditada, { force: true });
    cy.contains('ion-card.consulta-card', textoEvolucaoEditada).should('be.visible').within(() => {
      cy.contains('ion-button', 'Excluir').click();
    });

    // 4. Confirmar exclusão
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.aguardarCarregamento();
  });

  it('Deve solicitar um exame (Aguardando), lançar o laudo (Laudo Pronto) e excluir', () => {
    cy.visit('/exames');
    cy.aguardarCarregamento();

    // 1. Solicitar Exame sem resultado inicial
    cy.get('ion-button[title="Solicitar novo exame"]').click();
    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Solicitar Exame').should('be.visible');

    cy.get('ion-modal ion-select[formControlName="idPaciente"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();

    cy.get('ion-modal ion-input[formControlName="descricao"] input')
      .clear({ force: true })
      .type(descricaoExame, { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // 2. Verificar badge Aguardando e clicar em Lançar Laudo
    cy.get('ion-searchbar input').clear({ force: true }).type(descricaoExame, { force: true });
    cy.contains('ion-card.exame-card', descricaoExame).should('be.visible').within(() => {
      cy.contains('ion-badge', 'Aguardando').should('be.visible');
      cy.contains('ion-button', 'Lançar Laudo').click();
    });

    // 3. Preencher laudo médico
    cy.get('ion-modal').should('be.visible');
    cy.get('ion-modal ion-textarea[formControlName="resultado"] textarea')
      .clear({ force: true })
      .type(resultadoLaudo, { force: true });
    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // 4. Verificar mudança para Laudo Pronto e excluir
    cy.contains('ion-card.exame-card', descricaoExame).within(() => {
      cy.contains('ion-badge', 'Laudo Pronto').should('be.visible');
      cy.contains('.resultado-texto', resultadoLaudo).should('be.visible');
      cy.contains('ion-button', 'Excluir').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.aguardarCarregamento();
  });
});
