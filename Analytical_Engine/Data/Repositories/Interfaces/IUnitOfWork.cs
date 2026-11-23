namespace Analytical_Engine.Data.Repositories.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository Users { get; }
    IDatasetRepository Datasets { get; }
    IDatasetVersionRepository DatasetVersions { get; }
    IDatasetColumnRepository DatasetColumns { get; }
    IDataRecordRepository DataRecords { get; }
    IErrorLogRepository ErrorLogs { get; }
    IReportRepository Reports { get; }

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    void ClearTracker();
}