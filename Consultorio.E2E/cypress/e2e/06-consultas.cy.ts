describe('06 - Prontuário e Gestão de Consultas Médicas', () => {
  const textoEvolucaoInicial = 'Paciente relata cefaleia tensional e fadiga crônica E2E';
  const textoEvolucaoEditada = 'Conduta terapêutica atualizada com receituário e repouso E2E';

  beforeEach(() => {
    cy.loginApi();
    cy.garantirPacienteTeste();
    cy.garantirMedicoTeste();

    // Limpa consultas de teste anteriores para idempotência
    cy.request('GET', '/api/consulta').then((res) => {
      (res.body as any[]).forEach((c) => {
        if (c.observacao && c.observacao.includes('E2E')) {
          cy.request('DELETE', `/api/consulta/${c.id}`);
        }
      });
    });
  });

  it('Deve validar obrigatoriedade dos campos no modal e fechar sem salvar', () => {
    cy.visit('/consultas');
    cy.aguardarCarregamento();

    cy.get('ion-button[title="Registrar nova consulta"]').click();
    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Nova Consulta Médica').should('be.visible');

    // Botão deve iniciar desabilitado devido aos campos obrigatórios não preenchidos
    cy.get('ion-modal ion-button[type="submit"]').should('have.attr', 'disabled');

    // Fechar modal
    cy.contains('ion-modal ion-button', 'Fechar').click();
    cy.get('ion-modal').should('not.exist');
  });

  it('Deve registrar uma nova consulta médica completa com paciente, médico e evolução', () => {
    cy.visit('/consultas');
    cy.aguardarCarregamento();

    cy.get('ion-button[title="Registrar nova consulta"]').click();
    cy.get('ion-modal').should('be.visible');

    // Selecionar Paciente
    cy.get('ion-modal ion-select[formControlName="idPaciente"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').should('have.length.at.least', 1).first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.get('ion-alert').should('not.exist');

    // Selecionar Médico
    cy.get('ion-modal ion-select[formControlName="idMedico"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').should('have.length.at.least', 1).first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.get('ion-alert').should('not.exist');

    // Preencher evolução médica
    cy.get('ion-modal ion-textarea[formControlName="observacao"] textarea')
      .clear({ force: true })
      .type(textoEvolucaoInicial, { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // Verificar card gerado
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).should('be.visible').within(() => {
      cy.get('.observacao-texto').should('contain.text', textoEvolucaoInicial);
      cy.get('ion-card-subtitle').should('contain.text', 'Médico:');
    });
  });

  it('Deve filtrar consultas por texto da evolução e exibir estado vazio na ausência de resultados', () => {
    // Garante consulta criada via API para teste focado em filtro
    cy.request('GET', '/api/paciente').then((resPac) => {
      const pacId = (resPac.body as any[])[0].id;
      cy.request('GET', '/api/medico').then((resMed) => {
        const medId = (resMed.body as any[])[0].id;
        cy.request('POST', '/api/consulta', {
          idPaciente: pacId,
          idMedico: medId,
          observacao: textoEvolucaoInicial
        });
      });
    });

    cy.visit('/consultas');
    cy.aguardarCarregamento();

    // Filtrar pelo texto da evolução
    cy.get('ion-searchbar input').clear({ force: true }).type('cefaleia tensional', { force: true });
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).should('be.visible');

    // Filtrar por termo inexistente
    cy.get('ion-searchbar input').clear({ force: true }).type('TermoInexistente999', { force: true });
    cy.contains('.empty-state', 'Nenhuma consulta registrada').should('be.visible');

    // Limpar filtro e recuperar lista
    cy.get('ion-searchbar input').clear({ force: true });
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).should('be.visible');
  });

  it('Deve editar a evolução clínica da consulta e atualizar os dados no card', () => {
    // Cria consulta via API
    cy.request('GET', '/api/paciente').then((resPac) => {
      const pacId = (resPac.body as any[])[0].id;
      cy.request('GET', '/api/medico').then((resMed) => {
        const medId = (resMed.body as any[])[0].id;
        cy.request('POST', '/api/consulta', {
          idPaciente: pacId,
          idMedico: medId,
          observacao: textoEvolucaoInicial
        });
      });
    });

    cy.visit('/consultas');
    cy.aguardarCarregamento();

    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).within(() => {
      cy.contains('ion-button', 'Editar Observação').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Observações da Consulta').should('be.visible');

    cy.get('ion-modal ion-textarea[formControlName="observacao"] textarea')
      .clear({ force: true })
      .type(textoEvolucaoEditada, { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    cy.contains('ion-card.consulta-card', textoEvolucaoEditada).should('be.visible');
  });

  it('Deve cancelar exclusão no diálogo de confirmação e depois excluir a consulta', () => {
    // Cria consulta via API
    cy.request('GET', '/api/paciente').then((resPac) => {
      const pacId = (resPac.body as any[])[0].id;
      cy.request('GET', '/api/medico').then((resMed) => {
        const medId = (resMed.body as any[])[0].id;
        cy.request('POST', '/api/consulta', {
          idPaciente: pacId,
          idMedico: medId,
          observacao: textoEvolucaoInicial
        });
      });
    });

    cy.visit('/consultas');
    cy.aguardarCarregamento();

    // 1. Tentar excluir e cancelar
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).first().within(() => {
      cy.contains('ion-button', 'Excluir').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-cancel').click();
    cy.get('ion-alert').should('not.exist');
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).should('be.visible');

    // 2. Confirmar exclusão definitiva
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).first().within(() => {
      cy.contains('ion-button', 'Excluir').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.get('ion-alert').should('not.exist');
    cy.aguardarCarregamento();

    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).should('not.exist');
  });
});
