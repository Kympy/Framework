using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{

    public sealed class LongKeyDropdownItem : AdvancedDropdownItem
    {
        public readonly long Key;

        public LongKeyDropdownItem(string name, long key)
            : base(name)
        {
            Key = key;
        }
    }
    
    public sealed class StringDataDropdown : AdvancedDropdown
    {
        private readonly IReadOnlyList<long> _longKeys;
        private readonly Action<long> _onSelected;

        public StringDataDropdown(
            AdvancedDropdownState state,
            IReadOnlyList<long> longKeys,
            Action<long> onSelected)
            : base(state)
        {
            _longKeys = longKeys;
            _onSelected = onSelected;
            minimumSize = new Vector2(300, 400);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select String Key");

            foreach (var hashKey in _longKeys)
            {
                // note: 스트링 사용 방식이 unity localization으로 변경됨에 따라 주석처리
                // var data = GameDataManager.Editor.GetStringData(hashKey);
                // 여기에 이름과, 키 설정
                // var item = new LongKeyDropdownItem($"{data.KR} / {data.KEY}", hashKey);
                // root.AddChild(item);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is LongKeyDropdownItem longKeyDropdownItem)
            {
                _onSelected?.Invoke(longKeyDropdownItem.Key);
            }
        }
    }
}