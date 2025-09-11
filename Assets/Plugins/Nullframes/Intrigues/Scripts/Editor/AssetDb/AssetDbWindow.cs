using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.EDITOR {
    public class AssetDbWindow : EditorWindow {
        private static IEDatabase database;
        private bool isDbLoaded;
        private bool isUIInitialized;

        private VisualElement root;
        private VisualElement scrollView;
        private VisualElement tagList;

        private List< VisualElement > errorMsg;
        private ObjectField databaseField;

        private TextField searchField;
        private TextField newCategoryField;

        private Button newCategoryBtn;

        private readonly List< VisualElement > selectedItems = new();
        private VisualElement lastSelected;

        private bool isCtrlPressed;

        private List< VisualElement > currentTags = new();

        private List< VisualElement > categories {
            get {
                return scrollView.Query< VisualElement >()
                    .Where(e => e.ClassListContains("categoryField"))
                    .ToList();
            }
        }

        private void OnEnable() {
            isDbLoaded = false;
            isUIInitialized = false;

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable() {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnUndoRedoPerformed() {
            if ( database == null ) return;
            var _selectedItems = new List< VisualElement >(selectedItems);
            UpdateDbList(database);

            if ( _selectedItems.Count > 0 ) {
                var groupedByParent = _selectedItems
                    .Where(item => item is { parent: not null })
                    .GroupBy(item => item.parent)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if ( groupedByParent != null ) {
                    foreach ( var item in groupedByParent ) {
                        Select(item);
                    }
                }
            }
        }

        private void OnEditorUpdate() {
            if ( isCtrlPressed ) {
                foreach ( var tag in currentTags ) {
                    tag.Hide();
                }
            }
        }

        [MenuItem("Tools/Nullframes/Sprite dB")]
        public static void Open() {
            var wnd = GetWindow< AssetDbWindow >();
            wnd.position = new Rect(100, 100, 800, 800);
            wnd.titleContent = new GUIContent("SpriteDb");
        }

        public static void Open(IEDatabase db) {
            if ( db == null ) return;

            database = db;
            EditorPrefs.SetString("Nullframes.PendingDatabasePath", AssetDatabase.GetAssetPath(db));

            var wnd = GetWindow< AssetDbWindow >();
            wnd.position = new Rect(100, 100, 800, 800);
            wnd.titleContent = new GUIContent("SpriteDb");
        }

        private void OnGUI() {
            if ( !isUIInitialized ) return;

            if ( database == null ) {
                errorMsg[ 0 ].Show();
                errorMsg[ 1 ].Hide();
                titleContent = new GUIContent("SpriteDb - Not Initialized");

                isDbLoaded = false;
                return;
            }

            if ( !isDbLoaded ) {
                LoadDb();
                if ( !isDbLoaded ) {
                    return;
                }
            }

            errorMsg[ 0 ].Hide();

            if ( database.spriteDatabase.Count > 0 ) {
                errorMsg[ 1 ].Hide();
            } else {
                errorMsg[ 1 ].Show();
            }

            titleContent = new GUIContent("SpriteDb - " + database.name);

            // Key handler

            Event e = Event.current;
            if ( e.type == EventType.KeyDown && e.control ) {
                isCtrlPressed = true;
                currentTags = scrollView.Query< VisualElement >()
                    .Where(t => t.ClassListContains("tagLabel"))
                    .ToList();
            } else if ( e.type == EventType.KeyUp && e.keyCode == KeyCode.LeftControl ||
                        e.keyCode == KeyCode.RightControl ) {
                isCtrlPressed = false;
                foreach ( var tag in currentTags ) {
                    tag.Show();
                }

                currentTags.Clear();
            }

            if ( Event.current.type == EventType.KeyDown ) {
                if ( Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace ) {
                    if ( selectedItems.Count > 0 ) {
                        DeleteSelectedItems();
                        Save();
                        Event.current.Use();
                    }
                }
            }
        }

        public void CreateGUI() {
            root = rootVisualElement;

            var visualTree = (VisualTreeAsset)EditorGUIUtility.Load("Nullframes/AssetDb.uxml");
            visualTree.CloneTree(root);

            var styleSheet = (StyleSheet)EditorGUIUtility.Load(EditorGUIUtility.isProSkin
                ? "Nullframes/AssetDb_Dark.uss"
                : "Nullframes/AssetDb_Light.uss");
            root.styleSheets.Add(styleSheet);
            
            var toolbar = new Toolbar();

            var flexibleSpacer = new VisualElement {
                style = {
                    flexGrow = 1
                }
            };
            toolbar.Add(flexibleSpacer);

            var docButton = new Button(() => {
                Application.OpenURL("https://www.wlabsocks.com/wiki/index.php?title=Sprite_dB");
            }) { text = "?" };

            toolbar.Add(docButton);
            root.Insert(0, toolbar);

            errorMsg = new List< VisualElement > {
                root.Q< VisualElement >("errorMsg"),
                root.Q< VisualElement >("errorMsg2")
            };

            searchField = root.Q< TextField >("searchField");
            newCategoryField = root.Q< TextField >("newCategoryField");
            databaseField = root.Q< ObjectField >("databaseField");
            databaseField.objectType = typeof( IEDatabase );

            newCategoryBtn = root.Q< Button >("newCategoryBtn");

            scrollView = rootVisualElement.Q< ScrollView >("scrollView");
            tagList = rootVisualElement.Q< ScrollView >("tagList");

            #region New Category Button

            newCategoryBtn.clicked += () => {
                if ( database != null && newCategoryField.text != string.Empty ) {
                    if ( database.spriteDatabase.ContainsKey(newCategoryField.text) ) return;
                    // database.assetDatabase.Add(newCategoryField.text, new AssetDb());

                    var _categoryField = CreateCategory(newCategoryField.text);

                    scrollView.Add(_categoryField);

                    var assetField = CreateAssetField(scrollView);
                    assetField.userData = _categoryField;

                    _categoryField.userData = assetField;

                    Save();
                }
            };

            #endregion

            #region Object Field

            databaseField.RegisterValueChangedCallback(_ => {
                string path = string.Empty;

                if ( databaseField.value != null ) {
                    database = (IEDatabase)databaseField.value;
                    path = AssetDatabase.GetAssetPath(database);
                } else {
                    database = null;
                }

                UpdateDbList(database);

                EditorPrefs.SetString("Nullframes.PendingDatabasePath", path);
            });

            #endregion

            #region Search Field

            var searchField_placeHolder = searchField.Q< Label >("placeHolder");

            searchField.RegisterValueChangedCallback(_ => {
                if ( string.IsNullOrEmpty(searchField.value) ) {
                    searchField_placeHolder.style.display = DisplayStyle.Flex;
                    foreach ( var category in categories ) {
                        var _assetField = (VisualElement)category.userData;

                        foreach ( var _asset in _assetField.Children() ) {
                            var obj = _asset.userData as Sprite;
                            if ( obj == null ) continue;
                            _asset.Show();
                        }

                        category.style.display = DisplayStyle.Flex;
                    }
                } else {
                    searchField_placeHolder.style.display = DisplayStyle.None;

                    foreach ( var category in categories ) {
                        var _assetField = (VisualElement)category.userData;

                        foreach ( var _asset in _assetField.Children() ) {
                            var obj = _asset.userData as Sprite;
                            if ( obj == null ) continue;
                            if ( obj.name.Contains(searchField.value, StringComparison.OrdinalIgnoreCase) ) {
                                _asset.Show();
                            } else {
                                _asset.Hide();
                            }
                        }

                        if ( _assetField.Children().Count(c => c.style.display == DisplayStyle.Flex) == 0 ) {
                            category.style.display = DisplayStyle.None;
                        }
                    }
                }
            });

            #endregion

            #region Category Field

            var categoryField_placeHolder = newCategoryField.Q< Label >("placeHolder");

            newCategoryField.RegisterValueChangedCallback(_ => {
                categoryField_placeHolder.style.display = string.IsNullOrEmpty(newCategoryField.value)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            });

            #endregion

            bool isAnyChanged = false;

            root.RegisterCallback< MouseDownEvent >(evt => {
                if ( evt.button != 0 ) return;

                ClearSelection();


                var tagFields = scrollView.Query< TextField >()
                    .Where(e => e.ClassListContains("tagField"))
                    .ToList();

                var tagLabels = scrollView.Query< Label >()
                    .Where(e => e.ClassListContains("tagLabel"))
                    .ToList();

                foreach ( var tagLbl in tagLabels ) {
                    tagLbl.Show();
                }

                foreach (var tagField in tagFields)
                {
                    tagField.Hide();

                    if (tagField.userData is ValueTuple<bool, Label> { Item1: true } data)
                    {
                        tagField.userData = (false, data.Item2);
                        tagField.SetValueWithoutNotify(string.Empty);
                        isAnyChanged = true;
                    }
                }

                if ( isAnyChanged ) {
                    isAnyChanged = false;
                    Save();
                }
            });

            EditorRoutine.CallNextFrame(LoadDb);

            isUIInitialized = true;
        }

        private void LoadDb() {
            if ( isDbLoaded ) return;

            if ( database == null ) {
                string path = EditorPrefs.GetString("Nullframes.PendingDatabasePath", null);
                if ( !string.IsNullOrEmpty(path) ) {
                    database = AssetDatabase.LoadAssetAtPath< IEDatabase >(path);
                }
            }

            if ( database == null ) return;

            databaseField.value = database;

            UpdateDbList(database);
            
            // 

            UpdateTagList();

            isDbLoaded = true;
        }

        private void UpdateTagList() {
            var uniqueTags = scrollView.Query<Label>()
                .Where(c => c.ClassListContains("tagLabel"))
                .ToList()
                .Select(label => label.text)
                .Distinct()
                .ToList();
            
            tagList.Clear();

            foreach ( var tag in uniqueTags ) {
                var tagLabel = CreateTagLabel(tag, null, tagList);
                tagLabel.style.marginRight = (4);
                tagList.Add(tagLabel);
            }

            isDbLoaded = true;
        }

        private void UpdateDbList(IEDatabase db) {
            scrollView.Clear();
            ClearSelection();

            if ( db == null ) return;

            foreach ( var _db in db.spriteDatabase ) {
                var _categoryField = CreateCategory(_db.Key);

                scrollView.Add(_categoryField);

                var assetField = CreateAssetField(scrollView);
                assetField.userData = _categoryField;

                _categoryField.userData = assetField;

                foreach ( var asset in _db.Value.Sprites ) {
                    var _asset = CreateAsset(asset.Key, asset.Value);
                    assetField.Add(_asset);
                }
            }
        }

        private VisualElement CreateCategory(string categoryName) {
            var _categoryField = new VisualElement() {
                name = categoryName
            };

            _categoryField.AddManipulator(new ContextualMenuManipulator(tagEvt => {
                tagEvt.menu.InsertAction(0, "Remove Category", _ => {
                    var _assetField = (VisualElement)_categoryField.userData;

                    _categoryField.RemoveFromHierarchy();
                    _assetField.RemoveFromHierarchy();

                    Save();
                });
            }));

            _categoryField.AddToClassList("categoryField");

            var _foldButton = new VisualElement();
            _foldButton.AddToClassList("foldoutIcon");

            var _categoryLabel = new Label(categoryName);
            _categoryLabel.AddToClassList("categoryLabel");

            _categoryField.Add(_foldButton);
            _categoryField.Add(_categoryLabel);

            _categoryField.RegisterCallback< DragUpdatedEvent >(evt => {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            });

            _categoryField.RegisterCallback< DragPerformEvent >(evt => {
                var _assetField = _categoryField.userData as VisualElement;

                if ( _assetField == null ) return;

                DragAndDrop.AcceptDrag();

                if ( DragAndDrop.GetGenericData("DraggedAssetsKey") is List< VisualElement > list ) {
                    foreach ( var item in list ) {
                        item.RemoveFromHierarchy();
                        _assetField.Add(item);
                    }
                }

                if (DragAndDrop.objectReferences != null)
                {
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        string path = AssetDatabase.GetAssetPath(obj);
                        if (string.IsNullOrEmpty(path))
                            continue;

                        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();

                        foreach (var sprite in sprites)
                        {
                            if (database.spriteDatabase[categoryName].Sprites.ContainsKey(sprite))
                                continue;

                            var _asset = CreateAsset(sprite, new List<string>());
                            _assetField.Add(_asset);
                        }
                    }
                }

                Save();

                evt.StopPropagation();
            });

            bool foldOut = false;

            _foldButton.RegisterCallback< MouseDownEvent >(evt => {
                if ( evt.button != 0 ) return;
                Foldout();

                evt.StopPropagation();
            });

            void Foldout() {
                foldOut = !foldOut;

                var assetField = (VisualElement)_categoryField.userData;

                if ( foldOut ) {
                    _foldButton.style.rotate = new StyleRotate(new Rotate(new Angle(180f)));
                    foreach ( var _child in assetField.Children() ) {
                        _child.Hide();
                    }
                } else {
                    _foldButton.style.rotate = new StyleRotate(new Rotate(new Angle(270f)));
                    foreach ( var _child in assetField.Children() ) {
                        _child.Show();
                    }
                }
            }

            return _categoryField;
        }

        private VisualElement CreateAssetField(VisualElement categoryField) {
            var _assetField = new VisualElement();
            _assetField.AddToClassList("assetField");

            categoryField.userData = _assetField;

            categoryField.Add(_assetField);

            return _assetField;
        }

        private VisualElement CreateAsset(Sprite obj, IEnumerable< string > tags) {
            var _asset = new VisualElement() { userData = obj };
            _asset.AddToClassList("assetPreview");

            var tagNameField = IEGraphUtility.CreateTextField();
            tagNameField.userData = false;
            tagNameField.Hide();
            tagNameField.AddToClassList("tagField");
            _asset.Add(tagNameField);

            bool isAnyChanges = false;

            tagNameField.RegisterValueChangedCallback(evt => {
                if ( tagNameField.userData is ValueTuple< bool, Label > { Item2: not null } data ) {
                    var selected = data.Item2;

                    tagNameField.value = tagNameField.text.RemoveSpecialCharacters();
                    selected.text = "#" + tagNameField.value;

                    if ( evt.previousValue != evt.newValue ) {
                        isAnyChanges = true;
                        tagNameField.userData = ( true, selected );
                    }
                }
            });

            tagNameField.RegisterCallback< KeyDownEvent >(evt => {
                if ( evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter ) {
                    tagNameField.Hide();

                    var tagLabels = scrollView.Query< Label >()
                        .Where(e => e.ClassListContains("tagLabel"))
                        .ToList();

                    foreach ( var tagLabel in tagLabels )
                        tagLabel.Show();

                    if ( isAnyChanges )
                        Save();

                    evt.StopPropagation();
                }
            });


            foreach ( var tag in tags ) {
                var tagLabel = CreateTagLabel(tag, tagNameField, _asset);
                _asset.Add(tagLabel);
            }

            var _label = new Label() { text = obj.name.Shortener() };
            _label.AddToClassList("assetLabel");
            _asset.tooltip = obj.name;
            _asset.Add(_label);

            Texture2D texture = obj.texture;
            _asset.style.backgroundImage = texture;
            
            // EditorApplication.update += TryUpdatePreview;
            //
            // void TryUpdatePreview()
            // {
            //     Texture2D bgTexture = AssetPreview.GetAssetPreview(obj);
            //     if (bgTexture != null)
            //     {
            //         _asset.style.backgroundImage = bgTexture;
            //         EditorApplication.update -= TryUpdatePreview;
            //     }
            // }

            SetupSelectable(_asset);

            bool isDragging = false;
            Vector2 dragStart = Vector2.zero;

            _asset.RegisterCallback< MouseDownEvent >(evt => {
                if ( evt.button != 0 ) return;
                dragStart = evt.mousePosition;
                isDragging = true;
                evt.StopPropagation();
            });

            _asset.RegisterCallback< MouseMoveEvent >(evt => {
                if ( !isDragging ) return;

                if ( ( evt.mousePosition - dragStart ).sqrMagnitude > 16f ) {
                    DragAndDrop.PrepareStartDrag();
                    var itemsToDrag = selectedItems.Contains(_asset)
                        ? selectedItems.ToList()
                        : new List< VisualElement > { _asset };

                    DragAndDrop.SetGenericData("DraggedAssetsKey", itemsToDrag);
                    DragAndDrop.SetGenericData("MyAssetKey", _asset);
                    DragAndDrop.StartDrag("Dragging Element");

                    isDragging = false;
                }
            });

            _asset.RegisterCallback< MouseUpEvent >(evt => {
                if ( evt.button == 0 )
                    isDragging = false;
            });

            _asset.RegisterCallback< DragUpdatedEvent >(evt => {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            });

            _asset.RegisterCallback< DragPerformEvent >(evt => {
                DragAndDrop.AcceptDrag();

                var parent = _asset.parent;
                if ( parent == null )
                    return;

                int baseIndex = parent.IndexOf(_asset);
                if ( baseIndex < 0 )
                    return;

                if ( DragAndDrop.objectReferences != null ) {
                    string categoryName = ( (VisualElement)parent.userData ).name;

                    int insertIndex = baseIndex;
                    foreach (var _obj in DragAndDrop.objectReferences)
                    {
                        string path = AssetDatabase.GetAssetPath(_obj);

                        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();

                        foreach (var sprite in sprites)
                        {
                            if (database.spriteDatabase[categoryName].Sprites.ContainsKey(sprite))
                                continue;

                            var newAsset = CreateAsset(sprite, new List<string>());
                            parent.Insert(insertIndex++, newAsset);
                        }
                    }
                }

                if ( DragAndDrop.GetGenericData("MyTagKey") is Label draggedTag ) {
                    var _selectedItems = new List< VisualElement >(selectedItems) { _asset };
                    string tagText = draggedTag.text.TrimStart('#');

                    foreach ( var selectedItem in _selectedItems ) {
                        var existingTags = selectedItem.Query< Label >()
                            .Where(e => e.ClassListContains("tagLabel"))
                            .ToList() // <- UQueryBuilder<Label> → List<Label>
                            .Select(lbl => lbl.text.TrimStart('#'))
                            .ToList();

                        if ( existingTags.Contains(tagText) || existingTags.Count >= 5 )
                            continue;

                        var tagField = selectedItem.Q< TextField >();

                        var tagLabel = CreateTagLabel(tagText, tagField, selectedItem);
                        selectedItem.Add(tagLabel);
                    }

                    Save();
                }

                if ( DragAndDrop.GetGenericData("MyAssetKey") is VisualElement draggedItem ) {
                    if ( draggedItem == _asset || selectedItems.Contains(_asset) )
                        return;

                    var itemsToInsert = selectedItems.Contains(draggedItem)
                        ? selectedItems.ToList()
                        : selectedItems.Append(draggedItem).ToList();

                    foreach ( var item in itemsToInsert ) {
                        if ( item != null && item != _asset )
                            item.RemoveFromHierarchy();
                    }

                    int insertIndex = Mathf.Clamp(baseIndex, 0, parent.childCount);
                    foreach ( var item in itemsToInsert ) {
                        if ( item != null && item != _asset )
                            parent.Insert(insertIndex++, item);
                    }
                }

                Save();
                evt.StopPropagation();
            });

            _asset.AddManipulator(new ContextualMenuManipulator(evt => {
                evt.menu.InsertAction(0, "Add Tag", _ => {
                    var _selectedItems = new HashSet< VisualElement >(selectedItems) { _asset };

                    foreach ( var selectedItem in _selectedItems ) {
                        var tagLabels = selectedItem.Query< Label >()
                            .Where(e => e.ClassListContains("tagLabel"))
                            .ToList();

                        if ( tagLabels.Count > 4 ) continue;

                        var tagField = selectedItem.Q< TextField >();

                        var tagLabel = CreateTagLabel("Tag", tagField, selectedItem);
                        selectedItem.Add(tagLabel);
                    }

                    Save();
                });

                evt.menu.InsertSeparator("", 1);
                evt.menu.InsertAction(2, "Remove Asset", _ => {
                    _asset.RemoveFromHierarchy();
                    Save();
                });
                evt.menu.InsertSeparator("", 3);
                evt.menu.InsertAction(4, "Clear Tag", _ => {
                    var _selectedItems = new HashSet< VisualElement >(selectedItems) { _asset };

                    foreach ( var tag in _selectedItems.Select(selectedItem => selectedItem.Query< Label >()
                                 .Where(e => e.ClassListContains("tagLabel"))
                                 .ToList()).SelectMany(tagLabels => tagLabels) ) { tag.RemoveFromHierarchy(); }

                    Save();
                });
            }));

            return _asset;
        }

        private Label CreateTagLabel(string tag, TextField tagNameField, VisualElement _asset) {
            var tagLabel = new Label() {
                text = "#" + tag.RemoveSpecialCharacters()
            };
            tagLabel.AddToClassList("tagLabel");

            tagLabel.AddManipulator(new ContextualMenuManipulator(tagEvt => {
                tagEvt.menu.InsertAction(0, "Remove Tag", _ => {
                    tagLabel.RemoveFromHierarchy();
                    Save();
                });
            }));

            tagLabel.RegisterCallback< MouseDownEvent >(e => e.StopPropagation());
            tagLabel.RegisterCallback< ClickEvent >(e => e.StopPropagation());

            tagLabel.RegisterCallback< MouseUpEvent >(tagEvt => {
                if ( tagEvt.button != 0 || tagNameField == null) return;

                var parent = tagNameField.parent;
                if ( parent != null ) {
                    tagNameField.RemoveFromHierarchy();
                    parent.Add(tagNameField);
                }

                var tagLabels = _asset.Query< Label >()
                    .Where(e => e.ClassListContains("tagLabel"))
                    .ToList();

                foreach ( var tagLbl in tagLabels )
                    tagLbl.Hide();

                tagNameField.SetValueWithoutNotify(tagLabel.text);
                tagNameField.userData = ( true, tagLabel );
                tagNameField.Show();
                tagNameField.Focus();
                tagNameField.SelectAll();

                tagEvt.StopPropagation();
            });

            bool isDragging = false;
            Vector2 dragStart = Vector2.zero;

            tagLabel.RegisterCallback< MouseDownEvent >(evt => {
                if ( evt.button != 0 ) return;
                dragStart = evt.mousePosition;
                isDragging = true;
                evt.StopPropagation();
            });

            tagLabel.RegisterCallback< MouseMoveEvent >(evt => {
                if ( !isDragging ) return;

                if ( ( evt.mousePosition - dragStart ).sqrMagnitude > 16f ) {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("MyTagKey", tagLabel);
                    DragAndDrop.StartDrag("Dragging Element");
                    isDragging = false;
                }
            });

            tagLabel.RegisterCallback< MouseUpEvent >(evt => {
                if ( evt.button == 0 )
                    isDragging = false;
            });

            return tagLabel;
        }


        private float lastClickTime;
        private const float doubleClickThreshold = 0.3f;
        private VisualElement lastClickedItem;

        private void SetupSelectable(VisualElement item) {
            item.RegisterCallback< ClickEvent >(evt => {
                if ( evt.button != 0 ) return;
                bool shift = evt.shiftKey;
                bool ctrl = evt.ctrlKey || evt.commandKey;

                var parent = item.parent;
                int index = parent.IndexOf(item);

                if ( shift && lastSelected != null ) {
                    int lastIndex = parent.IndexOf(lastSelected);
                    if ( lastIndex < 0 ) {
                        ClearSelection();
                        Select(item);
                        lastSelected = item;
                        return;
                    }

                    int from = Mathf.Min(index, lastIndex);
                    int to = Mathf.Max(index, lastIndex);

                    for ( int i = from; i <= to && i < parent.childCount; i++ )
                        Select(parent[ i ]);
                } else if ( ctrl ) {
                    if ( selectedItems.Contains(item) )
                        Deselect(item);
                    else
                        Select(item);
                } else {
                    ClearSelection();
                    Select(item);
                }

                // Double-CLick

                float time = Time.realtimeSinceStartup;

                if ( item == lastClickedItem && time - lastClickTime < doubleClickThreshold ) {
                    var asset = item.userData as Sprite;
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);

                    lastClickedItem = null;
                    lastClickTime = 0;
                    return;
                }

                lastClickedItem = item;
                lastClickTime = time;

                //

                lastSelected = item;

                evt.StopPropagation();
            });
        }

        private void ClearSelection() {
            foreach ( var selected in selectedItems.ToList() )
                Deselect(selected);
        }

        private void DeleteSelectedItems() {
            foreach ( var item in selectedItems.ToList() ) {
                item.RemoveFromHierarchy();
                selectedItems.Remove(item);
            }
        }

        private void Select(VisualElement item) {
            if ( selectedItems.Contains(item) ) return;
            selectedItems.Add(item);
            item.AddToClassList("selected");
        }

        private void Deselect(VisualElement item) {
            selectedItems.Remove(item);
            item.RemoveFromClassList("selected");
        }

        private void Save() {
            Undo.RecordObject(database, "_intrigues_assetDb");

            database.spriteDatabase = new SerializableDictionary< string, AssetDb >();

            foreach ( var category in categories ) {
                var _assetDb = new AssetDb();

                var _assetField = (VisualElement)category.userData;

                foreach ( var _asset in _assetField.Children() ) {
                    var obj = _asset.userData as Sprite;
                    if ( obj == null ) continue;

                    var tags = _asset.Query< Label >()
                        .Where(label => label.ClassListContains("tagLabel"))
                        .ToList() // <- UQueryBuilder<Label> → List<Label>
                        .Select(label => {
                            var text = label.text.RemoveSpecialCharacters();
                            return string.IsNullOrEmpty(text) ? "Tag" : text;
                        })
                        .ToList();

                    _assetDb.Sprites.Add(obj, tags);
                }

                database.spriteDatabase.Add(category.name, _assetDb);
            }

            UpdateTagList();

            EditorUtility.SetDirty(database);
        }
    }
}