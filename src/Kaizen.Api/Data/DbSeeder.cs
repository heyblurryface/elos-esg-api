using Kaizen.Api.Models;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.Services.Diagnostico;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Data;

/// <summary>
/// Popula o banco com os dados de referência (pilares + 32 perguntas) e um
/// conjunto de demonstração (consultores, empresas, avaliações e respostas).
/// Os resultados e recomendações NÃO são inseridos manualmente: são calculados
/// pela <see cref="DiagnosticoEngine"/>, garantindo que o seed reflita exatamente
/// a mesma lógica usada em runtime (antiga responsabilidade das triggers Oracle).
///
/// Senha padrão de todos os consultores de demonstração: <c>kaizen123</c>.
/// Login sugerido para testes: <c>admin@kaizen.com.br</c> / <c>kaizen123</c>.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(KaizenDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        // Idempotente: se já há pilares, assume que o banco está populado.
        if (await db.Pilares.AnyAsync(ct))
            return;

        await SeedPilaresAsync(db, ct);
        await SeedPerguntasAsync(db, ct);
        await SeedConsultoresAsync(db, hasher, ct);
        await SeedEmpresasAsync(db, ct);
        await SeedAvaliacoesAsync(db, ct);
        await SeedRespostasAsync(db, ct);

        // Recalcula resultados + recomendações via engine (substitui as triggers).
        var engine = new DiagnosticoEngine(db);
        var avaliacoes = await db.Avaliacoes.Select(a => a.AvaId).ToListAsync(ct);
        foreach (var avaId in avaliacoes)
        {
            var perguntasRespondidas = await db.Respostas
                .Where(r => r.ResAvaId == avaId)
                .Select(r => r.ResPergId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var pergId in perguntasRespondidas)
                await engine.RecalcularAposRespostaAsync(avaId, pergId, ct);

            await engine.AtualizarStatusAvaliacaoAsync(avaId, ct);
        }
    }

    private static async Task SeedPilaresAsync(KaizenDbContext db, CancellationToken ct)
    {
        db.Pilares.AddRange(
            new Pilar { PilId = 1, PilNome = "Governança", PilPontuacaoMax = 12 },
            new Pilar { PilId = 2, PilNome = "Segurança da Informação", PilPontuacaoMax = 14 },
            new Pilar { PilId = 3, PilNome = "Ética e Integridade", PilPontuacaoMax = 12 },
            new Pilar { PilId = 4, PilNome = "Políticas e Documentação", PilPontuacaoMax = 14 },
            new Pilar { PilId = 5, PilNome = "Terceiros e Conscientização", PilPontuacaoMax = 12 }
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedPerguntasAsync(KaizenDbContext db, CancellationToken ct)
    {
        var perguntas = new (int Id, int Pilar, string Descricao)[]
        {
            (1, 1, "A empresa possui responsáveis definidos para temas de risco, compliance ou segurança?"),
            (2, 1, "A alta liderança demonstra apoio a práticas de governança e conformidade?"),
            (3, 1, "Existem papéis e responsabilidades formalmente definidos para processos críticos?"),
            (4, 1, "A empresa realiza acompanhamento periódico de riscos ou controles internos?"),
            (5, 1, "Há reuniões, comitês ou fóruns para tratar temas estratégicos, riscos ou conformidade?"),
            (6, 1, "As decisões importantes da empresa seguem critérios ou processos minimamente padronizados?"),
            (7, 2, "A empresa possui controle de acesso a sistemas e informações importantes?"),
            (8, 2, "Os usuários utilizam senhas individuais para acessar sistemas corporativos?"),
            (9, 2, "A empresa realiza backup de dados importantes?"),
            (10, 2, "Existe algum processo para tratar incidentes de segurança ou perda de informação?"),
            (11, 2, "Os acessos de colaboradores desligados são removidos ou revisados?"),
            (12, 2, "A empresa orienta os colaboradores sobre cuidados com e-mails suspeitos, senhas e arquivos?"),
            (13, 2, "Há proteção básica dos dispositivos e sistemas, como antivírus, atualizações ou monitoramento?"),
            (14, 3, "A empresa possui diretrizes de conduta ética para colaboradores?"),
            (15, 3, "Os colaboradores recebem orientação sobre comportamento ético e conflitos de interesse?"),
            (16, 3, "Existe algum canal ou meio para reportar irregularidades, mesmo que informalmente?"),
            (17, 3, "A empresa trata casos de descumprimento de regras de forma consistente?"),
            (18, 3, "Há preocupação com prevenção de fraude, favorecimento indevido ou conflito de interesses?"),
            (19, 3, "A liderança atua como exemplo em relação à conduta esperada?"),
            (20, 4, "A empresa possui políticas, normas ou procedimentos documentados para atividades importantes?"),
            (21, 4, "Os documentos internos são revisados ou atualizados periodicamente?"),
            (22, 4, "Os colaboradores sabem onde consultar regras, procedimentos ou orientações internas?"),
            (23, 4, "Há registro das principais rotinas e processos críticos da empresa?"),
            (24, 4, "A empresa mantém documentos importantes organizados e acessíveis para consulta?"),
            (25, 4, "Existe controle mínimo sobre versões ou atualizações de documentos relevantes?"),
            (26, 4, "Há comunicação interna para reforçar responsabilidades e cuidados em processos críticos?"),
            (27, 5, "A empresa avalia riscos ou impactos antes de contratar terceiros para atividades importantes?"),
            (28, 5, "Existem critérios mínimos para selecionar fornecedores ou parceiros críticos?"),
            (29, 5, "A empresa acompanha a atuação de terceiros que acessam informações, sistemas ou processos relevantes?"),
            (30, 5, "Os terceiros recebem orientações básicas sobre regras, segurança ou conduta quando necessário?"),
            (31, 5, "A empresa promove conscientização sobre segurança, ética ou conformidade no dia a dia?"),
            (32, 5, "Há comunicação interna para reforçar responsabilidades e cuidados em processos críticos?")
        };

        foreach (var p in perguntas)
            db.Perguntas.Add(new Pergunta
            {
                PergId = p.Id,
                PergPilId = p.Pilar,
                PergDescricao = p.Descricao,
                PergPeso = 2
            });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedConsultoresAsync(KaizenDbContext db, IPasswordHasher hasher, CancellationToken ct)
    {
        var senha = hasher.Hash("kaizen123");
        var cadastro = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Consultores.AddRange(
            new Consultor { ConId = 201, ConNome = "Administrador Kaizen", ConEmail = "admin@kaizen.com.br", ConSenha = senha, ConDtCadastro = cadastro },
            new Consultor { ConId = 202, ConNome = "Carlos Mendes", ConEmail = "carlos@kaizen.com.br", ConSenha = senha, ConDtCadastro = cadastro },
            new Consultor { ConId = 203, ConNome = "Ana Ferreira", ConEmail = "ana@kaizen.com.br", ConSenha = senha, ConDtCadastro = cadastro }
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedEmpresasAsync(KaizenDbContext db, CancellationToken ct)
    {
        var cadastro = new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        db.Empresas.AddRange(
            new Empresa { EmpId = 101, EmpNome = "TechStart Ltda", EmpSetor = "Tecnologia", EmpPorte = "Pequena", EmpNumFunc = 45, EmpResp = "João Silva", EmpRespCargo = "CTO", EmpRespAreas = "TI, Produto", EmpGestTerc = "S", EmpDtCadastro = cadastro },
            new Empresa { EmpId = 102, EmpNome = "Varejo Plus S/A", EmpSetor = "Varejo", EmpPorte = "Media", EmpNumFunc = 210, EmpResp = "Carla Souza", EmpRespCargo = "Compliance", EmpRespAreas = "Jurídico, RH", EmpGestTerc = "N", EmpDtCadastro = cadastro },
            new Empresa { EmpId = 103, EmpNome = "Construtora Ápex", EmpSetor = "Construção", EmpPorte = "Grande", EmpNumFunc = 850, EmpResp = "Marcos Dutra", EmpRespCargo = "Diretor Geral", EmpRespAreas = "Obras, Compras", EmpGestTerc = "S", EmpDtCadastro = cadastro },
            new Empresa { EmpId = 104, EmpNome = "Clínica VidaSana", EmpSetor = "Saúde", EmpPorte = "Pequena", EmpNumFunc = 32, EmpResp = "Dra. Lúcia", EmpRespCargo = "Sócia-diretora", EmpRespAreas = "Clínica, Admin", EmpGestTerc = "I", EmpDtCadastro = cadastro },
            new Empresa { EmpId = 105, EmpNome = "Logística Veloz ME", EmpSetor = "Logística", EmpPorte = "Micro", EmpNumFunc = 12, EmpResp = "Paulo Ramos", EmpRespCargo = "Sócio", EmpRespAreas = "Operações", EmpGestTerc = "N", EmpDtCadastro = cadastro }
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedAvaliacoesAsync(KaizenDbContext db, CancellationToken ct)
    {
        // Status final é recalculado pela engine; aqui só registramos o cabeçalho.
        db.Avaliacoes.AddRange(
            new Avaliacao { AvaId = 1, AvaEmpId = 101, AvaConId = 201, AvaDtAvaliacao = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc), AvaStatus = "A" },
            new Avaliacao { AvaId = 2, AvaEmpId = 102, AvaConId = 201, AvaDtAvaliacao = new DateTime(2025, 2, 15, 0, 0, 0, DateTimeKind.Utc), AvaStatus = "A" },
            new Avaliacao { AvaId = 3, AvaEmpId = 103, AvaConId = 202, AvaDtAvaliacao = new DateTime(2025, 3, 5, 0, 0, 0, DateTimeKind.Utc), AvaStatus = "A" }
        );
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedRespostasAsync(KaizenDbContext db, CancellationToken ct)
    {
        var dt = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var resId = 1;

        // Avaliação 1 (TechStart): respostas completas para as 32 perguntas
        // -> vira Finalizada e gera resultado + recomendação para os 5 pilares.
        int[] valoresAva1 =
        {
            2, 2, 1, 2, 1, 2, // pilar 1  (10/12 -> Avancado)
            2, 1, 2, 1, 1, 0, 1, // pilar 2 (8/14  -> Estruturado)
            1, 1, 0, 1, 1, 2, // pilar 3 (6/12  -> Em desenvolvimento)
            2, 2, 2, 2, 1, 2, 2, // pilar 4 (13/14 -> Avancado)
            0, 1, 0, 1, 1, 1  // pilar 5 (4/12  -> Em desenvolvimento)
        };
        for (var perg = 1; perg <= 32; perg++)
            db.Respostas.Add(new Resposta { ResId = resId++, ResAvaId = 1, ResPergId = perg, ResValor = valoresAva1[perg - 1], ResDtResposta = dt });

        // Avaliação 2 (Varejo Plus): respostas parciais (pilares 1 e 2)
        // -> permanece "Em andamento".
        int[] perguntasAva2 = { 1, 2, 3, 7, 8, 9 };
        int[] valoresAva2 = { 1, 1, 0, 2, 2, 1 };
        for (var i = 0; i < perguntasAva2.Length; i++)
            db.Respostas.Add(new Resposta { ResId = resId++, ResAvaId = 2, ResPergId = perguntasAva2[i], ResValor = valoresAva2[i], ResDtResposta = dt });

        await db.SaveChangesAsync(ct);
    }
}
