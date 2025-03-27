using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ExpertCs.EfCore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpertCs.EfCore;

public static class DbContextExtensions
{
    public static LogLevel LogLevel { get; set; } = LogLevel.Debug;

    public static bool CheckFound { get; set; }

    private static void LogData(this ILogger? logger, string message, params object?[] args)
    {
        if (logger == null)
            return;
        logger.Log(LogLevel, message, args);
    }

    private static void LogData(this ILogger? logger, object arg, int result, [CallerMemberName] string? method = default)
        => logger.LogData("{method}({arg}) result={result}", method, arg, result);

    public static IQueryable<T> GetQuery<T>(
        this DbContext dbContext,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTrackingWithIdentityResolution)
        where T : BaseEntity
    {
        var ret = dbContext.Set<T>();
        return tracking switch
        {
            QueryTrackingBehavior.TrackAll => ret.AsTracking(),
            QueryTrackingBehavior.NoTracking => ret.AsNoTracking(),
            QueryTrackingBehavior.NoTrackingWithIdentityResolution => ret.AsNoTrackingWithIdentityResolution(),
            _ => throw new ArgumentOutOfRangeException(nameof(tracking)),
        };
    }

    public static async Task<T?> GetById<T>(
        this DbContext dbContext,
        object id,
        CancellationToken token,
        Func<IQueryable<T>, IQueryable<T>>? include = default,
        QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTrackingWithIdentityResolution)
        where T : IdEntity
    {
        var q = dbContext.GetQuery<T>(tracking);
        q = include?.Invoke(q) ?? q;
        var ret = await q.Where(x => x.Id.Equals(id)).FirstOrDefaultAsync(token);
        if (CheckFound && ret == null)
            throw new NotFoundExcetion(id, typeof(T));
        return ret;
    }

    public static async Task<T?> AddItem<T>(
        this DbContext dbContext,
        T item,
        CancellationToken token,
        ILogger? logger = default)
        where T : BaseEntity
    {
        dbContext.Add(item);
        var ret = await dbContext.SaveChangesAsync(token);
        logger.LogData(item, ret);
        if (ret > 0)
            return item;
        return default;
    }

    public static async Task<T?> UpdateItem<T>(
        this DbContext dbContext,
        T item,
        CancellationToken token,
        ILogger? logger = default)
        where T : IdEntity
    {
        if (CheckFound)
            _ = await dbContext.GetById<T>(item.Id, token);

        dbContext.Add(item).State = EntityState.Modified;
        var ret = await dbContext.SaveChangesAsync(token);
        logger.LogData(item, ret);
        if (ret > 0)
            return item;
        return default;
    }

    public static async Task<int> DeleteItem<T>(
        this DbContext dbContext,
        object id,
        CancellationToken token,
        ILogger? logger = default)
        where T : IdEntity, new()
    {
        if (CheckFound)
            _ = await dbContext.GetById<T>(id, token);

        var item = new T { Id = id };
        dbContext.Add(item).State = EntityState.Deleted;
        var ret = await dbContext.SaveChangesAsync(token);
        logger.LogData(item, ret);
        return ret;
    }

    /// <summary>
    /// Удаление по ключу <see cref="int"/> через <see cref="DeleteItemsExecute"/>
    /// Не потдерживается провайдером InMemoryDatabase
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbContext"></param>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static async Task<int> DeleteItemExecute<T>(
        this DbContext dbContext,
        int id,
        CancellationToken token,
        ILogger? logger = default)
        where T : IdEntity<int>
    {
        if (CheckFound)
            _ = await dbContext.GetById<T>(id, token);

        return await dbContext.DeleteItemsExecute<T>(x => x.Id == id, token, logger);
    }

    /// <summary>
    /// Удаление через <see cref="EntityFrameworkQueryableExtensions.ExecuteDeleteAsync"/>
    /// Не потдерживается провайдером InMemoryDatabase
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dbContext">DbContext</param>
    /// <param name="where"></param>
    /// <param name="token"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static async Task<int> DeleteItemsExecute<T>(
        this DbContext dbContext,
        Expression<Func<T, bool>> where,
        CancellationToken token,
        ILogger? logger = default)
        where T : class
    {
        var ret = await dbContext.Set<T>().Where(where).ExecuteDeleteAsync(token);
        logger.LogData(where, ret);
        return ret;
    }
}
