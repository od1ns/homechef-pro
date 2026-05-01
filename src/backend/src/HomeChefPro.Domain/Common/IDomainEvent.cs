namespace HomeChefPro.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
