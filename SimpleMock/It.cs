namespace SimpleMock;

public static class It
{
    public static T IsAny<T>() => default!;

    internal static MethodInfo IsAnyMethod { get; } = typeof(It).GetMethod("IsAny")!;
}
