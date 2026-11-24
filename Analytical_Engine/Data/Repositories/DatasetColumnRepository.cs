using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analytical_Engine.Data.Repositories;

public class DatasetColumnRepository : Repository<DatasetColumn>, IDatasetColumnRepository
{
    public DatasetColumnRepository(ApplicationDbContext context) : base(context) { }
}