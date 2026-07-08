#if UNITY_EDITOR
using UnityEngine;
using VContainer;

namespace Script.ResourceSystem
{
	/// <summary>
	/// Strumenti di debug/editor per il sistema risorse, separati da <see cref="ResourceManager"/>
	/// per rispettare il Single Responsibility Principle.
	/// I metodi <see cref="ContextMenu"/> sono invocabili solo dall'Inspector in Editor.
	/// Racchiuso in #if UNITY_EDITOR: non compilato in build di produzione.
	/// </summary>
	public class ResourceEditorTools : MonoBehaviour
	{
		#region Dependencies (Injected by VContainer)

		private ResourceManager _resourceManager;
		private ResourceSpawner _resourceSpawner;

		[Inject]
		public void Construct(ResourceManager resourceManager, ResourceSpawner resourceSpawner)
		{
			_resourceManager = resourceManager;
			_resourceSpawner = resourceSpawner;
		}

		#endregion

		#region Editor Utilities

		[ContextMenu("Remove All Resources")]
		public void RemoveAllResources()
		{
			_resourceManager?.RemoveAllResources();
			Debug.Log("[ResourceEditorTools] All resources have been removed.");
		}

		[ContextMenu("Regenerate All Resources")]
		public void RegenerateAllResources()
		{
			_resourceManager?.RemoveAllResources();
			_resourceSpawner?.GenerateAllResources();
			Debug.Log("[ResourceEditorTools] All resources have been regenerated.");
		}

		#endregion
	}
}
#endif