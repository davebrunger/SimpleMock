namespace SimpleMock;

public class Mock<T>
{
    public sealed class SetupResult<TResult>
    {
        private readonly Mock<T> parent;
        private readonly MethodInfo info;

        internal SetupResult(Mock<T> parent, MethodInfo info)
        {
            this.parent = parent;
            this.info = info;
        }

        public Mock<T> Returns(TResult result)
        {
            parent.SetReturn(info, result);
            return parent;
        }
    }

    public sealed class Caller
    {
        internal Caller()
        {
        }

        public object? Call(T obj, MethodInfo info, object[] parameters)
        {
            return Mock<T>.Call(obj, info, parameters);
        }
    }

    private static readonly Dictionary<(T, MethodInfo), object?> returnValues = new();
    private static readonly Dictionary<(T, MethodInfo), List<object[]>> callDetails = new();
    private static readonly Caller caller = new();
    private static readonly TypeGenerator<T> typeGenerator = new(caller);

    public T MockObject { get; }

    public Mock()
    {
        MockObject = (T)Activator.CreateInstance(typeGenerator.Type)!;
    }

    protected static object? Call(T obj, MethodInfo info, object[] parameters)
    {
        var key = (obj, info);

        if (!callDetails.ContainsKey(key))
        {
            callDetails[key] = new List<object[]>();
        }
        callDetails[key].Add(parameters);
        if (returnValues.ContainsKey(key))
        {
            return returnValues[key];
        }
        if (info.ReturnType.IsValueType)
        {
            return Activator.CreateInstance(info.ReturnType);
        }    
        return null;
    }

    private void SetReturn(MethodInfo info, object? result)
    {
        returnValues[(MockObject, info)] = result;
    }

    public SetupResult<TResult> Setup<TResult>(Expression<Func<T, TResult>> expression)
    {
        return new SetupResult<TResult>(this, GetMethod(expression));
    }

    public int GetCallCount<TResult>(Expression<Func<T, TResult>> expression)
    {
        var key = (MockObject, GetMethod(expression));
        if (callDetails.ContainsKey(key))
        {
            return callDetails[key].Count;
        }
        return 0;
    }

    public object[] GetCallParameters<TResult>(Expression<Func<T, TResult>> expression, int callIndex)
    {
        var key = (MockObject, GetMethod(expression));
        if (callIndex < 0 || !callDetails.ContainsKey(key) || callIndex >= callDetails[key].Count)
        {
            throw new ArgumentOutOfRangeException(nameof(callIndex));
        }
        return callDetails[key][callIndex];
    }

    private static MethodInfo GetMethod<TResult>(Expression<Func<T, TResult>> expression)
    {
        if (expression.Body is MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method;
        }
        if (expression.Body is MemberExpression memberExpression
            && memberExpression.Member is PropertyInfo property
            && property.CanRead)
        {
            return property.GetGetMethod()!;
        }
        throw new ArgumentException("Only method call and get property expressions can be setup", nameof(expression));
    }
}