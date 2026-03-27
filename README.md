# SQL File API (.NET)

Servicio REST mínimo con Swagger que expone **un solo método** para devolver un archivo SQL de ejemplo.

## Endpoint

- `GET /api/sql/scripts/example`
  - Retorna un archivo `example-procedure.sql` (`text/plain`) con un procedimiento almacenado de ejemplo.

## Swagger

Al ejecutar la API:

- `https://localhost:<puerto>/swagger`

## Ejecutar

```bash
dotnet run --project ./SqlProcedureDeployer.csproj
```

## Ejemplo de consumo

### cURL

```bash
curl -L -o example-procedure.sql "https://localhost:5001/api/sql/scripts/example"
```

### Respuesta (contenido del archivo)

```sql
CREATE OR ALTER PROCEDURE dbo.usp_ObtenerClientes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 100 *
    FROM dbo.Clientes;
END
GO
```
