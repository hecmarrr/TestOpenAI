using Microsoft.AspNetCore.Mvc;
using SqlProcedureDeployer.Models;
using SqlProcedureDeployer.Services;

namespace SqlProcedureDeployer.Controllers;

[ApiController]
[Route("api/sql/scripts")]
public sealed class SqlDeploymentController(ISqlScriptStore scriptStore, ILogger<SqlDeploymentController> logger) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SqlScriptUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadScript([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Debes enviar un archivo .sql no vacío." });
        }

        if (!file.FileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Solo se aceptan archivos con extensión .sql" });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var stored = await scriptStore.SaveAsync(file.FileName, stream, cancellationToken);

            var response = new SqlScriptUploadResponse
            {
                ScriptId = stored.ScriptId,
                FileName = stored.FileName,
                UploadedAtUtc = stored.UploadedAtUtc,
                Message = "Script SQL cargado correctamente. Otro aplicativo puede consumirlo para ejecutarlo en SQL Server."
            };

            return CreatedAtAction(nameof(GetScriptById), new { scriptId = stored.ScriptId }, response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Archivo SQL inválido: {FileName}", file.FileName);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al cargar script SQL: {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocurrió un error al cargar el script SQL." });
        }
    }

    [HttpGet("{scriptId:guid}")]
    [ProducesResponseType(typeof(SqlScriptPayloadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetScriptById(Guid scriptId)
    {
        if (!scriptStore.TryGet(scriptId, out var script) || script is null)
        {
            return NotFound(new { message = "No se encontró el script solicitado." });
        }

        var payload = new SqlScriptPayloadResponse
        {
            ScriptId = script.ScriptId,
            FileName = script.FileName,
            SqlContent = script.SqlContent,
            UploadedAtUtc = script.UploadedAtUtc
        };

        return Ok(payload);
    }
}
