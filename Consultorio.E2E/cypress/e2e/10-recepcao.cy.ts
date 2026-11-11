describe('10 - Recepção · Fila do dia', () => {
  const hoje = (() => {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  })();
  // Horário de madrugada para não colidir com a agenda real do dia.
  const hora = `05:${String(10 + Math.floor(Math.random() * 45)).padStart(2, '0')}`;

  beforeEach(() => {
    cy.loginApi();
    cy.garantirMedicoTeste().then((medico) => {
      // Limpa horários de madrugada de execuções anteriores deste médico
      cy.request('GET', '/api/agenda').then((res) => {
        (res.body as any[])
          .filter((a) => a.idMedico === medico.id && a.data?.startsWith(hoje) && a.hora < '07:00')
          .forEach((a) => cy.request({ method: 'DELETE', url: `/api/agenda/${a.id}`, failOnStatusCode: false }));
      });
    });
  });

  it('Deve levar o paciente de "A chegar" até "Encerrados" pela fila do dia', () => {
    cy.garantirMedicoTeste().then((medico) => {
      cy.garantirPacienteTeste().then((paciente) => {
        cy.request('POST', '/api/agenda', {
          data: hoje,
          hora: `${hora}:00`,
          idMedico: medico.id,
          idPaciente: paciente.id
        });

        cy.visit('/recepcao');
        cy.aguardarCarregamento();

        // A chegar → Chegou
        cy.contains('ion-card.cartao-fila', hora).within(() => {
          cy.contains('ion-badge', 'Agendado').should('be.visible');
          cy.contains('ion-button', 'Chegou').click();
        });
        cy.contains('section.coluna', 'Aguardando').within(() => {
          cy.contains('ion-card.cartao-fila', hora).should('be.visible');
          cy.contains('Esperando há').should('be.visible');
        });

        // Aguardando → Iniciar atendimento (administrador é perfil clínico: abre o registro da consulta)
        cy.contains('ion-card.cartao-fila', hora).within(() => {
          cy.contains('ion-button', 'Iniciar atendimento').click();
        });
        cy.get('ion-modal').should('be.visible');
        cy.contains('ion-modal', 'Atendimento da agenda').should('be.visible');
        cy.contains('ion-modal ion-button', 'Fechar').click();
        cy.get('ion-modal').should('not.exist');

        // Em atendimento → Concluir
        cy.contains('section.coluna', 'Em atendimento').within(() => {
          cy.contains('ion-card.cartao-fila', hora).within(() => {
            cy.contains('ion-button', 'Concluir').click();
          });
        });
        cy.contains('section.coluna', 'Encerrados').within(() => {
          cy.contains('ion-card.cartao-fila', hora).within(() => {
            cy.contains('ion-badge', 'Atendido').should('be.visible');
          });
        });
      });
    });
  });
});
