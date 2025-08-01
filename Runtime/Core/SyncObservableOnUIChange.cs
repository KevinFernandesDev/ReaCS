using System.Reflection;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    public class SyncObservableOnUIChange : MonoBehaviour
    {
        public ObservableObject targetSO;

        void Update()
        {
            foreach (var field in targetSO.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.GetValue(targetSO) is IInitializableObservable observable)
                {
                    var syncMethod = field.FieldType.GetMethod("SyncFromBinding");
                    syncMethod?.Invoke(observable, null);
                }
            }
        }
    }

}
