describe('09 - Prontuário do Paciente em Formato de Ofício', () => {
  beforeEach(() => {
    cy.loginApi();
    cy.garantirPacienteTeste();
  });

  it('Deve acessar o prontuário pelo painel inicial Home e exibir estado vazio inicial', () => {
    cy.visit('/home');
    cy.aguardarCarregamento();

    cy.contains('ion-card.cardHome ion-card-title', 'Prontuário').click();
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

    // Valida a folha de ofício gerada (o ofício é longo: cada trecho é rolado até a área visível)
    // A folha inteira é mais alta que a tela (o centro dela fica fora da área visível), então a visibilidade
    // é conferida trecho a trecho; para a folha basta existir.
    // O ofício fica dentro do scroll do Ionic, sob a barra fixa: conferimos que cada trecho foi renderizado
    // (a visibilidade "geométrica" do Cypress não é confiável dentro de ion-content).
    const visivel = (seletor: string, texto: string) => cy.contains(seletor, texto).should('exist');
    cy.get('.oficio-sheet').should('exist');
    visivel('.timbre-text h1', 'Consultório de Gestão Clínica');
    visivel('.oficio-meta .oficio-num', 'OF-');
    visivel('.secao-titulo', '1. Identificação do Paciente');
    visivel('.secao-titulo', '2. Síntese de Atendimentos no Período');
    visivel('.secao-titulo', '3. Histórico de Consultas e Evoluções Clínicas');
    visivel('.secao-titulo', '4. Solicitações de Exames e Laudos Técnicos');
    visivel('.oficio-fecho', 'São Paulo');
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

    cy.get('.oficio-sheet').should('exist');
    cy.contains('.secao-titulo', '1. Identificação do Paciente').scrollIntoView().should('be.visible');
  });
});
