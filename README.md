# SQL Procedure Deployer API (.NET)

Servicio REST en C#/.NET para recibir un archivo `.sql` con un procedimiento almacenado y ejecutarlo en SQL Server.

## ¿Qué hace?
- Expone `POST /api/sql/deploy-procedure`.
- Recibe un archivo SQL por `multipart/form-data`.
- Valida que el script sea de tipo `CREATE/ALTER PROCEDURE`.
- Separa bloques por `GO`.
- Ejecuta los bloques en transacción sobre SQL Server.

## Configuración
Editar `appsettings.json`:

```json
"ConnectionStrings": {
  "SqlServer": "Server=localhost;Database=master;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=True"
}
```

> Si envías `targetDatabase` en el formulario, el servicio usará esa base para el despliegue.

## Ejecución
```bash
dotnet restore
dotnet run
```

## Ejemplo de request
```bash
curl -X POST "https://localhost:5001/api/sql/deploy-procedure" \
  -F "file=@./MiProcedimiento.sql" \
  -F "targetDatabase=MiBaseDestino"
```

## Ejemplo de script permitido
```sql
CREATE OR ALTER PROCEDURE dbo.usp_ObtenerClientes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 100 * FROM dbo.Clientes;
END
GO
```

## Notas de seguridad
- El usuario de conexión SQL debe tener privilegios mínimos.
- El endpoint aplica validaciones básicas, pero para producción se recomienda:
  - autenticación/autorización fuerte,
  - auditoría de cambios,
  - pipeline CI/CD para despliegues.
