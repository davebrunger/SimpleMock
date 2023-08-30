namespace SimpleMock.Tests;

public class MockTests
{
    private Mock<IWorker> worker;

    [SetUp]
    public void Setup()
    {
        worker = new Mock<IWorker>();
    }

    [Test]
    public void TestInt()
    {
        worker.Setup(w => w.DoSomething(1, "2", false)).Returns(10);
        var result = worker.MockObject.DoSomething(1, "2", false);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void TestString()
    {
        worker.Setup(w => w.DoSomethingStringy(7)).Returns("Hello");
        var result = worker.MockObject.DoSomethingStringy(7);
        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void TestProperty()
    {
        worker.Setup(w => w.Height).Returns(51);
        var result = worker.MockObject.Height;
        Assert.That(result, Is.EqualTo(51));
    }

    [Test]
    public void TestAll()
    {
        worker
            .Setup(w => w.DoSomething(8, "9", true))
            .Returns(-23)
            .Setup(w => w.DoSomethingStringy(21))
            .Returns("Goodbye")
            .Setup(w => w.Height)
            .Returns(-12);

        Assert.Multiple(() =>
        {
            Assert.That(worker.MockObject.DoSomething(12, "Hello", false), Is.EqualTo(-23));
            Assert.That(worker.MockObject.DoSomethingStringy(51), Is.EqualTo("Goodbye"));
            Assert.That(worker.MockObject.Height, Is.EqualTo(-12));
        });
    }

    [Test]
    public void TestMultiple()
    {
        var worker1 = new Mock<IWorker>();
        var worker2 = new Mock<IWorker>();
        var worker3 = new Mock<IWorker>();

        worker1.Setup(w => w.DoSomething(8, "9", true)).Returns(31);
        worker2.Setup(w => w.DoSomething(8, "9", true)).Returns(93);
        worker3.Setup(w => w.DoSomething(8, "9", true)).Returns(76);

        Assert.Multiple(() =>
        {
            Assert.That(worker1.MockObject.DoSomething(9, "9", true), Is.EqualTo(31));
            Assert.That(worker2.MockObject.DoSomething(10, "9", true), Is.EqualTo(93));
            Assert.That(worker3.MockObject.DoSomething(11, "9", true), Is.EqualTo(76));
        });
    }

    [Test]
    public void TestNoSetup()
    {
        Assert.Multiple(() =>
        {
            Assert.That(worker.MockObject.DoSomething(8, "9", true), Is.Zero);
            Assert.That(worker.MockObject.DoSomethingStringy(7), Is.Null);
            Assert.That(worker.MockObject.Height, Is.Zero);
        });
    }

    [Test]
    public void TestCallCount()
    {
        Assert.That(worker.GetCallCount(w => w.DoSomething(0, "", false)), Is.Zero);
        for (var i = 0; i < 67; i++)
        {
            worker.MockObject.DoSomething(i, $"Param: {i}", i % 2 == 0);
        }
        Assert.That(worker.GetCallCount(w => w.DoSomething(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())), Is.EqualTo(67));
        var parameters = worker.GetCallParameters(w => w.DoSomething(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>()), 11);
        Assert.Multiple(() =>
        {
            Assert.That(parameters.Length, Is.EqualTo(3));
            Assert.That(parameters[0], Is.EqualTo(11));
            Assert.That(parameters[1], Is.EqualTo("Param: 11"));
            Assert.That(parameters[2], Is.False);
        });
    }
}