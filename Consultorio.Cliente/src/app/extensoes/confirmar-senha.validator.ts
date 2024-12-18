import { AbstractControl, FormGroup, ValidatorFn } from '@angular/forms';

// Validador personalizado para confirmar senha
export function ConfirmarSenha(controlName: string, matchingControlName: string): ValidatorFn {
  return (control: AbstractControl): { [key: string]: any } | null => {
    const formGroup = control as FormGroup;
    const controlToMatch = formGroup.controls[controlName];
    const matchingControl = formGroup.controls[matchingControlName];

    if (!controlToMatch || !matchingControl) {
      return null;
    }

    if (matchingControl.errors && !matchingControl.errors['confirmarSenha']) {
      // Retorna se outro validador já encontrou um erro no campo matchingControl
      return null;
    }

    // Define um erro no campo matchingControl se a validação falhar
    if (controlToMatch.value !== matchingControl.value) {
      matchingControl.setErrors({ confirmarSenha: true });
      return { confirmarSenha: true };
    } else {
      matchingControl.setErrors(null);
      return null;
    }
  };
}
