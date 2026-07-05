using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StrategyGame.Data;

namespace StrategyGame.UI.ProductionMenu
{
    // Represents a single item in the production menu or information panel.
    // When Bind() is called, the data is injected; the click callback is passed through.
    [RequireComponent(typeof(Button))]
    public class ProductionItemView : MonoBehaviour
    {
        //-------Public Variables-------//
        public EntityData Data => _data;

        //------Serialized Fields-------//
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameText;

        //------Private Variables-------//
        private Button _button;
        private EntityData _data;
        private Action<EntityData> _onClick;

        #region UNITY_METHODS

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        #endregion

        #region PUBLIC_METHODS

        // Fills the visual fields and the click callback.
        // Should be called immediately after spawning.
        public void Bind(EntityData data, Action<EntityData> onClick)
        {
            _data = data;
            _onClick = onClick;

            _nameText.SetText(data.DisplayName);

            bool hasIcon = data.Icon != null;
            _icon.sprite = data.Icon;
            _icon.enabled = hasIcon;
        }

        #endregion

        #region PRIVATE_METHODS

        private void HandleClick()
        {
            _onClick?.Invoke(_data);
        }

        #endregion
    }
}
