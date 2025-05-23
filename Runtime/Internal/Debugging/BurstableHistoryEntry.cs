using Unity.Collections;
using Unity.Mathematics;

namespace ReaCS.Runtime.Internal.Debugging
{
    public struct BurstableHistoryEntry
    {
        public int frame;
        public FixedString64Bytes soName;
        public FixedString64Bytes fieldName;
        public FixedString64Bytes systemName;

        public ReaCSValueKind valueType;
        public float3 valueOld;
        public float3 valueNew;

#if UNITY_EDITOR
        public FixedString64Bytes debugOld;
        public FixedString64Bytes debugNew;
#endif
    }

}
