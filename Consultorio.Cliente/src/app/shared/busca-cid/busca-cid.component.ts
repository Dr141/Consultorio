import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { Subject, Subscription, catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { Cid } from '../../types/types';

/**
 * Campo de diagnóstico CID-10 com busca por código ("J06") ou descrição ("faringite").
 * Emite o CID escolhido ou null quando o diagnóstico é removido.
 */
@Component({
  selector: 'app-busca-cid',
  template: `
    @if (selecionado) {
      <div class="cid-selecionado">
        <ion-chip color="primary" class="cid-chip">
          <ion-icon name="pricetag-outline"></ion-icon>
          <ion-label><strong>{{ selecionado.codigo }}</strong> · {{ selecionado.descricao }}</ion-label>
        </ion-chip>
        @if (!desabilitado) {
          <ion-button fill="clear" size="small" color="medium" (click)="limpar()" title="Remover diagnóstico">
            <ion-icon slot="icon-only" name="close-circle-outline"></ion-icon>
          </ion-button>
        }
      </div>
    } @else {
      <ion-searchbar
        class="cid-busca"
        placeholder="Buscar CID-10 por código ou descrição (ex.: J06, hipertensão)"
        [debounce]="0"
        [disabled]="desabilitado"
        (ionInput)="buscar($any($event).detail.value)"
        (ionClear)="resultados = []">
      </ion-searchbar>
      @if (carregando) {
        <ion-progress-bar type="indeterminate"></ion-progress-bar>
      }
      @if (resultados.length) {
        <ion-list class="cid-resultados" lines="full">
          @for (c of resultados; track c.codigo) {
            <ion-item button detail="false" (click)="escolher(c)">
              <ion-label>
                <strong>{{ c.codigo }}</strong> · {{ c.descricao }}
              </ion-label>
            </ion-item>
          }
        </ion-list>
      } @else if (termo.length >= 2 && !carregando) {
        <p class="cid-dica">Nenhum código encontrado. A lista cobre os CIDs mais usados na atenção primária.</p>
      }
    }
  `,
  standalone: false
})
export class BuscaCidComponent implements OnInit, OnDestroy {
  @Input() set valor(cid: Cid | null | undefined) {
    this.selecionado = cid && cid.codigo ? { codigo: cid.codigo, descricao: cid.descricao ?? '' } : null;
  }
  @Input() desabilitado = false;
  @Output() valorChange = new EventEmitter<Cid | null>();

  selecionado: Cid | null = null;
  resultados: Cid[] = [];
  carregando = false;
  termo = '';

  private termos = new Subject<string>();
  private inscricao?: Subscription;

  constructor(private api: ConsultorioApiService) {}

  ngOnInit() {
    this.inscricao = this.termos.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(t => {
        if (t.length < 2) {
          this.carregando = false;
          return of([] as Cid[]);
        }
        this.carregando = true;
        return this.api.buscarCid(t).pipe(catchError(() => of([] as Cid[])));
      })
    ).subscribe(lista => {
      this.carregando = false;
      this.resultados = lista;
    });
  }

  ngOnDestroy() {
    this.inscricao?.unsubscribe();
  }

  buscar(valor: string | null | undefined) {
    this.termo = (valor ?? '').trim();
    this.termos.next(this.termo);
  }

  escolher(cid: Cid) {
    this.selecionado = cid;
    this.resultados = [];
    this.termo = '';
    this.valorChange.emit(cid);
  }

  limpar() {
    this.selecionado = null;
    this.valorChange.emit(null);
  }
}
