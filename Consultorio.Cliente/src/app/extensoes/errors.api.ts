export function obterMensagemErro(errors: { [key: string]: string[] } | string[]) : string {
  let errorString = '';

  // Verifica se errors é um array
  if (Array.isArray(errors)) {
    errors.forEach((message: string) => {
      errorString += `${message}\n`;
    });
  } else {
    // Itera sobre o objeto de erros
    for (const field in errors) {
      if (errors.hasOwnProperty(field)) {
        errors[field].forEach((message: string) => {
          errorString += `${message}\n`;
        });
      }
    }
  }

  return errorString;
}
