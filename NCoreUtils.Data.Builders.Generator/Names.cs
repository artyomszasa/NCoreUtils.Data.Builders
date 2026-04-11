namespace NCoreUtils.Data;

internal static class Names
{
    public static string GetDefaultValueMethod(string propertyName)
        => $"GetDefault{propertyName}Value";

    public static string InitializeMethod(string propertyName)
        => $"Initialize{propertyName}";

    public static string BuildMethod(string propertyName)
        => $"Build{propertyName}";
}