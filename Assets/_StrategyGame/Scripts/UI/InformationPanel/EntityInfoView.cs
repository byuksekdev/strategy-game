using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StrategyGame.Core;
using StrategyGame.Data;

namespace StrategyGame.UI.InformationPanel
{
    // Displays the name, icon, and HP information of a selected entity (building or unit).
    public class EntityInfoView : MonoBehaviour
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//
        [Header("Entity Info")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _iconImage;

        [Header("HP")]
        [Tooltip("The root GameObject of the HP row. Hidden if HP is not available.")]
        [SerializeField] private GameObject _hpRow;
        [SerializeField] private TextMeshProUGUI _hpText;

        [Header("Dimensions")]
        [Tooltip("The root GameObject of the Dimensions row. Hidden for non-building entities.")]
        [SerializeField] private GameObject _dimensionRow;
        [SerializeField] private TextMeshProUGUI _dimensionText;

        //------Private Variables-------//

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        // Fills the view with static data only (preview mode — no live entity).
        // Shows max HP from BuildingData if available ("HP: 100").
        public void Bind(EntityData data)
        {
            BindCore(data);
            RefreshHPStatic(data as BuildingData);
        }

        // Fills the view with static data and runtime HP information.
        public void Bind(EntityData data, IDamageable damageable)
        {
            BindCore(data);
            RefreshHP(damageable);
        }

        // Updates the live HP information (without a Bind() call).
        public void RefreshHP(IDamageable damageable)
        {
            if (_hpRow == null) return;

            if (damageable == null)
            {
                _hpRow.SetActive(false);
                return;
            }

            _hpRow.SetActive(true);
            if (_hpText != null)
                _hpText.SetText($"Current HP: {damageable.CurrentHP} / {damageable.MaxHP}");
        }

        // Clears the view and hides it.
        public void Clear()
        {
            _nameText.SetText(string.Empty);
            _iconImage.sprite = null;
            _iconImage.enabled = false;

            if (_hpRow != null)
                _hpRow.SetActive(false);

            if (_dimensionRow != null)
                _dimensionRow.SetActive(false);

            gameObject.SetActive(false);
        }

        #endregion

        #region PRIVATE_METHODS

        private void BindCore(EntityData data)
        {
            if (data == null)
            {
                Clear();
                return;
            }

            _nameText.SetText(data.DisplayName);

            bool hasIcon = data.Icon != null;
            _iconImage.sprite = data.Icon;
            _iconImage.enabled = hasIcon;

            RefreshDimensions(data as BuildingData);

            gameObject.SetActive(true);
        }

        // Shows static max HP from BuildingData in preview (menu) mode: "HP: 100".
        private void RefreshHPStatic(BuildingData buildingData)
        {
            if (_hpRow == null) return;

            if (buildingData == null)
            {
                _hpRow.SetActive(false);
                return;
            }

            _hpRow.SetActive(true);
            if (_hpText != null)
                _hpText.SetText($"HP: {buildingData.MaxHP}");
        }

        // Shows or hides the dimension row based on whether the entity is a building.
        // Only BuildingData carries a Size; units and unplaced items hide this row.
        private void RefreshDimensions(BuildingData buildingData)
        {
            if (_dimensionRow == null) return;

            if (buildingData == null)
            {
                _dimensionRow.SetActive(false);
                return;
            }

            _dimensionRow.SetActive(true);
            if (_dimensionText != null)
                _dimensionText.SetText($"Size: {buildingData.Size.x}×{buildingData.Size.y}");
        }

        #endregion
    }
}
