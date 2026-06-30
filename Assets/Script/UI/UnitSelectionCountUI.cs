using Script.UnitSystem;
using TMPro;
using UnityEngine;
using VContainer;

namespace Script.UI
{
    /// <summary>
    /// Mostra il conteggio unita' selezionate.
    /// </summary>
    public sealed class UnitSelectionCountUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private string _prefix = "Selected Units: ";

        private UnitSelectionService _selectionService;

        [Inject]
        public void Construct(UnitSelectionService selectionService)
        {
            _selectionService = selectionService;
        }

        private void OnEnable()
        {
            if (_selectionService != null)
            {
                _selectionService.OnSelectionCountChanged += HandleSelectionCountChanged;
                HandleSelectionCountChanged(_selectionService.SelectedCount);
            }
        }

        private void OnDisable()
        {
            if (_selectionService != null)
            {
                _selectionService.OnSelectionCountChanged -= HandleSelectionCountChanged;
            }
        }

        private void HandleSelectionCountChanged(int count)
        {
            if (_countLabel != null)
            {
                _countLabel.text = _prefix + count;
            }
        }
    }
}
