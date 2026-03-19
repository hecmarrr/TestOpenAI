# TransactionSyncService

Solución completa en C# para mover registros transaccionales desde **SQL Server local** hacia una **base SQL Server centralizada** usando **REST**, transmitiendo cada registro en **XML**.

Incluye dos componentes:

1. **`TransactionSyncService`**: Worker/servicio de Windows o consola que lee tablas transaccionales y envía cada registro.
2. **`TransactionSyncCentralApi`**: API REST receptora que guarda los registros en SQL Server central con idempotencia.

## Arquitectura

### 1) Emisor (`TransactionSyncService`)

- Lee varias tablas configuradas.
- Ejecuta la extracción de forma dinámica con `SELECT * FROM <tabla>`.
- Usa `watermark + primary key` como cursor para reanudación segura.
- Construye un sobre XML que incluye la tabla, la consulta origen y el registro.
- Envía cada registro al API central usando `X-Idempotency-Key`.
- Mantiene estado local en SQL Server (`SyncDeliveryLog` y `SyncCheckpoint`).

### 2) Receptor (`TransactionSyncCentralApi`)

- Expone `POST /api/inbox/records`.
- Recibe el XML del `SyncEnvelope`.
- Guarda el XML recibido en la tabla central `IntegrationInbox`.
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
- `Tables`: configuración de tablas a replicar.

Ejemplo:

```json
{
  "TableName": "dbo.Facturas",
  "PrimaryKeyColumn": "IdFactura",
  "WatermarkColumn": "FechaUltimaActualizacion",
  "Route": "api/inbox/records",
  "Enabled": true,
  "PayloadFormat": "Xml"
}
```

## Formato XML transmitido

Cada envío se transmite como XML, por ejemplo:

```xml
<SyncEnvelope>
  <sourceTable>dbo.Facturas</sourceTable>
  <sourceQuery><![CDATA[SELECT * FROM dbo.Facturas WHERE IdFactura = '123']]></sourceQuery>
  <primaryKeyValue>123</primaryKeyValue>
  <watermarkUtc>2026-03-19T10:00:00.0000000Z</watermarkUtc>
  <fingerprint>...</fingerprint>
  <payloadFormat>Xml</payloadFormat>
  <record>
    <Record table="dbo.Facturas" primaryKey="123" watermarkUtc="2026-03-19T10:00:00.0000000Z">
      <IdFactura>123</IdFactura>
      <Serie>A</Serie>
      <Total>150.00</Total>
    </Record>
  </record>
</SyncEnvelope>
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
