using Kaizen.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaizen.Api.Data;

public class KaizenDbContext : DbContext
{
    public KaizenDbContext(DbContextOptions<KaizenDbContext> options) : base(options) { }

    public DbSet<Consultor> Consultores => Set<Consultor>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Pilar> Pilares => Set<Pilar>();
    public DbSet<Pergunta> Perguntas => Set<Pergunta>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();
    public DbSet<Resposta> Respostas => Set<Resposta>();
    public DbSet<Resultado> Resultados => Set<Resultado>();
    public DbSet<Recomendacao> Recomendacoes => Set<Recomendacao>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ---------- KZ_CONSULTOR ----------
        b.Entity<Consultor>(e =>
        {
            e.ToTable("KZ_CONSULTOR");
            e.HasKey(x => x.ConId).HasName("PK_CON");
            e.Property(x => x.ConId).HasColumnName("CON_ID").ValueGeneratedNever();
            e.Property(x => x.ConNome).HasColumnName("CON_NOME").HasMaxLength(100).IsRequired();
            e.Property(x => x.ConEmail).HasColumnName("CON_EMAIL").HasMaxLength(150).IsRequired();
            e.Property(x => x.ConSenha).HasColumnName("CON_SENHA").HasMaxLength(255).IsRequired();
            e.Property(x => x.ConFoto).HasColumnName("CON_FOTO").HasMaxLength(255);
            e.Property(x => x.ConDtCadastro).HasColumnName("CON_DT_CADASTRO").IsRequired();
            e.HasIndex(x => x.ConEmail).IsUnique().HasDatabaseName("UQ_CON_EMAIL");
        });

        // ---------- KZ_EMPRESA ----------
        b.Entity<Empresa>(e =>
        {
            e.ToTable("KZ_EMPRESA", t =>
                t.HasCheckConstraint("CK_EMP_GEST", "EMP_GEST_TERC IN ('S','N','I')"));
            e.HasKey(x => x.EmpId).HasName("PK_EMP");
            e.Property(x => x.EmpId).HasColumnName("EMP_ID").ValueGeneratedNever();
            e.Property(x => x.EmpNome).HasColumnName("EMP_NOME").HasMaxLength(150).IsRequired();
            e.Property(x => x.EmpSetor).HasColumnName("EMP_SETOR").HasMaxLength(100);
            e.Property(x => x.EmpPorte).HasColumnName("EMP_PORTE").HasMaxLength(20);
            e.Property(x => x.EmpNumFunc).HasColumnName("EMP_NUM_FUNC");
            e.Property(x => x.EmpResp).HasColumnName("EMP_RESP").HasMaxLength(100);
            e.Property(x => x.EmpRespCargo).HasColumnName("EMP_RESP_CARGO").HasMaxLength(100);
            e.Property(x => x.EmpRespAreas).HasColumnName("EMP_RESP_AREAS").HasMaxLength(255);
            e.Property(x => x.EmpGestTerc).HasColumnName("EMP_GEST_TERC").HasMaxLength(1).IsRequired();
            e.Property(x => x.EmpFoto).HasColumnName("EMP_FOTO").HasMaxLength(255);
            e.Property(x => x.EmpDtCadastro).HasColumnName("EMP_DT_CADASTRO").IsRequired();
        });

        // ---------- KZ_PILAR ----------
        b.Entity<Pilar>(e =>
        {
            e.ToTable("KZ_PILAR");
            e.HasKey(x => x.PilId).HasName("PK_PIL");
            e.Property(x => x.PilId).HasColumnName("PIL_ID").ValueGeneratedNever();
            e.Property(x => x.PilNome).HasColumnName("PIL_NOME").HasMaxLength(100).IsRequired();
            e.Property(x => x.PilPontuacaoMax).HasColumnName("PIL_PONTUACAO_MAX").IsRequired();
        });

        // ---------- KZ_PERGUNTA ----------
        b.Entity<Pergunta>(e =>
        {
            e.ToTable("KZ_PERGUNTA");
            e.HasKey(x => x.PergId).HasName("PK_PERG");
            e.Property(x => x.PergId).HasColumnName("PERG_ID").ValueGeneratedNever();
            e.Property(x => x.PergPilId).HasColumnName("PERG_PIL_ID").IsRequired();
            e.Property(x => x.PergDescricao).HasColumnName("PERG_DESCRICAO").HasMaxLength(500).IsRequired();
            e.Property(x => x.PergPeso).HasColumnName("PERG_PESO").IsRequired();
            e.HasOne(x => x.Pilar).WithMany(p => p.Perguntas)
                .HasForeignKey(x => x.PergPilId).HasConstraintName("FK_PERG_PIL_ID");
        });

