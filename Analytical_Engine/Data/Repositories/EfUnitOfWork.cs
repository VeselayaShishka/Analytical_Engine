using Microsoft.EntityFrameworkCore.Storage;
using Analytical_Engine.Data.Repositories.Interfaces;

namespace Analytical_Engine.Data.Repositories;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _users;
    private IDatasetRepository? _datasets;
    private IDatasetVersionRepository? _datasetVersions;
    private IDatasetColumnRepository? _datasetColumns;
    private IDataRecordRepository? _dataRecords;
    private IErrorLogRepository? _errorLogs;

    public EfUnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IDatasetRepository Datasets =>
        _datasets ??= new DatasetRepository(_context);

    public IDatasetVersionRepository DatasetVersions =>
        _datasetVersions ??= new DatasetVersionRepository(_context);

    public IDatasetColumnRepository DatasetColumns =>
        _datasetColumns ??= new DatasetColumnRepository(_context);

    public IDataRecordRepository DataRecords =>
        _dataRecords ??= new DataRecordRepository(_context);

    public IErrorLogRepository ErrorLogs =>
        _errorLogs ??= new ErrorLogRepository(_context);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }

    public void ClearTracker()
    {
        _context.ChangeTracker.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction == null)
            return;

        var transaction = _transaction;

        try
        {
            await transaction.RollbackAsync();
        }
        finally
        {
            try
            {
                await transaction.DisposeAsync();
            }
            finally
            {
                _transaction = null;
            }
        }
    }
}