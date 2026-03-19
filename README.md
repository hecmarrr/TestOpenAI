# TransactionSyncService

Solución completa en C# para mover registros transaccionales desde **SQL Server local** hacia una **base SQL Server centralizada** usando **REST** entre ambos extremos.

Incluye dos componentes:

1. **`TransactionSyncService`**: Worker/servicio de Windows o consola que lee tablas transaccionales y envía cada registro.
2. **`TransactionSyncCentralApi`**: API REST receptora que guarda los registros en SQL Server central con idempotencia.

## Arquitectura

### 1) Emisor (`TransactionSyncService`)

- Lee varias tablas configuradas.
- Usa `watermark + primary key` como cursor para reanudación segura.
- Serializa cada fila a JSON o XML.
- Envuelve cada fila en un `RecordEnvelope`.
- Envía cada registro al API central usando `X-Idempotency-Key`.
- Mantiene estado local en SQL Server (`SyncDeliveryLog` y `SyncCheckpoint`).

### 2) Receptor (`TransactionSyncCentralApi`)

- Expone `POST /api/inbox/records`.
- Recibe el `RecordEnvelope`.
- Guarda el payload recibido en la tabla central `IntegrationInbox`.
- Evita duplicados con un `UNIQUE` por `Fingerprint`.

## Cómo se evita la duplicidad

La solución tiene protección en dos niveles:

1. **Emisor**: no reenvía registros ya entregados porque conserva checkpoint y log local.
2. **Receptor**: si el emisor reintenta, el API central responde sin volver a insertar porque el `Fingerprint` ya existe.

Esto permite reinicios, reintentos y recuperación de errores sin duplicar información.

## Requisitos

- .NET 8 SDK
- SQL Server origen
- SQL Server para estado local del servicio
- SQL Server central

## Configuración del emisor

Archivo: `src/TransactionSyncService/appsettings.json`

- `SourceConnectionString`: base transaccional.
- `LocalStateConnectionString`: base local para checkpoints y bitácora de entregas.
- `RestEndpoint`: URL base del API central.
- `BatchSize`: tamaño del lote.
- `PollingIntervalSeconds`: frecuencia de consulta.
- `Tables`: tablas a leer.

Ejemplo:

```json
{
  "TableName": "dbo.Facturas",
  "PrimaryKeyColumn": "IdFactura",
  "WatermarkColumn": "FechaUltimaActualizacion",
  "Columns": [ "IdFactura", "Serie", "Numero", "Total", "FechaUltimaActualizacion" ],
  "Route": "api/inbox/records",
  "PayloadFormat": "Json"
}
```

## Configuración del API central

Archivo: `src/TransactionSyncCentralApi/appsettings.json`

- `CentralApi:ConnectionString`: cadena de conexión a la base SQL Server centralizada.

## Ejecución local

### Emisor como consola

```bash
dotnet run --project src/TransactionSyncService
```

### API central

```bash
dotnet run --project src/TransactionSyncCentralApi
```

## Publicación del servicio de Windows

```bash
dotnet publish src/TransactionSyncService -c Release -r win-x64 --self-contained false
```

Luego:

```powershell
sc.exe create TransactionSyncService binPath= "C:\ruta\publish\TransactionSyncService.exe"
```

## Siguiente mejora recomendada

- Procesar `IntegrationInbox` hacia tablas finales mediante stored procedures por tabla.
- Agregar autenticación JWT y autorización mutua.
- Incorporar reintentos exponenciales con `Polly`.
- Añadir health checks y métricas.
