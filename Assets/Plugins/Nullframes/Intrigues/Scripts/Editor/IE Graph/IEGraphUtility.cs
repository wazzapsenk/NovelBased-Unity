using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Graph.Nodes;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph {
    public static class IEGraphUtility {
        public static TextField CreateTextField(string value = null,
            string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null) {
            var textField = new TextField() {
                value = value,
                label = label
            };
            if (onValueChanged != null) textField.RegisterValueChangedCallback(onValueChanged);

            return textField;
        }

        public static IntegerField CreateIntField(int value = 0,
            string label = null, EventCallback<ChangeEvent<int>> onValueChanged = null) {
            var intField = new IntegerField() {
                value = value,
                label = label
            };
            if (onValueChanged != null) intField.RegisterValueChangedCallback(onValueChanged);

            return intField;
        }

        public static void Disable(this VisualElement visualElement) {
            visualElement.SetEnabled(false);
        }

        public static void Enable(this VisualElement visualElement) {
            visualElement.SetEnabled(true);
        }

        public static void Hide(this VisualElement visualElement) {
            visualElement.style.display = DisplayStyle.None;
        }

        public static Rect CalculateRectToFitAllEx(this GraphView graphView, VisualElement container,
            bool reachedFirstChild = false) {
            Rect rectToFit = container.layout;
            graphView.graphElements.ForEach(ge => {
                var rect = new Rect(0.0f, 0.0f, ge.layout.width, ge.layout.height);
                if (ge is Edge or Port or Group)
                    return;
                if (!reachedFirstChild) {
                    rectToFit = ge.ChangeCoordinatesTo(graphView.contentViewContainer, rect);
                    reachedFirstChild = true;
                }
                else
                    rectToFit = RectUtils.Encompass(rectToFit,
                        ge.ChangeCoordinatesTo(graphView.contentViewContainer, rect));
            });
            return rectToFit;
        }

        public static Rect CalculateRectToFitAllEx(this GraphView graphView, VisualElement container,
            Func<GraphElement, bool> rule, bool reachedFirstChild = false) {
            Rect rectToFit = container.layout;
            graphView.graphElements.ForEach(ge => {
                var rect = new Rect(0.0f, 0.0f, ge.layout.width, ge.layout.height);
                if (ge is Edge or Port or Group)
                    return;
                if (!rule.Invoke(ge)) {
                    return;
                }

                if (!reachedFirstChild) {
                    rectToFit = ge.ChangeCoordinatesTo(graphView.contentViewContainer, rect);
                    reachedFirstChild = true;
                }
                else
                    rectToFit = RectUtils.Encompass(rectToFit,
                        ge.ChangeCoordinatesTo(graphView.contentViewContainer, rect));
            });
            return rectToFit;
        }

        public static void Show(this VisualElement visualElement) {
            visualElement.style.display = DisplayStyle.Flex;
        }

        public static void GetChilds<T>(this VisualElement element, Action<T> action) where T : VisualElement {
            foreach (var e in element.Children())
                if (e is T visualElement)
                    action.Invoke(visualElement);
        }

        public static void GetChilds(this VisualElement element, Action action) {
            foreach (var _ in element.Children()) action.Invoke();
        }

        public static T GetChild<T>(this VisualElement element) where T : VisualElement {
            foreach (var child in element.Children())
                if (child is T visualElement)
                    return visualElement;

            return null;
        }

        public static List<OutputData> CloneNodeOutputs(this IEnumerable<OutputData> nodeOutputs) {
            return nodeOutputs.Select(outputData => {
                    var dataCollection = new List<PortData>();
                    foreach (var data in outputData.DataCollection) {
                        dataCollection.Add(new PortData(data.NextID, data.NextName, data.ActorID));
                    }

                    return new OutputData {
                        Name = outputData.Name, Sprite = outputData.Sprite,
                        DataCollection = new List<PortData>(dataCollection), Disabled = outputData.Disabled,
                        Primary = outputData.Primary, ValidatorMode = outputData.ValidatorMode, HideIfDisable = outputData.HideIfDisable
                    };
                })
                .ToList();
        }

        public static T GetChild<T>(this VisualElement element, string name) where T : VisualElement {
            foreach (var child in element.Children().Where(e => e.name == name))
                if (child is T visualElement)
                    return visualElement;

            return null;
        }

        public static int GetActiveSlot(this IEGraphView graphView, string groupid, string roleId) {
            var lst = from ClanMemberNode element in graphView.graphElements.OfType<ClanMemberNode>().Where(member =>
                    member.RoleID == roleId && member.Group.ID == groupid &&
                    member.actor.CurrentState == Actor.IState.Active)
                select element.RoleID;
            return lst.Count();
        }

        public static Role GetMisson(this IEDatabase ieDatabase, string missionName) {
            return ieDatabase.roleDefinitions.FirstOrDefault(m => m.RoleName == missionName);
        }

        public static Label CreateLabel(string text) {
            var label = new Label() {
                text = text
            };

            return label;
        }

        public static ObjectField CreateObjectField(Type type) {
            var objectField = new ObjectField() {
                objectType = type,
            };
            return objectField;
        }

        public static FloatField CreateFloatField(float value = 0f, string label = null,
            EventCallback<ChangeEvent<float>> onValueChanged = null) {
            var floatField = new FloatField() {
                value = value,
                label = label
            };
            if (onValueChanged != null)
                floatField.RegisterValueChangedCallback(onValueChanged);

            return floatField;
        }

        public static TextField CreateTextArea(string value = null,
            string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null) {
            var textArea = CreateTextField(value, label, onValueChanged);
            textArea.multiline = true;
            return textArea;
        }

        public static Foldout CreateFoldout(string title, bool collapsed = false) {
            var foldout = new Foldout() {
                text = title,
                value = !collapsed
            };
            return foldout;
        }

        public static Button CreateButton(string text, Action onClick = null) {
            var btn = new Button() {
                text = text
            };
            btn.RegisterCallback<ClickEvent>(_ => onClick?.Invoke());
            btn.SetBorderRadius(0f);
            return btn;
        }

        public static Toggle CreateToggle(string label) {
            var toggle = new Toggle(label);
            return toggle;
        }

        public static Port CreatePort(this INode node, string portName = "", Type type = null,
            Orientation orientation = Orientation.Horizontal,
            Direction direction = Direction.Output, Port.Capacity capacity = Port.Capacity.Single) {
            var port = node.InstantiatePort(orientation, direction, capacity, type ?? typeof(bool));

            port.portColor = STATIC.DefaultColor;

            port.ElementAt(1).pickingMode = PickingMode.Position;
            port.style.height = 35;
            var connector = port.Q<VisualElement>("connector");
            connector.style.width = 12;
            connector.style.height = 12;
            connector.pickingMode = PickingMode.Position;

            var cap = connector.Q<VisualElement>("cap");
            cap.style.width = 8;
            cap.style.height = 8;

            if (node.GenericType == GenericNodeType.Family) {
                if (orientation == Orientation.Horizontal && direction == Direction.Output) {
                    connector.style.borderBottomLeftRadius = 0;
                    connector.style.borderTopLeftRadius = 0;

                    cap.style.borderBottomLeftRadius = 0;
                    cap.style.borderTopLeftRadius = 0;
                }

                if (orientation == Orientation.Horizontal && direction == Direction.Input) {
                    connector.style.borderBottomRightRadius = 0;
                    connector.style.borderTopRightRadius = 0;

                    cap.style.borderBottomRightRadius = 0;
                    cap.style.borderTopRightRadius = 0;
                }

                if (orientation == Orientation.Vertical && direction == Direction.Output) {
                    connector.style.borderTopLeftRadius = 0;
                    connector.style.borderTopRightRadius = 0;

                    cap.style.borderTopLeftRadius = 0;
                    cap.style.borderTopRightRadius = 0;
                }

                if (orientation == Orientation.Vertical && direction == Direction.Input) {
                    connector.style.borderBottomLeftRadius = 0;
                    connector.style.borderBottomRightRadius = 0;

                    cap.style.borderBottomLeftRadius = 0;
                    cap.style.borderBottomRightRadius = 0;
                }
            }

            if (direction == Direction.Input) {
                port.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            }

            port.portName = portName;
            port.name = portName;

            if (string.IsNullOrEmpty(portName)) port.GetChild<Label>().Hide();
            return port;
        }

        public static Port CreatePort(this INode node, string portName, Port.Capacity capacity) =>
            CreatePort(node, portName, null, Orientation.Horizontal, Direction.Output, capacity);

        public static void TraverseVisualElement<T>(this VisualElement element, Action<T> action)
            where T : VisualElement {
            var children = element.Children();

            using var enumerator = children.GetEnumerator();
            while (enumerator.MoveNext()) {
                var child = enumerator.Current;
                if (child is T visualElement) action.Invoke(visualElement);

                TraverseVisualElement(child, action);
            }
        }

        public static bool TraverseChildNode<T>(this INode node, Action<T> action, Func<T, bool> runWhile = null, HashSet<INode> visitedNodes = null)
            where T : INode {

            visitedNodes ??= new HashSet<INode>();

            if (!visitedNodes.Add(node)) {
                return false;
            }

            var children = node.children.OrderBy(n => n.GetPosition().position.Equals(Vector2.zero) ? n.Pos.y : n.GetPosition().position.y);

            using var enumerator = children.GetEnumerator();
            while (enumerator.MoveNext()) {
                var child = enumerator.Current;
                if (runWhile != null && runWhile.Invoke(child as T)) {
                    return true;
                }

                if (child is T iNode) action.Invoke(iNode);

                if (TraverseChildNode(child, action, runWhile, visitedNodes)) return true;
            }

            return false;
        }
        
        public static void TraverseSequencerFlow(this INode sequencerNode, Action<INode> apply)
        {
            var sequencerChildren = sequencerNode.children
                .OrderBy(n => n.Pos.y)
                .ToList();

            foreach (var child in sequencerChildren)
            {
                if (ContainsOnlyHardBreak(child))
                    break;

                if (child is SequencerNode)
                {
                    apply(child);
                    continue;
                }

                TraverseUntilBreak(child, apply);
            }
        }

        private static void TraverseUntilBreak(INode node, Action<INode> apply, HashSet<INode> visited = null)
        {
            visited ??= new HashSet<INode>();
            if (!visited.Add(node)) return;

            if (node is SequencerNode && visited.Count > 1)
                return;

            if (node is BreakSequencerNode or BreakNode or ContinueNode)
                return;

            apply(node);

            foreach (var child in node.children)
            {
                TraverseUntilBreak(child, apply, visited);
            }
        }


        private static bool ContainsOnlyHardBreak(INode node, HashSet<INode> visited = null)
        {
            visited ??= new HashSet<INode>();
            if (!visited.Add(node)) return true;

            if (node is BreakSequencerNode or BreakNode or ContinueNode)
                return true;

            if (node.children == null || node.children.Count == 0)
                return false;

            bool allBreak = true;

            foreach (var child in node.children)
            {
                if (!ContainsOnlyHardBreak(child, visited))
                {
                    allBreak = false;
                    break;
                }
            }

            return allBreak;
        }


        public static List<T> GetElements<T>(this VisualElement root) {
            var portElements = new List<T>();
            var queue = new Queue<VisualElement>();
            queue.Enqueue(root);
            while (queue.Count > 0) {
                var currentElement = queue.Dequeue();
                if (currentElement is T element) portElements.Add(element);

                foreach (var child in currentElement.Children()) queue.Enqueue(child);
            }

            return portElements;
        }

        public static DropdownField CreateDropdown(IEnumerable<string> items) {
            var dropdownField = new DropdownField() {
                index = 0
            };

            if (items != null) {
                dropdownField.choices = items.ToList();
            }

            return dropdownField;
        }

        public static Slider CreateSlider(float currentValue, float minValue, float highValue,
            EventCallback<ChangeEvent<float>> onValueChanged = null) {
            var slider = new Slider() {
                lowValue = minValue,
                highValue = highValue,
                value = currentValue
            };

            if (onValueChanged != null)
                slider.RegisterValueChangedCallback(onValueChanged);

            return slider;
        }

        public static SliderInt CreateSlider(int currentValue, int minValue, int highValue,
            EventCallback<ChangeEvent<int>> onValueChanged = null) {
            var slider = new SliderInt() {
                lowValue = minValue,
                highValue = highValue,
                value = currentValue
            };

            if (onValueChanged != null)
                slider.RegisterValueChangedCallback(onValueChanged);

            return slider;
        }

        public static CurveField CreateCurveField(AnimationCurve defaultValue = null) {
            var curveField = new CurveField {
                value = defaultValue
            };
            return curveField;
        }

        public static VisualElement AddStyleSheets(this VisualElement element, params string[] styleSheets) {
            foreach (var name in styleSheets) {
                var styleSheet = (StyleSheet)EditorGUIUtility.Load(name);
                if (styleSheet != null)
                    element.styleSheets.Add(styleSheet);
            }

            return element;
        }

        public static VisualElement AddClasses(this VisualElement element, params string[] classNames) {
            foreach (var @class in classNames) element.AddToClassList(@class);
            
            if(element is DropdownField or ObjectField) return element;

            foreach (var e in element.GetElements<VisualElement>()) {
                e.RemoveFromClassList("unity-text-element");
            }
            
            return element;
        }

        public static VisualElement LeftToRight(this VisualElement element) {
            element.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            element.style.justifyContent = new StyleEnum<Justify>(Justify.FlexStart);
            return element;
        }

        public static VisualElement SetBorderWidth(this VisualElement element, float width) {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            return element;
        }

        public static VisualElement SetBorderColor(this VisualElement element, string htmlColor) {
            var color = NullUtils.HTMLColor(htmlColor);
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            return element;
        }

        public static VisualElement SetBorderRadius(this VisualElement element, float radius) {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            return element;
        }

        public static VisualElement SetBorderColor(this VisualElement element, Color color) {
            element.style.borderTopColor = new StyleColor(color);
            element.style.borderBottomColor = new StyleColor(color);
            element.style.borderLeftColor = new StyleColor(color);
            element.style.borderRightColor = new StyleColor(color);
            return element;
        }

        public static VisualElement SetMargin(this VisualElement element, float margin) {
            element.style.marginTop = margin;
            element.style.marginBottom = margin;
            element.style.marginLeft = margin;
            element.style.marginRight = margin;
            return element;
        }

        public static VisualElement SetPadding(this VisualElement element, float padding) {
            element.style.paddingTop = padding;
            element.style.paddingBottom = padding;
            element.style.paddingLeft = padding;
            element.style.paddingRight = padding;
            return element;
        }
    }
}