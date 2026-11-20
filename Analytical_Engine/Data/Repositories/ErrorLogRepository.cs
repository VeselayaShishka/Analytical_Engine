using Analytical_Engine.Data.Repositories.Interfaces;
using Analytical_Engine.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analytical_Engine.Data.Repositories;

public class ErrorLogRepository : Repository<ErrorLog>, IErrorLogRepository
{
    public ErrorLogRepository(ApplicationDbContext context) : base(context)
    {

    }
}
