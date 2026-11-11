import { gerarCpfValido, sufixoExecucao } from '../support/dados-teste';

describe('04 - Corpo Clínico / Médicos (cadastro, edição e inativação)', () => {
  // Médicos não são apagados (apenas inativados): dados próprios por execução.
  const sufixo = sufixoExecucao();
  const cpfMedico = gerarCpfValido();
  const crmMedico = `${sufixo}-SP`;
  const nomeMedico = `Dra. Fernanda Lima E2E ${sufixo}`;

  beforeEach(() => {
    cy.loginApi();
  });

  it('Deve cadastrar, filtrar, editar e excluir um médico pela interface', () => {
    cy.visit('/medicos');
    cy.aguardarCarregamento();

    // 1. Abrir modal de Novo Médico
    cy.get('ion-button[title="Cadastrar novo médico"]').click();
    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Novo Médico').should('be.visible');

    cy.get('ion-modal ion-input[formControlName="nome"] input').clear({ force: true }).type(nomeMedico, { force: true });
    cy.get('ion-modal ion-input[formControlName="cpf"] input').clear({ force: true }).type(cpfMedico, { force: true });
    cy.get('ion-modal ion-input[formControlName="crm"] input').clear({ force: true }).type(crmMedico, { force: true });
    cy.get('ion-modal ion-input[formControlName="dtNasc"] input').clear({ force: true }).type('1984-04-15', { force: true });
    cy.get('ion-modal ion-input[formControlName="telefone"] input').clear({ force: true }).type('11955443322', { force: true });

    // Selecionar especialidades no ion-select
    cy.get('ion-modal ion-select[formControlName="especialidades"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-checkbox-button').first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();

    cy.get('ion-modal ion-textarea[formControlName="observacao"] textarea')
      .clear({ force: true })
      .type('Atendimento terça e quinta', { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // 2. Filtrar pelo CRM
    cy.get('ion-searchbar input').clear({ force: true }).type(crmMedico, { force: true });
    cy.contains('ion-card-title', nomeMedico).should('be.visible');
    cy.contains('ion-card-subtitle', `CRM: ${crmMedico}`).should('be.visible');

    // 3. Editar Médico
    cy.contains('ion-card.registro-card', nomeMedico).within(() => {
      cy.contains('ion-button', 'Editar').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Editar Médico').should('be.visible');
    cy.get('ion-modal ion-textarea[formControlName="observacao"] textarea')
      .clear({ force: true })
      .type('Horário ampliado E2E', { force: true });
    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    cy.contains('.observacao-box', 'Horário ampliado E2E').should('be.visible');

    // 4. Inativar Médico (histórico preservado)
    cy.contains('ion-card.registro-card', nomeMedico).within(() => {
      cy.contains('ion-button', 'Inativar').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.aguardarCarregamento();

    cy.contains('ion-card-title', nomeMedico).should('not.exist');
  });
});
