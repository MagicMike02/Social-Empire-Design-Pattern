using UnityEngine;
using System.Collections.Generic;

namespace Script.UnitSystem.Config
{
    /// <summary>
    /// Configurazione globale del sistema unita'.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/UnitSystem/UnitSystemConfig", fileName = "UnitSystemConfig")]
    public sealed class UnitSystemConfigSO : ScriptableObject
    {
        [Header("Catalog")]
        [SerializeField] private UnitCatalogSO _unitCatalog;
        [SerializeField] private string _defaultUnitId = "unit";
        [SerializeField] private int _defaultUnitLevel = 1;
        [SerializeField] private List<string> _defaultUnlockedUnitIds = new();

        [Header("Capacity")]
        [SerializeField] private int _maxUnits = 35;

        [Header("Selection")]
        [SerializeField] private int _overlapBufferSize = 256;
        [SerializeField] private float _dragMinSqrDistance = 16f;

        [Header("Commanding")]
        [SerializeField] private int _maxFormationSearchRadius = 12;

        public UnitCatalogSO UnitCatalog => _unitCatalog;
        public string DefaultUnitId => _defaultUnitId;
        public int DefaultUnitLevel => _defaultUnitLevel;
        public IReadOnlyList<string> DefaultUnlockedUnitIds => _defaultUnlockedUnitIds;
        public int MaxUnits => _maxUnits;
        public int OverlapBufferSize => _overlapBufferSize;
        public float DragMinSqrDistance => _dragMinSqrDistance;
        public int MaxFormationSearchRadius => _maxFormationSearchRadius;

        private void OnValidate()
        {
            _maxUnits = Mathf.Max(1, _maxUnits);
            _defaultUnitLevel = Mathf.Max(1, _defaultUnitLevel);
            _overlapBufferSize = Mathf.Clamp(_overlapBufferSize, 32, 2048);
            _dragMinSqrDistance = Mathf.Max(1f, _dragMinSqrDistance);
            _maxFormationSearchRadius = Mathf.Clamp(_maxFormationSearchRadius, 2, 64);
        }
    }
}
