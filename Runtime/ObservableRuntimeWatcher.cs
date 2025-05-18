using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class ObservableRuntimeWatcher : MonoBehaviour
{
    private static readonly HashSet<ObservableScriptableObject> _observables = new();
    private static readonly Dictionary<ObservableScriptableObject, float> debounceTimers = new();
    private static readonly HashSet<ObservableScriptableObject> _debouncedSet = new();

    private static ObservableRuntimeWatcher _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (_instance == null)
        {
            var go = new GameObject("ObservableRuntimeWatcher");
            _instance = go.AddComponent<ObservableRuntimeWatcher>();
            DontDestroyOnLoad(go);
        }
    }

    public static void Register(ObservableScriptableObject obj)
    {
        _observables.Add(obj);
    }

    public static void Unregister(ObservableScriptableObject obj)
    {
        _observables.Remove(obj);
        debounceTimers.Remove(obj);
        _debouncedSet.Remove(obj);
    }

    public static void DebounceChange(ObservableScriptableObject obj, float delay)
    {
        debounceTimers[obj] = delay;
        _debouncedSet.Add(obj);
    }

    public static void ForceUpdate()
    {
        foreach (var obj in _observables)
        {
            if (_debouncedSet.Contains(obj)) continue;
            obj.CheckForChanges();
        }
    }

    private void Update()
    {
        ForceUpdate();

        if (_debouncedSet.Count > 0)
        {
            var toClear = new List<ObservableScriptableObject>();
            foreach (var obj in _debouncedSet)
            {
                debounceTimers[obj] -= Time.deltaTime;
                if (debounceTimers[obj] <= 0f)
                {
                    obj.CheckForChanges();
                    toClear.Add(obj);
                }
            }

            foreach (var obj in toClear)
            {
                _debouncedSet.Remove(obj);
                debounceTimers.Remove(obj);
            }
        }
    }
}