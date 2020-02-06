using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NpmPublisherSupport
{
    public class NpmPackagesWindow : EditorWindow
    {
        private static readonly List<int> TreeEmptySelection = new List<int>();

        private TreeViewState _treeViewState;
        private PackageTreeView _treeView;

        [MenuItem("Window/Npm/Packages Window", priority = 499)]
        public static void ShowNpmPackagesWindow()
        {
            var window = GetWindow<NpmPackagesWindow>();
            window.titleContent = new GUIContent("Npm Packages");
            window.Show();
        }

        private void OnLostFocus()
        {
            _treeView?.SetSelection(TreeEmptySelection);
        }

        private void OnGUI()
        {
            if (_treeViewState == null)
                _treeViewState = new TreeViewState();

            if (_treeView == null)
                _treeView = new PackageTreeView(_treeViewState, OnTreeSelectionChanged);

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                DrawToolbar();
                DrawContent();
            }
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    _treeView.Reload();
                    Repaint();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawContent()
        {
            GUILayout.Label("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var rect = GUILayoutUtility.GetLastRect();
            _treeView.OnGUI(rect);
        }

        private void OnTreeSelectionChanged()
        {
            Repaint();
        }

        private class PackageTreeView : TreeView
        {
            private const string NpmMenuItemPrefix = "Assets/NPM/";
            private static readonly List<PackageMenuItem> PackageMenuItems;

            private readonly Action _onSelectionChanged;

            static PackageTreeView()
            {
                PackageMenuItems = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(asm => asm.GetTypes())
                    .SelectMany(type =>
                        type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    .Where(m => m.GetParameters().Length == 0)
                    .Where(m =>
                    {
                        var attr = (MenuItem) m.GetCustomAttribute(typeof(MenuItem), false);
                        return attr != null && !attr.validate && attr.menuItem.StartsWith(NpmMenuItemPrefix);
                    })
                    .Select(method =>
                    {
                        var attr = (MenuItem) method.GetCustomAttribute(typeof(MenuItem), false);
                        var menu = attr.menuItem.Substring(NpmMenuItemPrefix.Length);
                        return new PackageMenuItem {MethodInfo = method, MenuItem = new GUIContent(menu)};
                    })
                    .ToList();
            }

            public PackageTreeView(TreeViewState treeViewState, Action onSelectionChanged)
                : base(treeViewState)
            {
                _onSelectionChanged = onSelectionChanged;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem {id = -1, depth = -1, displayName = "Root"};

                var index = 0;
                foreach (var packageAsset in UpmClientUtils.FindLocalPackages())
                {
                    root.AddChild(new PackageTreeViewItem(index++, packageAsset));
                }

                return root;
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                base.SelectionChanged(selectedIds);

                Selection.objects = selectedIds
                    .Select(id => rootItem.children[id])
                    .Select(item => ((PackageTreeViewItem) item).SelectionObject)
                    .ToArray();

                _onSelectionChanged?.Invoke();
            }

            protected override float GetCustomRowHeight(int row, TreeViewItem item) => 40;

            protected override void RowGUI(RowGUIArgs args)
            {
                if (!(args.item is PackageTreeViewItem item))
                {
                    base.RowGUI(args);
                    return;
                }

                var rect = args.rowRect;

                var displayNameContent = new GUIContent(item.Package.displayName);
                var displayNameSize = Styles.HeaderDisplayNameLabel.CalcSize(displayNameContent);
                var displayNameRect = new Rect(rect)
                {
                    width = displayNameSize.x,
                    height = displayNameSize.y,
                };
                GUI.Label(displayNameRect, displayNameContent, Styles.HeaderDisplayNameLabel);

                var versionRect = new Rect(rect) {x = rect.x + displayNameSize.x};
                GUI.Label(versionRect, item.Package.version, Styles.HeaderVersionLabel);

                var nameRect = new Rect(rect) {y = rect.y + displayNameSize.y - 3};
                GUI.Label(nameRect, item.Package.name, EditorStyles.label);
            }

            protected override void ContextClicked()
            {
                var contextMenu = new GenericMenu();

                foreach (var item in PackageMenuItems)
                {
                    contextMenu.AddItem(item.MenuItem, false, () => item.MethodInfo.Invoke(null, null));
                }

                contextMenu.ShowAsContext();
            }
        }

        private class PackageTreeViewItem : TreeViewItem
        {
            public Package Package { get; }

            public Object SelectionObject { get; }

            public PackageTreeViewItem(int id, TextAsset packageJsonAsset) : base(id, 0)
            {
                Package = JsonUtility.FromJson<Package>(packageJsonAsset.text);

                var rootFolderPath = NpmPublishMenu.GetPackageRootFolder(packageJsonAsset);
                SelectionObject = AssetDatabase.LoadMainAssetAtPath(rootFolderPath);
            }
        }

        private class PackageMenuItem
        {
            public MethodInfo MethodInfo;
            public GUIContent MenuItem;
        }
    }
}