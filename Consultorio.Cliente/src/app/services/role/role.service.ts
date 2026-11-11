import { Injectable } from '@angular/core';
import { CacheService } from '../cache/cache.service';

export type Perfil = 'Administrador' | 'Administrativo' | 'Auxiliar' | 'Medico';

/**
 * Perfis do usuário logado (cookie "Roles", separados por "-").
 * Espelha as políticas do backend apenas para esconder o que o usuário não pode usar;
 * a proteção real continua sendo feita pela API.
 */
@Injectable({
  providedIn: 'root'
})
export class RoleService {

  constructor(private cache: CacheService) { }

  getRole() {
    return this.cache.recuperarCookie('Roles');
  }

  perfis(): Perfil[] {
    const bruto = this.getRole() ?? '';
    return bruto.split('-').map(p => p.trim()).filter(Boolean) as Perfil[];
  }

  possui(...perfis: Perfil[]): boolean {
    const meus = this.perfis();
    return perfis.some(p => meus.includes(p));
  }

  /** Lê e escreve prontuário (consultas, laudos, relatório). */
  ehClinico(): boolean {
    return this.possui('Administrador', 'Medico');
  }

  /** Cadastros administrativos e inativações. */
  ehGestao(): boolean {
    return this.possui('Administrador', 'Administrativo');
  }

  ehAdmin(): boolean {
    return this.possui('Administrador');
  }
}
