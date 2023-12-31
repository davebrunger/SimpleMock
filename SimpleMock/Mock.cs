﻿namespace SimpleMock;

public class Mock<T>
{
    public sealed class SetupResult<TResult>
    {
        private readonly Mock<T> parent;
        private readonly MethodInfo method;
        private readonly List<Func<object, bool>> argumentPredicates;

        internal SetupResult(Mock<T> parent, MethodInfo method, List<Func<object, bool>> argumentPredicates)
        {
            this.parent = parent;
            this.method = method;
            this.argumentPredicates = argumentPredicates;
        }

        public Mock<T> Returns(TResult result)
        {
            parent.SetReturn(method, result!, argumentPredicates);
            return parent;
        }
    }

    public sealed class Caller
    {
        internal Caller()
        {
        }

        public object? Call(T mockObject, MethodInfo method, object[] parameters)
        {
            var mockProperty = mockObject!.GetType().GetProperty(MockPropertyName)!;
            var mock = mockProperty.GetValue(mockObject) as Mock<T>;
            return mock!.Call(method, parameters);
        }
    }

    public const string MockPropertyName = "Mock";

    private static readonly Caller caller = new();
    private static readonly TypeGenerator<T> typeGenerator = new(caller, MockPropertyName);
    private static readonly MethodInfo equals = typeof(object).GetMethod(nameof(Equals), BindingFlags.Static | BindingFlags.Public)!;
    
    private readonly Dictionary<MethodInfo, List<(List<Func<object, bool>> ArgumentPredicates, object ReturnValue)>> returnValues = new();
    private readonly Dictionary<MethodInfo, List<object[]>> callDetails = new();

    public T MockObject { get; }

    public Mock()
    {
        MockObject = (T)Activator.CreateInstance(typeGenerator.Type, this)!;
    }

    protected object? Call(MethodInfo method, object[] parameters)
    {
        // Add call to call history
        if (!callDetails.ContainsKey(method))
        {
            callDetails[method] = new List<object[]>();
        }
        callDetails[method].Add(parameters);

        // Get return value 
        if (returnValues.ContainsKey(method))
        {
            foreach (var (argumentPredicates, returnValue) in returnValues[method])
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
        if (method.ReturnType.IsValueType && method.ReturnType != typeof(void))
        {
            return Activator.CreateInstance(method.ReturnType);
        }    
        return null;
    }

    private void SetReturn(MethodInfo method, object result, List<Func<object, bool>> argumentPredicates)
    {
        if (!returnValues.ContainsKey(method))
        {
            returnValues[method] = new List<(List<Func<object, bool>> ArgumentPredicates, object ReturnValue)>();
        }
        returnValues[method].Add((argumentPredicates, result));
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

    public int GetSetCallCount<TValue>(Expression<Func<T, TValue>> expression, Expression<Func<TValue>> value)
    {
        return GetCallDetails(expression, value).Count();
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

    public TValue GetSetCallParameters<TValue>(Expression<Func<T, TValue>> expression, Expression<Func<TValue>> value, int callIndex)
    {
        var callDetails = GetCallDetails(expression, value).ToList();
        if (callIndex < 0 || callIndex >= callDetails.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(callIndex));
        }
        return callDetails[callIndex];
    }

    private IEnumerable<object[]> GetCallDetails(LambdaExpression expression)
    {
        var (method, argumentPredicates) = GetMethod(expression);
        if (!callDetails.ContainsKey(method))
        {
            return Enumerable.Empty<object[]>();
        }
        return callDetails[method].Where(cd => argumentPredicates.Select((p, i) => (p, i)).All(a => a.p(cd[a.i])));
    }

    private IEnumerable<TValue> GetCallDetails<TValue>(Expression<Func<T, TValue>> expression, Expression<Func<TValue>> value)
    {
        var (method, argumentPredicates) = GetMethod(expression, value);
        if (!callDetails.ContainsKey(method))
        {
            return Enumerable.Empty<TValue>();
        }
        return callDetails[method]
            .Where(cd => argumentPredicates.Select((p, i) => (p, i)).All(a => a.p(cd[a.i])))
            .Select(o => (TValue)o[0]);
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
                            return _ => true;
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
        throw new ArgumentException("Only method calls and readable properties can be setup or queried with this method", nameof(expression));
    }
    
    private static (MethodInfo MethodInfo, List<Func<object, bool>> ArgumentPredicates) GetMethod<TValue>(Expression<Func<T, TValue>> expression, Expression<Func<TValue>> value)
    {
        if (expression.Body is MemberExpression memberExpression
            && memberExpression.Member is PropertyInfo property
            && property.CanWrite)
        {
            var parameter = Expression.Parameter(typeof(object));
            if (value.Body is MethodCallExpression methodCall && methodCall.Method.IsGenericMethod)
            {
                if (methodCall.Method.GetGenericMethodDefinition() == It.IsAnyMethod)
                {
                    return (property.GetSetMethod()!, new List<Func<object, bool>> { _ => true });
                }
                if (methodCall.Method.GetGenericMethodDefinition() == It.AffirmsMethod)
                {
                    var castToTValue = Expression.Convert(parameter, typeof(TValue));
                    var bodyLambda = methodCall.Arguments[0] as LambdaExpression;
                    var invoke = Expression.Invoke(methodCall.Arguments[0], castToTValue);
                    var affirmsPredicate = Expression.Lambda<Func<object, bool>>(invoke, parameter).Compile();
                    return (property.GetSetMethod()!, new List<Func<object, bool>> { affirmsPredicate });
                }
            }
            var cast = Expression.Convert(value.Body, typeof(object));
            var body = Expression.Call(null, equals, parameter, cast);
            var equalsPredicate = Expression.Lambda<Func<object, bool>>(body, parameter).Compile();
            return (property.GetSetMethod()!, new List<Func<object, bool>> { equalsPredicate });
        }
        throw new ArgumentException("Only read/write properties can be queried with this method", nameof(expression));
    }
}