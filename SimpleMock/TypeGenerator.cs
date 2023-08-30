﻿namespace SimpleMock;

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
            throw new InvalidOperationException("Abstract classes not supported, yet!");
        }
        else
        {
            throw new InvalidOperationException("Mocked type must be an interface or an abstract type!");
        }

        foreach (var method in mockedType.GetMethods())
        {
            GenerateMethod(typeBuilder, method);
        }

        return typeBuilder.CreateType()!;
    }

    private void GenerateMethod(TypeBuilder typeBuilder, MethodInfo method)
    {
        var methodBuilder = typeBuilder.DefineMethod(
            method.Name,
            method.Attributes & ~MethodAttributes.Abstract,
            method.ReturnType,
            method.GetParameters().Select(p => p.ParameterType).ToArray());
        var ilGenerator = methodBuilder.GetILGenerator();

        var parameterValues = LoadParameterValues(ilGenerator, method);
        LoadObject(ilGenerator, caller);
        ilGenerator.Emit(OpCodes.Ldarg_0); // this, IE MockObject.
        LoadObject(ilGenerator, method);
        ilGenerator.Emit(OpCodes.Ldloc, parameterValues);
        
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

    private static LocalBuilder LoadParameterValues(ILGenerator ilGenerator, MethodInfo method)
    {
        var localBuilder = ilGenerator.DeclareLocal(typeof(object[]));

        var parameters = method.GetParameters();

        ilGenerator.Emit(OpCodes.Ldc_I4, parameters.Length);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, localBuilder);

        foreach( var parameter in parameters.Select((p, i) => (p, i)))
        {
            ilGenerator.Emit(OpCodes.Ldloc, localBuilder);
            ilGenerator.Emit(OpCodes.Ldc_I4, parameter.i);
            ilGenerator.Emit(OpCodes.Ldarg, parameter.i + 1);
            if (parameter.p.ParameterType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, parameter.p.ParameterType);
            }
            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }
        return localBuilder;        
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