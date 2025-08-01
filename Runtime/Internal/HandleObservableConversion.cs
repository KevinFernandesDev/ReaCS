using UnityEngine;
using ReaCS.Runtime.Core;
using UnityEditor;
using UnityEngine.UIElements;

namespace ReaCS.Runtime.Internal
{
    public static class HandleObservableConversion
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void RegisterObservableFloat()
        {
            var group = new ConverterGroup("Observable<float> Sync");

            group.AddConverter<Observable<float>, float>((ref Observable<float> observable) =>
            {
                return observable._value; // Exposes raw value to UI
            });

            group.AddConverter<float, Observable<float>>((ref float newValue) =>
            {
                var obs = new Observable<float>();
                obs.Value = newValue;
                return obs;
            });

            // 🔁 Hook to sync after binding writes `.value` (play mode only)
            group.AddConverter<Observable<float>, Observable<float>>((ref Observable<float> observable) =>
            {
                if (Application.isPlaying)
                    observable.SyncFromBinding();
                return observable;
            });

            ConverterGroups.RegisterConverterGroup(group);
        }

    }
}
