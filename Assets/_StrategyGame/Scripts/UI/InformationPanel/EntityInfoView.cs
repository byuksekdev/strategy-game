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

        //------Private Variables-------//

        #region UNITY_METHODS

        #endregion

        #region PUBLIC_METHODS

        // Fills the view with static data and hides the HP row.
        public void Bind(EntityData data)
        {
            Bind(data, null);
        }

        // Fills the view with static data and runtime HP information.
        public void Bind(EntityData data, IDamageable damageable)
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

            RefreshHP(damageable);

            gameObject.SetActive(true);
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
                _hpText.SetText($"HP: {damageable.CurrentHP} / {damageable.MaxHP}");
        }

        // Clears the view and hides it.
        public void Clear()
        {
            _nameText.SetText(string.Empty);
            _iconImage.sprite = null;
            _iconImage.enabled = false;

            if (_hpRow != null)
                _hpRow.SetActive(false);

            gameObject.SetActive(false);
        }

        #endregion

        #region PRIVATE_METHODS

        #endregion
    }
}
