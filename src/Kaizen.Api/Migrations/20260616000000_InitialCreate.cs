using Kaizen.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaizen.Api.Migrations;

/// <summary>
/// Migração inicial do schema Kaizen.
/// Cria as 8 tabelas KZ_ com PKs, FKs, constraints de verificação e índices únicos.
///
/// Observação de projeto: as 4 triggers do banco Oracle original (cálculo de score,
/// resultado por pilar, classificação de maturidade e geração de recomendação) NÃO são
/// recriadas aqui — essa lógica foi reimplementada em C# na DiagnosticoEngine, que passa
/// a ser a única fonte de verdade da regra de negócio (testável por unidade).
///
/// O DDL é escrito em Oracle explícito para garantir compatibilidade com o provider
/// Oracle.EntityFrameworkCore tanto no banco da FIAP quanto no container gvenzl/oracle-free.
/// </summary>
[DbContext(typeof(KaizenDbContext))]
[Migration("20260616000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE TABLE ""KZ_CONSULTOR"" (
    ""CON_ID""          NUMBER(10) NOT NULL,
    ""CON_NOME""        VARCHAR2(100) NOT NULL,
    ""CON_EMAIL""       VARCHAR2(150) NOT NULL,
    ""CON_SENHA""       VARCHAR2(255) NOT NULL,
    ""CON_FOTO""        VARCHAR2(255),
    ""CON_DT_CADASTRO"" TIMESTAMP NOT NULL,
    CONSTRAINT ""PK_CON"" PRIMARY KEY (""CON_ID""),
    CONSTRAINT ""UQ_CON_EMAIL"" UNIQUE (""CON_EMAIL"")
)");

        migrationBuilder.Sql(@"
CREATE TABLE ""KZ_EMPRESA"" (
    ""EMP_ID""          NUMBER(10) NOT NULL,
    ""EMP_NOME""        VARCHAR2(150) NOT NULL,
    ""EMP_SETOR""       VARCHAR2(100),
    ""EMP_PORTE""       VARCHAR2(20),
    ""EMP_NUM_FUNC""    NUMBER(10),
    ""EMP_RESP""        VARCHAR2(100),
    ""EMP_RESP_CARGO""  VARCHAR2(100),
    ""EMP_RESP_AREAS""  VARCHAR2(255),
    ""EMP_GEST_TERC""   VARCHAR2(1) NOT NULL,
    ""EMP_FOTO""        VARCHAR2(255),
    ""EMP_DT_CADASTRO"" TIMESTAMP NOT NULL,
    CONSTRAINT ""PK_EMP"" PRIMARY KEY (""EMP_ID""),
    CONSTRAINT ""CK_EMP_GEST"" CHECK (""EMP_GEST_TERC"" IN ('S','N','I'))
)");

        migrationBuilder.Sql(@"
CREATE TABLE ""KZ_PILAR"" (
    ""PIL_ID""            NUMBER(10) NOT NULL,
    ""PIL_NOME""          VARCHAR2(100) NOT NULL,
    ""PIL_PONTUACAO_MAX"" NUMBER(10) NOT NULL,
    CONSTRAINT ""PK_PIL"" PRIMARY KEY (""PIL_ID"")
)");

        migrationBuilder.Sql(@"
CREATE TABLE ""KZ_PERGUNTA"" (
    ""PERG_ID""        NUMBER(10) NOT NULL,
    ""PERG_PIL_ID""    NUMBER(10) NOT NULL,
    ""PERG_DESCRICAO"" VARCHAR2(500) NOT NULL,
    ""PERG_PESO""      NUMBER(10) NOT NULL,
    CONSTRAINT ""PK_PERG"" PRIMARY KEY (""PERG_ID""),
    CONSTRAINT ""FK_PERG_PIL_ID"" FOREIGN KEY (""PERG_PIL_ID"") REFERENCES ""KZ_PILAR"" (""PIL_ID"")
)");

        migrationBuilder.Sql(@"
CREATE TABLE ""KZ_AVALIACAO"" (
    ""AVA_ID""            NUMBER(10) NOT NULL,
    ""AVA_EMP_ID""        NUMBER(10) NOT NULL,
    ""AVA_CON_ID""        NUMBER(10) NOT NULL,
    ""AVA_DT_AVALIACAO""  TIMESTAMP NOT NULL,
    ""AVA_DT_ATUALIZACAO"" TIMESTAMP,
    ""AVA_STATUS""        VARCHAR2(1) NOT NULL,
    CONSTRAINT ""PK_AVA"" PRIMARY KEY (""AVA_ID""),
    CONSTRAINT ""FK_AVA_EMP"" FOREIGN KEY (""AVA_EMP_ID"") REFERENCES ""KZ_EMPRESA"" (""EMP_ID""),
    CONSTRAINT ""FK_AVA_CON"" FOREIGN KEY (""AVA_CON_ID"") REFERENCES ""KZ_CONSULTOR"" (""CON_ID""),
    CONSTRAINT ""CK_AVA_STATUS"" CHECK (""AVA_STATUS"" IN ('A','E','F'))
)");

        migrationBuilder.Sql(@"
CREATE TABLE ""KZ_RESPOSTA"" (
    ""RES_ID""          NUMBER(10) NOT NULL,
    ""RES_AVA_ID""      NUMBER(10) NOT NULL,
    ""RES_PERG_ID""     NUMBER(10) NOT NULL,
    ""RES_VALOR""       NUMBER(10) NOT NULL,
    ""RES_DT_RESPOSTA"" TIMESTAMP NOT NULL,
    CONSTRAINT ""PK_RES"" PRIMARY KEY (""RES_ID""),
    CONSTRAINT ""FK_RES_AVA"" FOREIGN KEY (""RES_AVA_ID"") REFERENCES ""KZ_AVALIACAO"" (""AVA_ID""),
    CONSTRAINT ""FK_RES_PERG"" FOREIGN KEY (""RES_PERG_ID"") REFERENCES ""KZ_PERGUNTA"" (""PERG_ID""),
    CONSTRAINT ""UQ_RES_AVA_PERG"" UNIQUE (""RES_AVA_ID"", ""RES_PERG_ID""),
    CONSTRAINT ""CK_RES_VALOR"" CHECK (""RES_VALOR"" IN (0,1,2))
)");

        migrationBuilder.Sql(@"
CREATE TABLE ""KZ_RESULTADO"" (
    ""RST_ID""              NUMBER(10) NOT NULL,
    ""RST_AVA_ID""          NUMBER(10) NOT NULL,
    ""RST_PIL_ID""          NUMBER(10) NOT NULL,
    ""RST_PONTUACAO""       NUMBER(10) NOT NULL,
    ""RST_NIVEL_MATURIDADE"" VARCHAR2(30),
    ""RST_PRIORIDADE""      VARCHAR2(10),
    CONSTRAINT ""KZ_RESULTADO_PK"" PRIMARY KEY (""RST_ID""),
    CONSTRAINT ""FK_RST_AVA_ID"" FOREIGN KEY (""RST_AVA_ID"") REFERENCES ""KZ_AVALIACAO"" (""AVA_ID""),
    CONSTRAINT ""FK_RST_PIL_ID"" FOREIGN KEY (""RST_PIL_ID"") REFERENCES ""KZ_PILAR"" (""PIL_ID""),
    CONSTRAINT ""UQ_RST_AVA_PIL"" UNIQUE (""RST_AVA_ID"", ""RST_PIL_ID"")
)");

        migrationBuilder.Sql(@"
CREATE TABLE ""KZ_RECOMENDACAO"" (
    ""REC_ID""        NUMBER(10) NOT NULL,
    ""REC_RST_ID""    NUMBER(10) NOT NULL,
    ""REC_TIPO""      VARCHAR2(50),
    ""REC_DESCRICAO"" VARCHAR2(500),
    ""REC_STATUS""    VARCHAR2(20) NOT NULL,
    ""REC_DT_GERACAO"" TIMESTAMP NOT NULL,
    CONSTRAINT ""PK_REC"" PRIMARY KEY (""REC_ID""),
    CONSTRAINT ""FK_REC_RST_ID"" FOREIGN KEY (""REC_RST_ID"") REFERENCES ""KZ_RESULTADO"" (""RST_ID"")
)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP TABLE ""KZ_RECOMENDACAO"" CASCADE CONSTRAINTS");
        migrationBuilder.Sql(@"DROP TABLE ""KZ_RESULTADO"" CASCADE CONSTRAINTS");
        migrationBuilder.Sql(@"DROP TABLE ""KZ_RESPOSTA"" CASCADE CONSTRAINTS");
        migrationBuilder.Sql(@"DROP TABLE ""KZ_AVALIACAO"" CASCADE CONSTRAINTS");
        migrationBuilder.Sql(@"DROP TABLE ""KZ_PERGUNTA"" CASCADE CONSTRAINTS");
        migrationBuilder.Sql(@"DROP TABLE ""KZ_PILAR"" CASCADE CONSTRAINTS");
        migrationBuilder.Sql(@"DROP TABLE ""KZ_EMPRESA"" CASCADE CONSTRAINTS");
        migrationBuilder.Sql(@"DROP TABLE ""KZ_CONSULTOR"" CASCADE CONSTRAINTS");
    }
}
