using UnityEngine;

namespace Script.UnitSystem.Production
{
    public struct UnitSpawnRequest
    {
        public string UnitId;
        public int Level;
        public int Count;
        public UnitSpawnSourceType Source;
        public bool UseWorldPosition;
        public Vector3 WorldPosition;
        public int SearchRadius;
        public bool IgnoreUnlock;
        public Transform ParentOverride;
    }
}
