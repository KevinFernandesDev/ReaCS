using System.Linq;
using UnityEngine;

namespace ReaCS.Editor
{
    public static class ReaCSGraphViewWindowHelper
    {
        public static void ResetOpenGraphView()
        {
            var window = Resources.FindObjectsOfTypeAll<ReaCSGraphViewWindow>().FirstOrDefault();
            if (window != null)
            {
                window.HandleReset();
            }
        }
    }
}