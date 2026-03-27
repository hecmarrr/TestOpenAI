using SqlProcedureDeployer.Models;

namespace SqlProcedureDeployer.Services;

public interface ISqlDeploymentService
{
    Task<DeploySqlResponse> DeployProcedureScriptAsync(Stream sqlFileStream, string fileName, string? targetDatabase, CancellationToken cancellationToken);
}
