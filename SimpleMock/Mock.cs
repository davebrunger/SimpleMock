using System.Reflection.Metadata.Ecma335;

namespace SimpleMock;

public class Mock<T>
{
    public sealed class SetupResult<TResult>
    {
        private readonly Mock<T> parent;
        private readonly MethodInfo info;
        private readonly List<Func<object, bool>> argumentPredicates;

        internal SetupResult(Mock<T> parent, MethodInfo info, List<Func<object, bool>> argumentPredicates)
        {
            this.parent = parent;
            this.info = info;
            this.argumentPredicates = argumentPredicates;
        }

        public Mock<T> Returns(TResult result)
        {
            parent.SetReturn(info, result!, argumentPredicates);
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

    private static readonly Dictionary<(T, MethodInfo), List<(List<Func<object, bool>> ArgumentPredicates, object ReturnValue)>> returnValues = new();
    private static readonly Dictionary<(T, MethodInfo), List<object[]>> callDetails = new();
    private static readonly Caller caller = new();
    private static readonly TypeGenerator<T> typeGenerator = new(caller);
    private static readonly MethodInfo equals = typeof(object).GetMethod(nameof(Equals), BindingFlags.Static | BindingFlags.Public)!;


    public T MockObject { get; }

    public Mock()
    {
        MockObject = (T)Activator.CreateInstance(typeGenerator.Type)!;
    }

    protected static object? Call(T obj, MethodInfo info, object[] parameters)
    {
        var key = (obj, info);

        // Add call to call history
        if (!callDetails.ContainsKey(key))
        {
            callDetails[key] = new List<object[]>();
        }
        callDetails[key].Add(parameters);

        // Get return value 
        if (returnValues.ContainsKey(key))
        {
            foreach (var (argumentPredicates, returnValue) in returnValues[key])
            {
                var predicateMatch = argumentPredicates
                    .Select((p, i) => (p, i))
                    .All(a => a.p(parameters[a.i]));
                if (predicateMatch)
                {
                    return returnValue;
                }
            }
        }

        // If it doesn't match a setup result return the default instance
        if (info.ReturnType.IsValueType && info.ReturnType != typeof(void))
        {
            return Activator.CreateInstance(info.ReturnType);
        }    
        return null;
    }

    private void SetReturn(MethodInfo info, object result, List<Func<object, bool>> argumentPredicates)
    {
        var key = (MockObject, info);
        if (!returnValues.ContainsKey(key))
        {
            returnValues[key] = new List<(List<Func<object, bool>> ArgumentPredicates, object ReturnValue)>();
        }
        returnValues[(MockObject, info)].Add((argumentPredicates, result));
    }

    public SetupResult<TResult> Setup<TResult>(Expression<Func<T, TResult>> expression)
    {
        var (method, argumentPredicates) = GetMethod(expression);
        return new SetupResult<TResult>(this, method, argumentPredicates);
    }

    public int GetCallCount<TResult>(Expression<Func<T, TResult>> expression)
    {
        return GetCallDetails(expression).Count();
    }

    public int GetCallCount(Expression<Action<T>> expression)
    {
        return GetCallDetails(expression).Count();
    }
    
    public int GetSetCallCount(Action<T> action)
    {
        var token = action.Method.MetadataToken;
        var handle = MetadataTokens.EntityHandle(token);


        return handle.GetHashCode();
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
    
    public object[] GetCallParameters(Expression<Action<T>> expression, int callIndex)
    {
        var callDetails = GetCallDetails(expression).ToList();
        if (callIndex < 0 || callIndex >= callDetails.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(callIndex));
        }
        return callDetails[callIndex];
    }

    private IEnumerable<object[]> GetCallDetails(LambdaExpression expression)
    {
        var (method, argumentPredicates) = GetMethod(expression);
        var key = (MockObject, method);
        if (!callDetails.ContainsKey(key))
        {
            return Enumerable.Empty<object[]>();
        }
        return callDetails[key].Where(cd => argumentPredicates.Select((p, i) => (p, i)).All(a => a.p(cd[a.i])));
    }

    private static (MethodInfo MethodInfo, List<Func<object, bool>> ArgumentPredicates) GetMethod(LambdaExpression expression)
    {
        if (expression.Body is MethodCallExpression methodCall)
        {
            var argumentPredicates = methodCall.Arguments
                .Select(a =>
                {
                    var parameter = Expression.Parameter(typeof(object));
                    if (a is MethodCallExpression methodCall && methodCall.Method.IsGenericMethod)
                    {
                        if (methodCall.Method.GetGenericMethodDefinition() == It.IsAnyMethod)
                        {
                            return o => true;
                        }
                        if (methodCall.Method.GetGenericMethodDefinition() == It.AffirmsMethod)
                        {
                            var castToT = Expression.Convert(parameter, methodCall.Type);
                            var bodyLambda = methodCall.Arguments[0] as LambdaExpression;
                            var invoke = Expression.Invoke(methodCall.Arguments[0], castToT);
                            return Expression.Lambda<Func<object, bool>>(invoke, parameter).Compile();
                        }
                    }
                    var cast = Expression.Convert(a, typeof(object));
                    var body = Expression.Call(null, equals, parameter, cast);
                    return Expression.Lambda<Func<object, bool>>(body, parameter).Compile();
                });
            return (methodCall.Method, argumentPredicates.ToList());
        }
        if (expression.Body is MemberExpression memberExpression
            && memberExpression.Member is PropertyInfo property
            && property.CanRead)
        {
            return (property.GetGetMethod()!, Enumerable.Empty<Func<object, bool>>().ToList());
        }
        throw new ArgumentException("Only method call and get property expressions can be setup", nameof(expression));
    }
}