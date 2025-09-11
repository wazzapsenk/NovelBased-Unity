using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.EDITOR
{
    public abstract class EActor : Editor
    {
        public override bool UseDefaultMargins()
        {
            return false;
        }

        private VisualElement root;
        private VisualElement container;
        private VisualElement variableBox;
        private VisualElement infoBox;

        private SerializedProperty _id;
        private SerializedProperty _variables;
        private Actor actor;
        private DropdownField actorList;
        private DropdownField variableField;

        private Label variableTitle;
        private Label infoTitle;

        private VisualElement managerNotExists;
        private VisualElement databaseNotExists;

        private bool actorExists;

        private IEActor ieActor;

        private Culture currentCulture;

        private IERoutine routine;

        private void OnEnable()
        {
            // Each editor window contains a root VisualElement object
            root = new VisualElement();

            // Import UXML
            var visualTree = (VisualTreeAsset)EditorGUIUtility.Load("Nullframes/Actor.uxml");
            visualTree.CloneTree(root);

            var styleSheet = (StyleSheet)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "Nullframes/ActorStyle_Dark.uss" : "Nullframes/ActorStyle_Light.uss");
            root.styleSheets.Add(styleSheet);

            _id = serializedObject.FindProperty("id");
            _variables = serializedObject.FindProperty("variables");
            actor = (Actor)target;

            container = root.Q<VisualElement>("container");
            managerNotExists = root.Q<VisualElement>("ManagerNotExists");
            databaseNotExists = root.Q<VisualElement>("DatabaseNotExists");
            container.AddClasses("container");
        }

        private void OnDisable()
        {
            EditorApplication.update -= Update;
            routine?.Dispose();
        }

        private void OnDestroy()
        {
            EditorApplication.update -= Update;
            routine?.Dispose();
        }

        private void Update()
        {
            if (!IM.Exists)
            {
                managerNotExists.Show();
                return;
            }

            if (!IM.DatabaseExists)
            {
                databaseNotExists.Show();
                return;
            }
            
            managerNotExists.Hide();
            databaseNotExists.Hide();

            if (actorExists)
            {
                actorList.Hide();
                variableBox.Show();
                variableTitle.Show();
                infoBox?.Show();
                if (infoBox == null) GetInfo();
            }
            else
            {
                variableBox.Hide();
                actorList.Show();
                variableTitle.Hide();
                actorList.index = 0;
                infoBox?.Hide();
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (routine != null) return root;
            
            if (!IM.Exists)
            {
                managerNotExists.Show();
            } else if (!IM.DatabaseExists)
            {
                databaseNotExists.Show();    
            }
            
            routine = EditorRoutine.StartRoutine(() => IM.Exists && IM.DatabaseExists, () =>
            {
                ieActor = IM.IEDatabase.actorRegistry.FirstOrDefault(a => a.ID == _id.stringValue);
                actorExists = ieActor != null || EditorApplication.isPlaying;
                currentCulture = IM.IEDatabase.culturalProfiles.Find(c => c.ID == ieActor?.CultureID);
                Customize();
                GetActorList();
                GetInfo();
                GetVariableList();
                EditorApplication.update += Update;
                routine = null;
            });
            return root;
        }

        private void GetInfo()
        {
            if (!actorExists) return;
            infoBox = new VisualElement()
            {
                name = "InfoBox",
                style =
                {
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None)
                }
            };
            infoBox.AddClasses("infoBox");

            var rowField = new VisualElement()
            {
                name = "rowField",
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };

            var columnField = new VisualElement()
            {
                name = "columnField",
                style =
                {
                    justifyContent = new StyleEnum<Justify>(Justify.Center)
                }
            };

            infoTitle = IEGraphUtility.CreateLabel("Character Info");
            infoTitle.AddClasses("title-field");

            var portraitField = new VisualElement()
            {
                style =
                {
                    width = 80f,
                    height = 80f,
                    marginRight = 10,
                    marginBottom = 5,
                    marginLeft = 5,
                    marginTop = 5
                }
            };
            rowField.Add(portraitField);
            portraitField.AddClasses("info-label-field");

            var isPlaying = EditorApplication.isPlaying;
            var portrait = isPlaying ? actor.Portrait : ieActor.Portrait;
            if (portrait != null)
            {
                portraitField.style.backgroundImage = new StyleBackground(portrait);
            }
            else
            {
                var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/portrait.png");
                portraitField.style.backgroundImage = new StyleBackground(texture);
            }

            var _name = isPlaying ? actor.FullName : ieActor.Name;
            var nameField = IEGraphUtility.CreateLabel($"Name: {_name}");
            columnField.Add(nameField);
            nameField.AddClasses("info-label-field");

            var age = isPlaying ? actor.Age : ieActor.Age;
            var ageField = IEGraphUtility.CreateLabel($"Age: {age}");
            columnField.Add(ageField);
            ageField.AddClasses("info-label-field");

            var gender = isPlaying ? actor.Gender : ieActor.Gender;
            var genderField = IEGraphUtility.CreateLabel($"Gender: {gender.ToString()}");
            columnField.Add(genderField);
            genderField.AddClasses("info-label-field");

            var culture = isPlaying ? actor.Culture?.CultureName : currentCulture?.CultureName;
            var cultureField = IEGraphUtility.CreateLabel($"Culture: {culture}");
            columnField.Add(cultureField);
            cultureField.AddClasses("info-label-field");
            
            rowField.RegisterCallback<ClickEvent>(_ =>
            {
                GraphWindow.Open(IM.IEDatabase);
                EditorRoutine.StartRoutine(.1f, () => GraphWindow.instance.GotoActor(actor.ID));
            });

            rowField.Add(columnField);
            infoBox.Add(rowField);
            container.Insert(0, infoTitle);
            container.Insert(1, infoBox);
        }

        private void GetVariableList()
        {
            variableBox = new VisualElement()
            {
                name = "VariableBox",
                style =
                {
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None)
                }
            };
            variableBox.AddClasses("variableBox");

            var createGroup = new VisualElement()
            {
                name = "createGroup",
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    marginBottom = 10,
                    marginTop = 10,
                    paddingLeft = 15,
                    paddingBottom = 15,
                    borderBottomWidth = 1,
                    borderBottomColor = NullUtils.HTMLColor("#989898")
                }
            };

            variableTitle = IEGraphUtility.CreateLabel("Variables");
            variableTitle.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            variableTitle.AddClasses("title-field");

            //Dropdown
            variableField = IEGraphUtility.CreateDropdown(null);

            variableField.RegisterCallback<MouseDownEvent>(_ => { LoadVariables(); });

            LoadVariables();

            var dropdownChild = variableField.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#FFFFFF" : "#212121"));

            variableField.style.marginLeft = 0f;
            variableField.style.marginBottom = 1f;
            variableField.style.marginTop = 1f;
            variableField.style.marginRight = 3f;
            variableField.AddClasses("inspector-variable-dropdown-field");

            var addBtn = IEGraphUtility.CreateButton("Set Override", () =>
            {
                //Add Variable
                if (actor.Variables.Any(v => v.name == variableField.value)) return;
                var variable = IM.IEDatabase.variablePool.Find(v => v.name == variableField.value);
                if (variable == null) return;
                CreateVariableField(variableField.value,
                    variable.Duplicate());
            });
            addBtn.AddClasses("add-variable-button");

            createGroup.Add(variableField);
            createGroup.Add(addBtn);
            variableBox.Add(createGroup);

            foreach (var variable in actor.Variables) CreateVariableField(variable.name, variable);

            container.Add(variableTitle);
            container.Add(variableBox);
        }

        private void LoadVariables()
        {
            var variableList = IM.IEDatabase.variablePool.Select(v => v.name);
            variableField.choices = new List<string>(variableList);
            variableField.choices.Insert(0, "Variable: NULL");
            variableField.index = 0;
        }

        private void CreateVariableField(string variableName, NVar variable)
        {
            var variableGroup = new VisualElement()
            {
                name = "variableGroup",
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center),
                    paddingLeft = 15,
                    paddingTop = 10
                }
            };

            var variableLabel = IEGraphUtility.CreateLabel(variableName);
            variableLabel.AddClasses("variableLabel");

            variableGroup.Add(variableLabel);

            switch (variable.Type)
            {
                case NType.String:
                {
                    var stringInput = IEGraphUtility.CreateTextArea((string)variable);
                    stringInput.AddClasses("variable-string-text-area");
                    variableGroup.Add(stringInput);

                    stringInput.RegisterCallback<FocusOutEvent>(_ =>
                    {
                        Undo.RecordObject(actor, "variable_value");
                        variable.value = stringInput.value;
                        EditorUtility.SetDirty(actor);
                    });
                    break;
                }
                case NType.Integer:
                {
                    var integerInput = IEGraphUtility.CreateIntField((int)variable);
                    integerInput.AddClasses("variable-integer-field");
                    variableGroup.Add(integerInput);

                    integerInput.RegisterCallback<FocusOutEvent>(_ =>
                    {
                        Undo.RecordObject(actor, "variable_value");
                        variable.value = integerInput.value;
                        EditorUtility.SetDirty(actor);
                    });
                    break;
                }
                case NType.Float:
                {
                    var floatInput = IEGraphUtility.CreateFloatField((float)variable);
                    floatInput.AddClasses("variable-float-field");
                    variableGroup.Add(floatInput);

                    floatInput.RegisterCallback<FocusOutEvent>(_ =>
                    {
                        Undo.RecordObject(actor, "variable_value");
                        variable.value = floatInput.value;
                        EditorUtility.SetDirty(actor);
                    });
                    break;
                }
                case NType.Object:
                {
                    var objectInput = IEGraphUtility.CreateObjectField(typeof(Object));
                    objectInput.value = (Object)variable;
                    objectInput.AddClasses("object-field");
                    variableGroup.Add(objectInput);

                    objectInput.RegisterCallback<ChangeEvent<string>>(_ =>
                    {
                        Undo.RecordObject(actor, "variable_value");
                        variable.value = objectInput.value;
                        EditorUtility.SetDirty(actor);
                    });
                    break;
                }
                case NType.Enum:
                {
                    var enumField = IEGraphUtility.CreateDropdown(null);

                    enumField.choices = new List<string>(((NEnum)variable).Values);
                    enumField.index = (int)variable;

                    var dropdownChild = enumField.GetChild<VisualElement>();
                    dropdownChild.SetPadding(5);
                    dropdownChild.style.paddingLeft = 10;
                    dropdownChild.style.paddingRight = 10;
                    dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#FFFFFF" : "#212121"));

                    enumField.style.marginLeft = 0f;
                    enumField.style.marginBottom = 1f;
                    enumField.style.marginTop = 1f;
                    enumField.style.marginRight = 3f;
                    enumField.AddClasses("inspector-enum-dropdown-field");

                    enumField.SetMargin(0);

                    enumField.RegisterCallback<ChangeEvent<string>>(_ =>
                    {
                        //Bool
                        Undo.RecordObject(actor, "variable_value");
                        variable.value = enumField.value;
                        EditorUtility.SetDirty(actor);
                    });

                    variableGroup.Add(enumField);
                    break;
                }
                case NType.Bool:
                {
                    var boolField = IEGraphUtility.CreateDropdown(null);

                    boolField.choices = new List<string>(new[] { "True", "False" });
                    boolField.index = (bool)variable == false ? 1 : 0;

                    var dropdownChild = boolField.GetChild<VisualElement>();
                    dropdownChild.SetPadding(5);
                    dropdownChild.style.paddingLeft = 10;
                    dropdownChild.style.paddingRight = 10;
                    dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#FFFFFF" : "#212121"));

                    boolField.style.marginLeft = 0f;
                    boolField.style.marginBottom = 1f;
                    boolField.style.marginTop = 1f;
                    boolField.style.marginRight = 3f;
                    boolField.AddClasses("inspector-bool-dropdown-field");

                    boolField.SetMargin(0);

                    boolField.RegisterCallback<ChangeEvent<string>>(_ =>
                    {
                        //Bool
                        Undo.RecordObject(actor, "variable_value");
                        variable.value = boolField.index == 0;
                        EditorUtility.SetDirty(actor);
                    });

                    variableGroup.Add(boolField);
                    break;
                }
            }

            var removeBtn = IEGraphUtility.CreateButton("Remove", () =>
            {
                //Add Variable
                Undo.RecordObject(actor, "Actor_Variable");
                var vars = _variables.GetValue<List<NVar>>();
                vars.Remove(variable);
                variableBox.Remove(variableGroup);
                EditorUtility.SetDirty(actor);
            });
            removeBtn.AddClasses("remove-variable-button");

            variableGroup.Add(removeBtn);

            if (actor.Variables.All(v => v.name != variableName))
            {
                Undo.RecordObject(actor, "Actor_Variable");
                var vars = _variables.GetValue<List<NVar>>();
                vars.Add(variable);
                EditorUtility.SetDirty(actor);
            }

            variableBox.Add(variableGroup);
        }

        private void GetActorList()
        {
            var actors = FindObjectsOfType<Actor>(true)
                .Select(a => a.ID).ToList();
            var i = 1;
            var characters = IM.IEDatabase.actorRegistry.Where(_actor => !actors.Contains(_actor.ID))
                .ToDictionary(_actor => _actor.ID, _actor => $"[{i++}]: {_actor.Name}({_actor.Age})");
            actorList = IEGraphUtility.CreateDropdown(null);
            actorList.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            actorList.choices = new List<string>(characters.Values);
            actorList.choices.Insert(0, "Actor: NULL");
            actorList.index = 0;

            var dropdownChild = actorList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#FFFFFF" : "#212121"));

            actorList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (actorList.index <= 0) return;
                ieActor = IM.IEDatabase.actorRegistry.First(a =>
                    a.ID == characters.Keys.ElementAt(actorList.index - 1));
                actorExists = true;
                currentCulture = IM.IEDatabase.culturalProfiles.Find(c => c.ID == ieActor.CultureID);
                actor.gameObject.name = $"{ieActor.Name}";
                if (currentCulture != null && actor.IsPlayer == false) actor.gameObject.name += $"({currentCulture.CultureName})";
                if (actor.IsPlayer) actor.gameObject.name += "(Player)";
                Setup(ieActor);
            });
            actorList.style.marginLeft = 0f;
            actorList.style.marginBottom = 1f;
            actorList.style.marginTop = 1f;
            actorList.style.marginRight = 3f;
            actorList.AddClasses("inspector-actor-dropdown-field");

            container.Add(actorList);
        }

        private void Customize()
        {
            var isPlaying = EditorApplication.isPlaying;

            if (actor is RuntimeActor)
            {
                var titleTexture = root.Q<VisualElement>("titleTexture");
                titleTexture.ClearClassList();
                titleTexture.AddClasses("titleTextureRuntime");
                return;
            }

            if (isPlaying || ieActor == null)
            {
                if (actor.IsPlayer)
                {
                    var titleTexture = root.Q<VisualElement>("titleTexture");
                    titleTexture.ClearClassList();
                    titleTexture.AddClasses("titleTexturePlayer");
                    return;
                }

                if (!actor.IsPlayer)
                {
                    var titleTexture = root.Q<VisualElement>("titleTexture");
                    titleTexture.ClearClassList();
                    titleTexture.AddClasses("titleTextureAI");
                }
            }
            else
            {
                if (ieActor.IsPlayer)
                {
                    var titleTexture = root.Q<VisualElement>("titleTexture");
                    titleTexture.ClearClassList();
                    titleTexture.AddClasses("titleTexturePlayer");
                    return;
                }

                if (!ieActor.IsPlayer)
                {
                    var titleTexture = root.Q<VisualElement>("titleTexture");
                    titleTexture.ClearClassList();
                    titleTexture.AddClasses("titleTextureAI");
                }
            }
        }

        private void Setup(IEActor dbActor)
        {
            _id.stringValue = dbActor.ID;

            serializedObject.ApplyModifiedProperties();
        }
    }
}