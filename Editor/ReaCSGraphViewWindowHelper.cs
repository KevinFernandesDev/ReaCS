using System.Linq;
using UnityEngine;

namespace ReaCS.Editor
{
    public static class ReaCSGraphViewWindowHelper
    {
        public static void ResetOpenGraphView()
        {
            var window = Resources.FindObjectsOfTypeAll<StaticDependencyGraphWindow>().FirstOrDefault();
            if (window != null)
            {
                window.HandleReset();
            }
        }
    }
}