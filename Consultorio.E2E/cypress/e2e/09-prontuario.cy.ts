describe('09 - Prontuário do Paciente em Formato de Ofício', () => {
  beforeEach(() => {
    cy.loginApi();
    cy.garantirPacienteTeste();
  });

  it('Deve acessar o prontuário pelo painel inicial Home e exibir estado vazio inicial', () => {
    cy.visit('/home');
    cy.aguardarCarregamento();

    cy.contains('ion-card.cardHome', 'Prontuário').click();
    cy.url().should('include', '/prontuario');
    cy.contains('ion-title', 'Prontuário do Paciente').should('be.visible');
    cy.contains('Nenhum prontuário carregado').should('be.visible');
  });

  it('Deve abrir o prontuário diretamente a partir do card do paciente com id na URL e emitir ofício', () => {
    cy.visit('/pacientes');
    cy.aguardarCarregamento();

    // Clica no botão Prontuário do primeiro paciente
    cy.get('ion-card.registro-card').first().within(() => {
      cy.contains('ion-button', 'Prontuário').click();
    });

    cy.url().should('include', '/prontuario?idPaciente=');
    cy.aguardarCarregamento();

    // Valida a folha de ofício gerada
    cy.get('.oficio-sheet').should('be.visible');
    cy.contains('.timbre-text h1', 'Consultório de Gestão Clínica').should('be.visible');
    cy.contains('.oficio-meta .oficio-num', 'OF-').should('be.visible');
    cy.contains('.secao-titulo', '1. Identificação do Paciente').should('be.visible');
    cy.contains('.secao-titulo', '2. Síntese de Atendimentos no Período').should('be.visible');
    cy.contains('.secao-titulo', '3. Histórico de Consultas e Evoluções Clínicas').should('be.visible');
    cy.contains('.secao-titulo', '4. Solicitações de Exames e Laudos Técnicos').should('be.visible');
    cy.contains('.oficio-fecho', 'São Paulo').should('be.visible');
  });

  it('Deve aplicar chips de período rápido e filtrar histórico', () => {
    cy.visit('/prontuario');
    cy.aguardarCarregamento();

    // Seleciona paciente
    cy.get('ion-select[label="Paciente *"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();

    // Clica no chip Últimos 30 dias
    cy.contains('ion-chip', 'Últimos 30 dias').click();
    cy.get('ion-input[label="Data Inicial"] input').should('not.have.value', '');
    cy.get('ion-input[label="Data Final"] input').should('not.have.value', '');

    // Clica em Gerar Prontuário
    cy.contains('ion-button', 'Gerar Prontuário').click();
    cy.aguardarCarregamento();

    cy.get('.oficio-sheet').should('be.visible');
  });
});