        // ---------- KZ_AVALIACAO ----------
        b.Entity<Avaliacao>(e =>
        {
            e.ToTable("KZ_AVALIACAO", t =>
                t.HasCheckConstraint("CK_AVA_STATUS", "AVA_STATUS IN ('A','E','F')"));
            e.HasKey(x => x.AvaId).HasName("PK_AVA");
            e.Property(x => x.AvaId).HasColumnName("AVA_ID").ValueGeneratedNever();
            e.Property(x => x.AvaEmpId).HasColumnName("AVA_EMP_ID").IsRequired();
            e.Property(x => x.AvaConId).HasColumnName("AVA_CON_ID").IsRequired();
            e.Property(x => x.AvaDtAvaliacao).HasColumnName("AVA_DT_AVALIACAO").IsRequired();
            e.Property(x => x.AvaDtAtualizacao).HasColumnName("AVA_DT_ATUALIZACAO");
            e.Property(x => x.AvaStatus).HasColumnName("AVA_STATUS").HasMaxLength(1).IsRequired();
            e.HasOne(x => x.Empresa).WithMany(em => em.Avaliacoes)
                .HasForeignKey(x => x.AvaEmpId).HasConstraintName("FK_AVA_EMP");
            e.HasOne(x => x.Consultor).WithMany(c => c.Avaliacoes)
                .HasForeignKey(x => x.AvaConId).HasConstraintName("FK_AVA_CON");
        });

        // ---------- KZ_RESPOSTA ----------
        b.Entity<Resposta>(e =>
        {
            e.ToTable("KZ_RESPOSTA", t =>
                t.HasCheckConstraint("CK_RES_VALOR", "RES_VALOR IN (0,1,2)"));
            e.HasKey(x => x.ResId).HasName("PK_RES");
            e.Property(x => x.ResId).HasColumnName("RES_ID").ValueGeneratedNever();
            e.Property(x => x.ResAvaId).HasColumnName("RES_AVA_ID").IsRequired();
            e.Property(x => x.ResPergId).HasColumnName("RES_PERG_ID").IsRequired();
            e.Property(x => x.ResValor).HasColumnName("RES_VALOR").IsRequired();
            e.Property(x => x.ResDtResposta).HasColumnName("RES_DT_RESPOSTA").IsRequired();
            e.HasIndex(x => new { x.ResAvaId, x.ResPergId }).IsUnique().HasDatabaseName("UQ_RES_AVA_PERG");
            e.HasOne(x => x.Avaliacao).WithMany(a => a.Respostas)
                .HasForeignKey(x => x.ResAvaId).HasConstraintName("FK_RES_AVA");
            e.HasOne(x => x.Pergunta).WithMany(p => p.Respostas)
                .HasForeignKey(x => x.ResPergId).HasConstraintName("FK_RES_PERG");
        });

        // ---------- KZ_RESULTADO ----------
        b.Entity<Resultado>(e =>
        {
            e.ToTable("KZ_RESULTADO");
            e.HasKey(x => x.RstId).HasName("KZ_RESULTADO_PK");
            e.Property(x => x.RstId).HasColumnName("RST_ID").ValueGeneratedNever();
            e.Property(x => x.RstAvaId).HasColumnName("RST_AVA_ID").IsRequired();
            e.Property(x => x.RstPilId).HasColumnName("RST_PIL_ID").IsRequired();
            e.Property(x => x.RstPontuacao).HasColumnName("RST_PONTUACAO").IsRequired();
            e.Property(x => x.RstNivelMaturidade).HasColumnName("RST_NIVEL_MATURIDADE").HasMaxLength(30);
            e.Property(x => x.RstPrioridade).HasColumnName("RST_PRIORIDADE").HasMaxLength(10);
            e.HasIndex(x => new { x.RstAvaId, x.RstPilId }).IsUnique().HasDatabaseName("UQ_RST_AVA_PIL");
            e.HasOne(x => x.Avaliacao).WithMany(a => a.Resultados)
                .HasForeignKey(x => x.RstAvaId).HasConstraintName("FK_RST_AVA_ID");
            e.HasOne(x => x.Pilar).WithMany(p => p.Resultados)
                .HasForeignKey(x => x.RstPilId).HasConstraintName("FK_RST_PIL_ID");
        });

        // ---------- KZ_RECOMENDACAO ----------
        b.Entity<Recomendacao>(e =>
        {
            e.ToTable("KZ_RECOMENDACAO");
            e.HasKey(x => x.RecId).HasName("PK_REC");
            e.Property(x => x.RecId).HasColumnName("REC_ID").ValueGeneratedNever();
            e.Property(x => x.RecRstId).HasColumnName("REC_RST_ID").IsRequired();
            e.Property(x => x.RecTipo).HasColumnName("REC_TIPO").HasMaxLength(50);
            e.Property(x => x.RecDescricao).HasColumnName("REC_DESCRICAO").HasMaxLength(500);
            e.Property(x => x.RecStatus).HasColumnName("REC_STATUS").HasMaxLength(20).IsRequired();
            e.Property(x => x.RecDtGeracao).HasColumnName("REC_DT_GERACAO").IsRequired();
            e.HasOne(x => x.Resultado).WithMany(r => r.Recomendacoes)
                .HasForeignKey(x => x.RecRstId).HasConstraintName("FK_REC_RST_ID");
        });
    }
}
