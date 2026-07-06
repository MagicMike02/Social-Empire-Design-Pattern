using UnityEngine;

namespace Script.Core.AutoSave
{
	/// <summary>
	/// Configurazione del salvataggio automatico, esposta in Inspector via ScriptableObject.
	/// Permette di tarare intervallo e debounce degli eventi critici senza toccare il codice.
	/// </summary>
	[CreateAssetMenu(menuName = "ScriptableObjects/Config/AutoSave", fileName = "AutoSaveConfig")]
	public class AutoSaveConfigSO : ScriptableObject
	{
		#region Fields

		[Header("Timer Periodico")]
		[Tooltip("Intervallo tra un salvataggio automatico e il successivo (secondi).")]
		[SerializeField, Min(1f)] private float autoSaveIntervalSeconds = 60f;

		[Header("Debounce Eventi Critici")]
		[Tooltip("Tempo minimo tra due salvataggi reattivi (secondi). Evita burst di save " +
				 "quando più eventi critici si susseguono ravvicinati.")]
		[SerializeField, Min(0f)] private float eventDebounceSeconds = 2f;

		[Header("Opzioni")]
		[Tooltip("Se true, salva immediatamente al verificarsi di un evento critico " +
				 "(rispettando comunque il debounce).")]
		[SerializeField] private bool saveOnCriticalEvents = true;

		#endregion

		#region Properties

		/// <summary>Intervallo del timer periodico (secondi).</summary>
		public float AutoSaveIntervalSeconds => autoSaveIntervalSeconds;

		/// <summary>Debounce per salvataggi reattivi (secondi).</summary>
		public float EventDebounceSeconds => eventDebounceSeconds;

		/// <summary>True se gli eventi critici devono triggerare un salvataggio.</summary>
		public bool SaveOnCriticalEvents => saveOnCriticalEvents;

		#endregion
	}
}
