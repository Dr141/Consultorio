import { sufixoExecucao } from '../support/dados-teste';

describe('06 - Prontuário e Gestão de Consultas Médicas', () => {
  // Consultas não são apagadas (apenas anuladas): textos únicos por execução.
  const sufixo = sufixoExecucao();
  const textoEvolucaoInicial = `Paciente relata cefaleia tensional e fadiga crônica E2E ${sufixo}`;
  const textoEvolucaoEditada = `Conduta terapêutica atualizada com receituário e repouso E2E ${sufixo}`;

  beforeEach(() => {
    cy.loginApi();
    cy.garantirPacienteTeste();
    cy.garantirMedicoTeste();

    // Anula consultas de execuções anteriores (o registro permanece no histórico).
    cy.request('GET', '/api/consulta').then((res) => {
      (res.body as any[]).forEach((c) => {
        if (c.observacao && c.observacao.includes('E2E') && !c.observacao.includes(sufixo) && c.status !== 'Anulada') {
          cy.request({
            method: 'PATCH',
            url: `/api/consulta/${c.id}/anular`,
            body: { motivo: 'Limpeza automática de teste E2E' },
            failOnStatusCode: false
          });
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

    // Selecionar Paciente (espera a lista de pacientes carregar: o ion-select monta as opções do alerta ao abrir)
    cy.get('ion-modal ion-select[formControlName="idPaciente"] ion-select-option').should('have.length.at.least', 1);
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
    cy.contains('.empty-state', 'Nenhuma consulta encontrada').should('be.visible');

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

  it('Deve desistir da anulação no diálogo e depois anular a consulta informando o motivo', () => {
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

    // 1. Abrir a anulação e desistir
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).first().within(() => {
      cy.contains('ion-button', 'Anular').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert button.alert-button-role-cancel').click();
    cy.get('ion-alert').should('not.exist');
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).should('be.visible');

    // 2. Anular informando o motivo
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).first().within(() => {
      cy.contains('ion-button', 'Anular').click();
    });

    cy.get('ion-alert').should('be.visible');
    cy.get('ion-alert textarea').type('Registrada no paciente errado', { force: true });
    cy.get('ion-alert button.alert-button-role-destructive').click();
    cy.get('ion-alert').should('not.exist');
    cy.aguardarCarregamento();

    // O registro continua no histórico, marcado como anulado e com o motivo
    cy.contains('ion-card.consulta-card', textoEvolucaoInicial).within(() => {
      cy.contains('ion-badge', 'Anulada').should('be.visible');
      cy.contains('.aviso-anulacao', 'Registrada no paciente errado').should('be.visible');
    });
  });
});
