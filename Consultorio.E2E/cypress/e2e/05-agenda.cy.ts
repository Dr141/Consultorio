describe('05 - Gestão de Agenda de Consultas e Exames', () => {
  const dataTeste1 = '2027-08-10';
  const dataTeste2 = '2027-08-11';
  const horaOriginal = '09:00';
  const horaEditada = '10:30';
  const horaComPaciente = '15:00';

  beforeEach(() => {
    cy.loginApi();
    cy.garantirMedicoTeste();
    cy.garantirPacienteTeste();

    // Limpa agendamentos das datas de teste para garantir idempotência
    cy.request('GET', '/api/agenda').then((res) => {
      (res.body as any[]).forEach((a) => {
        if (a.data && (a.data.startsWith(dataTeste1) || a.data.startsWith(dataTeste2))) {
          cy.request('DELETE', `/api/agenda/${a.id}`);
        }
      });
    });
  });

  it('Deve validar campos obrigatórios e permitir fechar o modal de agendamento sem salvar', () => {
    cy.visit('/agenda');
    cy.aguardarCarregamento();

    cy.get('ion-button[title="Adicionar horário na agenda"]').click();
    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Novo Horário na Agenda').should('be.visible');

    // Botão salvar deve estar desabilitado se o médico não for selecionado
    cy.get('ion-modal ion-button[type="submit"]').should('have.attr', 'disabled');

    // Fechar o modal clicando em Fechar
    cy.contains('ion-modal ion-button', 'Fechar').click();
    cy.get('ion-modal').should('not.exist');
  });

  it('Deve criar um horário disponível, filtrar por médico e editar horário existente', () => {
    cy.visit('/agenda');
    cy.aguardarCarregamento();

    // 1. Criar novo horário disponível
    cy.get('ion-button[title="Adicionar horário na agenda"]').click();
    cy.get('ion-modal').should('be.visible');

    cy.get('ion-modal ion-input[formControlName="data"] input').clear({ force: true }).type(dataTeste1, { force: true });
    cy.get('ion-modal ion-input[formControlName="hora"] input').clear({ force: true }).type(horaOriginal, { force: true });

    // Selecionar Médico
    cy.get('ion-modal ion-select[formControlName="idMedico"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').should('have.length.at.least', 1).first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.get('ion-alert').should('not.exist');

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // 2. Verificar exibição do card com horário e badge Disponível
    cy.contains('ion-card.agenda-card', horaOriginal).should('be.visible').within(() => {
      cy.contains('ion-badge', 'Disponível').should('be.visible');
      cy.contains('Horário vago para agendamento').should('be.visible');
    });

    // 3. Filtrar pelo termo de busca
    cy.get('ion-searchbar input').clear({ force: true }).type('InexistenteXYZ', { force: true });
    cy.contains('.empty-state', 'Nenhum horário na agenda').should('be.visible');

    cy.get('ion-searchbar input').clear({ force: true });
    cy.contains('ion-card.agenda-card', horaOriginal).should('be.visible');

    // 4. Editar horário para 10:30
    cy.contains('ion-card.agenda-card', horaOriginal).within(() => {
      cy.contains('ion-button', 'Editar').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Editar Horário').should('be.visible');
    cy.get('ion-modal ion-input[formControlName="hora"] input').clear({ force: true }).type(horaEditada, { force: true });
    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    cy.contains('ion-card.agenda-card', horaEditada).should('be.visible');
  });

  it('Deve agendar paciente em horário vago pelo alerta rápido e depois liberar o horário', () => {
    cy.visit('/agenda');
    cy.aguardarCarregamento();

    // Cria horário prévio
    cy.get('ion-button[title="Adicionar horário na agenda"]').click();
    cy.get('ion-modal ion-input[formControlName="data"] input').clear({ force: true }).type(dataTeste1, { force: true });
    cy.get('ion-modal ion-input[formControlName="hora"] input').clear({ force: true }).type('14:00', { force: true });

    cy.get('ion-modal ion-select[formControlName="idMedico"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').should('have.length.at.least', 1).first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.get('ion-alert').should('not.exist');

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // Agendar paciente
    cy.contains('ion-card.agenda-card', '14:00').within(() => {
      cy.contains('ion-badge', 'Disponível').should('be.visible');
      cy.contains('ion-button', 'Agendar').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').should('have.length.at.least', 1).first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.get('ion-alert').should('not.exist');
    cy.aguardarCarregamento();

    // Verificar alteração de status para Ocupado
    cy.contains('ion-card.agenda-card', '14:00').within(() => {
      cy.contains('ion-badge', 'Ocupado').should('be.visible');
      cy.contains('strong', 'Paciente:').should('be.visible');
      cy.contains('ion-button', 'Liberar').click();
    });

    // Liberar horário
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.get('ion-alert').should('not.exist');
    cy.aguardarCarregamento();

    cy.contains('ion-card.agenda-card', '14:00').within(() => {
      cy.contains('ion-badge', 'Disponível').should('be.visible');
    });
  });

  it('Deve criar horário com paciente já vinculado pelo modal e validar cancelamento/confirmação de exclusão', () => {
    cy.visit('/agenda');
    cy.aguardarCarregamento();

    // 1. Criar já com paciente selecionado no modal
    cy.get('ion-button[title="Adicionar horário na agenda"]').click();
    cy.get('ion-modal ion-input[formControlName="data"] input').clear({ force: true }).type(dataTeste2, { force: true });
    cy.get('ion-modal ion-input[formControlName="hora"] input').clear({ force: true }).type(horaComPaciente, { force: true });

    // Selecionar Médico
    cy.get('ion-modal ion-select[formControlName="idMedico"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').should('have.length.at.least', 1).first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.get('ion-alert').should('not.exist');

    // Selecionar Paciente
    cy.get('ion-modal ion-select[formControlName="idPaciente"]').click();
    cy.get('ion-alert').should('be.visible');
    // Aguarda opções carregarem: opção 0 é "Nenhum", opção 1 é o primeiro paciente
    cy.get('ion-alert .alert-radio-button').should('have.length.at.least', 2).eq(1).click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.get('ion-alert').should('not.exist');

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // 2. Verificar que já surge Ocupado
    cy.contains('ion-card.agenda-card', horaComPaciente).should('be.visible').within(() => {
      cy.contains('ion-badge', 'Ocupado').should('be.visible');
      cy.contains('ion-button', 'Excluir').click();
    });

    // 3. Cancelar exclusão primeiro
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-cancel').click();
    cy.get('ion-alert').should('not.exist');
    cy.contains('ion-card.agenda-card', horaComPaciente).should('be.visible');

    // 4. Confirmar exclusão
    cy.contains('ion-card.agenda-card', horaComPaciente).within(() => {
      cy.contains('ion-button', 'Excluir').click();
    });
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.get('ion-alert').should('not.exist');
    cy.aguardarCarregamento();

    cy.contains('ion-card.agenda-card', horaComPaciente).should('not.exist');
  });
});
