using System;

[Serializable] 
public static class TokenPropertiesTypes
{
    public const int TEXT = 0;
    public const int NUMERIC = 1;
    public const int BAR = 2;
    public const int BOOLEAN = 3;
    public const int COLOR = 4;

    public static string ToString(int i)
    {
        return i switch
        {
            0 => "text",
            1 => "numeric",
            2 => "bar",
            3 => "boolean",
            4 => "color",
            _ => "null"
        };
    }
}