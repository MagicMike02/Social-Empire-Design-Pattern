#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Script.ResourceSystem
{
	/// <summary>
	/// Strumenti di debug/editor per il sistema risorse, separati da <see cref="ResourceManager"/>
	/// per rispettare il Single Responsibility Principle.
	/// Utility statiche con [MenuItem]: invocabili dal menu SocialEmpire > Resources.
	/// NON richiedono un GameObject in scena né registrazione DI nel runtime scope.
	/// Racchiuso in #if UNITY_EDITOR: non compilato in build di produzione.
	/// </summary>
	public static class ResourceEditorTools
	{
		[MenuItem("SocialEmpire/Resources/Remove All Resources")]
		public static void RemoveAllResources()
		{
			var resourceManager = Object.FindAnyObjectByType<ResourceManager>();
			if (resourceManager == null)
			{
				Debug.LogWarning("[ResourceEditorTools] ResourceManager non trovato in scena.");
				return;
			}

			resourceManager.RemoveAllResources();
			Debug.Log("[ResourceEditorTools] All resources have been removed.");
		}

		[MenuItem("SocialEmpire/Resources/Regenerate All Resources")]
		public static void RegenerateAllResources()
		{
			var resourceManager = Object.FindAnyObjectByType<ResourceManager>();
			var resourceSpawner = Object.FindAnyObjectByType<ResourceSpawner>();
			if (resourceManager == null || resourceSpawner == null)
			{
				Debug.LogWarning("[ResourceEditorTools] ResourceManager o ResourceSpawner non trovati in scena.");
				return;
			}

			resourceManager.RemoveAllResources();
			resourceSpawner.GenerateAllResources();
			Debug.Log("[ResourceEditorTools] All resources have been regenerated.");
		}
	}
}
#endif