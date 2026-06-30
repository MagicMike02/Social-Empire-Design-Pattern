using System.Collections;
using System.Collections.Generic;
using Script.GridSystem;
using Script.PathfindingSystem;
using Script.InputSystem;
using Script.UnitSystem.Config;
using UnityEngine;
using VContainer;

namespace Script.UnitSystem
{
    /// <summary>
    /// Rappresenta una singola unita' selezionabile e comandabile.
    /// Movimento a tile con pathfinding e collisione logica su celle occupate.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class UnitController : MonoBehaviour, IHoverable
    {
        #region Dependencies

        private GridManager _gridManager;
        private PathfindingManager _pathfindingManager;
        private UnitRegistryService _registry;
        private UnitSystemConfigSO _unitSystemConfig;

        [Inject]
        public void Construct(GridManager gridManager, PathfindingManager pathfindingManager, UnitRegistryService registry)
        {
            _gridManager = gridManager;
            _pathfindingManager = pathfindingManager;
            _registry = registry;
        }

        #endregion

        #region Inspector

        [Header("Identity")]
        [SerializeField] private string _unitId = "unit";

        [Header("Config")]
        [SerializeField] private UnitConfigSO _config;
        [SerializeField] private int _unitLevel = 1;

        [Header("Stats")]
        [SerializeField] private float _moveSpeed = 3.5f;
        [SerializeField] private int _hitPoints = 100;
        [SerializeField] private int _attack = 10;
        [SerializeField] private float _attackRange = 1f;

        [Header("Selection Visual")]
        [SerializeField] private GameObject _selectionMarker;

        [Header("Collision Movement")]
        [SerializeField] private float _cellAcquireRetrySeconds = 0.15f;
        [SerializeField] private float _cellAcquireTimeoutSeconds = 1.2f;

        #endregion

        #region State

        private bool _isSelected;
        private bool _isMoving;
        private Vector2Int _currentCell;
        private bool _hasCurrentCell;
        private Vector2Int _reservedCell;
        private bool _hasReservedCell;
        private Coroutine _moveRoutine;

        #endregion

        #region Properties

        public string UnitId => _unitId;
        public bool IsSelected => _isSelected;
        public bool IsMoving => _isMoving;
        public Vector2Int CurrentCell => _currentCell;
        public float MoveSpeed => _moveSpeed;
        public int UnitLevel => _unitLevel;
        public UnitConfigSO Config => _config;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ResolveDependenciesIfNeeded();

            ApplyConfig(_config, _unitLevel);

            if (_gridManager != null && _gridManager.TryWorldToCell(transform.position, out var cell3))
            {
                _currentCell = new Vector2Int(cell3.x, cell3.y);
                _gridManager.OccupyCell(_currentCell, gameObject);
                _hasCurrentCell = true;
            }

            if (_selectionMarker != null)
            {
                _selectionMarker.SetActive(false);
            }

            if (_registry != null)
            {
                bool registered = _registry.Register(this);
                if (!registered)
                {
                    if (_gridManager != null)
                    {
                        ReleaseAllOccupiedCells();
                    }

                    gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            _registry?.Unregister(this);

            if (_gridManager != null)
            {
                ReleaseAllOccupiedCells();
            }
        }

        #endregion

        #region Selection

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (_selectionMarker != null)
            {
                _selectionMarker.SetActive(selected);
            }
        }

        public void ApplyConfig(UnitConfigSO config, int level)
        {
            if (config == null)
            {
                return;
            }

            _config = config;
            _unitLevel = Mathf.Max(1, level);
            _unitId = config.UnitId;

            var levelData = config.GetLevelData(_unitLevel);

            _hitPoints = Mathf.Max(1, levelData.HitPoints);
            _moveSpeed = Mathf.Max(0.1f, levelData.MoveSpeed);
            _cellAcquireRetrySeconds = Mathf.Max(0.01f, levelData.CellAcquireRetrySeconds);
            _cellAcquireTimeoutSeconds = Mathf.Max(_cellAcquireRetrySeconds, levelData.CellAcquireTimeoutSeconds);
            _attack = Mathf.Max(0, levelData.Attack);
            _attackRange = Mathf.Max(0.1f, levelData.AttackRange);
        }

        #endregion

        #region Movement

        public bool MoveToCell(Vector2Int destination)
        {
            if (_gridManager == null || _pathfindingManager == null)
            {
                return false;
            }

            if (destination.x < 0 || destination.y < 0 || destination.x >= _gridManager.Width || destination.y >= _gridManager.Height)
            {
                return false;
            }

            if (!_gridManager.IsCellFree(destination) && destination != _currentCell)
            {
                return false;
            }

            var path = _pathfindingManager.FindPath(_currentCell, destination);
            if (path == null || path.Count == 0)
            {
                return false;
            }

            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;

                if (_hasReservedCell)
                {
                    _gridManager.FreeCell(_reservedCell);
                    _hasReservedCell = false;
                }
            }

            _moveRoutine = StartCoroutine(MoveAlongPath(path));
            return true;
        }

