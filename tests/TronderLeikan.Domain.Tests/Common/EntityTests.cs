using FluentAssertions;
using TronderLeikan.Domain.Common;

namespace TronderLeikan.Domain.Tests.Common;

// Konkret testklasse siden Entity er abstrakt
file sealed class TestEntity : Entity
{
    private TestEntity() { }

    public static TestEntity Create() => new() { Id = Guid.NewGuid() };

    public void RaiseDomainEvent(IDomainEvent @event) => AddDomainEvent(@event);
}

file sealed record TestEvent : IDomainEvent;

public class EntityTests
{
    [Fact]
    public void NyEntity_HarIngenDomeneHendelser()
    {
        var entity = TestEntity.Create();
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddDomainEvent_LeggTilHendelse()
    {
        var entity = TestEntity.Create();
        var @event = new TestEvent();

        entity.RaiseDomainEvent(@event);

        entity.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(@event);
    }

    [Fact]
    public void ClearDomainEvents_FjernerAlleHendelser()
    {
        var entity = TestEntity.Create();
        entity.RaiseDomainEvent(new TestEvent());

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }
}
