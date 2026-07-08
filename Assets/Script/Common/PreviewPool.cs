using UnityEngine;

namespace Script.Common
{
	/// <summary>
	/// Gestione del GameObject di preview: get/release dal pool, setup SpriteRenderer,
	/// disable collider, position/scale/name. Pure C#, nessun MonoBehaviour.
	/// Estratto da <see cref="GenericPreviewSystem"/> per rispettare il Single Responsibility Principle.
	/// </summary>
	public sealed class PreviewPool
	{
		#region Fields

		private readonly PrefabPoolManager _poolManager;

		private GameObject _currentPreview;
		private GameObject _currentPrefabRef;
		private SpriteRenderer _previewSpriteRenderer;
		private string _previewName = "Preview";

		#endregion

		#region Properties

		public bool HasActivePreview => _currentPreview != null;
		public GameObject CurrentPreview => _currentPreview;
		public SpriteRenderer CurrentSpriteRenderer => _previewSpriteRenderer;

		#endregion

		#region Constructor

		public PreviewPool(PrefabPoolManager poolManager)
		{
			_poolManager = poolManager;
		}

		#endregion

		#region Public API

		/// <summary>
		/// Estrae o genera l'oggetto preview dal pool, preparandone i componenti visuali.
		/// </summary>
		public void CreatePreview(GameObject prefab, string sortingLayer, int sortingOrderOffset)
		{
			if (_poolManager == null)
			{
#if UNITY_EDITOR
				Debug.LogError("[PreviewPool] PoolManager non inizializzato!");
#endif
				return;
			}

			_currentPrefabRef = prefab;
			_currentPreview = _poolManager.Get(prefab, Vector3.zero, Quaternion.identity);
			_currentPreview.name = _previewName;
			_previewSpriteRenderer = _currentPreview.GetComponentInChildren<SpriteRenderer>();

			if (_previewSpriteRenderer == null)
			{
#if UNITY_EDITOR
				Debug.LogWarning($"[PreviewPool] Nessun SpriteRenderer trovato in '{prefab.name}'!");
#endif
				return;
			}

			_previewSpriteRenderer.sortingLayerName = sortingLayer;
			_previewSpriteRenderer.sortingOrder = sortingOrderOffset;

			DisableCollider();
		}

		/// <summary>
		/// Rilascia il GameObject corrente al pool, ripristinandone lo stato neutro.
		/// </summary>
		public void Release()
		{
			if (_currentPreview == null) return;

			if (_previewSpriteRenderer != null)
				_previewSpriteRenderer.color = Color.white;

			if (_poolManager != null && _currentPrefabRef != null)
			{
				_poolManager.Release(_currentPrefabRef, _currentPreview);
			}
			else
			{
				Object.Destroy(_currentPreview);
			}

			_currentPreview = null;
			_currentPrefabRef = null;
			_previewSpriteRenderer = null;
		}

		/// <summary>
		/// Imposta la posizione world del GameObject preview.
		/// </summary>
		public void SetPosition(Vector3 worldPosition)
		{
			if (_currentPreview == null) return;
			_currentPreview.transform.position = worldPosition;
		}

		/// <summary>
		/// Imposta la scala uniforme della preview.
		/// </summary>
		public void SetScale(Vector3 scale)
		{
			if (_currentPreview != null)
				_currentPreview.transform.localScale = scale;
		}

		/// <summary>
		/// Rinomina il GameObject della preview.
		/// </summary>
		public void SetPreviewName(string name)
		{
			_previewName = name;
			if (_currentPreview != null)
				_currentPreview.name = name;
		}

		#endregion

		#region Private Methods

		private void DisableCollider()
		{
			if (_currentPreview == null) return;
			Collider2D col2D = _currentPreview.GetComponent<Collider2D>();
			if (col2D != null)
				col2D.enabled = false;
		}

		#endregion
	}
}