        private IEnumerator MoveAlongPath(List<Vector2Int> path)
        {
            _isMoving = true;

            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int nextCell = path[i];

                float waited = 0f;
                while (!_gridManager.IsCellFree(nextCell))
                {
                    if (waited >= _cellAcquireTimeoutSeconds)
                    {
                        if (_hasReservedCell)
                        {
                            _gridManager.FreeCell(_reservedCell);
                            _hasReservedCell = false;
                        }

                        _isMoving = false;
                        _moveRoutine = null;
                        yield break;
                    }

                    waited += _cellAcquireRetrySeconds;
                    yield return new WaitForSeconds(_cellAcquireRetrySeconds);
                }

                _gridManager.OccupyCell(nextCell, gameObject);
                _reservedCell = nextCell;
                _hasReservedCell = true;

                Vector3 target = _gridManager.CellToWorld(new Vector3Int(nextCell.x, nextCell.y, 0));
                target.z = transform.position.z;

                while ((transform.position - target).sqrMagnitude > 0.0001f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, _moveSpeed * Time.deltaTime);
                    yield return null;
                }

                if (_hasCurrentCell)
                {
                    _gridManager.FreeCell(_currentCell);
                }
                _currentCell = nextCell;
                _hasCurrentCell = true;
                _hasReservedCell = false;
            }

            _isMoving = false;
            _moveRoutine = null;
        }

        #endregion

        #region IHoverable

        public void OnHoverEnter() { }
        public void OnHoverExit() { }
        public void OnClick() { }
        public void OnRightClick(Vector3 worldPosition) { }

        #endregion

        #region Helpers

        private void ResolveDependenciesIfNeeded()
        {
            if (_gridManager == null)
            {
                _gridManager = FindFirstObjectByType<GridManager>(FindObjectsInactive.Exclude);
            }

            if (_pathfindingManager == null)
            {
                _pathfindingManager = FindFirstObjectByType<PathfindingManager>(FindObjectsInactive.Exclude);
            }

            if (_registry == null)
            {
                _registry = FindFirstObjectByType<UnitRegistryService>(FindObjectsInactive.Exclude);
            }

            if (_unitSystemConfig == null)
            {
                _unitSystemConfig = Resources.Load<UnitSystemConfigSO>("UnitSystem/UnitSystemConfig");
            }

            if (_config == null && _unitSystemConfig != null && _unitSystemConfig.UnitCatalog != null)
            {
                if (_unitSystemConfig.UnitCatalog.TryGetById(_unitSystemConfig.DefaultUnitId, out var fallbackConfig))
                {
                    _config = fallbackConfig;
                    _unitLevel = Mathf.Max(1, _unitSystemConfig.DefaultUnitLevel);
                }
            }
        }

        private void ReleaseAllOccupiedCells()
        {
            if (_gridManager == null)
            {
                return;
            }

            if (_hasReservedCell)
            {
                _gridManager.FreeCell(_reservedCell);
                _hasReservedCell = false;
            }

            if (_hasCurrentCell)
            {
                _gridManager.FreeCell(_currentCell);
                _hasCurrentCell = false;
            }
        }

        #endregion
    }
}
