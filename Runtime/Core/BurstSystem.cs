using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using static ReaCS.Runtime.ReaCS;
using ReaCS.Runtime.Registries;
using System.Linq;

namespace ReaCS.Runtime.Core
{
    public abstract class BurstSystemBase<TSO, TField> : MonoBehaviour
        where TSO : ObservableScriptableObject
        where TField : struct
    {
        protected abstract TField Map(TSO so);
        protected abstract void Apply(TSO so, TField updated);
        protected abstract void Execute(ref TField value, float deltaTime);

        private NativeArray<TField> _data;
        private List<TSO> _sources;

        void Update()
        {
            var registry = Query<ReaCSIndexRegistry>();
            _sources = registry.GetAll<TSO>().ToList();

            if (_sources.Count == 0)
                return;

            _data = new NativeArray<TField>(_sources.Count, Allocator.TempJob);
            for (int i = 0; i < _sources.Count; i++)
                _data[i] = Map(_sources[i]);

            var job = new FieldJob { deltaTime = Time.deltaTime, data = _data };
            job.Schedule(_data.Length, 16).Complete();

            for (int i = 0; i < _sources.Count; i++)
                Apply(_sources[i], _data[i]);

            _data.Dispose();
        }

        [BurstCompile]
        private struct FieldJob : IJobParallelFor
        {
            public NativeArray<TField> data;
            public float deltaTime;

            public void Execute(int index)
            {
                var value = data[index];
                ExecuteInternal(ref value, deltaTime);
                data[index] = value;
            }

            [BurstCompile]
            private static void ExecuteInternal(ref TField val, float dt)
            {
                // This is replaced at runtime by outer instance's Execute.
            }
        }
    }
}