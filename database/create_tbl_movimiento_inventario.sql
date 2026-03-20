/*
  Crea la tabla TblMovimientoInventario para Kardex.
*/

IF OBJECT_ID('dbo.TblMovimientoInventario', 'U') IS NULL
BEGIN
  CREATE TABLE dbo.TblMovimientoInventario
  (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TblMovimientoInventario PRIMARY KEY,
    IdInventario INT NOT NULL,
    TipoMovimiento VARCHAR(1) NOT NULL, -- I/E/A
    Cantidad INT NOT NULL,
    Fecha DATETIME NOT NULL CONSTRAINT DF_TblMovimientoInventario_Fecha DEFAULT (GETDATE()),
    Motivo VARCHAR(1000) NOT NULL,
    IdRequisicion INT NULL,
    IdRequisicionDetalle INT NULL,
    IdUsuario INT NOT NULL,
    Anulado BIT NOT NULL CONSTRAINT DF_TblMovimientoInventario_Anulado DEFAULT (0),
    MotivoAnulacion VARCHAR(1000) NULL,
    FechaAnulacion DATETIME NULL,
    IdUsuarioAnula INT NULL,
    CONSTRAINT FK_TblMovimientoInventario_tblInventario
      FOREIGN KEY (IdInventario) REFERENCES dbo.tblInventario(Id)
  );

  CREATE INDEX IX_TblMovimientoInventario_IdInventario_Fecha
    ON dbo.TblMovimientoInventario(IdInventario, Fecha DESC);
END

