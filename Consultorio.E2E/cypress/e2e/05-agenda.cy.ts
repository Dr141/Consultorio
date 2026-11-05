describe('05 - Gestão de Agenda de Consultas e Exames', () => {
  beforeEach(() => {
    cy.loginApi();
    cy.garantirMedicoTeste();
    cy.garantirPacienteTeste();

    // Limpa agendamentos da data de teste 2027-06-15 para idempotência
    cy.request('GET', '/api/agenda').then((res) => {
      (res.body as any[]).forEach((a) => {
        if (a.data && a.data.startsWith('2027-06-15')) {
          cy.request('DELETE', `/api/agenda/${a.id}`);
        }
      });
    });
  });

  it('Deve criar um horário disponível na agenda, agendar paciente, liberar horário e excluir', () => {
    cy.visit('/agenda');
    cy.aguardarCarregamento();

    // 1. Adicionar horário disponível
    cy.get('ion-button[title="Adicionar horário na agenda"]').click();
    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Novo Horário na Agenda').should('be.visible');

    cy.get('ion-modal ion-input[formControlName="data"] input').clear({ force: true }).type('2027-06-15', { force: true });
    cy.get('ion-modal ion-input[formControlName="hora"] input').clear({ force: true }).type('14:30', { force: true });

    // Selecionar Médico
    cy.get('ion-modal ion-select[formControlName="idMedico"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // 2. Verificar que o horário 15/06/2027 às 14:30 aparece como Disponível
    cy.contains('ion-card.agenda-card', '14:30').should('be.visible').within(() => {
      cy.contains('ion-badge', 'Disponível').should('be.visible');
      cy.contains('ion-button', 'Agendar').click();
    });

    // 3. Selecionar Paciente no alerta de Agendamento Rápido
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.aguardarCarregamento();

    // 4. Verificar mudança para Ocupado e liberar horário
    cy.contains('ion-card.agenda-card', '14:30').within(() => {
      cy.contains('ion-badge', 'Ocupado').should('be.visible');
      cy.contains('ion-button', 'Liberar').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.aguardarCarregamento();

    cy.contains('ion-card.agenda-card', '14:30').within(() => {
      cy.contains('ion-badge', 'Disponível').should('be.visible');
      cy.contains('ion-button', 'Excluir').click();
    });

    // 5. Confirmar exclusão do horário
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.aguardarCarregamento();
  });
});
