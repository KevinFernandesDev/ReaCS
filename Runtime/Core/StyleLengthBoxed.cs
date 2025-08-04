using UnityEngine;
using UnityEngine.UIElements;
using System;

[Serializable]
public class StyleLengthBoxed
{
    public StyleLength Value;

    public StyleLengthBoxed()
    {
        Value = new StyleLength(new Length(0f, LengthUnit.Pixel));
    }

    public StyleLengthBoxed(StyleLength v)
    {
        Value = v;
    }

    public static implicit operator StyleLength(StyleLengthBoxed b)
        => b?.Value ?? new StyleLength(new Length(0f, LengthUnit.Pixel));

    public static implicit operator StyleLengthBoxed(StyleLength v)
        => new StyleLengthBoxed(v);
}
