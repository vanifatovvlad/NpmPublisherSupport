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

        [SerializeField]
        private bool compactMode = false;

        [SerializeField]
        private TreeViewState treeViewState;

        private PackageTreeView _treeView;

        [MenuItem("Window/Npm/Packages Window", priority = 499)]
        public static void ShowNpmPackagesWindow()
        {
            var window = GetWindow<NpmPackagesWindow>();
            window.titleContent = new GUIContent("Npm Packages");
            window.Show();
        }

        private void OnEnable()
        {
            NpmPublishAssetProcessor.PackageImported += PackageImported;
        }

        private void OnDisable()
        {
            NpmPublishAssetProcessor.PackageImported -= PackageImported;
        }

        private void OnLostFocus()
        {
            _treeView?.SetSelection(TreeEmptySelection);
        }

        private void OnGUI()
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            if (_treeView == null)
                _treeView = new PackageTreeView(treeViewState);

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                DrawToolbar();
                DrawContent();
            }
        }

        private void DrawToolbar()
        {
            var expand = GUILayout.ExpandWidth(true);
            var noExpand = GUILayout.ExpandWidth(false);

            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, noExpand))
                {
                    _treeView.searchString = "";
                    _treeView.Reload();
                }

                var search = EditorGUILayout.TextField(_treeView.searchString, EditorStyles.toolbarSearchField, expand);
                if (search != _treeView.searchString)
                {
                    _treeView.searchString = search;
                    _treeView.Reload();
                }

                compactMode = GUILayout.Toggle(compactMode, "Compact", EditorStyles.toolbarButton, noExpand);
                if (compactMode != _treeView.CompactMode)
                {
                    _treeView.SetCompactMode(compactMode);
                    _treeView.Repaint();
                }
            }
        }

        private void DrawContent()
        {
            GUILayout.Label("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var rect = GUILayoutUtility.GetLastRect();
            _treeView.OnGUI(rect);
        }

        private void PackageImported()
        {
            _treeView.Reload();
            Repaint();
        }

        private class PackageTreeView : TreeView
        {
            private const string NpmMenuItemPrefix = "Assets/NPM/";
            private static readonly List<PackageMenuItem> PackageMenuItems;

            public bool CompactMode { get; private set; }

            static PackageTreeView()
            {
                var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                PackageMenuItems = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(asm => asm.GetTypes())
                    .SelectMany(type => type.GetMethods(flags))
                    .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0)
                    .SelectMany(method =>
                    {
                        return method.GetCustomAttributes(typeof(MenuItem), false)
                            .Select(attr => (MenuItem) attr)
                            .Where(attr => !attr.validate && attr.menuItem.StartsWith(NpmMenuItemPrefix))
                            .Select(attr =>
                            {
                                var menu = attr.menuItem.Substring(NpmMenuItemPrefix.Length);
                                return new PackageMenuItem {MethodInfo = method, MenuItem = new GUIContent(menu)};
                            });
                    })
                    .ToList();
            }

            public PackageTreeView(TreeViewState treeViewState)
                : base(treeViewState)
            {
                Reload();
            }

            public void SetCompactMode(bool compact)
            {
                CompactMode = compact;
                RefreshCustomRowHeights();
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem {id = -1, depth = -1, displayName = "Root"};

                var search = searchString.ToLower();

                var children = UpmClientUtils.FindLocalPackages()
                    .Select((packageAsset, index) => new PackageTreeViewItem(index, packageAsset))
                    .Where(item => MatchSearch(item, search))
                    .ToList()
                    .OrderBy(item => item.displayName)
                    .ToList()
                    .Select(item => (TreeViewItem) item)
                    .ToList();

                SetupParentsAndChildrenFromDepths(root, children);

                return root;
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                base.SelectionChanged(selectedIds);

                Selection.objects = selectedIds
                    .Select(id => (PackageTreeViewItem) FindItem(id, rootItem))
                    .Select(item => item.SelectionObject)
                    .ToArray();
            }

            protected override float GetCustomRowHeight(int row, TreeViewItem item) =>
                CompactMode ? EditorGUIUtility.singleLineHeight : 42;

            protected override void RowGUI(RowGUIArgs args)
            {
                if (!(args.item is PackageTreeViewItem item))
                {
                    base.RowGUI(args);
                    return;
                }

                var rect = args.rowRect;

                if (CompactMode)
                {
                    GUI.Label(rect, item.displayName, EditorStyles.largeLabel);
                    return;
                }

                var nameRect = DrawLabel(rect.x, rect.y, item.Package.displayName, Styles.HeaderDisplayNameLabel);
                DrawLabel(nameRect.xMax, nameRect.yMin, item.Package.version, Styles.HeaderVersionLabel);
                DrawLabel(nameRect.xMin, nameRect.yMax - 3, item.Package.name, Styles.HeaderNameLabel);
            }

            private static Rect DrawLabel(float x, float y, string label, GUIStyle style)
            {
                var content = new GUIContent(label);
                var size = style.CalcSize(content);
                var rect = new Rect {x = x, y = y, width = size.x, height = size.y};
                GUI.Label(rect, content, style);
                return rect;
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

            private static bool MatchSearch(PackageTreeViewItem item, string searchLower)
            {
                if (string.IsNullOrEmpty(searchLower)) return true;
                return item.Package.name.ToLower().Contains(searchLower) ||
                       item.Package.displayName.ToLower().Contains(searchLower);
            }
        }

        private sealed class PackageTreeViewItem : TreeViewItem
        {
            public Package Package { get; }

            public Object SelectionObject { get; }

            public PackageTreeViewItem(int id, TextAsset packageJsonAsset) : base(id, 0)
            {
                Package = JsonUtility.FromJson<Package>(packageJsonAsset.text);

                var rootFolderPath = NpmPublishMenu.GetPackageRootFolder(packageJsonAsset);
                SelectionObject = AssetDatabase.LoadMainAssetAtPath(rootFolderPath);

                displayName = Package.displayName;
            }
        }

        private class PackageMenuItem
        {
            public MethodInfo MethodInfo;
            public GUIContent MenuItem;
        }
    }
}