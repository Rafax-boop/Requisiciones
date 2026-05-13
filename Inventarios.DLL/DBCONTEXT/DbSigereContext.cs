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

    public virtual DbSet<TblAdjudicacion> TblAdjudicacions { get; set; }

    public virtual DbSet<TblArticulo> TblArticulos { get; set; }

    public virtual DbSet<TblArticulosProgramado> TblArticulosProgramados { get; set; }

    public virtual DbSet<TblBitacoraEstatus> TblBitacoraEstatuses { get; set; }

    public virtual DbSet<TblConsolidada> TblConsolidadas { get; set; }

    public virtual DbSet<TblConsolidadasDetalle> TblConsolidadasDetalles { get; set; }

    public virtual DbSet<TblCotizacione> TblCotizaciones { get; set; }

    public virtual DbSet<TblDepartamento> TblDepartamentos { get; set; }

    public virtual DbSet<TblEstatus> TblEstatuses { get; set; }

    public virtual DbSet<TblFormato> TblFormatos { get; set; }

    public virtual DbSet<TblFuentesFinanciamiento> TblFuentesFinanciamientos { get; set; }

    public virtual DbSet<TblInventario> TblInventarios { get; set; }

    public virtual DbSet<TblMunicipio> TblMunicipios { get; set; }

    public virtual DbSet<TblPartidaPresupuestal> TblPartidaPresupuestals { get; set; }

    public virtual DbSet<TblProgramaPresupuestario> TblProgramaPresupuestarios { get; set; }

    public virtual DbSet<TblProvedor> TblProvedors { get; set; }

    public virtual DbSet<TblProveedorGanador> TblProveedorGanadors { get; set; }

    public virtual DbSet<TblRegistroDiseno> TblRegistroDisenos { get; set; }

    public virtual DbSet<TblRequisicion> TblRequisicions { get; set; }

    public virtual DbSet<TblRequisicionDetalle> TblRequisicionDetalles { get; set; }

    public virtual DbSet<TblRequisicionDetalleMovimiento> TblRequisicionDetalleMovimientos { get; set; }

    public virtual DbSet<TblRequisicionDetalleMunicipio> TblRequisicionDetalleMunicipios { get; set; }

    public virtual DbSet<TblRol> TblRols { get; set; }

    public virtual DbSet<TblTablaApiHistorial> TblTablaApiHistorials { get; set; }

    public virtual DbSet<TblUsuario> TblUsuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblAdjudicacion>(entity =>
        {
            entity.ToTable("TblAdjudicacion");

            entity.Property(e => e.MontoMax).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MontoMin).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Tipo).HasMaxLength(500);
        });

        modelBuilder.Entity<TblArticulo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tblArtic__3214EC07CBCDD3F4");

            entity.ToTable("tblArticulos");

            entity.HasIndex(e => e.Descripcion, "IX_TblArticulo_Descripcion");

            entity.Property(e => e.Clave)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Cog).HasColumnName("COG");
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Marca).HasMaxLength(30);
            entity.Property(e => e.Modelo).HasMaxLength(50);
            entity.Property(e => e.Tipo)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.UnidadMedida).HasMaxLength(20);
        });

        modelBuilder.Entity<TblArticulosProgramado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TblArtic__3214EC075D80BFDB");

            entity.Property(e => e.Llenado1).HasDefaultValue(0);
            entity.Property(e => e.Llenado10).HasDefaultValue(0);
            entity.Property(e => e.Llenado11).HasDefaultValue(0);
            entity.Property(e => e.Llenado12).HasDefaultValue(0);
            entity.Property(e => e.Llenado2).HasDefaultValue(0);
            entity.Property(e => e.Llenado3).HasDefaultValue(0);
            entity.Property(e => e.Llenado4).HasDefaultValue(0);
            entity.Property(e => e.Llenado5).HasDefaultValue(0);
            entity.Property(e => e.Llenado6).HasDefaultValue(0);
            entity.Property(e => e.Llenado7).HasDefaultValue(0);
            entity.Property(e => e.Llenado8).HasDefaultValue(0);
            entity.Property(e => e.Llenado9).HasDefaultValue(0);
            entity.Property(e => e.TipoProgramacion)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdArticuloNavigation).WithMany(p => p.TblArticulosProgramados)
                .HasForeignKey(d => d.IdArticulo)
                .HasConstraintName("FK_TblArticulosProgramados_tblArticulos");

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblArticulosProgramados)
                .HasForeignKey(d => d.IdRequisicion)
                .HasConstraintName("FK_TblArticulosProgramados_tblRequisicion");

            entity.HasOne(d => d.IdRequisicionDetalleNavigation).WithMany(p => p.TblArticulosProgramados)
                .HasForeignKey(d => d.IdRequisicionDetalle)
                .HasConstraintName("FK_TblArticulosProgramados_TblRequisicionDetalle");
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

        modelBuilder.Entity<TblConsolidada>(entity =>
        {
            entity.HasKey(e => e.ConsolidadaId);

            entity.HasIndex(e => e.FolioConsolidada, "UQ_TblConsolidadas_Folio").IsUnique();

            entity.HasIndex(e => e.Hash, "UQ_TblConsolidadas_Hash").IsUnique();

            entity.Property(e => e.ConsolidadaId).HasColumnName("ConsolidadaID");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Ff)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("FF");
            entity.Property(e => e.FolioConsolidada)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Hash)
                .HasMaxLength(16)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.IdEstatus).HasDefaultValue(1);
            entity.Property(e => e.IdPp).HasColumnName("IdPP");
            entity.Property(e => e.NumApi)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.NumPedido)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TipoPrograma)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdEstatusNavigation).WithMany(p => p.TblConsolidada)
                .HasForeignKey(d => d.IdEstatus)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TblConsolidadas_TblEstatus");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblConsolidadaIdUsuarioNavigations)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_TblConsolidadas_TblUsuario2");

            entity.HasOne(d => d.IdUsuarioFinanNavigation).WithMany(p => p.TblConsolidadaIdUsuarioFinanNavigations)
                .HasForeignKey(d => d.IdUsuarioFinan)
                .HasConstraintName("FK_TblConsolidadas_TblUsuario1");

            entity.HasOne(d => d.IdUsuarioMatNavigation).WithMany(p => p.TblConsolidadaIdUsuarioMatNavigations)
                .HasForeignKey(d => d.IdUsuarioMat)
                .HasConstraintName("FK_TblConsolidadas_TblUsuario");
        });

        modelBuilder.Entity<TblConsolidadasDetalle>(entity =>
        {
            entity.HasKey(e => e.ConsolidadaDetalleId);

            entity.ToTable("TblConsolidadasDetalle");

            entity.HasIndex(e => new { e.ConsolidadaId, e.IdRequisicion }, "UQ_ConsolidadaRequisicion").IsUnique();

            entity.Property(e => e.ConsolidadaDetalleId).HasColumnName("ConsolidadaDetalleID");
            entity.Property(e => e.ConsolidadaId).HasColumnName("ConsolidadaID");
            entity.Property(e => e.FechaAgregada)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Consolidada).WithMany(p => p.TblConsolidadasDetalles)
                .HasForeignKey(d => d.ConsolidadaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConsDetalle_Consolidada");

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblConsolidadasDetalles)
                .HasForeignKey(d => d.IdRequisicion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConsDetalle_Requisicion");
        });

        modelBuilder.Entity<TblCotizacione>(entity =>
        {
            entity.HasKey(e => e.IdCotizacion);

            entity.Property(e => e.Importe).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Iva).HasColumnName("IVA");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.TblCotizaciones)
                .HasForeignKey(d => d.IdProveedor)
                .HasConstraintName("FK_TblCotizaciones_tblProvedor");

            entity.HasOne(d => d.IdRequiDetalleNavigation).WithMany(p => p.TblCotizaciones)
                .HasForeignKey(d => d.IdRequiDetalle)
                .HasConstraintName("FK_TblCotizaciones_TblRequisicionDetalle");

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblCotizaciones)
                .HasForeignKey(d => d.IdRequisicion)
                .HasConstraintName("FK_TblCotizaciones_tblRequisicion");
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

            entity.Property(e => e.IdEstatus).ValueGeneratedNever();
            entity.Property(e => e.NombreEstatus)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblFormato>(entity =>
        {
            entity.HasKey(e => e.IdFormato).HasName("PK__TblForma__A7043164A21296AF");

            entity.ToTable("TblFormato");

            entity.HasIndex(e => new { e.NumeroFormato, e.TipoFormato }, "UQ_Formato_Numero_Tipo").IsUnique();

            entity.Property(e => e.FechaFormato).HasColumnType("datetime");
            entity.Property(e => e.RutaArchivo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TipoFormato)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblFormatos)
                .HasForeignKey(d => d.IdRequisicion)
                .HasConstraintName("FK_Formato_Requisicion");
        });

        modelBuilder.Entity<TblFuentesFinanciamiento>(entity =>
        {
            entity.ToTable("TblFuentesFinanciamiento");

            entity.Property(e => e.Clave).HasMaxLength(5);
            entity.Property(e => e.Etiquetado).HasMaxLength(50);
            entity.Property(e => e.FuenteFinanciamiento).HasMaxLength(100);
            entity.Property(e => e.Tipo).HasMaxLength(50);
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
            entity.Property(e => e.NombreProvedor)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Rfc)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("RFC");
        });

        modelBuilder.Entity<TblProveedorGanador>(entity =>
        {
            entity.HasKey(e => e.IdGanador).HasName("PK__TblRequi__2CD13744A8959712");

            entity.ToTable("TblProveedorGanador");

            entity.HasIndex(e => e.IdRequisicion, "UQ_Ganador_Requi").IsUnique();

            entity.Property(e => e.FechaSeleccion).HasColumnType("datetime");
            entity.Property(e => e.Iva).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<TblRegistroDiseno>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__registro__3214EC077678AF4C");

            entity.Property(e => e.FechaSubida).HasColumnType("datetime");
            entity.Property(e => e.Ruta)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Tipo).HasMaxLength(50);

            entity.HasOne(d => d.IdConsolidadaNavigation).WithMany(p => p.TblRegistroDisenos)
                .HasForeignKey(d => d.IdConsolidada)
                .HasConstraintName("FK_TblRegistroDisenos_TblConsolidadas");

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblRegistroDisenos)
                .HasForeignKey(d => d.IdRequisicion)
                .HasConstraintName("FK_TblRegistroDisenos_tblRequisicion");
        });

        modelBuilder.Entity<TblRequisicion>(entity =>
        {
            entity.HasKey(e => e.IdRequisicion);

            entity.ToTable("tblRequisicion");

            entity.Property(e => e.IdRequisicion).HasColumnName("idRequisicion");
            entity.Property(e => e.ConsolidadaId).HasColumnName("ConsolidadaID");
            entity.Property(e => e.Correo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaModificacion).HasColumnType("datetime");
            entity.Property(e => e.Ff)
                .HasMaxLength(100)
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
            entity.Property(e => e.NumApi)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.NumPedido)
                .HasMaxLength(15)
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
            entity.Property(e => e.TipoServicio)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UsoEspecifico).IsUnicode(false);

            entity.HasOne(d => d.Consolidada).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.ConsolidadaId)
                .HasConstraintName("FK_Requisicion_Consolidada");

            entity.HasOne(d => d.IdAdjudicacionNavigation).WithMany(p => p.TblRequisicions)
                .HasForeignKey(d => d.IdAdjudicacion)
                .HasConstraintName("FK_tblRequisicion_TblAdjudicacion");

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

            entity.HasOne(d => d.IdUsuarioFinanNavigation).WithMany(p => p.TblRequisicionIdUsuarioFinanNavigations)
                .HasForeignKey(d => d.IdUsuarioFinan)
                .HasConstraintName("FK_tblRequisicion_TblUsuario2");

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

            entity.HasOne(d => d.IdArticuloNavigation).WithMany(p => p.TblRequisicionDetalles)
                .HasForeignKey(d => d.IdArticulo)
                .HasConstraintName("FK_TblRequisicionDetalle_tblArticulos");

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblRequisicionDetalles)
                .HasForeignKey(d => d.IdRequisicion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tblRequisicionDetalle_tblRequisicion");
        });

        modelBuilder.Entity<TblRequisicionDetalleMovimiento>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento).HasName("PK__TblRequi__881A6AE081B2055F");

            entity.ToTable("TblRequisicionDetalleMovimiento");

            entity.Property(e => e.FechaConfirmacion).HasColumnType("datetime");
            entity.Property(e => e.FechaMovimiento)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Observacion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdFormatoNavigation).WithMany(p => p.TblRequisicionDetalleMovimientos)
                .HasForeignKey(d => d.IdFormato)
                .HasConstraintName("FK_Movimiento_Formato");

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblRequisicionDetalleMovimientos)
                .HasForeignKey(d => d.IdRequisicion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movimiento_Requisicion");

            entity.HasOne(d => d.IdRequisicionDetalleNavigation).WithMany(p => p.TblRequisicionDetalleMovimientos)
                .HasForeignKey(d => d.IdRequisicionDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movimiento_Detalle");
        });

        modelBuilder.Entity<TblRequisicionDetalleMunicipio>(entity =>
        {
            entity.HasKey(e => e.IdDetalleMunicipio).HasName("PK__TblRequi__2EEA9C5E8FCB48C6");

            entity.ToTable("TblRequisicionDetalleMunicipio");

            entity.Property(e => e.Cantidad).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.IdMunicipioNavigation).WithMany(p => p.TblRequisicionDetalleMunicipios)
                .HasForeignKey(d => d.IdMunicipio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetalleMunicipio_Municipio");

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblRequisicionDetalleMunicipios)
                .HasForeignKey(d => d.IdRequisicion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetalleMunicipio_Requisicion");

            entity.HasOne(d => d.IdRequisicionDetalleNavigation).WithMany(p => p.TblRequisicionDetalleMunicipios)
                .HasForeignKey(d => d.IdRequisicionDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetalleMunicipio_Detalle");
        });

        modelBuilder.Entity<TblRol>(entity =>
        {
            entity.ToTable("TblRol");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TblTablaApiHistorial>(entity =>
        {
            entity.HasKey(e => e.IdHistorial).HasName("PK__TblTabla__9CC7DBB4AB0CBD01");

            entity.ToTable("TblTablaApiHistorial");

            entity.Property(e => e.FechaGeneracion)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Observacion).HasMaxLength(500);

            entity.HasOne(d => d.IdRequisicionNavigation).WithMany(p => p.TblTablaApiHistorials)
                .HasForeignKey(d => d.IdRequisicion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TablaApiHistorial_Requisicion");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.TblTablaApiHistorials)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TablaApiHistorial_Usuario");
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