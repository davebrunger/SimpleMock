namespace SimpleMock;

internal class TypeGenerator<T>
{
    private readonly Mock<T>.Caller caller;
    private readonly Lazy<Type> type;
    private static readonly MethodInfo callMethod = typeof(Mock<T>.Caller).GetMethod("Call")!;

    public Type Type => type.Value;

    public TypeGenerator(Mock<T>.Caller caller)
    {
        this.caller = caller;
        type = new Lazy<Type>(() => GenerateType());
    }

    private Type GenerateType()
    {
        var mockedType = typeof(T);
        var typeBuilder = MockAssembly.ModuleBuilder.DefineType($"{MockAssembly.AssemblyName}.{mockedType.FullName}");

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
            var methodBuilder = typeBuilder.DefineMethod(
                method.Name, 
                method.Attributes & ~MethodAttributes.Abstract, 
                method.ReturnType, 
                parameters.Select(p => p.ParameterType).ToArray());
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

        return typeBuilder.CreateType()!;
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
}
