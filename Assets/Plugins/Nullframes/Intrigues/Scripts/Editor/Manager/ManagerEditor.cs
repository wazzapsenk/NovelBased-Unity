using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.UI;
using Nullframes.Intrigues.Utils;
using Nullframes.Intrigues.XML;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.EDITOR {
    [CustomEditor(typeof( IntrigueManager ))]
    public class ManagerEditor : Editor {
        public override bool UseDefaultMargins() {
            return false;
        }

        private IMGUIContainer root;
        private VisualElement container;
        private VisualElement referenceField;
        private VisualElement LocalisationMenuField;
        private VisualElement languagesField;

        private VisualElement createGroup;

        private SerializedProperty database;
        private SerializedProperty dialogueManager;
        private SerializedProperty currentLanguage;

        private ObjectField databaseInput;

        private Label referenceLabel;
        private VisualElement LocalisationButton;
        private VisualElement AssetDbButton;

        private enum Menu {
            Localisation = 0,
        }

        private Menu currentMenu = Menu.Localisation;

        private IEDatabase ieDatabase => (IEDatabase)database.objectReferenceValue;

        private void OnEnable() {
            // Each editor window contains a root VisualElement object
            root = new IMGUIContainer();

            // Import UXML
            var visualTree = (VisualTreeAsset)EditorGUIUtility.Load("Nullframes/Manager.uxml");
            visualTree.CloneTree(root);

            var styleSheet = (StyleSheet)EditorGUIUtility.Load(EditorGUIUtility.isProSkin
                ? "Nullframes/ManagerStyles_Dark.uss"
                : "Nullframes/ManagerStyles_Light.uss");
            root.styleSheets.Add(styleSheet);

            database = serializedObject.FindProperty("ieDatabase");
            dialogueManager = serializedObject.FindProperty("dialogueManager");
            currentLanguage = serializedObject.FindProperty("currentLanguage");

            root.onGUIHandler += Update;

            root.name = "root";

            root.RegisterCallback< MouseDownEvent >(_ => {
                List< VisualElement > children = new List< VisualElement >();

                if ( languagesField != null ) {
                    languagesField.GetChilds< VisualElement >(c => {
                        if ( c is not Button )
                            children.Add(c);
                    });
                    foreach ( var child in children ) {
                        languagesField.Remove(child);
                    }
                }
            });

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable() {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            root.onGUIHandler -= Update;
        }

        private void Update() {
            if ( database.objectReferenceValue == null ) {
                //Database Not Exists
                createGroup?.Show();
                LocalisationMenuField?.Hide();
                LocalisationButton?.Hide();
                AssetDbButton?.Hide();
                referenceField?.Hide();
                referenceLabel?.Hide();
            } else {
                //Database Exists
                createGroup?.Hide();
                LocalisationButton?.Show();
                AssetDbButton?.Show();
                referenceField?.Show();
                referenceLabel?.Show();

                switch ( currentMenu ) {
                    case Menu.Localisation: {
                        LocalisationMenuField?.Show();
                        if ( LocalisationMenuField == null )
                            Localisation();
                        break;
                    }
                }
            }
        }

        private void OnUndoRedoPerformed() {
            if ( ieDatabase != null )
                Reload();
        }

        public override VisualElement CreateInspectorGUI() {
            container = root.Q< VisualElement >("container");
            container.Clear();
            container.AddClasses("container");

            AttachDatabaseField();
            References();
            LoadMenuButtons();
            Localisation();
            return root;
        }

        private void Reload() {
            container.Clear();

            AttachDatabaseField();
            References();
            LoadMenuButtons();
            Localisation();
        }

        private void LoadMenuButtons() {
            // Button Field
            AssetDbButton = new VisualElement {
                style = {
                    display = new StyleEnum< DisplayStyle >(DisplayStyle.None),
                    flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row)
                }
            };
            var AssetDbLbl = IEGraphUtility.CreateLabel("SpriteDb");
            var dbIcon = EditorGUIUtility.IconContent("d_Project").image as Texture2D;
            var dbImage = new Image {
                image = dbIcon,
                style = {
                    width = 16,
                    height = 16,
                    marginRight = 8
                }
            };

            AssetDbButton.Add(dbImage);
            AssetDbButton.Add(AssetDbLbl);

            AssetDbButton.AddClasses("menuField");

            // End

            // Button Field
            LocalisationButton = new VisualElement {
                style = {
                    display = new StyleEnum< DisplayStyle >(DisplayStyle.None),
                    flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row)
                }
            };
            var LocalisationElementLabel = IEGraphUtility.CreateLabel("Localisation");
            var lclICon = EditorGUIUtility.IconContent("d_Text Icon").image as Texture2D;
            var lclImg = new Image {
                image = lclICon,
                style = {
                    width = 16,
                    height = 16,
                    marginRight = 8
                }
            };

            LocalisationButton.Add(lclImg);
            LocalisationButton.Add(LocalisationElementLabel);

            LocalisationButton.AddClasses("menuField");

            // End

            LocalisationButton.RegisterCallback< MouseDownEvent >(_ => {
                currentMenu = Menu.Localisation;
            });

            AssetDbButton.RegisterCallback< MouseDownEvent >(_ => {
                AssetDbWindow.Open(ieDatabase);
            });
            
        }

        private void Localisation() {
            if ( database.objectReferenceValue == null ) return;
            LocalisationMenuField = new VisualElement() {
                name = "LocalisationMenuField",
                style = {
                    display = new StyleEnum< DisplayStyle >(DisplayStyle.None)
                }
            };
            LocalisationMenuField.AddClasses("box-field");

            #region LOCALISATION

            if ( ieDatabase.localisationTexts.Count > 0 ) {
                #region CURRENT_LANGUAGE

                var languageGroup = new VisualElement() {
                    name = "currentLanguage",
                    style = {
                        flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row),
                        alignItems = new StyleEnum< Align >(Align.Center),
                        paddingLeft = 15
                    }
                };

                var languageLabel = IEGraphUtility.CreateLabel("Current Language");
                languageLabel.AddClasses("referenceLabel");

                languageGroup.Add(languageLabel);

                var languageList = IEGraphUtility.CreateDropdown(ieDatabase.localisationTexts.Keys);

                var index = languageList.choices.IndexOf(currentLanguage.stringValue);
                if ( index == -1 && languageList.choices.Count > 0 ) {
                    currentLanguage.stringValue = languageList.choices[ 0 ];
                    serializedObject.ApplyModifiedProperties();
                }

                languageList.index = index == -1 ? 0 : index;

                var dropdownChild = languageList.GetChild< VisualElement >();
                dropdownChild.SetPadding(5);
                dropdownChild.style.paddingLeft = 10;
                dropdownChild.style.paddingRight = 10;
                dropdownChild.GetChild< TextElement >().style.color =
                    new StyleColor(NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#FFFFFF" : "#212121"));

                languageList.style.marginLeft = 0f;
                languageList.style.marginBottom = 1f;
                languageList.style.marginTop = 1f;
                languageList.style.marginRight = 3f;
                languageList.AddClasses("inspector-language-dropdown-field");

                languageList.SetMargin(0);

                languageList.RegisterCallback< ChangeEvent< string > >(_ => {
                    //Bool
                    Undo.RecordObject(target, "language_value");
                    if ( Application.isPlaying )
                        IM.ChangeLanguage(languageList.value);

                    currentLanguage.stringValue = languageList.value;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                });

                languageList.AddManipulator(new ContextualMenuManipulator(evt => {
                    evt.menu.AppendAction("Remove This", _ => {
                        Undo.RecordObject(ieDatabase, "IDE_Localisation");
                        ieDatabase.localisationTexts.Remove(languageList.value);
                        languageList.choices = new List< string >(ieDatabase.localisationTexts.Keys);
                        languageList.index = 0;
                        EditorUtility.SetDirty(ieDatabase);
                        Reload();
                    });
                }));

                languageGroup.Add(languageList);

                LocalisationMenuField.Add(languageGroup);

                #endregion

                #region ADD_KEY

                var addKeyField = new VisualElement() {
                    name = "addLanguage",
                    style = {
                        flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row),
                        alignItems = new StyleEnum< Align >(Align.Center),
                        paddingLeft = 15,
                        paddingTop = 5,
                        marginTop = 5,
                        borderTopWidth = 1,
                        borderTopColor = NullUtils.HTMLColor("#878282")
                    }
                };

                var addKeyLabel = IEGraphUtility.CreateLabel("New Key");
                addKeyLabel.AddClasses("referenceLabel");

                addKeyField.Add(addKeyLabel);

                var keyTextField = IEGraphUtility.CreateTextField();
                keyTextField.AddClasses("language_key_text-field", "language_key_text-element");

                var addKeyBtn = IEGraphUtility.CreateButton("+", () => {
                    if ( string.IsNullOrEmpty(keyTextField.value) ||
                         ieDatabase.localisationTexts.First().Value.Keys.Contains(keyTextField.value) ) return;

                    Undo.RecordObject(ieDatabase, "IDE_Localisation");
                    foreach ( var text in ieDatabase.localisationTexts )
                        text.Value.Add(keyTextField.value, string.Empty);

                    EditorUtility.SetDirty(ieDatabase);

                    AddKeyButton(keyTextField.value);
                });
                addKeyBtn.AddClasses("addLanguage");

                addKeyField.Add(keyTextField);
                addKeyField.Add(addKeyBtn);

                LocalisationMenuField.Add(addKeyField);

                #endregion

                #region LANGUAGE_KEYS_TITLE

                var titleField = new VisualElement() {
                    name = "title",
                    style = {
                        alignItems = new StyleEnum< Align >(Align.Center),
                        paddingLeft = 15,
                        paddingBottom = 5,
                        marginBottom = 5,
                        marginTop = 10,
                        borderBottomWidth = 1,
                        borderBottomColor = NullUtils.HTMLColor("#878282"),
                        justifyContent = new StyleEnum< Justify >(Justify.Center)
                    }
                };

                var languagesTitle = IEGraphUtility.CreateLabel("KEYS");
                languagesTitle.AddClasses("languageKeysLabel");

                titleField.Add(languagesTitle);

                LocalisationMenuField.Add(titleField);

                #endregion

                #region SEARCH

                var searchField = new VisualElement() {
                    name = "searchField",
                    style = {
                        flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row),
                        alignItems = new StyleEnum< Align >(Align.Center),
                        paddingLeft = 15,
                        paddingTop = 5,
                        marginTop = 5,
                        borderTopWidth = 1,
                        borderTopColor = NullUtils.HTMLColor("#878282"),
                        flexGrow = 1
                    }
                };

                var searchLabel = IEGraphUtility.CreateLabel("Search");
                searchLabel.AddClasses("referenceLabel");

                searchField.Add(searchLabel);

                var searchTextField = IEGraphUtility.CreateTextField();

                searchTextField.RegisterValueChangedCallback(_ => {
                    if ( searchTextField.value.Length > 0 ) {
                        languagesField.Clear();
                        foreach ( var text in ieDatabase.localisationTexts.First().Value
                                     .Where(a => a.Key.Contains(searchTextField.value)) ) AddKeyButton(text.Key);
                    } else {
                        languagesField.Clear();
                        foreach ( var text in ieDatabase.localisationTexts.First().Value ) AddKeyButton(text.Key);
                    }
                });

                searchTextField.AddClasses("language_key_text-field", "language_key_text-element");

                searchField.Add(searchTextField);

                titleField.Add(searchField);

                #endregion

                #region LANGUAGES

                languagesField = new ScrollView() {
                    name = "languageKeys",
                    style = {
                        flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Column),
                        alignItems = new StyleEnum< Align >(Align.Center),
                        paddingLeft = 15,
                    }
                };

                languagesField.contentContainer.style.flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row);
                languagesField.contentContainer.style.flexWrap = new StyleEnum< Wrap >(Wrap.Wrap);

                foreach ( var text in ieDatabase.localisationTexts.First().Value ) AddKeyButton(text.Key);

                LocalisationMenuField.Add(languagesField);

                #endregion
            }

            #region ADD_LANGUAGE

            {
                var addLanguageField = new VisualElement() {
                    name = "addLanguage",
                    style = {
                        flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row),
                        alignItems = new StyleEnum< Align >(Align.Center),
                        paddingLeft = 15,
                        paddingTop = 5,
                        marginTop = 5
                    }
                };

                var addLanguageLabel = IEGraphUtility.CreateLabel("Add Language");
                addLanguageLabel.AddClasses("referenceLabel");

                addLanguageField.Add(addLanguageLabel);

                var allLanguages = IEGraphUtility.CreateDropdown(NullUtils.GetLanguageCodes());

                allLanguages.value = "en-GB";

                var dropdownChild = allLanguages.GetChild< VisualElement >();
                dropdownChild.SetPadding(5);
                dropdownChild.style.paddingLeft = 10;
                dropdownChild.style.paddingRight = 10;
                dropdownChild.GetChild< TextElement >().style.color =
                    new StyleColor(NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#FFFFFF" : "#212121"));

                allLanguages.style.marginLeft = 0f;
                allLanguages.style.marginBottom = 1f;
                allLanguages.style.marginTop = 1f;
                allLanguages.style.marginRight = 3f;
                allLanguages.AddClasses("inspector-add-language-dropdown-field");

                allLanguages.SetMargin(0);

                var addButton = IEGraphUtility.CreateButton("+", () => {
                    if ( ieDatabase.localisationTexts.ContainsKey(allLanguages.value) ) {
                        NDebug.Log(STATIC.EXISTS_LANGUAGE_KEY, NLogType.Error);
                        return;
                    }

                    Undo.RecordObject(ieDatabase, "IDE_Localisation");

                    if ( ieDatabase.localisationTexts.Any() ) {
                        var copyTo =
                            new SerializableDictionary< string, string >(ieDatabase.localisationTexts.First().Value);
                        ieDatabase.localisationTexts.Add(allLanguages.value, copyTo);
                    } else {
                        ieDatabase.localisationTexts.Add(allLanguages.value,
                            new SerializableDictionary< string, string >());
                    }

                    EditorUtility.SetDirty(ieDatabase);
                    Reload();
                });
                addButton.AddClasses("addLanguage");

                addLanguageField.Add(allLanguages);
                addLanguageField.Add(addButton);

                LocalisationMenuField.Insert(LocalisationMenuField.childCount > 1 ? 1 : 0, addLanguageField);
            }

            #endregion

            #region XML

            var xmlGroup = new VisualElement() {
                name = "currentLanguage",
                style = {
                    flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row),
                    alignItems = new StyleEnum< Align >(Align.Center),
                    paddingLeft = 15
                }
            };

            var saveXML = IEGraphUtility.CreateButton("Save XML", () => {
                XMLUtils.CreateXML(ieDatabase);
            });
            saveXML.AddClasses("saveXml");
            xmlGroup.Add(saveXML);

            var loadXML = IEGraphUtility.CreateButton("Load XML", () => {
                XMLUtils.LoadXML(ieDatabase);
                Reload();
            });
            loadXML.AddClasses("loadXml");
            xmlGroup.Add(loadXML);

            LocalisationMenuField.Insert(LocalisationMenuField.childCount, xmlGroup);

            #endregion

            #endregion

            container.Add(LocalisationMenuField);
        }

        private void AddKeyButton(string keyName) {
            var languageKeyBtn = IEGraphUtility.CreateButton(keyName);

            languageKeyBtn.clickable = new Clickable(_ => {
                // if (languageKeyBtn.userData is VisualElement data)
                // {
                //     languageKeyBtn.style.backgroundColor = (StyleColor)data.userData;
                //     languageKeyBtn.userData = null;
                //     languagesField.Remove(data);
                //     return;
                // }

                List< VisualElement > children = new List< VisualElement >();

                languagesField.GetChilds< VisualElement >(c => {
                    if ( c is not Button )
                        children.Add(c);
                });
                foreach ( var child in children ) {
                    languagesField.Remove(child);
                }

                var keyField = new VisualElement() {
                    name = "keyField",
                    style = {
                        flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Column),
                        paddingLeft = 15,
                        paddingTop = 5,
                        marginTop = 5,
                        width = new StyleLength(Length.Percent(100)),
                    }
                };
                keyField.SetPadding(5);
                keyField.SetMargin(3);

                foreach ( var text in ieDatabase.localisationTexts ) {
                    if ( !text.Value.Keys.Contains(keyName) ) continue;
                    var keyColumn = new VisualElement() {
                        style = {
                            flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row)
                        }
                    };
                    var addKeyLabel = IEGraphUtility.CreateLabel(text.Key);
                    addKeyLabel.AddClasses("referenceLabel");

                    addKeyLabel.AddManipulator(new ContextualMenuManipulator(evt => {
                        evt.menu.AppendAction("Remove This", _ => {
                            Undo.RecordObject(ieDatabase, "IDE_Localisation");
                            ieDatabase.localisationTexts.Remove(text.Key);
                            EditorUtility.SetDirty(ieDatabase);
                            keyField.Remove(keyColumn);
                        });
                    }));

                    var keyTextField = IEGraphUtility.CreateTextArea(text.Value[ keyName ]);
                    keyTextField.AddClasses("language_key_text-field", "language_key_text-element");

                    keyTextField.RegisterCallback< FocusOutEvent >(_ => {
                        Undo.RecordObject(ieDatabase, "IDE_Localisation");
                        text.Value[ keyName ] = keyTextField.value;
                        EditorUtility.SetDirty(ieDatabase);
                    });

                    keyColumn.Add(addKeyLabel);
                    keyColumn.Add(keyTextField);
                    keyField.Add(keyColumn);
                }

                // var index = languagesField.IndexOf(languageKeyBtn) + 1;
                // languagesField.Insert(index, keyField);
                languagesField.Add(keyField);

                languageKeyBtn.userData = keyField;
                keyField.userData = languageKeyBtn.style.backgroundColor;

                #region BOX

                keyField.SetBorderColor(EditorGUIUtility.isProSkin
                    ? NullUtils.HTMLColor("#54504D")
                    : NullUtils.HTMLColor("#8B9395"));
                keyField.SetBorderWidth(8);
                keyField.style.borderTopWidth = 0;
                keyField.style.marginTop = -2;

                #endregion
            });

            languageKeyBtn.AddClasses("languageKeyButton");

            var removeKeyBtn = IEGraphUtility.CreateButton("X");
            removeKeyBtn.AddClasses("removeLanguageKeyBtn");

            languageKeyBtn.AddManipulator(new ContextualMenuManipulator(evt => {
                evt.menu.AppendAction("Remove This", _ => {
                    Undo.RecordObject(ieDatabase, "IDE_Localisation");
                    foreach ( var text in ieDatabase.localisationTexts ) text.Value.Remove(keyName);
                    if ( (VisualElement)languageKeyBtn.userData != null )
                        languagesField.Remove((VisualElement)languageKeyBtn.userData);
                    languagesField.Remove(languageKeyBtn);
                    EditorUtility.SetDirty(ieDatabase);
                });
            }));

            languagesField.Add(languageKeyBtn);
        }

        private void References() {
            referenceField = new VisualElement() {
                name = "referenceField",
                style = {
                    display = new StyleEnum< DisplayStyle >(DisplayStyle.None)
                }
            };
            referenceField.AddClasses("box-field");

            referenceLabel = IEGraphUtility.CreateLabel("References");
            referenceLabel.style.display = new StyleEnum< DisplayStyle >(DisplayStyle.None);
            referenceLabel.AddClasses("title-field");

            #region DIALOGUE_MANAGER

            var dialogueGroup = new VisualElement() {
                name = "audioSourceGroup",
                style = {
                    flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row),
                    alignItems = new StyleEnum< Align >(Align.Center),
                    paddingLeft = 15
                }
            };

            var dialogueLabel = IEGraphUtility.CreateLabel("Dialogue Manager");
            dialogueLabel.AddClasses("referenceLabel");

            dialogueGroup.Add(dialogueLabel);

            var dialogueInput = new ObjectField {
                objectType = typeof( DialogueManager ),
                allowSceneObjects = false,
                value = (DialogueManager)dialogueManager.objectReferenceValue
            };

            dialogueInput.AddClasses("object-field");
            dialogueGroup.Add(dialogueInput);

            dialogueInput.RegisterCallback< ChangeEvent< string > >(_ => {
                Undo.RecordObject(target, "reference_Value");
                dialogueManager.objectReferenceValue = dialogueInput.value;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            });
            
            referenceField.Add(dialogueGroup);

            #endregion
            
            #region DATABASE

            var databaseGroup = new VisualElement() {
                name = "audioSourceGroup",
                style = {
                    flexDirection = new StyleEnum< FlexDirection >(FlexDirection.Row),
                    alignItems = new StyleEnum< Align >(Align.Center),
                    paddingLeft = 15
                }
            };

            var databaseLabel = IEGraphUtility.CreateLabel("Database");
            databaseLabel.AddClasses("referenceLabel");

            databaseGroup.Add(databaseLabel);

            databaseInput = IEGraphUtility.CreateObjectField(typeof( IEDatabase ));
            databaseInput.value = (IEDatabase)database.objectReferenceValue;
            databaseInput.AddClasses("object-field");
            databaseGroup.Add(databaseInput);

            databaseInput.RegisterCallback< ChangeEvent< string > >(_ => {
                Undo.RecordObject(target, "reference_Value");
                database.objectReferenceValue = databaseInput.value;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            });

            databaseInput.AddManipulator(new ContextualMenuManipulator(evt => {
                evt.menu.AppendAction("Open", _ => {
                    if ( databaseInput.value == null ) return;
                    GraphWindow.Open((IEDatabase)databaseInput.value);
                });
            }));
            
            var assetDb = IEGraphUtility.CreateButton("Sprite dB", () => {
                AssetDbWindow.Open((IEDatabase)databaseInput.value);
            });
            assetDb.AddClasses("asset-db-button");
            
            databaseGroup.Add(assetDb);

            referenceField.Add(databaseGroup);

            #endregion

            container.Add(referenceLabel);
            container.Add(referenceField);
        }

        private void AttachDatabaseField() {
            createGroup = new VisualElement() {
                style = {
                    display = new StyleEnum< DisplayStyle >(DisplayStyle.None)
                }
            };

            var message = new VisualElement();
            message.AddClasses("messageBox");

            var errorIcon = new VisualElement() {
                style = {
                    width = 28,
                    height = 28,
                    minWidth = 28,
                    minHeight = 28,
                }
            };

            var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/error.png");
            errorIcon.style.backgroundImage = new StyleBackground(texture);

            var messageLabel = IEGraphUtility.CreateLabel(
                "The database could not be found. You need to create a new database or load an existing one.");
            messageLabel.AddClasses("messageLabel");
            message.Add(errorIcon);
            message.Add(messageLabel);

            var createBtn = IEGraphUtility.CreateButton("Create Database", CreateDatabase);
            createBtn.AddClasses("createManagerBtn");

            var loadBtn = IEGraphUtility.CreateButton("Load Database", LoadDatabase);
            loadBtn.AddClasses("loadManagerBtn");

            var dbField = IEGraphUtility.CreateObjectField(typeof( IEDatabase ));

            dbField.RegisterValueChangedCallback(evt => {
                if ( evt.newValue == null ) return;
                database.objectReferenceValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                databaseInput.value = database.objectReferenceValue;
            });

            createGroup.Add(message);
            createGroup.Add(createBtn);
            // createGroup.Add(loadBtn);
            createGroup.Add(dbField);

            container.Add(createGroup);
        }

        private void LoadDatabase() {
            var path = EditorUtility.OpenFilePanel("Create Database", "Assets", "asset");
            if ( string.IsNullOrEmpty(path) ) return;
            path = EditorUtilities.ToRelativePath(path);

            var obj = AssetDatabase.LoadAssetAtPath< IEDatabase >(path);
            if ( obj == null ) return;
            database.objectReferenceValue = obj;
            serializedObject.ApplyModifiedProperties();
            databaseInput.value = database.objectReferenceValue;
        }

        private void CreateDatabase() {
            var path = EditorUtility.SaveFilePanel("Create Database", "Assets", "New IEDatabase.asset", "asset");
            if ( string.IsNullOrEmpty(path) ) return;
            path = EditorUtilities.ToRelativePath(path);

            var obj = CreateInstance< IEDatabase >();
            AssetDatabase.CreateAsset(obj, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = obj;
            database.objectReferenceValue = obj;
            serializedObject.ApplyModifiedProperties();
            databaseInput.value = database.objectReferenceValue;
        }
    }
}