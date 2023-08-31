# SimpleMock
SimpleMock is a very simple mocking framework, something that I have always wanted to try to write but I was prompted to have a go after a discussion at work.

## Usage
The examples below use the following interface as an example

```C#
public interface IWorker
{
    int DoSomething(int anInt, string aString, bool aBool);
    string DoSomethingStringy(int anInt);
    int Height { get; }
}
```

### Creating the Mock
```C#
var workerMock = new Mock<IWorker>();
```

### Mocking a Method
```C#
workerMock
    .Setup(w => w.DoSomething(0, "", false))
    .Returns(5);
```

The following two code blocks will return 5 for method calls that passed an empty string for the second parameter regardless of the values of the other two parameters.

```C#
workerMock
    .Setup(w => w.DoSomething(It.IsAny<int>(), "", It.IsAny<bool>()))
    .Retruns(5);
```
```C#
workerMock
    .Setup(w => w.DoSomething(It.IsAny<int>(), It.Is<string>(s => s == ""), It.IsAny<bool>()))
    .Retruns(5);
```

### Mocking a Readable Property
```C#
workerMock
    .Setup(w => w.Height)
    .Returns(10);
```

### Supplying the Mock to a Dependant
```C#
var dependant = new Dependant(workerMock.MockObject);
```

### Getting the Call Count
```C#
workerMock.GetCallCount(w => w.DoSomething(0, "", false));
```

The following code will only match method calls that passed an empty string for the second parameter regardless of the values of the other two parameters.

```C#
workerMock.GetCallCount(w => w.DoSomething(It.IsAny<int>(), "", It.IsAny<bool>()));
```

### Getting the Parameter Values of a Specific Call
```C#
workerMock.GetCallParameters(w => w.DoSomething(0, "", false), 7);
```

**N.B.** The index is zero-based.

The following code will only match method calls that passed 59 for the first parameter regardless of the values of the other two parameters.

```C#
workerMock.GetCallParameters(w => w.DoSomething(59, It.IsAny<string>(), It.IsAny<bool>()), 2);
```

## Limitations
This is only a very simple and niave implementation so there are a number of limitations, amongst which are:
* Can only mock interfaces
* No support for callbacks