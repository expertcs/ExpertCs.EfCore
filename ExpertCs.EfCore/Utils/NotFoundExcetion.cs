namespace ExpertCs.EfCore;

public class NotFoundExcetion : Exception
{
    public NotFoundExcetion(object id, Type type)
        : base($"Entity of type {type.Name} with id {id} not found") { }
}
