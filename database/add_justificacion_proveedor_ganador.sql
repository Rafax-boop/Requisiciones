IF COL_LENGTH('TblProveedorGanador', 'Justificacion') IS NULL
BEGIN
    ALTER TABLE TblProveedorGanador
    ADD Justificacion VARCHAR(MAX) NULL;
END
