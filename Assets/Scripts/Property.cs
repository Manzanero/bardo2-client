using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Property
{
    public string name;
    public List<string> values;
    
    [Serializable]
    public class Info
    {
        public int type;
        public bool control;
        public bool extra;
    }
    
    public static class Types
    {
        public const int Text = 0;
        public const int Numeric = 1;
        public const int Bar = 2;
        public const int Boolean = 3;
        public const int Color = 4;
        public const int Percentage = 5;

        public static string ToString(int i) => i switch
        {
            0 => "text",
            1 => "numeric",
            2 => "bar",
            3 => "boolean",
            4 => "color",
            5 => "percentage",
            _ => null
        };
    }

    public string ToText() => values[0];

    public float ToNumeric() => values[0] == "" ? 0 : float.Parse(values[0]);

    public float[] ToBar() => new[]
    {
        values[0] == "" ? 0 : float.Parse(values[0]),
        values[1] == "" ? 0 : float.Parse(values[1])
    };

    public bool ToBoolean() => values[0] == "true";

    public Color ToColor() => new Color(
        values[0] == "" ? 100 : Mathf.Clamp(float.Parse(values[0]), 0, 100),
        values[1] == "" ? 100 : Mathf.Clamp(float.Parse(values[1]), 0, 100),
        values[0] == "" ? 100 : Mathf.Clamp(float.Parse(values[2]), 0, 100)) / 100;

    // public string ToText() => type == Types.Text
    //     ? values[0]
    //     : throw new Exception($"Property '{name}' is not type 'text'. It is '{TypeName}'");
    //
    // public float ToNumeric() => type == Types.Numeric
    //     ? values[0] == "" ? 0 : float.Parse(values[0])
    //     : throw new Exception($"Property '{name}' is not type 'numeric'. It is '{TypeName}'");
    //
    // public float[] ToBar() => type == Types.Bar
    //     ? new[] {values[0] == "" ? 0 : float.Parse(values[0]), values[1] == "" ? 0 : float.Parse(values[1])}
    //     : throw new Exception($"Property '{name}' is not type 'bar'. It is '{TypeName}'");
    //
    // public bool ToBoolean() => type == Types.Boolean
    //     ? values[0] == "true"
    //     : throw new Exception($"Property '{name}' is not type 'boolean'. It is '{TypeName}'");
    //
    // public Color ToColor() => type == Types.Color
    //     ? new Color(
    //         values[0] == "" ? 100 : Mathf.Clamp(float.Parse(values[0]), 0, 100),
    //         values[1] == "" ? 100 : Mathf.Clamp(float.Parse(values[1]), 0, 100),
    //         values[0] == "" ? 100 : Mathf.Clamp(float.Parse(values[2]), 0, 100)) / 100
    //     : throw new Exception($"Property '{name}' is not type 'color'. It is '{TypeName}'");
}