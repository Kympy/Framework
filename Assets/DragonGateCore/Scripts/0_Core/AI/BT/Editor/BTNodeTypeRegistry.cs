using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DragonGate;
using UnityEditor;

/// <summary>
/// [BTNode] 어트리뷰트가 붙은 BTNode 서브클래스를 스캔해서
/// 에디터 컨텍스트 메뉴에 제공한다.
/// </summary>
public static class BTNodeTypeRegistry
{
    private static List<BTNodeEntry> _entries;

    public struct BTNodeEntry
    {
        public string TypeName;     // AssemblyQualifiedName
        public string DisplayName;
        public string Category;
    }

    public static IReadOnlyList<BTNodeEntry> GetAllEntries()
    {
        if (_entries == null) Build();
        return _entries;
    }

    public static string GetDisplayName(string typeName)
    {
        if (_entries == null) Build();
        var entry = _entries.FirstOrDefault(e => e.TypeName == typeName);
        return string.IsNullOrEmpty(entry.DisplayName) ? typeName : entry.DisplayName;
    }

    private static void Build()
    {
        _entries = new List<BTNodeEntry>();

        // TypeCache는 Editor 전용이지만 빠름
        var types = TypeCache.GetTypesWithAttribute<BTNodeAttribute>()
            .Where(t => !t.IsAbstract && typeof(BTNode).IsAssignableFrom(t));

        foreach (var type in types)
        {
            var nodeAttribute = type.GetCustomAttribute<BTNodeAttribute>();
            var categoryAttribute = type.GetCustomAttribute<BTCategoryAttribute>();
            _entries.Add(new BTNodeEntry
            {
                TypeName    = type.AssemblyQualifiedName,
                DisplayName = nodeAttribute.DisplayName,
                Category    = categoryAttribute?.Category ?? string.Empty
            });
        }

        // 카테고리 → 이름 순으로 정렬
        _entries = _entries
            .OrderBy(e => e.Category)
            .ThenBy(e => e.DisplayName)
            .ToList();
    }
}