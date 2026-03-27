# SQL Script Upload + Consumer (.NET)

Esta solución tiene **2 aplicativos**:

1. **Servicio REST (ASP.NET Core)**: recibe un archivo `.sql` de procedimiento almacenado y lo publica para consumo.
2. **Aplicativo consumidor (Console .NET)**: consume el servicio REST y ejecuta el SQL en SQL Server.

## 1) Servicio REST con Swagger

### Funcionalidad
- `POST /api/sql/scripts`
  - Recibe `multipart/form-data` con campo `file`.
  - Guarda el contenido SQL en memoria.
  - Devuelve `scriptId` para que otro aplicativo lo consuma.
- `GET /api/sql/scripts/{scriptId}`
  - Devuelve el contenido del script SQL para el consumidor.

### Swagger
Al ejecutar la API, Swagger queda disponible en:
- `https://localhost:<puerto>/swagger`

## 2) Aplicativo consumidor que ejecuta en SQL Server

El consumidor llama al `GET /api/sql/scripts/{scriptId}`, toma el SQL, separa por `GO` y ejecuta en transacción sobre SQL Server.

## Ejecución

### API REST
```bash
dotnet run --project ./SqlProcedureDeployer.csproj
```

### Cargar script SQL desde cliente REST
```bash
curl -X POST "https://localhost:5001/api/sql/scripts" \
  -F "file=@./MiProcedimiento.sql"
```

Respuesta esperada (ejemplo):
```json
{
  "scriptId": "7f5b1d3c-d3e5-4c23-baf8-4cc8a6f9d7f0",
  "fileName": "MiProcedimiento.sql",
  "uploadedAtUtc": "2026-03-27T12:00:00+00:00",
  "message": "Script SQL cargado correctamente. Otro aplicativo puede consumirlo para ejecutarlo en SQL Server."
}
```

### Consumir y ejecutar en SQL Server
```bash
dotnet run --project ./SqlScriptConsumer/SqlScriptConsumer.csproj -- \
  "https://localhost:5001" \
  "7f5b1d3c-d3e5-4c23-baf8-4cc8a6f9d7f0" \
  "Server=localhost;Database=master;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=True" \
  "MiBaseDestino"
```

## Notas
- El almacenamiento del script en la API es en memoria (demo). Para producción usar persistencia (SQL/Blob/Queue).
- Asegurar autenticación/autorización en la API.
- El consumidor ejecuta SQL recibido; usar controles de seguridad y validaciones adicionales para entornos productivos.
