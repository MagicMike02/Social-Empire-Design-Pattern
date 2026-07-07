using UnityEngine;
using VContainer;

namespace Script.GridSystem
{
	/// <summary>
	/// Service handling visual preview of grid cells (e.g., placement previews).
	/// </summary>
	public class GridVisualService
	{
		private TileManager _tileManager;
		private GridPreviewTracker _previewTracker;

		[Inject]
		public void Construct(TileManager tileManager)
		{
			_tileManager = tileManager;
			_previewTracker = new GridPreviewTracker(_tileManager);
		}

		public void SetCellsPreview(Vector3Int originCell, int width, int height, bool isValid)
		{
			_previewTracker?.SetCellsPreview(originCell, width, height, isValid);
		}

		public void ClearPreview()
		{
			_previewTracker?.ClearPreview();
		}
	}
}
