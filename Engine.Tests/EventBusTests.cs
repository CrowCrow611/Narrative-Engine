using Engine.Events;

namespace Engine.Tests;

public class EventBusTests {
    
    [Fact]
    public void Subscribe_RPE() {
        var bus = new EventBus();
        string? recieved = null;

        bus.Subscribe<NodeEnteredEvent>(e => recieved = e.NodeId);
        bus.Publish(new NodeEnteredEvent("tavern"));

        Assert.Equal("tavern", recieved);
    }

    [Fact] 
    public void Unsubscribe_STE() {
        var bus = new EventBus();
        int count = 0;

        Action<NodeEnteredEvent> handler = e => count++;
        bus.Subscribe(handler);
        bus.Publish(new NodeEnteredEvent("a"));
        bus.Unsubscribe(handler);
        bus.Publish(new NodeEnteredEvent("b"));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Publish_NoSubscribers_DNT() {
        var bus = new EventBus();
        var ex = Record.Exception(() => bus.Publish(new NodeEnteredEvent("x")));
        Assert.Null(ex);
    }

    [Fact]
    public void MultipleSubscribvers_ARE() {
        var bus = new EventBus();
        int count = 0;

        bus.Subscribe<NodeEnteredEvent>(_ => count++);
        bus.Subscribe<NodeEnteredEvent>(_ => count++);
        bus.Publish(new NodeEnteredEvent("x"));

        Assert.Equal(2, count);
    }
}