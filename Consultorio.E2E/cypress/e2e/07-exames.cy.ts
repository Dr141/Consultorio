import { sufixoExecucao } from '../support/dados-teste';

describe('07 - Gestão e Laudos de Exames Clínicos', () => {
  // Exames não são apagados (apenas cancelados): descrições únicas por execução.
  const sufixo = sufixoExecucao();
  const descricaoExame = `Eletrocardiograma de Repouso E2E ${sufixo}`;
  const resultadoInicial = 'Traçado eletrocardiográfico dentro dos limites fisiológicos da normalidade E2E';
  const resultadoEditado = 'Laudo revisado: ritmo sinusal preservado sem extrassístoles E2E';

  beforeEach(() => {
    cy.loginApi();
    cy.garantirPacienteTeste();

    // Cancela exames pendentes de execuções anteriores (o registro permanece no histórico).
    cy.request('GET', '/api/exame').then((res) => {
      (res.body as any[]).forEach((e) => {
        if (e.descricao && e.descricao.includes('E2E') && !e.descricao.includes(sufixo) && !e.resultado && !e.cancelado) {
          cy.request({
            method: 'PATCH',
            url: `/api/exame/${e.id}/cancelar`,
            body: { motivo: 'Limpeza automática de teste E2E' },
            failOnStatusCode: false
          });
        }
      });
    });
  });

  it('Deve validar obrigatoriedade dos campos no modal de solicitação e fechar sem salvar', () => {
    cy.visit('/exames');
    cy.aguardarCarregamento();

    cy.get('ion-button[title="Solicitar novo exame"]').click();
    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Solicitar Exame').should('be.visible');

    // Botão de salvar deve estar desabilitado enquanto os campos obrigatórios não forem preenchidos
    cy.get('ion-modal ion-button[type="submit"]').should('have.attr', 'disabled');

    // Fechar modal
    cy.contains('ion-modal ion-button', 'Fechar').click();
    cy.get('ion-modal').should('not.exist');
  });

  it('Deve solicitar um novo exame sem laudo com status inicial Aguardando', () => {
    cy.visit('/exames');
    cy.aguardarCarregamento();

    cy.get('ion-button[title="Solicitar novo exame"]').click();
    cy.get('ion-modal').should('be.visible');

    // Selecionar Paciente (espera a lista de pacientes carregar: o ion-select monta as opções do alerta ao abrir)
    cy.get('ion-modal ion-select[formControlName="idPaciente"] ion-select-option').should('have.length.at.least', 1);
    cy.get('ion-modal ion-select[formControlName="idPaciente"]').click();
    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert .alert-radio-button').should('have.length.at.least', 1).first().click();
    cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
    cy.get('ion-alert').should('not.exist');

    // Preencher Descrição do Exame
    cy.get('ion-modal ion-input[formControlName="descricao"] input')
      .clear({ force: true })
      .type(descricaoExame, { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // Verificar card gerado
    cy.contains('ion-card.exame-card', descricaoExame).should('be.visible').within(() => {
      cy.contains('ion-badge', 'Aguardando').should('be.visible');
      cy.contains('.sem-resultado-box', 'Aguardando emissão do laudo clínico').should('be.visible');
      cy.contains('ion-button', 'Lançar Laudo').should('be.visible');
    });
  });

  it('Deve lançar laudo médico, atualizar status para Laudo Pronto e alternar botão para Editar Laudo', () => {
    // Cria exame prévio via API
    cy.request('GET', '/api/paciente').then((resPac) => {
      const pacId = (resPac.body as any[])[0].id;
      cy.request('POST', '/api/exame', {
        idPaciente: pacId,
        descricao: descricaoExame
      });
    });

    cy.visit('/exames');
    cy.aguardarCarregamento();

    cy.contains('ion-card.exame-card', descricaoExame).within(() => {
      cy.contains('ion-button', 'Lançar Laudo').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-title', 'Resultado / Editar Exame').should('be.visible');

    // Preencher resultado/laudo
    cy.get('ion-modal ion-textarea[formControlName="resultado"] textarea')
      .clear({ force: true })
      .type(resultadoInicial, { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    // Verificar que o card mudou de status
    cy.contains('ion-card.exame-card', descricaoExame).within(() => {
      cy.contains('ion-badge', 'Laudo Pronto').should('be.visible');
      cy.contains('.resultado-texto', resultadoInicial).should('be.visible');
      cy.contains('ion-button', 'Editar Laudo').should('be.visible');
    });
  });

  it('Deve editar um laudo clínico já emitido', () => {
    // Cria exame com laudo via API
    cy.request('GET', '/api/paciente').then((resPac) => {
      const pacId = (resPac.body as any[])[0].id;
      cy.request('POST', '/api/exame', {
        idPaciente: pacId,
        descricao: descricaoExame,
        resultado: resultadoInicial
      });
    });

    cy.visit('/exames');
    cy.aguardarCarregamento();

    cy.contains('ion-card.exame-card', descricaoExame).within(() => {
      cy.contains('ion-button', 'Editar Laudo').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.get('ion-modal ion-textarea[formControlName="resultado"] textarea')
      .clear({ force: true })
      .type(resultadoEditado, { force: true });

    cy.get('ion-modal ion-button[type="submit"]').click();
    cy.get('ion-modal').should('not.exist');
    cy.aguardarCarregamento();

    cy.contains('ion-card.exame-card', descricaoExame).within(() => {
      cy.contains('.resultado-texto', resultadoEditado).should('be.visible');
    });
  });

  it('Deve filtrar exames por descrição e exibir estado vazio na ausência de correspondência', () => {
    cy.request('GET', '/api/paciente').then((resPac) => {
      const pacId = (resPac.body as any[])[0].id;
      cy.request('POST', '/api/exame', {
        idPaciente: pacId,
        descricao: descricaoExame
      });
    });

    cy.visit('/exames');
    cy.aguardarCarregamento();

    // Busca com correspondência
    cy.get('ion-searchbar input').clear({ force: true }).type('Eletrocardiograma', { force: true });
    cy.contains('ion-card.exame-card', descricaoExame).should('be.visible');

    // Busca sem correspondência
    cy.get('ion-searchbar input').clear({ force: true }).type('ExameInexistente999', { force: true });
    cy.contains('.empty-state', 'Nenhum exame encontrado').should('be.visible');

    // Limpa busca
    cy.get('ion-searchbar input').clear({ force: true });
    cy.contains('ion-card.exame-card', descricaoExame).should('be.visible');
  });

  it('Deve desistir do cancelamento no diálogo e depois cancelar o exame com motivo', () => {
    // Descrição própria: o teste de busca já criou um exame com a descrição padrão nesta execução.
    const descricaoCancelamento = `Ultrassonografia para Cancelamento E2E ${sufixo}`;

    cy.request('GET', '/api/paciente').then((resPac) => {
      const pacId = (resPac.body as any[])[0].id;
      cy.request('POST', '/api/exame', {
        idPaciente: pacId,
        descricao: descricaoCancelamento
      });
    });

    cy.visit('/exames');
    cy.aguardarCarregamento();

    // 1. Abrir o cancelamento e desistir
    cy.contains('ion-card.exame-card', descricaoCancelamento).first().within(() => {
      cy.contains('ion-button', 'Cancelar').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-cancel').click();
    cy.get('ion-alert').should('not.exist');
    cy.contains('ion-card.exame-card', descricaoCancelamento).should('be.visible');

    // 2. Cancelar informando o motivo
    cy.contains('ion-card.exame-card', descricaoCancelamento).first().within(() => {
      cy.contains('ion-button', 'Cancelar').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert textarea').type('Pedido duplicado', { force: true });
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.get('ion-alert').should('not.exist');
    cy.aguardarCarregamento();

    // Sai da lista principal e aparece na aba de cancelados, com o motivo
    cy.contains('ion-card.exame-card', descricaoCancelamento).should('not.exist');
    cy.contains('ion-segment-button', 'Cancelados').click();
    cy.contains('ion-card.exame-card', descricaoCancelamento).within(() => {
      cy.contains('ion-badge', 'Cancelado').should('be.visible');
      cy.contains('Pedido duplicado').should('be.visible');
    });
  });
});
