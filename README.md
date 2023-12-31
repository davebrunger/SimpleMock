# SimpleMock
SimpleMock is a very simple mocking framework, something that I have always wanted to try to write but I was prompted to have a go after a discussion at work.

## Installation
```
dotnet add package SimpleMock
```
or
```
NuGet\Install-Package SimpleMock
```

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
    .Returns(5);
```
```C#
workerMock
    .Setup(w => w.DoSomething(It.IsAny<int>(), It.Affirms<string>(s => s == ""), It.IsAny<bool>()))
    .Returns(5);
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

### Getting the Count of Set Operations on a Read/Write Property
```C#
workerMock.GetSetCallCount(w => w.Height, () => It.IsAny<int>());
```

### Getting the Parameter Values of Set Operations on a Read/Write Property
```C#
workerMock.GetSetCallParameters(w => w.Height, () => It.Affirms<int>(h => h < 10), 7);
```

**N.B.** The index is zero-based.

## Limitations
This is only a very simple and niave implementation so there are a number of limitations, amongst which are:
* Limited support for set only parameters
  * This is much harder to implement. It involves decompiling and intepretring a call to the set code as a set
  property call is not a legal expression that can be interpreted as an expression tree.
* Can only mock interfaces, not abstract classes
* No support for callbacks
