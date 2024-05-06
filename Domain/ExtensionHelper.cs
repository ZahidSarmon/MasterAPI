namespace Master.Domain;

public static class ExtensionHelper
{
    public static bool IsSafedKey(this string? key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            if (key.ToLower().Equals("null") || key.ToLower().Equals("id"))
                return false;
        }

        if (string.IsNullOrWhiteSpace(key)) return false;

        return true;
    }
}
