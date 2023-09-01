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

    private static bool GetTrue()
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
            Assert.That(parameters, Has.Length.EqualTo(3));
            Assert.That(parameters[0], Is.EqualTo(1));
            Assert.That(parameters[1], Is.EqualTo("Param: 1"));
            Assert.That(parameters[2], Is.False);
        });
    }

    [Test]
    public void TestCall()
    {
        worker.Setup(w => w.DoSomething(15, "Greetings", true)).Returns(1);
        worker.Setup(w => w.DoSomething(15, It.IsAny<string>(), true)).Returns(2);
        worker.Setup(w => w.DoSomething(15, It.IsAny<string>(), It.IsAny<bool>())).Returns(3);
        worker.Setup(w => w.DoSomething(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(4);
        Assert.Multiple(() => {
            Assert.That(worker.MockObject.DoSomething(15, "Greetings", true), Is.EqualTo(1));
            Assert.That(worker.MockObject.DoSomething(15, "Bonjour", true), Is.EqualTo(2));
            Assert.That(worker.MockObject.DoSomething(15, "Hi", false), Is.EqualTo(3));
            Assert.That(worker.MockObject.DoSomething(17, "Greetings", true), Is.EqualTo(4));
            Assert.That(worker.MockObject.DoSomething(19, "Hello", false), Is.EqualTo(4));
        });
    }

    [Test]
    public void TestIs()
    {
        worker.Setup(w => w.DoSomethingStringy(It.Affirms<int>(a => a == 7))).Returns("Hello");
        Assert.Multiple(() => {
            Assert.That(worker.MockObject.DoSomethingStringy(8), Is.Null);
            Assert.That(worker.MockObject.DoSomethingStringy(7), Is.EqualTo("Hello"));
        });

    }
}
