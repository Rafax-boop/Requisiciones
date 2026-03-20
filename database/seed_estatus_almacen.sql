/*
  Seed de estatus para Almacén.
  En tu TblEstatus ya existen:
    4 = REQUISICION AUTORIZADA
    5 = REQUISICION RECHAZADA
  Aquí solo agregamos:
    10 = REQUISICION AUTORIZADA PARCIAL
*/

IF NOT EXISTS (SELECT 1 FROM TblEstatus WHERE IdEstatus = 10)
BEGIN
  INSERT INTO TblEstatus (IdEstatus, NombreEstatus, Actvio)
  VALUES (10, 'REQUISICION AUTORIZADA PARCIAL', 1);
END
