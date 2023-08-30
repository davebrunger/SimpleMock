namespace SimpleMock
{
    internal static class MockAssembly
    {
        private static readonly Lazy<AssemblyName> assemblyName = new(() => new AssemblyName($"{typeof(MockAssembly).Assembly.GetName().Name}.Dynamic"));

        private static readonly Lazy<ModuleBuilder> moduleBuilder = new(() =>
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run);
            return assemblyBuilder.DefineDynamicModule(AssemblyName.FullName!);
        });

        public static AssemblyName AssemblyName => assemblyName.Value;
        public static ModuleBuilder ModuleBuilder => moduleBuilder.Value;
    }
}
