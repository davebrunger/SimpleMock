using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SimpleMock;

internal static class Mock
{
    public static AssemblyName AssemblyName { get; }
    public static ModuleBuilder ModuleBuilder { get; }

    static Mock()
    {
        AssemblyName = new AssemblyName($"{typeof(Mock<>).Assembly.GetName().Name}.Dynamic");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = assemblyBuilder.DefineDynamicModule(AssemblyName.FullName!);
    }
}

public class Mock<T>
{
    public sealed class SetupResult<TResult>
    {
        private readonly Mock<T> parent;
        private readonly object info;

        internal SetupResult(Mock<T> parent, object info)
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

        public object? Call(object obj, object info)
        {
            return Mock<T>.Call(obj, info);
        }
    }

    private static readonly IDictionary<(object, object), object?> returnValues = new Dictionary<(object, object), object?>();
    private static readonly Caller caller = new();
    private static readonly Type objectType;
    private static readonly MethodInfo callMethod = typeof(Caller).GetMethod("Call")!;


    public T MockObject { get; }

    static Mock()
    {
        var mockedType = typeof(T);
        var typeBuilder = Mock.ModuleBuilder.DefineType($"{Mock.AssemblyName}.{mockedType.FullName}");

        if (mockedType.IsInterface)
        {
            typeBuilder.AddInterfaceImplementation(mockedType);
        }
        else if (mockedType.IsAbstract)
        {
            throw new Exception("Abstract classes not supported, yet!");
        }
        else
        {
            throw new Exception("Mocked type must be an interface or an abstract type!");
        }

        foreach (var method in mockedType.GetMethods())
        {
            var parameters = method.GetParameters();
            var methodBuilder = typeBuilder.DefineMethod(method.Name, method.Attributes & ~MethodAttributes.Abstract, method.ReturnType, parameters.Select(p => p.ParameterType).ToArray());
            var ilGenerator = methodBuilder.GetILGenerator();

            LoadObject(ilGenerator, caller);
            ilGenerator.Emit(OpCodes.Ldarg_0); // this, IE MockObject.
            LoadObject(ilGenerator, method);
            ilGenerator.EmitCall(OpCodes.Callvirt, callMethod!, null);
            if (method.ReturnType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, method.ReturnType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Castclass, method.ReturnType);
            }
            ilGenerator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(methodBuilder, method);
        }

        objectType = typeBuilder.CreateType()!;
    }

    public Mock()
    {
        MockObject = (T)Activator.CreateInstance(objectType)!;
    }

    private static void LoadObject<TObject>(ILGenerator ilGenerator, TObject obj)
    {
        var handle = GCHandle.Alloc(obj);
        var pointer = GCHandle.ToIntPtr(handle);

        if (IntPtr.Size == 4)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4, pointer.ToInt32());
        }
        else
        {
            ilGenerator.Emit(OpCodes.Ldc_I8, pointer.ToInt64());
        }

        ilGenerator.Emit(OpCodes.Ldobj, typeof(TObject));
    }

    protected static object? Call(object obj, object info)
    {
        var callResult = returnValues.TryGetValue((obj, info), out var result) ? result : null;
        return callResult;
    }

    private void SetReturn(object info, object? result)
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