using Consultorio.Aplicacao.Interfaces;

namespace Consultorio.Server.Rotinas;

/// <summary>
/// Executa periodicamente as rotinas da agenda: envio de lembretes e marcação de faltas.
/// Configuração (seção "Rotinas"): Habilitado (padrão true) e IntervaloMinutos (padrão 15).
/// Cada execução usa um escopo novo de DI, como uma requisição HTTP.
/// </summary>
public class RotinasAgendaHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<RotinasAgendaHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Rotinas:Habilitado", true))
        {
            logger.LogInformation("Rotinas automáticas da agenda desabilitadas (Rotinas:Habilitado = false).");
            return;
        }

        var intervalo = TimeSpan.FromMinutes(Math.Clamp(configuration.GetValue("Rotinas:IntervaloMinutos", 15), 1, 24 * 60));

        // Aguarda a aplicação terminar de subir (migrations, cache) antes da primeira execução.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(intervalo);
        do
        {
            await ExecutarAsync(stoppingToken);
        }
        while (await AguardarProximoCicloAsync(timer, stoppingToken));
    }

    private static async Task<bool> AguardarProximoCicloAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task ExecutarAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var rotinas = scope.ServiceProvider.GetRequiredService<IRotinasAgendaAppService>();

            var faltas = await rotinas.MarcarFaltasAsync(stoppingToken);
            var lembretes = await rotinas.EnviarLembretesAsync(stoppingToken);

            if (faltas > 0 || lembretes.Processados > 0)
            {
                logger.LogInformation(
                    "Rotinas da agenda: {Faltas} falta(s) registrada(s); lembretes: {Enviados} enviado(s), {SemContato} sem contato, {Falhas} falha(s).",
                    faltas, lembretes.Enviados, lembretes.SemContato, lembretes.Falhas);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento da aplicação.
        }
        catch (Exception ex)
        {
            // Uma falha (ex.: banco indisponível) não pode derrubar o servidor; tenta de novo no próximo ciclo.
            logger.LogError(ex, "Falha ao executar as rotinas da agenda.");
        }
    }
}
