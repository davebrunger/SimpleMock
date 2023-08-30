# SimpleMock
SimpleMock is a very simple mocking framework, something that I have always wanted to try to write but I was prompted to have a go after a discussion at work.

## Usage
The examples below use the following interface as an example

    public interface IWorker
    {
        int DoSomething(int anInt, string aString, bool aBool);
        string DoSomethingStringy(int anInt);
        int Height { get; }
    }

### Creating the Mock
    var workerMock = new Mock<IWorker>();

### Mocking a Method
    
    workerMock
        .Setup(w => w.DoSomething(0, "", false))
        .Returns(5);

**N.B.** Currently the values supplied as parameters are ignored. In this example, any call to `DoSomething` will return 5.

### Mocking a Readable Property

    workerMock
        .Setup(w => w.Height)
        .Returns(10);

### Supplying the Mock to a Dependant

    var dependant = new Dependant(workerMock.Object);

## Limitations
This is only a very simple and niave implementation so there are a number of limitations, amongst which are:
* Can only mock interfaces
* No support for callbacks
* Argument values passed in the `Setup` method are ignored.
* No verification on number of calls to mock.