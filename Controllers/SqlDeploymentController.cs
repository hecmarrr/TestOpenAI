using Microsoft.AspNetCore.Mvc;
using SqlProcedureDeployer.Services;

namespace SqlProcedureDeployer.Controllers;

[ApiController]
[Route("api/sql")]
public sealed class SqlDeploymentController(ISqlDeploymentService deploymentService, ILogger<SqlDeploymentController> logger) : ControllerBase
{
    [HttpPost("deploy-procedure")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> DeployProcedure([FromForm] IFormFile file, [FromForm] string? targetDatabase, CancellationToken cancellationToken)
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
            var result = await deploymentService.DeployProcedureScriptAsync(stream, file.FileName, targetDatabase, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Validación inválida para archivo {FileName}", file.FileName);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al desplegar script {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocurrió un error al ejecutar el script SQL." });
        }
    }
}
