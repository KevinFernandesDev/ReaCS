using System;
using UnityEngine;

namespace ReaCS.Runtime.Core
{

    [Serializable]
    public class TranslateData
    {
        [Observable]
        public Observable<float> X;

        [Observable]
        public Observable<float> Y;
    }

}
