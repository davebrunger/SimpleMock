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

        public object? Call(T obj, MethodInfo info)
        {
            return Mock<T>.Call(obj, info);
        }
    }

    private static readonly Dictionary<(T, MethodInfo), object?> returnValues = new();
    private static readonly Caller caller = new();
    private static readonly TypeGenerator<T> typeGenerator = new(caller);

    public T MockObject { get; }

    public Mock()
    {
        MockObject = (T)Activator.CreateInstance(typeGenerator.Type)!;
    }

    protected static object? Call(T obj, MethodInfo info)
    {
        if (returnValues.ContainsKey((obj, info)))
        {
            return returnValues[(obj, info)];
        }
        if (info.ReturnType.IsValueType)
        {
            return Activator.CreateInstance(info.ReturnType);
        }    
        return null;
    }

    private void SetReturn(MethodInfo info, object? result)
    {
        returnValues[(MockObject!, info)] = result;
    }

    public SetupResult<TResult> Setup<TResult>(Expression<Func<T, TResult>> expression)
    {
        if (expression.Body is MethodCallExpression methodCallExpression)
        {
            return new SetupResult<TResult>(this, methodCallExpression.Method);
        }
        if (expression.Body is MemberExpression memberExpression
            && memberExpression.Member is PropertyInfo property
            && property.CanRead)
        {
            return new SetupResult<TResult>(this, property.GetGetMethod()!);
        }
        throw new Exception("Only method call and get property expressions can be setup");
    }
}