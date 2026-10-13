namespace Kaizen.Api.Services.Diagnostico;

public readonly record struct ClassificacaoPilar(string NivelMaturidade, string Prioridade);

/// <summary>
/// Regras de classificação de maturidade de um pilar — espelham a trigger
/// TRG_CLASSIFICA_MATURIDADE do banco Oracle original. Função pura (sem dependências),
/// portanto trivial de cobrir com testes unitários.
/// </summary>
public static class ClassificadorMaturidade
{
    public static ClassificacaoPilar Classificar(int pontuacao, int pontuacaoMaxima)
    {
        double percentual = pontuacaoMaxima > 0 ? (double)pontuacao / pontuacaoMaxima * 100 : 0;

        return percentual switch
        {
            <= 25 => new ClassificacaoPilar("Inicial", "Alta"),
            <= 50 => new ClassificacaoPilar("Em desenvolvimento", "Alta"),
            <= 75 => new ClassificacaoPilar("Estruturado", "Media"),
            _     => new ClassificacaoPilar("Avancado", "Manter")
        };
    }

    /// <summary>Tipo + descrição da recomendação por nível (espelha TRG_GERA_RECOMENDACAO).</summary>
    public static (string tipo, string descricao) GerarRecomendacao(string? nivelMaturidade) => nivelMaturidade switch
    {
        null => ("Indefinido",
            "Nível de maturidade não calculado. Verifique se todas as respostas do pilar foram registradas corretamente."),
        "Inicial" => ("Urgente",
            "Nível inicial: priorize a estruturação imediata deste pilar. Defina responsáveis, documente processos básicos e estabeleça controles mínimos para reduzir riscos de conformidade e governança."),
        "Em desenvolvimento" => ("Urgente",
            "Pilar em desenvolvimento: existem práticas iniciadas mas incompletas. Formalize os processos existentes, amplie o treinamento das equipes e estabeleça indicadores para evoluir ao nível Estruturado."),
        "Estruturado" => ("Melhoria",
            "Pilar estruturado: processos formalizados e funcionando. Avance para controles proativos, revise políticas periodicamente e implante métricas de monitoramento contínuo para atingir o nível Avançado."),
        _ => ("Manter",
            "Pilar em nível avançado: boas práticas consolidadas. Mantenha cadência de revisão, invista em inovação e use este pilar como referência de maturidade ESG para os demais.")
    };
}
