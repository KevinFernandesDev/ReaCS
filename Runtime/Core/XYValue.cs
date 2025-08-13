using System;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class XYValue
    {
        [Observable] public Observable<float> X = new();
        [Observable] public Observable<float> Y = new();
    }
}
