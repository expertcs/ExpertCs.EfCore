using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Reflection;
using ExpertCs.Model;
using ExpertCs.Utils;

namespace ExpertCs.EfCore.Model;

public abstract class BaseEntity
{
    private static readonly ConcurrentDictionary<Type, string?> _displayNames = new();

    private string? GenToString()
    {
        var expression = _displayNames.GetOrAdd(
            GetType(),
            t => t.GetCustomAttribute<DisplayNameAttribute>(true)?.DisplayName
                ?? t.GetCustomAttribute<DebuggerDisplayAttribute>(true)?.Value);

        if (expression == null)
            return base.ToString();

        return this.GetInterpolatedString(expression);
    }

    public override string? ToString() => new Func<string?>(GenToString).InvokeIgnoreException(_ => base.ToString());
}

[DebuggerDisplay("{GetType().Name} #{Id}")]
public abstract class IdEntity : BaseEntity, IId
{
    public object Id { get; set; } = default!;

    public override int GetHashCode() => Id?.GetHashCode() ?? 0;

    public override bool Equals(object? obj)
        => GetType().Equals(obj?.GetType())
        && obj is IdEntity be
        && Id != default
        && Id.Equals(be.Id);
}


public abstract class IdEntity<T> : IdEntity, IId<T>
    where T : IComparable, IEquatable<T>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new T Id { get => (T)base.Id; set { base.Id = value; } }

    public override bool Equals(object? obj)
        => !Id.Equals(default)
        && base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();
}
