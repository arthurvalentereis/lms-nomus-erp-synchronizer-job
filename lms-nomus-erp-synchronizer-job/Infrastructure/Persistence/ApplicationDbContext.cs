using lms_nomus_erp_synchronizer_job.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Persistence;

/// <summary>
/// DbContext para persistência dos dados sincronizados do Nomus
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Boleto> Boletos { get; set; }
    public DbSet<Recebimento> Recebimentos { get; set; }
    public DbSet<ContaReceber> ContasReceber { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de Boletos
        modelBuilder.Entity<Boleto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdContaReceber);
            entity.HasIndex(e => e.IdEmpresa);
            entity.HasIndex(e => e.IdPessoa);
            entity.Property(e => e.Valor).HasPrecision(18, 2);
        });

        // Configuração de Recebimentos
        modelBuilder.Entity<Recebimento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdContaReceber);
            entity.HasIndex(e => e.IdEmpresa);
            entity.Property(e => e.ValorRecebido).HasPrecision(18, 2);
            entity.Property(e => e.Desconto).HasPrecision(18, 2);
            entity.Property(e => e.MultaJuros).HasPrecision(18, 2);
        });

        // Configuração de Contas a Receber
        modelBuilder.Entity<ContaReceber>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdPessoa);
            entity.HasIndex(e => e.IdEmpresa);
            entity.Property(e => e.ValorReceber).HasPrecision(18, 2);
            entity.Property(e => e.ValorRecebido).HasPrecision(18, 2);
            entity.Property(e => e.SaldoReceber).HasPrecision(18, 2);
        });
    }
}

