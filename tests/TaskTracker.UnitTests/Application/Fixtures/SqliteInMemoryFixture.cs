using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Abstractions.Repositories;
using TaskTracker.Infrastructure.Persistence;
using TaskTracker.Infrastructure.Persistence.Repositories;

namespace TaskTracker.UnitTests.Application.Fixtures;

// SQLite in-memory needs the connection kept open for the schema to survive
// across DbContext operations. We open it in the ctor and dispose at the end.
public sealed class SqliteInMemoryFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Db { get; }
    public IUnitOfWork UnitOfWork { get; }
    public IUserRepository Users { get; }
    public IProjectRepository Projects { get; }
    public ITaskRepository Tasks { get; }

    public SqliteInMemoryFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();

        UnitOfWork = new UnitOfWork(Db);
        Users = new UserRepository(Db);
        Projects = new ProjectRepository(Db);
        Tasks = new TaskRepository(Db);
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
