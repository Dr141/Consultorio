import { sufixoExecucao } from '../support/dados-teste';

/**
 * Documentos em PDF e assinatura digital.
 * O certificado em fixtures/certificado-teste.pfx é autoassinado e serve só para testes
 * (fora da ICP-Brasil, senha "e2e-teste", válido até 2036).
 */
describe('13 - Documentos em PDF e assinatura digital', () => {
  const sufixo = sufixoExecucao();

  beforeEach(() => {
    cy.loginApi();
    cy.garantirPacienteTeste();
    cy.garantirMedicoTeste();
  });

  const emitirReceita = (texto: string) =>
    cy.garantirPacienteTeste().then((paciente) =>
      cy.garantirMedicoTeste().then((medico) =>
        cy.request({
          method: 'POST',
          url: '/api/documento',
          body: { tipo: 1, idPaciente: paciente.id, idMedico: medico.id, conteudo: texto },
          failOnStatusCode: false
        }).then((res) => {
          expect(res.status, JSON.stringify(res.body)).to.eq(201);
          return res.body as { id: number; codigoVerificacao: string };
        })
      )
    );

  it('Deve gerar o PDF de um documento emitido', () => {
    emitirReceita(`Dipirona 500 mg — 1 comprimido de 6/6 h se dor (${sufixo})`).then((doc) => {
      cy.request({ url: `/api/documento/${doc.id}/pdf`, encoding: 'binary', failOnStatusCode: false }).then((res) => {
        expect(res.status, String(res.body).slice(0, 300)).to.eq(200);
        expect(res.headers['content-type']).to.include('application/pdf');
        expect(String(res.body).slice(0, 5)).to.eq('%PDF-');
        expect(String(res.body)).not.to.include('/ByteRange');
      });
    });
  });

  it('Deve cadastrar o certificado do médico, assinar o documento e exibir a assinatura na verificação', () => {
    cy.garantirMedicoTeste().then((medico) => {
      // Começa sem certificado (o médico de teste não tem login vinculado: o administrador gerencia e assina).
      cy.request({ method: 'DELETE', url: `/api/medico/${medico.id}/certificado`, failOnStatusCode: false });
    });

    cy.intercept('POST', '/api/medico/*/certificado').as('enviarCertificado');
    cy.visit('/medicos');
    cy.aguardarCarregamento();
    cy.contains('ion-card', '123456-SP').within(() => {
      cy.contains('ion-button', 'Certificado digital').click();
    });

    cy.get('ion-modal').should('be.visible');
    cy.contains('ion-modal', 'Nenhum certificado cadastrado').should('exist');
    cy.get('ion-modal input[type=file]').selectFile('cypress/fixtures/certificado-teste.pfx', { force: true });
    cy.get('ion-modal ion-input input').type('e2e-teste', { force: true });
    cy.contains('ion-modal ion-button', 'Cadastrar certificado').click();
    cy.wait('@enviarCertificado').then(({ response }) => {
      expect(response?.statusCode, JSON.stringify(response?.body)).to.eq(200);
    });
    cy.contains('ion-modal .cert-atual', 'DR HENRIQUE SOUZA E2E').should('exist');
    cy.contains('ion-modal .cert-atual', 'Fora da ICP-Brasil').should('exist');
    cy.contains('ion-modal ion-button', 'Fechar').click();

    emitirReceita(`Amoxicilina 500 mg — 1 cápsula de 8/8 h por 7 dias (${sufixo})`).then((doc) => {
      cy.intercept('POST', `/api/documento/${doc.id}/assinar`).as('assinar');
      cy.visit(`/documento/${doc.id}`);
      cy.aguardarCarregamento();
      cy.contains('ion-button', 'Assinar digitalmente').click();
      cy.get('ion-alert').should('be.visible');
      cy.get('ion-alert button.alert-button:not(.alert-button-role-cancel)').click();
      cy.wait('@assinar').then(({ response }) => {
        expect(response?.statusCode, JSON.stringify(response?.body)).to.eq(200);
      });
      cy.contains('.aviso-assinado', 'DR HENRIQUE SOUZA E2E').should('exist');
      cy.contains('ion-button', 'Assinar digitalmente').should('not.exist');

      cy.request({ url: `/api/documento/${doc.id}/pdf`, encoding: 'binary' }).then((res) => {
        expect(res.headers['content-type']).to.include('application/pdf');
        expect(String(res.body)).to.include('/SubFilter /ETSI.CAdES.detached');
      });

      cy.request(`/api/publico/documento/${doc.codigoVerificacao}`).its('body').should('include', 'Assinatura digital');

      // Uma segunda assinatura é recusada.
      cy.request({ method: 'POST', url: `/api/documento/${doc.id}/assinar`, failOnStatusCode: false })
        .its('status').should('eq', 400);
    });
  });
});
