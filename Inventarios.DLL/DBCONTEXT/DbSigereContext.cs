using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Inventario.Entity;

namespace Inventario.DAL.DBCONTEXT;

public partial class DbSigereContext : DbContext
{
    public DbSigereContext()
    {
    }

    public DbSigereContext(DbContextOptions<DbSigereContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblArticulo> TblArticulos { get; set; }

    public virtual DbSet<TblBitacoraEstatus> TblBitacoraEstatuses { get; set; }

    public virtual DbSet<TblDepartamento> TblDepartamentos { get; set; }

    public virtual DbSet<TblEstatus> TblEstatuses { get; set; }

    public virtual DbSet<TblPartidaPresupuestal> TblPartidaPresupuestals { get; set; }

    public virtual DbSet<TblPerido> TblPeridos { get; set; }

    public virtual DbSet<TblPrioridad> TblPrioridads { get; set; }

    public virtual DbSet<TblProvedor> TblProvedors { get; set; }

    public virtual DbSet<TblProvedorTipo> TblProvedorTipos { get; set; }

    public virtual DbSet<TblRequisicion> TblRequisicions { get; set; }

    public virtual DbSet<TblRequisicionDetalle> TblRequisicionDetalles { get; set; }

    public virtual DbSet<TblRol> TblRols { get; set; }

    public virtual DbSet<TblUnidadMedidum> TblUnidadMedida { get; set; }

    public virtual DbSet<TblUsuario> TblUsuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblArticulo>(entity =>
        {
            entity.HasKey(e => e.IdArticulo);

            entity.ToTable("tblArticulo");

            entity.Property(e => e.IdArticulo).ValueGeneratedNever();
            entity.Property(e => e.Articulo)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Capitulo)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.ClaveActividadEspecifica)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.ClaveUnidadMedida)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.CostoEstimado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CostoPromedio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DescripcionUnidadMedida)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NombreActividadEspecifica)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.PartidaEspecifica)
                .HasMaxLength(5)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblBitacoraEstatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TblBitacoraEstatus");

            entity.Property(e => e.FechaEstatus).HasColumnType("datetime");
            entity.Property(e => e.Observacion).HasMaxLength(100);
        });

        modelBuilder.Entity<TblDepartamento>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TblDepartamento");

            entity.Property(e => e.CargoDirector)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.CargoJefe)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CveRf)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("CveRF");
            entity.Property(e => e.NivelOrganigrama)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreDepartamento)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NombreDirector)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NombreJefe)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<TblEstatus>(entity =>
        {
            entity.HasKey(e => e.IdEstatus);

            entity.ToTable("TblEstatus");

            entity.Property(e => e.NombreEstatus)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblPartidaPresupuestal>(entity =>
        {
            entity.HasKey(e => e.IdPartida);

            entity.ToTable("tblPartidaPresupuestal");

            entity.Property(e => e.Cog)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.Disponible).HasColumnType("decimal(18, 4)");
        });

        modelBuilder.Entity<TblPerido>(entity =>
        {
            entity.HasKey(e => e.IdPeriodo);

            entity.ToTable("tblPerido");

            entity.Property(e => e.IdPeriodo).HasColumnName("idPeriodo");
        });

        modelBuilder.Entity<TblPrioridad>(entity =>
        {
            entity.HasKey(e => e.IdPrioridad);

            entity.ToTable("tblPrioridad");

            entity.Property(e => e.NombrePrioridad)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblProvedor>(entity =>
        {
            entity.HasKey(e => e.IdProvedor);

            entity.ToTable("tblProvedor");

            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaBaja).HasColumnType("datetime");
            entity.Property(e => e.FechaSistema).HasColumnType("datetime");
            entity.Property(e => e.NombreProvedor)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Presupuesto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Rfc)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("RFC");

            entity.HasOne(d => d.IdProvedorTipoNavigation).WithMany(p => p.TblProvedors)
                .HasForeignKey(d => d.IdProvedorTipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblProvedor_tblProvedorTipo");
        });

        modelBuilder.Entity<TblProvedorTipo>(entity =>
        {
            entity.HasKey(e => e.IdProvedorTipo).HasName("PK_tblTipoProveedor");

            entity.ToTable("tblProvedorTipo");

            entity.Property(e => e.Descricpion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Sigla)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblRequisicion>(entity =>
        {
            entity.HasKey(e => e.IdRequisicion);

            entity.ToTable("tblRequisicion");

            entity.Property(e => e.IdRequisicion).HasColumnName("idRequisicion");
            entity.Property(e => e.Domicilio)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FechaEmision).HasColumnType("datetime");
            entity.Property(e => e.FechaSistema).HasColumnType("datetime");
            entity.Property(e => e.IdPrioridad).HasColumnName("idPrioridad");
            entity.Property(e => e.IdUsuario).HasColumnName("idUsuario");
            entity.Property(e => e.Justificacion).IsUnicode(false);
            entity.Property(e => e.LugarEntrega)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NomResponsableDepartamento)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NumRequisicion)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.UsoEspecifico).IsUnicode(false);

            entity.HasOne(d => d.IdEstatusNavigation).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.IdEstatus)
                .HasConstraintName("FK_tblRequisicion_TblEstatus");

            entity.HasOne(d => d.IdPeriodoNavigation).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.IdPeriodo)
                .HasConstraintName("FK_tblRequisicion_tblPerido");

            entity.HasOne(d => d.IdPrioridadNavigation).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.IdPrioridad)
                .HasConstraintName("FK_tblRequisicion_tblPrioridad");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_tblRequisicion_TblUsuario");
        });

        modelBuilder.Entity<TblRequisicionDetalle>(entity =>
        {
            entity.HasKey(e => e.IdRequisicionDetalle);

            entity.ToTable("tblRequisicionDetalle");

            entity.Property(e => e.IdRequisicionDetalle).ValueGeneratedNever();
            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasColumnType("datetime");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblRequisicionDetalles)
                .HasForeignKey(d => d.IdRequisicion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRequisicionDetalle_tblRequisicion");
        });

        modelBuilder.Entity<TblRol>(entity =>
        {
            entity.ToTable("TblRol");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblUnidadMedidum>(entity =>
        {
            entity.HasKey(e => e.IdUnidadMedida);

            entity.ToTable("tblUnidadMedida");

            entity.Property(e => e.DescripcionUnidadMedida)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblUsuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.ToTable("TblUsuario");

            entity.Property(e => e.Area)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CargoEnlace)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Correoenlace)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("correoenlace");
            entity.Property(e => e.NombreEnlace)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Pasword)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Usuario)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.TblUsuarios)
                .HasForeignKey(d => d.IdRol)
                .HasConstraintName("FK_TblUsuario_TblRol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
