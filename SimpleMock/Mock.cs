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
    private static readonly MethodInfo equals = typeof(object).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public)!;


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
        var (method, _) = GetMethod(expression);
        return new SetupResult<TResult>(this, method);
    }

    public int GetCallCount<TResult>(Expression<Func<T, TResult>> expression)
    {
        return GetCallDetails(expression).Count();
    }

    public object[] GetCallParameters<TResult>(Expression<Func<T, TResult>> expression, int callIndex)
    {
        var callDetails = GetCallDetails(expression).ToList();
        if (callIndex < 0 || callIndex >= callDetails.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(callIndex));
        }
        return callDetails[callIndex];
    }

    private IEnumerable<object[]> GetCallDetails<TResult>(Expression<Func<T, TResult>> expression)
    {
        var (method, isProperty) = GetMethod(expression);
        var key = (MockObject, method);
        if (!callDetails.ContainsKey(key))
        {
            return Enumerable.Empty<object[]>();
        }
        if (isProperty)
        {
            return callDetails[key];
        }
        var argumentPredicates = GetArgumentPredicates((expression.Body as MethodCallExpression)!).ToList();
        return callDetails[key].Where(cd => argumentPredicates.Select((p, i) => (p, i)).All(a => a.p(cd[a.i])));
    }

    private static IEnumerable<Func<object, bool>> GetArgumentPredicates(MethodCallExpression methodCall)
    {
        return methodCall.Arguments
            .Select(a =>
            {
                if (a is MethodCallExpression methodCall
                    && methodCall.Method.IsGenericMethod
                    && methodCall.Method.GetGenericMethodDefinition() == It.IsAnyMethod)
                {
                    return o => true;
                }
                var parameter = Expression.Parameter(typeof(object));
                var cast = Expression.Convert(a, typeof(object));
                var body = Expression.Call(null, equals, parameter, cast);
                return Expression.Lambda<Func<object, bool>>(body, parameter).Compile();
            });
    }

    private static (MethodInfo MethodInfo, bool IsProperty) GetMethod<TResult>(Expression<Func<T, TResult>> expression)
    {
        if (expression.Body is MethodCallExpression methodCallExpression)
        {
            return (methodCallExpression.Method, false);
        }
        if (expression.Body is MemberExpression memberExpression
            && memberExpression.Member is PropertyInfo property
            && property.CanRead)
        {
            return (property.GetGetMethod()!, true);
        }
        throw new ArgumentException("Only method call and get property expressions can be setup", nameof(expression));
    }
}