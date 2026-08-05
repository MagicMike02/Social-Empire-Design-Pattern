using Script.Core.Events;
using Script.InputSystem;
using UnityEngine;

namespace Script.Core.Entities
{
	/// <summary>
	/// Reusable selection + hover component for any entity prefab.
	/// Implements <see cref="ISelectable"/> (drives <see cref="EntitySelectionManager"/>)
	/// and <see cref="IHoverable"/> (integrates with the existing <c>InputManager</c>).
	/// Delegates <see cref="EntityInfo"/> construction to <see cref="EntityInfoProvider"/>
	/// and visual feedback to <see cref="IHighlightable"/> when present.
	/// </summary>
	[RequireComponent(typeof(EntityInfoProvider))]
	public sealed class SelectableComponent : MonoBehaviour, ISelectable, IHoverable
	{
		#region Fields

		private EntityInfoProvider _infoProvider;
		private IHighlightable _highlightable;
		private bool _isSelected;

		#endregion

		#region Properties

		public bool IsSelected => _isSelected;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			_infoProvider = GetComponent<EntityInfoProvider>();
			_highlightable = GetComponent<IHighlightable>();
		}

		#endregion

		#region ISelectable

		/// <inheritdoc/>
		public void Select()
		{
			_isSelected = true;
			_highlightable?.Highlight(HighlightType.Selected);
		}

		/// <inheritdoc/>
		public void Deselect()
		{
			_isSelected = false;
			_highlightable?.ClearHighlight();
		}

		/// <inheritdoc/>
		public EntityInfo GetEntityInfo() => _infoProvider.GetEntityInfo();

		#endregion

		#region IHoverable

		/// <inheritdoc/>
		public void OnHoverEnter()
		{
			if (!_isSelected)
			{
				_highlightable?.Highlight(HighlightType.Hover);
			}
		}

		/// <inheritdoc/>
		public void OnHoverExit()
		{
			if (!_isSelected)
			{
				_highlightable?.ClearHighlight();
			}
		}

		/// <inheritdoc/>
		public void OnClick()
		{
			// Selection is routed through the manager so it can deselect the previous entity.
			// EntitySelectionManager is resolved lazily to avoid hard coupling in Awake.
			var manager = EntitySelectionResolver.Instance;
			if (manager != null)
			{
				manager.SelectEntity(this);
			}
#if UNITY_EDITOR
			else
			{
				Debug.LogWarning($"[SelectableComponent] EntitySelectionManager not available on {name}. Is GameLifetimeScope in the scene?");
			}
#endif
		}

		/// <inheritdoc/>
		public void OnRightClick(Vector3 worldPosition)
		{
			// Right-click deselects: route through the manager for consistency.
			var manager = EntitySelectionResolver.Instance;
			manager?.DeselectCurrent();
		}

		#endregion
	}
}
