using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using TMPro;
using StrategyGame.Core;
using StrategyGame.Data;

namespace StrategyGame.UI.InformationPanel
{
    // Manages the unit production list in the information panel.
    // InformationPanelController fills it with ShowForProducer() and clears it with Hide().
    // This class is only visual; it does not listen to events, InformationPanelController orchestrates.
    // Unit items are spawned/despawned on LeanPool.
    public class UnitListController : MonoBehaviour
    {
        //-------Public Variables-------//

        //------Serialized Fields-------//
        [Tooltip("The root container (production section) to be shown/hidden.")]
        [SerializeField] private GameObject _container;

        [Tooltip("The parent transform for the unit items to be spawned.")]
        [SerializeField] private Transform _itemParent;

        [Tooltip("The unit item prefab with the ProductionItemView component.")]
        [SerializeField] private GameObject _unitItemPrefab;

        //------Private Variables-------//
        private readonly List<GameObject> _spawnedItems = new();
        private IUnitProducer _currentProducer;

        #region UNITY_METHODS

        private void Awake()
        {
            _container.SetActive(false);
        }

        #endregion

        #region PUBLIC_METHODS

        // Lists the producible units of the given producer and shows the container.
        public void ShowForProducer(IUnitProducer producer)
        {
            _currentProducer = producer;
            ClearItems();

            foreach (UnitData unitData in producer.ProducibleUnits)
            {
                GameObject go = LeanPool.Spawn(_unitItemPrefab, _itemParent);
                go.GetComponent<ProductionItemView>().Bind(unitData, OnUnitItemClicked);
                _spawnedItems.Add(go);
            }

            _container.SetActive(true);
        }

        // Clears the list and hides the container.
        public void Hide()
        {
            ClearItems();
            _currentProducer = null;
            _container.SetActive(false);
        }

        #endregion

        #region PRIVATE_METHODS

        private void OnUnitItemClicked(EntityData data)
        {
            if (_currentProducer == null || data is not UnitData unitData) return;
            EventBus<UnitProductionRequestedEvent>.Publish(new UnitProductionRequestedEvent(unitData, _currentProducer));
        }

        private void ClearItems()
        {
            foreach (GameObject go in _spawnedItems)
                LeanPool.Despawn(go);

            _spawnedItems.Clear();
        }

        #endregion
    }
}
