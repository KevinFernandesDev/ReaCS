using ReaCS.Runtime.Core;
using UnityEngine;
using Unity.Mathematics;

namespace ReaCS.Examples
{
    public class PhysicsDataComponent : ComponentDataBinding<PositionSO>
    {
        private void Update()
        {
            if (data != null)
            {
                var pos = data.Value;
                transform.position = new Vector3(pos.Value.x, pos.Value.y, transform.position.z);
            }
        }
    }
}