namespace SimpleMock;

public static class It
{
    public static T IsAny<T>() => default!;

    internal static MethodInfo IsAnyMethod { get; } = typeof(It).GetMethod("IsAny")!;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required")]
    public static T Is<T>(Func<T, bool> predicate) => default!;

    internal static MethodInfo IsMethod { get; } = typeof(It).GetMethod("Is")!;
}
