using Consultorio.Aplicacao.Interfaces;

namespace Consultorio.Server.Rotinas;

/// <summary>
/// Executa periodicamente as rotinas da agenda (lembretes, faltas) e da clínica
/// (cobranças, aviso de laudo, retorno, resumo do dia, indicadores do mês e revisão de acessos).
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

        await ExecutarRotinasClinicaAsync(stoppingToken);
    }

    /// <summary>
    /// Rotinas da Fase 4. Cada uma roda isolada: a falha de uma (ex.: e-mail fora do ar)
    /// não impede as demais nem derruba o servidor.
    /// </summary>
    private async Task ExecutarRotinasClinicaAsync(CancellationToken stoppingToken)
    {
        IServiceScope scope;
        IRotinasClinicaAppService rotinas;
        Consultorio.Server.Servicos.RevisaoAcessosService revisao;
        try
        {
            scope = scopeFactory.CreateScope();
            rotinas = scope.ServiceProvider.GetRequiredService<IRotinasClinicaAppService>();
            revisao = scope.ServiceProvider.GetRequiredService<Consultorio.Server.Servicos.RevisaoAcessosService>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao preparar as rotinas da clínica.");
            return;
        }

        using var _ = scope;

        await Isolar("cobranças dos atendimentos", async () =>
        {
            var criadas = await rotinas.GerarCobrancasAsync(stoppingToken);
            if (criadas > 0) logger.LogInformation("Rotinas: {Quantidade} cobrança(s) gerada(s) a partir de atendimentos concluídos.", criadas);
        });
        await Isolar("aviso de laudo pronto", async () =>
        {
            var r = await rotinas.AvisarLaudosProntosAsync(stoppingToken);
            if (r.Processados > 0) logger.LogInformation("Rotinas: avisos de laudo — {Enviados} enviado(s), {SemContato} sem contato, {Falhas} falha(s).", r.Enviados, r.SemContato, r.Falhas);
        });
        await Isolar("lembrete de retorno", async () =>
        {
            var r = await rotinas.LembrarRetornosAsync(stoppingToken);
            if (r.Processados > 0) logger.LogInformation("Rotinas: lembretes de retorno — {Enviados} enviado(s), {SemContato} sem contato, {Falhas} falha(s).", r.Enviados, r.SemContato, r.Falhas);
        });
        await Isolar("resumo do dia", async () =>
        {
            if (await rotinas.EnviarResumoDiarioAsync(stoppingToken)) logger.LogInformation("Rotinas: resumo do dia enviado.");
        });
        await Isolar("indicadores mensais", async () =>
        {
            if (await rotinas.EnviarIndicadoresMensaisAsync(stoppingToken)) logger.LogInformation("Rotinas: indicadores do mês enviados à gestão.");
        });
        await Isolar("revisão de acessos", async () =>
        {
            if (await revisao.EnviarLembreteMensalAsync(stoppingToken)) logger.LogInformation("Rotinas: lembrete de revisão de acessos enviado aos administradores.");
        });

        async Task Isolar(string nome, Func<Task> acao)
        {
            if (stoppingToken.IsCancellationRequested) return;
            try
            {
                await acao();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha na rotina: {Rotina}.", nome);
            }
        }
    }
}
