using System;
using System.Collections.Generic;
using Lean.Pool;
using StrategyGame.Data;
using StrategyGame.UI.ProductionMenu;
using UnityEngine;
using UnityEngine.UI;

namespace StrategyGame.UI
{
    /// Infinite scroll view with pool support
    /// Only viewport + 1 buffer item (for both sides) is active
    /// The ScrollRect.onValueChanged event is listened to; 
    /// and items that are out of the viewport are despawned and spawned again in the opposite side
    public class InfiniteScrollView : MonoBehaviour
    {
        //------ Public Variables -------//

        //------ Serialized Fields -------//
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _content;
        [SerializeField] private ProductionItemView _itemPrefab;
        [SerializeField] private float _itemSpacing = 10f;

        //------ Private Variables -------//
        private List<BuildingData> _dataList;
        private Action<EntityData> _onEntityClicked;

        // Active items are stored in a doubly linked list: head = topmost, tail = bottommost
        private readonly LinkedList<ItemEntry> _activeItems = new();

        private float _itemHeight;
        private float _itemStep; // itemHeight + spacing

        private struct ItemEntry
        {
            public ProductionItemView View;
            public RectTransform Rt;
            public int DataIndex;
        }

        #region UNITY_METHODS

        private void OnDestroy()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrolled);
        }

        #endregion

        #region PUBLIC_METHODS

        public void Initialize(List<BuildingData> dataList, Action<BuildingData> onBuildingClicked)
        {
            if (dataList == null || dataList.Count == 0)
            {
                Debug.LogWarning("[InfiniteScrollView] Cannot initialize with empty data list.");
                return;
            }

            _dataList = dataList;
            // ProductionItemView.Bind Action<EntityData> is needed; so we need to bridge the lambda
            _onEntityClicked = entity => onBuildingClicked?.Invoke(entity as BuildingData);

            MeasureItemSize();

            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _content.anchoredPosition = Vector2.zero;

            // To fill the viewport + 1 buffer item for each side
            int spawnCount = Mathf.CeilToInt(_viewport.rect.height / _itemStep) + 2;
            for (int i = 0; i < spawnCount; i++)
                AppendItemToEnd();

            _scrollRect.onValueChanged.AddListener(OnScrolled);
        }

        public void Clear()
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrolled);
            foreach (var entry in _activeItems)
                LeanPool.Despawn(entry.View);
            _activeItems.Clear();
        }

        #endregion

        #region PRIVATE_METHODS

        // For size measurement, a single spawn is performed, and then returned immediately.
        private void MeasureItemSize()
        {
            var sample = LeanPool.Spawn(_itemPrefab, _content);
            Canvas.ForceUpdateCanvases();
            _itemHeight = sample.GetComponent<RectTransform>().rect.height;
            LeanPool.Despawn(sample);
            _itemStep = _itemHeight + _itemSpacing;
        }

        /// <summary>
        /// When the scroll event is triggered, the top and bottom items are checked.
        /// If the content scrolls up (down scroll), the top item is removed; if the content scrolls down, the bottom item is removed.
        /// The items that are out of the viewport are despawned and spawned again in the opposite side.
        ///
        /// Coordinate system (content pivot.y = 1, top anchor):
        ///   - contentY increases → content scrolls up → top items are removed
        ///   - contentY decreases → content scrolls down → bottom items are removed
        ///   - Item Y values are negative (below the content top)
        ///   - Item is centered in the viewport: contentY + item.anchoredPosition.y
        /// </summary>
        private void OnScrolled(Vector2 _)
        {
            if (_activeItems.Count == 0) return;

            float cy = _content.anchoredPosition.y;
            float vh = _viewport.rect.height;
            float halfH = _itemHeight * 0.5f;

            // Down scroll → content scrolls up (cy increases) → top items are removed
            // Item's bottom edge (center - halfH) is above the viewport top (0) → recycle
            while (_activeItems.Count > 0 &&
                   cy + _activeItems.First.Value.Rt.anchoredPosition.y - halfH > _itemStep)
            {
                LeanPool.Despawn(_activeItems.First.Value.View);
                _activeItems.RemoveFirst();
                AppendItemToEnd();
            }

            // Up scroll → content scrolls down (cy decreases) → bottom items are removed
            // Item's top edge (center + halfH) is below the viewport bottom (-vh) → recycle
            while (_activeItems.Count > 0 &&
                   cy + _activeItems.Last.Value.Rt.anchoredPosition.y + halfH < -vh - _itemStep)
            {
                LeanPool.Despawn(_activeItems.Last.Value.View);
                _activeItems.RemoveLast();
                PrependItemToFront();
            }
        }

        // Adds a new item to the end of the list.
        private void AppendItemToEnd()
        {
            int dataIndex;
            float yPos;

            if (_activeItems.Count == 0)
            {
                dataIndex = 0;
                yPos = -_itemHeight * 0.5f; // content top
            }
            else
            {
                var last = _activeItems.Last.Value;
                dataIndex = (last.DataIndex + 1) % _dataList.Count;
                yPos = last.Rt.anchoredPosition.y - _itemStep; // one step down
            }

            _activeItems.AddLast(CreateEntry(dataIndex, yPos));
        }

        // Adds a new item to the front of the list.
        private void PrependItemToFront()
        {
            var first = _activeItems.First.Value;
            int dataIndex = (first.DataIndex - 1 + _dataList.Count) % _dataList.Count;
            float yPos = first.Rt.anchoredPosition.y + _itemStep; // one step up
            _activeItems.AddFirst(CreateEntry(dataIndex, yPos));
        }

        private ItemEntry CreateEntry(int dataIndex, float yPos)
        {
            var view = LeanPool.Spawn(_itemPrefab, _content);
            var rt = view.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, yPos);
            view.Bind(_dataList[dataIndex], _onEntityClicked);
            return new ItemEntry { View = view, Rt = rt, DataIndex = dataIndex };
        }

        #endregion
    }
}
