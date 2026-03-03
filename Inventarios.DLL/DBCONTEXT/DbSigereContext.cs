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

    public virtual DbSet<TblInventario> TblInventarios { get; set; }

    public virtual DbSet<TblMunicipio> TblMunicipios { get; set; }

    public virtual DbSet<TblPartidaPresupuestal> TblPartidaPresupuestals { get; set; }

    public virtual DbSet<TblPrioridad> TblPrioridads { get; set; }

    public virtual DbSet<TblProgramaPresupuestario> TblProgramaPresupuestarios { get; set; }

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
            entity.HasKey(e => e.Id).HasName("PK__tblArtic__3214EC07CBCDD3F4");

            entity.ToTable("tblArticulos");

            entity.HasIndex(e => e.Descripcion, "IX_TblArticulo_Descripcion");

            entity.Property(e => e.Cog).HasColumnName("COG");
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Marca).HasMaxLength(30);
            entity.Property(e => e.Modelo).HasMaxLength(50);
            entity.Property(e => e.Tipo)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.UnidadMedida).HasMaxLength(20);
        });

        modelBuilder.Entity<TblBitacoraEstatus>(entity =>
        {
            entity.HasKey(e => e.IdBitacoraEstatus);

            entity.ToTable("TblBitacoraEstatus");

            entity.Property(e => e.FechaEstatus).HasColumnType("datetime");
            entity.Property(e => e.Observacion).HasMaxLength(1000);

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblBitacoraEstatuses)
                .HasForeignKey(d => d.IdRequisicion)
                .HasConstraintName("FK_TblBitacoraEstatus_tblRequisicion");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblBitacoraEstatuses)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_TblBitacoraEstatus_TblUsuario");
        });

        modelBuilder.Entity<TblDepartamento>(entity =>
        {
            entity.HasKey(e => e.IdDepartamento);

            entity.ToTable("TblDepartamento");

            entity.Property(e => e.IdDepartamento).ValueGeneratedNever();
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

        modelBuilder.Entity<TblInventario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tblInven__3214EC074AE24CDA");

            entity.ToTable("tblInventario");

            entity.Property(e => e.Clave).HasMaxLength(15);
            entity.Property(e => e.Costo).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Iva).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UnidadMedida).HasMaxLength(15);
        });

        modelBuilder.Entity<TblMunicipio>(entity =>
        {
            entity.HasKey(e => e.IdMunicipio);

            entity.ToTable("TblMunicipio");

            entity.Property(e => e.IdMunicipio).ValueGeneratedNever();
            entity.Property(e => e.ClaveRegion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NombreMunicipios)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NombreRegion).HasMaxLength(50);
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

        modelBuilder.Entity<TblPrioridad>(entity =>
        {
            entity.HasKey(e => e.IdPrioridad);

            entity.ToTable("tblPrioridad");

            entity.Property(e => e.NombrePrioridad)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblProgramaPresupuestario>(entity =>
        {
            entity.ToTable("TblProgramaPresupuestario");

            entity.Property(e => e.Actividad)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Area)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Componente)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Departamento)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.DescripcionActividad)
                .HasMaxLength(2500)
                .IsUnicode(false);
            entity.Property(e => e.Pp)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PP");
            entity.Property(e => e.ProgramaSocial)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(50)
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
            entity.Property(e => e.Correo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaSistema).HasColumnType("datetime");
            entity.Property(e => e.Ff)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FF");
            entity.Property(e => e.Hash)
                .HasMaxLength(16)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("hash");
            entity.Property(e => e.IdPp).HasColumnName("IdPP");
            entity.Property(e => e.IdUsuario).HasColumnName("idUsuario");
            entity.Property(e => e.Justificacion).IsUnicode(false);
            entity.Property(e => e.LugarEntrega)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NomResponsableDepartamento)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NombreDirector)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NumRequisicion)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.TipoPrograma)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsoEspecifico).IsUnicode(false);

            entity.HasOne(d => d.ClaveRegionNavigation).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.ClaveRegion)
                .HasConstraintName("FK_tblRequisicion_TblMunicipio");

            entity.HasOne(d => d.IdDepartamentoNavigation).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.IdDepartamento)
                .HasConstraintName("FK_tblRequisicion_TblDepartamento");

            entity.HasOne(d => d.IdEstatusNavigation).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.IdEstatus)
                .HasConstraintName("FK_tblRequisicion_TblEstatus");

            entity.HasOne(d => d.IdPpNavigation).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.IdPp)
                .HasConstraintName("FK_tblRequisicion_TblProgramaPresupuestario");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblRequisicionIdUsuarioNavigations)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_tblRequisicion_TblUsuario");

            entity.HasOne(d => d.IdUsuarioMatNavigation).WithMany(p => p.TblRequisicionIdUsuarioMatNavigations)
                .HasForeignKey(d => d.IdUsuarioMat)
                .HasConstraintName("FK_tblRequisicion_TblUsuario1");
        });

        modelBuilder.Entity<TblRequisicionDetalle>(entity =>
        {
            entity.HasKey(e => e.IdRequisicionDetalle).HasName("PK__TblRequi__1ECF53CA649517A9");

            entity.ToTable("TblRequisicionDetalle");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.DescripcionDetallada)
                .HasMaxLength(5000)
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

            entity.Property(e => e.Correo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Pasword)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.Usuario)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdDepartamentoNavigation).WithMany(p => p.TblUsuarios)
                .HasForeignKey(d => d.IdDepartamento)
                .HasConstraintName("FK_TblUsuario_TblDepartamento");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.TblUsuarios)
                .HasForeignKey(d => d.IdRol)
                .HasConstraintName("FK_TblUsuario_TblRol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}