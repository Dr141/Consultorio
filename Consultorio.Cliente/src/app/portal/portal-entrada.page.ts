import { Component, OnDestroy } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PortalService } from '../services/portal/portal.service';
import { cpfValidator, dataNaoFuturaValidator, mensagemErro } from '../shared/utils/validadores';

/** Entrada do portal: CPF + data de nascimento → código no e-mail → sessão. */
@Component({
  selector: 'app-portal-entrada',
  templateUrl: './portal-entrada.page.html',
  styleUrls: ['./portal.scss'],
  standalone: false
})
export class PortalEntradaPage implements OnDestroy {
  etapa: 'dados' | 'codigo' = 'dados';
  chave = '';
  mensagem = '';
  erro = '';
  enviando = false;
  segundosReenvio = 0;
  private temporizador?: ReturnType<typeof setInterval>;

  form = new FormGroup({
    cpf: new FormControl('', { nonNullable: true, validators: [Validators.required, cpfValidator()] }),
    dtNasc: new FormControl('', { nonNullable: true, validators: [Validators.required, dataNaoFuturaValidator()] })
  });
  codigo = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^\d{6}$/)] });

  constructor(private portal: PortalService, private router: Router, private rota: ActivatedRoute) {}

  ionViewWillEnter() {
    if (this.portal.autenticado) {
      this.router.navigate(['/portal/inicio'], { replaceUrl: true });
      return;
    }
    if (this.rota.snapshot.queryParamMap.get('expirou')) {
      this.erro = 'Sua sessão expirou. Entre novamente.';
    }
  }

  ngOnDestroy() {
    clearInterval(this.temporizador);
  }

  erroCampo(nome: 'cpf' | 'dtNasc'): string {
    return mensagemErro(this.form.get(nome));
  }

  solicitar() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.enviando = true;
    this.erro = '';
    const { cpf, dtNasc } = this.form.getRawValue();
    this.portal.solicitarCodigo(cpf, dtNasc).subscribe({
      next: (r) => {
        this.enviando = false;
        this.chave = r.chave;
        this.mensagem = r.mensagem;
        this.etapa = 'codigo';
        this.codigo.reset();
        this.iniciarContagem();
      },
      error: (e) => {
        this.enviando = false;
        this.erro = e.message;
      }
    });
  }

  private iniciarContagem() {
    this.segundosReenvio = 60;
    clearInterval(this.temporizador);
    this.temporizador = setInterval(() => {
      this.segundosReenvio = Math.max(0, this.segundosReenvio - 1);
      if (this.segundosReenvio === 0) clearInterval(this.temporizador);
    }, 1000);
  }

  entrar() {
    const codigo = this.codigo.value.replace(/\D/g, '');
    if (codigo.length !== 6) {
      this.codigo.markAsTouched();
      return;
    }
    this.enviando = true;
    this.erro = '';
    this.portal.validarCodigo(this.chave, codigo).subscribe({
      next: () => {
        this.enviando = false;
        this.router.navigate(['/portal/inicio'], { replaceUrl: true });
      },
      error: (e) => {
        this.enviando = false;
        this.erro = e.message;
      }
    });
  }

  voltar() {
    this.etapa = 'dados';
    this.erro = '';
  }
}
