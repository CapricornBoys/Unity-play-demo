using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    /// <summary>
    /// 运行时扫描界面层级并缓存 UI 组件。
    /// 节点名称唯一时可以直接用名称绑定；名称重复时使用相对路径。
    /// </summary>
    public sealed class UIComponentRegistry
    {
        private readonly Transform root;
        private readonly Dictionary<string, Transform> transformsByPath =
            new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly Dictionary<string, Transform> uniqueTransformsByName =
            new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly HashSet<string> duplicateNames =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<ComponentKey, Component> componentCache =
            new Dictionary<ComponentKey, Component>();

        public UIComponentRegistry(Transform root)
        {
            this.root = root != null
                ? root
                : throw new ArgumentNullException(nameof(root));
            RegisterHierarchy(root, string.Empty);
        }

        public T Get<T>(string id) where T : Component
        {
            if (TryGet(id, out T component))
            {
                return component;
            }

            throw new InvalidOperationException(
                $"UI 组件绑定失败：在 {root.name} 中找不到 '{id}' 上的 {typeof(T).Name}。");
        }

        public bool TryGet<T>(string id, out T component) where T : Component
        {
            ComponentKey key = new ComponentKey(id, typeof(T));
            if (componentCache.TryGetValue(key, out Component cached))
            {
                component = cached as T;
                return component != null;
            }

            Transform target = FindTransform(id);
            component = target != null ? target.GetComponent<T>() : null;
            if (component == null)
            {
                return false;
            }

            componentCache[key] = component;
            return true;
        }

        public T[] GetAll<T>(string id, bool includeInactive = true) where T : Component
        {
            Transform target = FindTransform(id);
            if (target == null)
            {
                throw new InvalidOperationException(
                    $"UI 节点绑定失败：在 {root.name} 中找不到 '{id}'。");
            }

            return target.GetComponentsInChildren<T>(includeInactive);
        }

        /// <summary>
        /// 运行时增删了界面节点后，调用此方法重建索引。
        /// </summary>
        public void Rebuild()
        {
            transformsByPath.Clear();
            uniqueTransformsByName.Clear();
            duplicateNames.Clear();
            componentCache.Clear();
            RegisterHierarchy(root, string.Empty);
        }

        private Transform FindTransform(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return root;
            }

            string normalizedId = id.Replace('\\', '/').Trim('/');
            if (transformsByPath.TryGetValue(normalizedId, out Transform byPath))
            {
                return byPath;
            }

            if (duplicateNames.Contains(normalizedId))
            {
                throw new InvalidOperationException(
                    $"UI 节点名称重复：'{normalizedId}'。请改用相对路径绑定。");
            }

            uniqueTransformsByName.TryGetValue(normalizedId, out Transform byName);
            return byName;
        }

        private void RegisterHierarchy(Transform current, string parentPath)
        {
            for (int i = 0; i < current.childCount; i++)
            {
                Transform child = current.GetChild(i);
                string path = string.IsNullOrEmpty(parentPath)
                    ? child.name
                    : $"{parentPath}/{child.name}";

                transformsByPath[path] = child;
                RegisterName(child);
                RegisterHierarchy(child, path);
            }
        }

        private void RegisterName(Transform target)
        {
            if (duplicateNames.Contains(target.name))
            {
                return;
            }

            if (uniqueTransformsByName.ContainsKey(target.name))
            {
                uniqueTransformsByName.Remove(target.name);
                duplicateNames.Add(target.name);
                return;
            }

            uniqueTransformsByName.Add(target.name, target);
        }

        private readonly struct ComponentKey : IEquatable<ComponentKey>
        {
            private readonly string id;
            private readonly Type type;

            public ComponentKey(string id, Type type)
            {
                this.id = id ?? string.Empty;
                this.type = type;
            }

            public bool Equals(ComponentKey other)
            {
                return string.Equals(id, other.id, StringComparison.Ordinal)
                       && type == other.type;
            }

            public override bool Equals(object obj)
            {
                return obj is ComponentKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (id.GetHashCode() * 397) ^ type.GetHashCode();
                }
            }
        }
    }
}
