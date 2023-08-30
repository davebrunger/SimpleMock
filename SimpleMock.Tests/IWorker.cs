namespace SimpleMock.Tests;

public interface IWorker
{
    int DoSomething(int anInt, string aString, bool aBool);
    string DoSomethingStringy(int anInt);
    int Height { get; }
}
