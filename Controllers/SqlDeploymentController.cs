using Microsoft.AspNetCore.Mvc;

namespace SqlProcedureDeployer.Controllers;

[ApiController]
[Route("api/sql/scripts")]
public sealed class SqlDeploymentController : ControllerBase
{
    [HttpGet("example")]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult GetExampleSqlFile()
    {
        const string sqlScript = """
CREATE OR ALTER PROCEDURE dbo.usp_ObtenerClientes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 100 *
    FROM dbo.Clientes;
END
GO
""";

        return File(System.Text.Encoding.UTF8.GetBytes(sqlScript), "text/plain", "example-procedure.sql");
    }
}
