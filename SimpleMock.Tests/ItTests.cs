namespace SimpleMock.Tests;

public class ItTests
{
    private Mock<IWorker> worker;

    [SetUp]
    public void Setup()
    {
        worker = new Mock<IWorker>();
    }

    [Test]
    public void TestSetup()
    {
        worker.Setup(w => w.DoSomethingStringy(It.IsAny<int>())).Returns("Hello");
        var result = worker.MockObject.DoSomethingStringy(7);
        Assert.That(result, Is.EqualTo("Hello"));
    }

    public bool GetTrue()
    {
        return true;
    }

    [Test]
    public void TestCallCount()
    {
        Assert.That(worker.GetCallCount(w => w.DoSomething(0, "", false)), Is.Zero);
        for (var i = 0; i < 3; i++)
        {
            worker.MockObject.DoSomething(i, $"Param: {i}", i % 2 == 0);
        }
        Assert.Multiple(() => {
            Assert.That(worker.GetCallCount(w => w.DoSomething(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())), Is.EqualTo(3));
            Assert.That(worker.GetCallCount(w => w.DoSomething(It.IsAny<int>(), It.IsAny<string>(), true)), Is.EqualTo(2));
            Assert.That(worker.GetCallCount(w => w.DoSomething(It.IsAny<int>(), It.IsAny<string>(), GetTrue())), Is.EqualTo(2));
            Assert.That(worker.GetCallCount(w => w.DoSomething(0, "Param: 0", true)), Is.EqualTo(1));
            Assert.That(worker.GetCallCount(w => w.DoSomething(1, "Param: 1", false)), Is.EqualTo(1));
            Assert.That(worker.GetCallCount(w => w.DoSomething(2, "Param: 2", true)), Is.EqualTo(1));
        });
        var parameters = worker.GetCallParameters(w => w.DoSomething(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>()), 1);
        Assert.Multiple(() =>
        {
            Assert.That(parameters.Length, Is.EqualTo(3));
            Assert.That(parameters[0], Is.EqualTo(1));
            Assert.That(parameters[1], Is.EqualTo("Param: 1"));
            Assert.That(parameters[2], Is.False);
        });
    }

}
