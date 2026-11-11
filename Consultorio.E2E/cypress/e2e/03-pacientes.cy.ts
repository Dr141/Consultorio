describe('03 - Gestão de Pacientes (CRUD Completo)', () => {
  const cpfValido = '11144477735';
  const cpfFormatado = '111.444.777-35';
  const nomePaciente = 'Mariana Costa E2E';

  beforeEach(() => {
    cy.loginApi();
    // Limpa paciente de teste anterior caso exista para garantir idempotência
    cy.request('GET', '/api/paciente').then((res) => {
      const encontrado = (res.body as any[]).find(
        (p) => p.cpf === cpfFormatado || p.cpf === cpfValido || p.nome === nomePaciente
      );
      if (encontrado) {
        cy.request('DELETE', `/api/paciente/${encontrado.id}`);
      }
    });
  });

  it('Deve carregar a tela de pacientes sem travar no spinner e validar CPF inválido no modal', () => {
    cy.visit('/pacientes');
    cy.aguardarCarregamento();

    cy.get('ion-button[title="Cadastrar novo paciente"]').click();
    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Novo Paciente').should('be.visible');

    cy.get('ion-modal ion-input[formControlName="nome"] input').type('Paciente CPF Invalido', { force: true });
    cy.get('ion-modal ion-input[formControlName="cpf"] input').type('12345678900', { force: true });
    cy.get('ion-modal ion-input[formControlName="dtNasc"] input').type('1990-01-01', { force: true });
    cy.get('ion-modal ion-input[formControlName="telefone"] input').type('11999998888', { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    // CPF inválido é barrado no próprio formulário, com aviso e mensagem no campo
    cy.get('ion-toast', { timeout: 8000 }).should('exist');
    cy.contains('ion-modal', 'CPF inválido').should('exist');

    // Fechar com dados preenchidos pede confirmação para descartar
    cy.contains('ion-modal ion-button', 'Fechar').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.get('ion-modal').should('not.exist');
  });

  it('Deve cadastrar, buscar, editar e excluir um paciente pela interface', () => {
    cy.visit('/pacientes');
    cy.aguardarCarregamento();

    // 1. Cadastrar Novo Paciente
    cy.get('ion-button[title="Cadastrar novo paciente"]').click();
    cy.get('ion-modal').should('be.visible');

    cy.get('ion-modal ion-input[formControlName="nome"] input').clear({ force: true }).type(nomePaciente, { force: true });
    cy.get('ion-modal ion-input[formControlName="cpf"] input').clear({ force: true }).type(cpfValido, { force: true });
    cy.get('ion-modal ion-input[formControlName="dtNasc"] input').clear({ force: true }).type('1992-08-20', { force: true });
    cy.get('ion-modal ion-input[formControlName="telefone"] input').clear({ force: true }).type('11998877665', { force: true });
    cy.get('ion-modal ion-textarea[formControlName="observacao"] textarea')
      .clear({ force: true })
      .type('Alergia a dipirona', { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // 2. Verificar listagem e filtrar na barra de busca
    cy.get('ion-searchbar input').clear({ force: true }).type(nomePaciente, { force: true });
    cy.contains('ion-card-title', nomePaciente).should('be.visible');
    cy.contains('ion-card-subtitle', cpfFormatado).should('be.visible');
    cy.contains('.observacao-box', 'Alergia a dipirona').should('be.visible');

    // 3. Editar Paciente
    cy.contains('ion-card.registro-card', nomePaciente).within(() => {
      cy.contains('ion-button', 'Editar').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Editar Paciente').should('be.visible');
    cy.get('ion-modal ion-textarea[formControlName="observacao"] textarea')
      .clear({ force: true })
      .type('Observação atualizada pelo Cypress', { force: true });
    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    cy.contains('.observacao-box', 'Observação atualizada pelo Cypress').should('be.visible');

    // 4. Excluir Paciente
    cy.contains('ion-card.registro-card', nomePaciente).within(() => {
      cy.contains('ion-button', 'Excluir').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.aguardarCarregamento();

    cy.contains('ion-card-title', nomePaciente).should('not.exist');
  });
});
