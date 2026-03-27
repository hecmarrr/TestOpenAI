namespace SqlProcedureDeployer.Services;

public interface ISqlScriptStore
{
    Task<StoredSqlScript> SaveAsync(string fileName, Stream sqlFileStream, CancellationToken cancellationToken);
    bool TryGet(Guid scriptId, out StoredSqlScript? script);
}
