using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Graph.Nodes;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph {
    public static class GraphSaveUtility {
        private static IEGraphView _graphView;

        private static List< INode > nodes;
        private static List< IGroup > groups;

        private static Dictionary< string, IGroup > loadedGroups;
        private static Dictionary< string, INode > loadedNodes;

        private static IERoutine routine;

        public static int processCount;

        private static bool isDirty;

        private static IEDatabase currentDatabase;

        private static void Init(IEGraphView graphView) {
            _graphView = graphView;

            nodes = new List< INode >();
            groups = new List< IGroup >();

            loadedGroups = new Dictionary< string, IGroup >();
            loadedNodes = new Dictionary< string, INode >();
        }

        public static void SaveCurrent(bool noUndo = false) {
            if ( currentDatabase == null ) return;
            Init(_graphView);
            GetElementsFromGraphView();

            if ( !noUndo )
                Undo.RecordObject(currentDatabase, "IEGraph");

            SaveGroups();
            SaveNodes();

            EditorUtility.SetDirty(currentDatabase);

            if ( noUndo ) return;
            if ( processCount > 12 ) {
                EditorUtilities.BackupAsset(currentDatabase);
                processCount = 0;
            } else {
                processCount++;
            }
        }

        public static void LoadCurrent(IEGraphView graphView) {
            if ( GraphWindow.CurrentDatabase == null || routine != null || graphView == null ) return;
            graphView.ClearGraph();
            Init(graphView);
            currentDatabase = GraphWindow.CurrentDatabase;

            LoadGroups();
            LoadNodes();
            LoadNodesConnections();

            routine = EditorRoutine.StartRoutine(.1f, () => { routine = null; });

            if ( isDirty ) {
                SaveCurrent(true);
                isDirty = false;
            }
        }

        private static void LoadNodesConnections() {
            int syncCount = 0;
            foreach ( var loadedNode in loadedNodes.Values ) {
                foreach ( var visualElement in loadedNode.GetElements< VisualElement >() ) {
                    if ( visualElement.GetType() != typeof( Port ) ) continue;
                    var outputPort = (Port)visualElement;
                    if ( outputPort.direction == Direction.Input ) continue;
                    var outputData = (OutputData)outputPort.userData;
                    if ( outputData == null ) continue;
                    var removeList = new List< PortData >();
                    foreach ( var data in
                             outputData.DataCollection.Where(data => !string.IsNullOrEmpty(data.NextID)) ) {
                        if ( !loadedNodes.ContainsKey(data.NextID) ) {
                            removeList.Add(data);
                            continue;
                        }

                        var nextNode = loadedNodes[ data.NextID ];
                        var nextNodeInputPort = nextNode.GetElements< Port >().FirstOrDefault(p =>
                            ( p.portName == data.NextName || ( p.userData is string str && str == data.ActorID ) ) &&
                            p.direction == Direction.Input);
                        if ( nextNodeInputPort == null ) {
                            removeList.Add(data);
                            continue;
                        }

                        var edge = outputPort.ConnectTo(nextNodeInputPort);

                        loadedNode.children.Add(nextNode);
                        nextNode.parents.Add(loadedNode);

                        if ( GraphWindow.instance.CurrentPage == GenericNodeType.Scheme &&
                             outputPort.portColor != STATIC.DefaultColor &&
                             nextNodeInputPort.portColor == STATIC.DefaultColor )
                            nextNodeInputPort.portColor = outputPort.portColor;

                        _graphView.AddElement(edge);
                        loadedNode.RefreshPorts();
                    }

                    foreach ( var pData in removeList ) {
                        outputData.DataCollection.Remove(pData);
                        loadedNode.SetDirty();
                        syncCount++;
                    }
                }
            }

            if ( syncCount > 0 ) {
                NDebug.Log($"Database is Synchronized. {syncCount}", NLogType.Log, true);
                isDirty = true;
            }

            _graphView.UpdateNodeIcons();
        }

        private static void LoadGroups() {
            var currentPage = GraphWindow.instance.CurrentPage;

            // 1.0.4
            bool sync = false;
            foreach ( var schemeGroup in currentDatabase.groupDataList.OfType< SchemeGroupData >() ) {
                var scheme = currentDatabase.schemeLibrary.FirstOrDefault(p =>
                    p.ID == schemeGroup.ID && string.IsNullOrEmpty(p.Description) &&
                    p.Description != schemeGroup.Description);
                if ( scheme == null ) continue;
                scheme.SetDescription(schemeGroup.Description);
                sync = true;
            }

            if ( sync ) {
                Debug.Log($"Scheme descriptions synchronized. [{STATIC.CURRENT_VERSION}]");
                EditorUtility.SetDirty(currentDatabase);
            }

            var _groups = currentPage == GenericNodeType.Scheme
                ? currentDatabase.groupDataList.Where(n => n.ID == GraphWindow.instance.storyKey)
                : currentPage == GenericNodeType.Clan
                    ? currentDatabase.groupDataList.Where(n => n.ID == GraphWindow.instance.clanKey)
                    : currentPage == GenericNodeType.Family
                        ? currentDatabase.groupDataList.Where(n => n.ID == GraphWindow.instance.familyKey)
                        : currentPage == GenericNodeType.Rule
                            ? currentDatabase.groupDataList.Where(n => n.ID == GraphWindow.instance.ruleKey)
                            : currentDatabase.groupDataList.Where(g => g.GenericType == currentPage);

            foreach ( var groupData in _groups ) {
                IGroup group = null;
                if ( groupData is SchemeGroupData schemeGroup )
                    group = _graphView.CreateSchemeGroup(schemeGroup.Title, schemeGroup.Position,
                        schemeGroup.Description,
                        schemeGroup.RuleID, schemeGroup.Variables,
                        schemeGroup.Icon, schemeGroup.HideIfNotCompatible, schemeGroup.TargetNotRequired,
                        schemeGroup.HideOnUI);

                if ( groupData is ClanGroupData clanGroup )
                    group = _graphView.CreateClanGroup(clanGroup.Title, clanGroup.Position, clanGroup.Story,
                        clanGroup.Emblem,
                        clanGroup.CultureID, clanGroup.Policies);

                if ( groupData is FamilyGroupData familyGroup )
                    group = _graphView.CreateFamilyGroup(familyGroup.Title,
                        familyGroup.Position, familyGroup.Story, familyGroup.Emblem, familyGroup.CultureID,
                        familyGroup.Policies);

                if ( groupData is RuleGroupData ruleGroupData )
                    group = _graphView.CreateRuleGroup(ruleGroupData.Title, ruleGroupData.Position);

                if ( group == null ) continue;
                group.ID = groupData.ID;
                loadedGroups.Add(group.ID, group);
            }
        }

        #region LOAD_NODES

        private static void LoadNodes() {
            var currentPage = GraphWindow.instance.CurrentPage;

            var storyKey = GraphWindow.instance.storyKey;
            var clanKey = GraphWindow.instance.clanKey;
            var familyKey = GraphWindow.instance.familyKey;
            var ruleKey = GraphWindow.instance.ruleKey;
            var actorKey = GraphWindow.instance.actorKey;

            IEnumerable<NodeData> _nodes = currentDatabase.nodeDataList
                .Where(n => n != null)
                .Where(n =>
                    currentPage == GenericNodeType.Scheme
                        ? n.GenericType == GenericNodeType.Scheme && n.GroupId == storyKey
                        : currentPage == GenericNodeType.Clan
                            ? (n.GenericType == GenericNodeType.Clan && n.GroupId == clanKey) ||
                              n is RoleData or HeirFilterData
                            : currentPage == GenericNodeType.Family
                                ? n.GenericType == GenericNodeType.Family && n.GroupId == familyKey
                                : currentPage == GenericNodeType.Policy
                                    ? n is PolicyData
                                    : currentPage == GenericNodeType.Rule
                                        ? n.GenericType == GenericNodeType.Rule && n.GroupId == ruleKey
                                        : currentPage == GenericNodeType.Actor
                                            ? n.GenericType == GenericNodeType.Actor && n.ID == actorKey
                                            : n.GenericType == currentPage
                );


            foreach ( var nodeData in _nodes.OrderBy(n => n is JumpData) ) // Jump Node loaded last
            {
                switch ( nodeData ) {
                    #region NEXUS

                    case StartData data: {
                        var node = _graphView.CreateNode< StartNode >(nodeData.Position, false);
                        node.ID = nodeData.ID;

                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case EndData data: {
                        var node = _graphView.CreateNode< EndNode >(nodeData.Position, false);
                        node.ID = nodeData.ID;

                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case DialogueData data: {
                        var node = _graphView.CreateNode< DialogueNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;

                        //Data
                        node.Title = data.Title;
                        node.Content = data.Content;
                        node.Break = data.Break;
                        node.Time = data.Time;
                        node.Background = data.Background;
                        node.Outputs = data.Outputs;
                        node.TypeWriter = data.TypeWriter;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case GlobalMessageData data: {
                        var node = _graphView.CreateNode< GlobalMessageNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;

                        //Data
                        node.Title = data.Title;
                        node.Content = data.Content;
                        node.TypeWriter = data.TypeWriter;
                        node.Background = data.Background;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ObjectiveData data: {
                        var node = _graphView.CreateNode< ObjectiveNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;

                        //Data
                        node.Outputs = data.Outputs;
                        node.Objective = data.Objective;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ClanMemberData data: {
                        var node = _graphView.CreateNode< ClanMemberNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;

                        //Data
                        node.ActorID = data.ActorID;
                        node.RoleID = data.RoleID;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }
                    case FamilyMemberData data: {
                        var node = _graphView.CreateNode< FamilyMemberNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;

                        //Data
                        node.ActorID = data.ActorID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case WaitData data: {
                        var node = _graphView.CreateNode< WaitNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;

                        //Data
                        node.Delay = data.Delay;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }
                    case WaitRandomData data: {
                        var node = _graphView.CreateNode< WaitRandomNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;

                        //Data
                        node.Min = data.Min;
                        node.Max = data.Max;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ChanceData data: {
                        var node = _graphView.CreateNode< ChanceNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Chance = data.Chance;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ChanceModifierData data: {
                        var node = _graphView.CreateNode< ChanceModifierNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.Positive = data.Positive;
                        node.Negative = data.Negative;
                        node.Opposite = data.Opposite;
                        node.Outputs = data.Outputs;
                        node.Mode = data.Mode;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case RandomData data: {
                        var node = _graphView.CreateNode< RandomNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SuccessData data: {
                        var node = _graphView.CreateNode< SuccessSchemeNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case FailData data: {
                        var node = _graphView.CreateNode< FailSchemeNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case InvokeData data: {
                        var node = _graphView.CreateNode< InvokeNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case JumpData data: {
                        var node = _graphView.CreateNode< JumpNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.LinkID = data.LinkID;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case LinkedData data: {
                        var node = _graphView.CreateNode< LinkedNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Name = data.Name;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SignalData data: {
                        var node = _graphView.CreateNode< SignalNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Signal = data.Signal;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case TriggerData data: {
                        var node = _graphView.CreateNode< TriggerNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.TriggerName = data.TriggerName;
                        node.Value = data.Value;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case WaitTriggerData data: {
                        var node = _graphView.CreateNode< WaitTriggerNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.TriggerName = data.TriggerName;
                        node.Timeout = data.Timeout;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetVariableData data: {
                        var node = _graphView.CreateNode< GetVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.Type = data.VariableType;
                        node.EnumType = data.EnumType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetFamilyVariableData data: {
                        var node = _graphView.CreateNode< GetFamilyVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.Type = data.VariableType;
                        node.EnumType = data.EnumType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetClanVariableData data: {
                        var node = _graphView.CreateNode< GetClanVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.Type = data.VariableType;
                        node.EnumType = data.EnumType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetRelationVariableData data: {
                        var node = _graphView.CreateNode< GetRelationVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.Type = data.VariableType;
                        node.EnumType = data.EnumType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case WaitUntilData data: {
                        var node = _graphView.CreateNode< WaitUntilNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.Type = data.VariableType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetVariableData data: {
                        var node = _graphView.CreateNode< SetVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.ObjectValue = data.ObjectValue;
                        node.Operation = data.Operation;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetFamilyVariableData data: {
                        var node = _graphView.CreateNode< SetFamilyVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.ObjectValue = data.ObjectValue;
                        node.Operation = data.Operation;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetClanVariableData data: {
                        var node = _graphView.CreateNode< SetClanVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.ObjectValue = data.ObjectValue;
                        node.Operation = data.Operation;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetRelationVariableData data: {
                        var node = _graphView.CreateNode< SetRelationVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.ObjectValue = data.ObjectValue;
                        node.Operation = data.Operation;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SequencerData data: {
                        var node = _graphView.CreateNode< SequencerNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SkipSequencerData data: {
                        var node = _graphView.CreateNode< SkipSequencerNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Index = data.Index;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case BreakSequencerData data: {
                        var node = _graphView.CreateNode< BreakSequencerNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case BreakRepeaterData data: {
                        var node = _graphView.CreateNode< BreakRepeaterNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case ContinueData data: {
                        var node = _graphView.CreateNode< ContinueNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case BreakData data: {
                        var node = _graphView.CreateNode< BreakNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }
                    
                    case PauseData data: {
                        var node = _graphView.CreateNode< PauseNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }
                    
                    case ResumeData data: {
                        var node = _graphView.CreateNode< ResumeNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case NewFlowData data: {
                        var node = _graphView.CreateNode< NewFlowNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case BackgroundWorkerData data: {
                        var node = _graphView.CreateNode< BackgroundWorkerNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case SoundData data: {
                        var node = _graphView.CreateNode< SoundNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Clip = data.Clip;
                        node.Volume = data.Volume;
                        node.Pitch = data.Pitch;
                        node.Priority = data.Priority;
                        node.WaitEnd = data.WaitEnd;
                        node.Outputs = data.Outputs;
                        node.AudioMixerGroup = data.AudioMixerGroup;
                        node.Loop = data.Loop;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SoundClassData data: {
                        var node = _graphView.CreateNode< SoundClassNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Clip = data.Clip;
                        node.Volume = data.Volume;
                        node.Pitch = data.Pitch;
                        node.Priority = data.Priority;
                        node.FadeOut = data.FadeOut;
                        node.AudioMixerGroup = data.AudioMixerGroup;
                        node.Loop = data.Loop;
                        node.StopWhenClosed = data.StopWhenClosed;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case VoiceClassData data: {
                        var node = _graphView.CreateNode< VoiceClassNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Clip = data.Clip;
                        node.Volume = data.Volume;
                        node.Pitch = data.Pitch;
                        node.Priority = data.Priority;
                        node.AudioMixerGroup = data.AudioMixerGroup;
                        node.Sync = data.Sync;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case RepeaterData data: {
                        var node = _graphView.CreateNode< RepeaterNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.RepetitionCount = data.RepetitionCount;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case NoteData data: {
                        var node = _graphView.CreateNode< CommentLeftToRightNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Note = data.Note;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case CommentRuleData data: {
                        var node = _graphView.CreateNode< CommentRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Note = data.Note;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case NoteDataRL data: {
                        var node = _graphView.CreateNode< CommentRightToLeftNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Note = data.Note;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case LogData data: {
                        var node = _graphView.CreateNode< LogNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Message = data.Log;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ChoiceTextData data: {
                        var node = _graphView.CreateNode< ChoiceDataNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Text = data.Text;
                        node.Text2 = data.Text2;
                        node.ChanceID = data.ChanceID;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SchemeIsActiveRuleData data: {
                        var node = _graphView.CreateNode< SchemeIsActiveRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;
                        node.SchemeID = data.SchemeID;
                        node.VerifyType = data.VerifyType;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SchemeIsActiveData data: {
                        var node = _graphView.CreateNode< SchemeIsActiveNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;
                        node.SchemeID = data.SchemeID;
                        node.VerifyType = data.VerifyType;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetTableVariableData data: {
                        var node = _graphView.CreateNode< GetTableVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.IntegerValue = data.IntegerValue;
                        node.FloatValue = data.FloatValue;
                        node.Type = data.VariableType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetSchemeTableVariableData data: {
                        var node = _graphView.CreateNode< GetSchemeTableVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.SchemeID = data.SchemeID;
                        node.StringValue = data.StringValue;
                        node.IntegerValue = data.IntegerValue;
                        node.FloatValue = data.FloatValue;
                        node.Type = data.VariableType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetSchemeTableVariableRuleData data: {
                        var node = _graphView.CreateNode< GetSchemeTableVariableRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.SchemeID = data.SchemeID;
                        node.StringValue = data.StringValue;
                        node.IntegerValue = data.IntegerValue;
                        node.FloatValue = data.FloatValue;
                        node.Type = data.VariableType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetTableVariableData data: {
                        var node = _graphView.CreateNode< SetTableVariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.IntegerValue = data.IntegerValue;
                        node.VariableType = data.VariableType;
                        node.FloatValue = data.FloatValue;
                        node.Operation = data.Operation;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetActorData data: {
                        var node = _graphView.CreateNode< SetActorNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case IsAIData data: {
                        var node = _graphView.CreateNode< IsAINode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetActorData data: {
                        var node = _graphView.CreateNode< GetActorNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.ActorID = data.ActorID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ReturnActorData data: {
                        var node = _graphView.CreateNode< ReturnActorNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case DualActorData data: {
                        var node = _graphView.CreateNode< DualActorNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.DualType = data.DualType;
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ReturnClanData data: {
                        var node = _graphView.CreateNode< ReturnClanNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ReturnFamilyData data: {
                        var node = _graphView.CreateNode< ReturnFamilyNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetConspiratorData data: {
                        var node = _graphView.CreateNode< SetConspiratorNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.ConspiratorID = data.ConspiratorID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case OnLoadData data: {
                        var node = _graphView.CreateNode< OnLoadNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetTargetData data: {
                        var node = _graphView.CreateNode< SetTargetNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.TargetID = data.TargetID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetFamilyData data: {
                        var node = _graphView.CreateNode< GetFamilyNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.FamilyID = data.FamilyID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetClanData data: {
                        var node = _graphView.CreateNode< GetClanNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.ClanID = data.ClanID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetClanData data: {
                        var node = _graphView.CreateNode< SetClanNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.ClanID = data.ClanID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetRoleData data: {
                        var node = _graphView.CreateNode< GetRoleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.RoleID = data.RoleID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case KeyHandlerData data: {
                        var node = _graphView.CreateNode< KeyHandlerNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.KeyCode = data.KeyCode;
                        node.KeyType = data.KeyType;
                        node.TapCount = data.TapCount;
                        node.HoldTime = data.HoldTime;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetCultureData data: {
                        var node = _graphView.CreateNode< GetCultureNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.CultureID = data.CultureID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetCultureData data: {
                        var node = _graphView.CreateNode< SetCultureNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.CultureID = data.CultureID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetRoleData data: {
                        var node = _graphView.CreateNode< SetRoleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.RoleID = data.RoleID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GenderData data: {
                        var node = _graphView.CreateNode< GenderNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case AgeData data: {
                        var node = _graphView.CreateNode< AgeNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Age = data.Age;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SpouseCountData data: {
                        var node = _graphView.CreateNode< SpouseCountNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ChildCountData data: {
                        var node = _graphView.CreateNode< ChildCountNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ParentCountData data: {
                        var node = _graphView.CreateNode< ParentCountNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GrandparentCountData data: {
                        var node = _graphView.CreateNode< GrandparentCountNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SiblingCountData data: {
                        var node = _graphView.CreateNode< SiblingCountNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GrandchildCountData data: {
                        var node = _graphView.CreateNode< GrandchildCountNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SameClanData data: {
                        var node = _graphView.CreateNode< SameClanNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SameFamilyData data: {
                        var node = _graphView.CreateNode< SameFamilyNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SameCultureData data: {
                        var node = _graphView.CreateNode< SameCultureNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SameGenderData data: {
                        var node = _graphView.CreateNode< SameGenderNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case IsParentData data: {
                        var node = _graphView.CreateNode< IsParentNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case HasHeirData data: {
                        var node = _graphView.CreateNode< HasHeirNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case IsGrandparentData data: {
                        var node = _graphView.CreateNode< IsGrandParentNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }
                    case IsGrandchildData data: {
                        var node = _graphView.CreateNode< IsGrandChildNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }
                    case IsSpouseData data: {
                        var node = _graphView.CreateNode< IsSpouseNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }
                    case IsSiblingData data: {
                        var node = _graphView.CreateNode< IsSiblingNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }
                    case IsChildData data: {
                        var node = _graphView.CreateNode< IsChildNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case IsRelativeData data: {
                        var node = _graphView.CreateNode< IsRelativeNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetPolicyData data: {
                        var node = _graphView.CreateNode< GetPolicyNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.PolicyID = data.PolicyID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case AddSpouseData data: {
                        var node = _graphView.CreateNode< AddSpouseNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.JoinSpouseFamily = data.JoinSpouseFamily;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetInheritorData data: {
                        var node = _graphView.CreateNode< SetInheritorNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Inheritor = data.Inheritor;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);

                        node.Draw();
                        break;
                    }

                    case RemoveSpousesData data: {
                        var node = _graphView.CreateNode< RemoveSpousesNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SetStateData data: {
                        var node = _graphView.CreateNode< SetStateNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.State = data.State;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetStateData data: {
                        var node = _graphView.CreateNode< GetStateNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.State = data.State;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case SchemeStateData data: {
                        var node = _graphView.CreateNode< SchemeStateNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.State = data.State;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case AddPolicyData data: {
                        var node = _graphView.CreateNode< AddPolicyNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.PolicyID = data.PolicyID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case RemovePolicyData data: {
                        var node = _graphView.CreateNode< RemovePolicyNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.PolicyID = data.PolicyID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case RuleData data: {
                        var node = _graphView.CreateNode< RuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.RuleID = data.RuleID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ValidatorData data: {
                        var node = _graphView.CreateNode< ValidatorNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.RuleID = data.RuleID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    #endregion

                    #region RULE

                    case StartRuleData data: {
                        var node = _graphView.CreateNode< StartRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case SuccessRuleData data: {
                        var node = _graphView.CreateNode< SuccessRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case CauseRuleData data: {
                        var node = _graphView.CreateNode< ErrorRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.ErrorName = data.ErrorName;
                        node.Error = data.Error;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case WarningRuleData data: {
                        var node = _graphView.CreateNode< WarningRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.WarningName = data.WarningName;
                        node.Warning = data.Warning;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetPolicyRuleData data: {
                        var node = _graphView.CreateNode< GetPolicyRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.PolicyID = data.PolicyID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetVariableRuleData data: {
                        var node = _graphView.CreateNode< GetVariableRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.Type = data.VariableType;
                        node.EnumType = data.EnumType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetFamilyVariableRuleData data: {
                        var node = _graphView.CreateNode< GetFamilyVariableRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.Type = data.VariableType;
                        node.EnumType = data.EnumType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetClanVariableRuleData data: {
                        var node = _graphView.CreateNode< GetClanVariableRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.Type = data.VariableType;
                        node.EnumType = data.EnumType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case IsAIRuleData data: {
                        var node = _graphView.CreateNode< IsAIRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetActorRuleData data: {
                        var node = _graphView.CreateNode< GetActorRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.ActorID = data.ActorID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetFamilyRuleData data: {
                        var node = _graphView.CreateNode< GetFamilyRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.FamilyID = data.FamilyID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetClanRuleData data: {
                        var node = _graphView.CreateNode< GetClanRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.ClanID = data.ClanID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetCultureRuleData data: {
                        var node = _graphView.CreateNode< GetCultureRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.CultureID = data.CultureID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetRoleRuleData data: {
                        var node = _graphView.CreateNode< GetRoleRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.RoleID = data.RoleID;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GenderRuleData data: {
                        var node = _graphView.CreateNode< GenderRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case AgeRuleData data: {
                        var node = _graphView.CreateNode< AgeRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Age = data.Age;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case SpouseCountRuleData data: {
                        var node = _graphView.CreateNode< SpouseCountRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case ChildCountRuleData data: {
                        var node = _graphView.CreateNode< ChildCountRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case ParentCountRuleData data: {
                        var node = _graphView.CreateNode< ParentCountRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case SiblingCountRuleData data: {
                        var node = _graphView.CreateNode< SiblingCountRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GrandchildCountRuleData data: {
                        var node = _graphView.CreateNode< GrandchildCountRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GrandparentCountRuleData data: {
                        var node = _graphView.CreateNode< GrandparentCountRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Count = data.Count;
                        node.IncludePassiveCharacters = data.IncludePassiveCharacters;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case SameClanRuleData data: {
                        var node = _graphView.CreateNode< SameClanRuleNode >(nodeData.Position, false);


                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case SameFamilyRuleData data: {
                        var node = _graphView.CreateNode< SameFamilyRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case SameCultureRuleData data: {
                        var node = _graphView.CreateNode< SameCultureRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case SameGenderRuleData data: {
                        var node = _graphView.CreateNode< SameGenderRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GetStateRuleData data: {
                        var node = _graphView.CreateNode< GetStateRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.State = data.State;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case IsParentRuleData data: {
                        var node = _graphView.CreateNode< IsParentRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case HasHeirRuleData data: {
                        var node = _graphView.CreateNode< HasHeirRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }

                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case IsGrandchildRuleData data: {
                        var node = _graphView.CreateNode< IsGrandChildRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case IsRelativeRuleData data: {
                        var node = _graphView.CreateNode< IsRelativeRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case IsSpouseRuleData data: {
                        var node = _graphView.CreateNode< IsSpouseRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case IsChildRuleData data: {
                        var node = _graphView.CreateNode< IsChildRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case IsSiblingRuleData data: {
                        var node = _graphView.CreateNode< IsSiblingRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case IsGrandparentRuleData data: {
                        var node = _graphView.CreateNode< IsGrandParentRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case ReturnActorRuleData data: {
                        var node = _graphView.CreateNode< ReturnActorRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ReturnClanRuleData data: {
                        var node = _graphView.CreateNode< ReturnClanRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case ReturnFamilyRuleData data: {
                        var node = _graphView.CreateNode< ReturnFamilyRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case DualActorRuleData data: {
                        var node = _graphView.CreateNode< DualActorRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.DualType = data.DualType;
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case GetRelationVariableRuleData data: {
                        var node = _graphView.CreateNode< GetRelationVariableRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableID = data.VariableID;
                        node.StringValue = data.StringValue;
                        node.FloatValue = data.FloatValue;
                        node.IntegerValue = data.IntegerValue;
                        node.Type = data.VariableType;
                        node.EnumType = data.EnumType;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case InvokeRuleData data: {
                        var node = _graphView.CreateNode< InvokeRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.MethodName = data.MethodName;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    case LogRuleData data: {
                        var node = _graphView.CreateNode< LogRuleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.Message = data.Log;
                        node.Outputs = data.Outputs;

                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        group?.AddElement(node);
                        _graphView.AddElement(node);
                        loadedNodes.Add(node.ID, node);


                        node.Draw();
                        break;
                    }

                    #endregion

                    #region GHOST

                    case GhostClanData data: {
                        var node = _graphView.CreateNode< GhostClanNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;

                        //Data
                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    case GhostFamilyData data: {
                        if ( !loadedGroups.ContainsKey(data.GroupId) ) break;
                        var node = _graphView.CreateNode< GhostFamilyNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;

                        //Data
                        IGroup group = null;
                        if ( !string.IsNullOrEmpty(data.GroupId) ) {
                            group = loadedGroups[ data.GroupId ];
                            node.Group = group;
                        }


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        group?.AddElement(node);
                        break;
                    }

                    #endregion

                    #region UNGROUPED

                    case PolicyData data: {
                        var node = _graphView.CreateNode< PolicyNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.PolicyName = data.PolicyName;
                        node.Description = data.Description;
                        node.PolicyIcon = data.Icon;
                        node.Type = data.Type;

                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        break;
                    }
                    case ActorData data: {
                        var node = _graphView.CreateNode< ActorNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.ActorName = data.ActorName;
                        node.Age = data.Age;
                        node.Gender = data.Gender;
                        node.State = data.State;
                        node.CultureID = data.CultureID;
                        node.Portrait = data.Portrait;
                        node.IsPlayer = data.IsPlayer;

                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        break;
                    }
                    case CultureData data: {
                        var node = _graphView.CreateNode< CultureNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.CultureName = data.CultureName;
                        node.Description = data.Description;
                        node.CultureIcon = data.Icon;
                        node.NamesForMale = new List< string >(data.MaleNames.ToList());
                        node.NamesForFemale = new List< string >(data.FemaleNames.ToList());


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        break;
                    }
                    case VariableData data: {
                        var node = _graphView.CreateNode< VariableNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.VariableName = data.VariableName;
                        node.Value = data.EnumValue;
                        node.Type = data.VariableType;
                        node.Variable = data.Variable.Duplicate();


                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        break;
                    }
                    case RoleData data: {
                        var node = _graphView.CreateNode< RoleNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.RoleName = data.RoleName;
                        node.Description = data.Description;
                        node.FilterID = data.FilterID;
                        node.RoleSlot = data.RoleSlot;
                        node.Legacy = data.Legacy;
                        node.TitleForMale = data.TitleForMale;
                        node.TitleForFemale = data.TitleForFemale;
                        node.RoleIcon = data.RoleIcon;
                        node.Priority = data.Priority;

                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        break;
                    }
                    case HeirFilterData data: {
                        var node = _graphView.CreateNode< HeirFilterNode >(nodeData.Position, false);

                        node.ID = nodeData.ID;
                        //Data
                        node.FilterName = data.FilterName;
                        node.Gender = data.Gender;
                        node.Age = data.Age;
                        node.Clan = data.Clan;
                        node.Relatives = new List< int >(data.Relatives);

                        node.Draw();
                        _graphView.AddElement(node);

                        loadedNodes.Add(node.ID, node);
                        break;
                    }

                    #endregion

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion

        private static void SaveNodes() {
            foreach ( var node in nodes ) {
                SaveNode(node);

                switch ( node ) {
                    case PolicyNode policyNode:
                        SavePolicyItem(policyNode);
                        break;
                    case CultureNode cultureNode:
                        SaveCultureItem(cultureNode);
                        break;
                    case ActorNode actorNode:
                        SaveActorItem(actorNode);
                        break;
                    case VariableNode variableNode:
                        SaveVariables(variableNode);
                        break;
                    case RoleNode roleNode:
                        SaveRoleItem(roleNode);
                        break;
                    // case ThemeNode themeNode:
                    //     SaveThemeItem(themeNode);
                    //     break;
                }
            }
        }

        private static void SaveNodeItem(NodeData data) {
            var _nodes = currentDatabase.nodeDataList;
            var index = _nodes.FindIndex(g => g.ID == data.ID);
            if ( index != -1 ) {
                _nodes[ index ] = data;
                return;
            }

            _nodes.Add(data);
        }

        public static void RemoveNodeItem(INode node) {
            var _nodes = currentDatabase.nodeDataList;
            var data = _nodes.Find(d => d.ID == node.ID);
            if ( data != null ) _nodes.Remove(data);

            switch ( node ) {
                case PolicyNode policyNode:

                    currentDatabase.policyCatalog.RemoveAll(p => p.ID == policyNode.ID);
                    break;
                case CultureNode cultureNode:
                    currentDatabase.culturalProfiles.RemoveAll(p => p.ID == cultureNode.ID);
                    break;
                case ActorNode actorNode:
                    currentDatabase.actorRegistry.RemoveAll(p => p.ID == actorNode.ID);
                    break;
                case VariableNode variableNode:
                    currentDatabase.variablePool.RemoveAll(v => v.name == variableNode.VariableName);
                    break;
                case RoleNode roleNode:
                    currentDatabase.roleDefinitions.RemoveAll(p => p.ID == roleNode.ID);
                    break;
                // case ThemeNode themeNode:
                //     currentDatabase.Themes.RemoveAll(p => p.ID == themeNode.ID);
                //     break;
            }
        }

        private static void SaveNode(INode savedNode) {
            if ( savedNode is null ) return;
            var pos = savedNode.GetPosition().position;

            switch ( savedNode ) {
                #region NEXUS

                case StartNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var startData = new StartData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(startData);
                    break;
                }

                case EndNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var startData = new EndData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(startData);
                    break;
                }

                case DialogueNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var dialogueData = new DialogueData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Title,
                        node.Content, node.Break, node.TypeWriter, node.Time, node.Background, outputs);
                    SaveNodeItem(dialogueData);
                    break;
                }

                case GlobalMessageNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var dialogueData = new GlobalMessageData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Title,
                        node.Content, node.TypeWriter, node.Background, outputs);
                    SaveNodeItem(dialogueData);
                    break;
                }

                case ClanMemberNode node: {
                    var memberData = new ClanMemberData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.ActorID,
                        node.RoleID);
                    SaveNodeItem(memberData);
                    break;
                }

                case ObjectiveNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var memberData = new ObjectiveData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        outputs, node.Objective);
                    SaveNodeItem(memberData);
                    break;
                }

                case FamilyMemberNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var memberData = new FamilyMemberData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.ActorID,
                        outputs);
                    SaveNodeItem(memberData);
                    break;
                }

                case WaitNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var waitData = new WaitData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Delay,
                        outputs);
                    SaveNodeItem(waitData);
                    break;
                }

                case WaitRandomNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var waitData = new WaitRandomData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Min, node.Max,
                        outputs);
                    SaveNodeItem(waitData);
                    break;
                }

                case ChanceNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var chanceData =
                        new ChanceData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.Chance, outputs);
                    SaveNodeItem(chanceData);
                    break;
                }
                case ChanceModifierNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var chanceModifierData =
                        new ChanceModifierData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.VariableID, node.Positive, node.Negative, node.Opposite, node.Mode, outputs);
                    SaveNodeItem(chanceModifierData);
                    break;
                }

                case RandomNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var randomData = new RandomData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(randomData);
                    break;
                }

                case SuccessSchemeNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var successData = new SuccessData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(successData);
                    break;
                }

                case FailSchemeNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var failData = new FailData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        outputs);
                    SaveNodeItem(failData);
                    break;
                }

                case InvokeNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var invokeNode = new InvokeData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.MethodName,
                        outputs);
                    SaveNodeItem(invokeNode);
                    break;
                }

                case JumpNode node: {
                    var jumpNode = new JumpData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.LinkID);
                    SaveNodeItem(jumpNode);
                    break;
                }

                case LinkedNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var linkedNode = new LinkedData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Name,
                        outputs);
                    SaveNodeItem(linkedNode);
                    break;
                }

                case SignalNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var signalNode = new SignalData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Signal,
                        outputs);
                    SaveNodeItem(signalNode);
                    break;
                }

                case TriggerNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var triggerData = new TriggerData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.TriggerName,
                        node.Value, outputs);
                    SaveNodeItem(triggerData);
                    break;
                }

                case WaitTriggerNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var waitTriggerData = new WaitTriggerData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.TriggerName, node.Timeout, outputs);
                    SaveNodeItem(waitTriggerData);
                    break;
                }

                case GetVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getVariableData = new GetVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.EnumType, node.Type,
                        outputs);
                    SaveNodeItem(getVariableData);
                    break;
                }

                case GetFamilyVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getVariableData = new GetFamilyVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.EnumType, node.Type,
                        outputs);
                    SaveNodeItem(getVariableData);
                    break;
                }

                case GetClanVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getVariableData = new GetClanVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.EnumType, node.Type,
                        outputs);
                    SaveNodeItem(getVariableData);
                    break;
                }

                case GetRelationVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getRelationVariableData = new GetRelationVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.EnumType, node.Type,
                        outputs);
                    SaveNodeItem(getRelationVariableData);
                    break;
                }

                case WaitUntilNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var waitUntilData = new WaitUntilData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.Type, outputs);
                    SaveNodeItem(waitUntilData);
                    break;
                }

                case SetVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var setVariableData = new SetVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.ObjectValue,
                        node.Operation, outputs);
                    SaveNodeItem(setVariableData);
                    break;
                }

                case SetClanVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var setVariableData = new SetClanVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.ObjectValue,
                        node.Operation, outputs);
                    SaveNodeItem(setVariableData);
                    break;
                }

                case SetFamilyVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var setVariableData = new SetFamilyVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.ObjectValue,
                        node.Operation, outputs);
                    SaveNodeItem(setVariableData);
                    break;
                }

                case SetRelationVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var setRelationVariableData = new SetRelationVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.ObjectValue,
                        node.Operation, outputs);
                    SaveNodeItem(setRelationVariableData);
                    break;
                }

                case SequencerNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var sequencerData =
                        new SequencerData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(sequencerData);
                    break;
                }

                case SkipSequencerNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var skipSequencerData =
                        new SkipSequencerData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.Index,
                            outputs);
                    SaveNodeItem(skipSequencerData);
                    break;
                }

                case BreakSequencerNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var breakSequencerData =
                        new BreakSequencerData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(breakSequencerData);
                    break;
                }

                case BreakRepeaterNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var breakRepeaterData =
                        new BreakRepeaterData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(breakRepeaterData);
                    break;
                }

                case ContinueNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var continueData =
                        new ContinueData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(continueData);
                    break;
                }

                case BreakNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var breakData =
                        new BreakData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(breakData);
                    break;
                }
                
                case PauseNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var breakData =
                        new PauseData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(breakData);
                    break;
                }
                case ResumeNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var breakData =
                        new ResumeData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(breakData);
                    break;
                }

                case NewFlowNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var breakData =
                        new NewFlowData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(breakData);
                    break;
                }

                case BackgroundWorkerNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var breakData =
                        new BackgroundWorkerData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(breakData);
                    break;
                }

                case SoundNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var soundData = new SoundData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Clip, node.Volume, node.Pitch, node.Priority, node.WaitEnd, node.Loop,
                        node.AudioMixerGroup, outputs);
                    SaveNodeItem(soundData);
                    break;
                }

                case SoundClassNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var soundData = new SoundClassData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Clip, node.Volume, node.Pitch, node.FadeOut, node.Priority, node.Loop, node.StopWhenClosed,
                        node.AudioMixerGroup, outputs);
                    SaveNodeItem(soundData);
                    break;
                }

                case VoiceClassNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var soundData = new VoiceClassData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Clip, node.Volume, node.Pitch, node.Priority, node.Sync, node.AudioMixerGroup, outputs);
                    SaveNodeItem(soundData);
                    break;
                }

                case RepeaterNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var repeatData = new RepeaterData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.RepetitionCount,
                        outputs);
                    SaveNodeItem(repeatData);
                    break;
                }

                case CommentLeftToRightNode node: {
                    var noteData = new NoteData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Note);
                    SaveNodeItem(noteData);
                    break;
                }

                case CommentRightToLeftNode node: {
                    var noteData = new NoteDataRL(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Note);
                    SaveNodeItem(noteData);
                    break;
                }

                case CommentRuleNode node: {
                    var noteData = new CommentRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Note);
                    SaveNodeItem(noteData);
                    break;
                }

                case LogNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var logData = new LogData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Message,
                        outputs);
                    SaveNodeItem(logData);
                    break;
                }

                case ChoiceDataNode node: {
                    var choiceData = new ChoiceTextData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Text, node.Text2, node.ChanceID);
                    SaveNodeItem(choiceData);
                    break;
                }

                case SchemeIsActiveNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var schemeIsActiveData = new SchemeIsActiveData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.SchemeID, node.VerifyType, outputs);
                    SaveNodeItem(schemeIsActiveData);
                    break;
                }

                case SchemeIsActiveRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var schemeIsActiveRuleData = new SchemeIsActiveRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.SchemeID, node.VerifyType, outputs);
                    SaveNodeItem(schemeIsActiveRuleData);
                    break;
                }

                case GetTableVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getVariableTableData = new GetTableVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.Type, outputs);
                    SaveNodeItem(getVariableTableData);
                    break;
                }

                case GetSchemeTableVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getVariableTableData = new GetSchemeTableVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.SchemeID, node.StringValue, node.IntegerValue, node.FloatValue, node.Type,
                        outputs);
                    SaveNodeItem(getVariableTableData);
                    break;
                }

                case GetSchemeTableVariableRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getVariableTableData = new GetSchemeTableVariableRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.SchemeID, node.StringValue, node.IntegerValue, node.FloatValue, node.Type,
                        outputs);
                    SaveNodeItem(getVariableTableData);
                    break;
                }

                case SetTableVariableNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var setVariableTableData = new SetTableVariableData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.VariableType,
                        node.Operation,
                        outputs);
                    SaveNodeItem(setVariableTableData);
                    break;
                }

                case SetActorNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var setActorData = new SetActorData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, outputs);
                    SaveNodeItem(setActorData);
                    break;
                }

                case IsAINode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var isAIData = new IsAIData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        outputs);
                    SaveNodeItem(isAIData);
                    break;
                }

                case GetActorNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getActorData =
                        new GetActorData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.ActorID,
                            outputs);
                    SaveNodeItem(getActorData);
                    break;
                }

                case ReturnActorNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var returnActorData =
                        new ReturnActorData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.MethodName,
                            outputs);
                    SaveNodeItem(returnActorData);
                    break;
                }

                case DualActorNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var dualActorData =
                        new DualActorData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.DualType,
                            node.MethodName,
                            outputs);
                    SaveNodeItem(dualActorData);
                    break;
                }

                case ReturnClanNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var returnClanNode =
                        new ReturnClanData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.MethodName,
                            outputs);
                    SaveNodeItem(returnClanNode);
                    break;
                }

                case ReturnFamilyNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var returnFamilyData =
                        new ReturnFamilyData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.MethodName,
                            outputs);
                    SaveNodeItem(returnFamilyData);
                    break;
                }

                case SetConspiratorNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var setConspiratorData =
                        new SetConspiratorData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.ConspiratorID,
                            outputs);
                    SaveNodeItem(setConspiratorData);
                    break;
                }

                case OnLoadNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var onLoadData =
                        new OnLoadData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(onLoadData);
                    break;
                }

                case SetTargetNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var setTargetData =
                        new SetTargetData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.TargetID,
                            outputs);
                    SaveNodeItem(setTargetData);
                    break;
                }

                case GetFamilyNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getFamilyData = new GetFamilyData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.FamilyID, outputs);
                    SaveNodeItem(getFamilyData);
                    break;
                }

                case GetClanNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getClanData = new GetClanData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.ClanID,
                        outputs);
                    SaveNodeItem(getClanData);
                    break;
                }

                case SetClanNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var setClanData = new SetClanData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.ClanID,
                        outputs);
                    SaveNodeItem(setClanData);
                    break;
                }

                case GetRoleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getRoleData = new GetRoleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.RoleID,
                        outputs);
                    SaveNodeItem(getRoleData);
                    break;
                }

                case KeyHandlerNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getRoleData = new KeyHandlerData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.KeyCode, node.KeyType, node.TapCount, node.HoldTime,
                        outputs);
                    SaveNodeItem(getRoleData);
                    break;
                }

                case GetCultureNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getCultureData = new GetCultureData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.CultureID, outputs);
                    SaveNodeItem(getCultureData);
                    break;
                }

                case SetCultureNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var setCultureData = new SetCultureData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.CultureID, outputs);
                    SaveNodeItem(setCultureData);
                    break;
                }

                case SetRoleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var setRoleData = new SetRoleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.RoleID,
                        outputs);
                    SaveNodeItem(setRoleData);
                    break;
                }

                case GenderNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var genderData = new GenderData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(genderData);
                    break;
                }

                case AgeNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var ageData = new AgeData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Age,
                        outputs);
                    SaveNodeItem(ageData);
                    break;
                }

                case SpouseCountNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var spouseCountData = new SpouseCountData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Count, node.IncludePassiveCharacters, outputs);
                    SaveNodeItem(spouseCountData);
                    break;
                }

                case ChildCountNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var childCountData = new ChildCountData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Count, node.IncludePassiveCharacters, outputs);
                    SaveNodeItem(childCountData);
                    break;
                }

                case ParentCountNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var parentCountData = new ParentCountData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Count, node.IncludePassiveCharacters, outputs);
                    SaveNodeItem(parentCountData);
                    break;
                }

                case GrandparentCountNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var grandparentCountData = new GrandparentCountData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Count, node.IncludePassiveCharacters, outputs);
                    SaveNodeItem(grandparentCountData);
                    break;
                }

                case SiblingCountNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var siblingCountData = new SiblingCountData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Count, node.IncludePassiveCharacters, outputs);
                    SaveNodeItem(siblingCountData);
                    break;
                }

                case GrandchildCountNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var grandchildCountData = new GrandchildCountData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Count, node.IncludePassiveCharacters, outputs);
                    SaveNodeItem(grandchildCountData);
                    break;
                }

                case SameClanNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var sameClanData =
                        new SameClanData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(sameClanData);
                    break;
                }

                case SameFamilyNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var sameFamilyData =
                        new SameFamilyData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(sameFamilyData);
                    break;
                }

                case SameCultureNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var sameCultureData =
                        new SameCultureData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(sameCultureData);
                    break;
                }

                case SameGenderNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var sameGenderData =
                        new SameGenderData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(sameGenderData);
                    break;
                }

                case IsParentNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var parentData = new IsParentData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(parentData);
                    break;
                }
                case HasHeirNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var heirData = new HasHeirData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(heirData);
                    break;
                }
                case IsGrandParentNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var grandparentData =
                        new IsGrandparentData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(grandparentData);
                    break;
                }
                case IsGrandChildNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var grandchildData =
                        new IsGrandchildData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(grandchildData);
                    break;
                }
                case IsSpouseNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var spouseData = new IsSpouseData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(spouseData);
                    break;
                }
                case IsSiblingNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var siblingData =
                        new IsSiblingData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(siblingData);
                    break;
                }
                case IsChildNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var childData = new IsChildData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(childData);
                    break;
                }

                case IsRelativeNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var relativeData =
                        new IsRelativeData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(relativeData);
                    break;
                }

                case GetPolicyNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getPolicyData = new GetPolicyData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.PolicyID, outputs);
                    SaveNodeItem(getPolicyData);
                    break;
                }

                case AddSpouseNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var addSpouseData = new AddSpouseData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.JoinSpouseFamily,
                        outputs);
                    SaveNodeItem(addSpouseData);
                    break;
                }

                case SetInheritorNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var addSpouseData = new SetInheritorData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Inheritor,
                        outputs);
                    SaveNodeItem(addSpouseData);
                    break;
                }

                case RemoveSpousesNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var removeSpousesData = new RemoveSpousesData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(removeSpousesData);
                    break;
                }

                case SetStateNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var stateData =
                        new SetStateData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.State, outputs);
                    SaveNodeItem(stateData);
                    break;
                }

                case GetStateNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var stateData =
                        new GetStateData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.State, outputs);
                    SaveNodeItem(stateData);
                    break;
                }

                case SchemeStateNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var stateData =
                        new SchemeStateData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.State, outputs);
                    SaveNodeItem(stateData);
                    break;
                }

                case AddPolicyNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var addPolicyData = new AddPolicyData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.PolicyID, outputs);
                    SaveNodeItem(addPolicyData);
                    break;
                }

                case RemovePolicyNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var removePolicyData = new RemovePolicyData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.PolicyID, outputs);
                    SaveNodeItem(removePolicyData);
                    break;
                }

                case RuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var ruledata = new RuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.RuleID, outputs);
                    SaveNodeItem(ruledata);
                    break;
                }

                case ValidatorNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var rulerdata = new ValidatorData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.RuleID, outputs);
                    SaveNodeItem(rulerdata);
                    break;
                }

                case LogRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var logData = new LogRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.Message,
                        outputs);
                    SaveNodeItem(logData);
                    break;
                }

                #endregion

                #region RULE

                case StartRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var startRuleData =
                        new StartRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(startRuleData);
                    break;
                }

                case SuccessRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var successRuleData =
                        new SuccessRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(successRuleData);
                    break;
                }

                case ErrorRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var successRuleData =
                        new CauseRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.ErrorName, node.Error, outputs);
                    SaveNodeItem(successRuleData);
                    break;
                }

                case WarningRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var successRuleData =
                        new WarningRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.WarningName, node.Warning, outputs);
                    SaveNodeItem(successRuleData);
                    break;
                }

                case GetPolicyRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getPolicyRuleData = new GetPolicyRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.PolicyID, outputs);
                    SaveNodeItem(getPolicyRuleData);
                    break;
                }

                case GetVariableRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getVariableRuleData = new GetVariableRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.EnumType, node.Type,
                        outputs);
                    SaveNodeItem(getVariableRuleData);
                    break;
                }

                case GetFamilyVariableRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getVariableData = new GetFamilyVariableRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.EnumType, node.Type,
                        outputs);
                    SaveNodeItem(getVariableData);
                    break;
                }

                case GetClanVariableRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getVariableData = new GetClanVariableRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.EnumType, node.Type,
                        outputs);
                    SaveNodeItem(getVariableData);
                    break;
                }

                case IsAIRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var isAIRuleData =
                        new IsAIRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(isAIRuleData);
                    break;
                }

                case GetActorRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getActorRuleData = new GetActorRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.ActorID, outputs);
                    SaveNodeItem(getActorRuleData);
                    break;
                }

                case GetFamilyRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getFamilyRuleData = new GetFamilyRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.FamilyID, outputs);
                    SaveNodeItem(getFamilyRuleData);
                    break;
                }

                case GetClanRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getClanRuleData = new GetClanRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.ClanID, outputs);
                    SaveNodeItem(getClanRuleData);
                    break;
                }

                case GetCultureRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getCultureRuleData = new GetCultureRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.CultureID, outputs);
                    SaveNodeItem(getCultureRuleData);
                    break;
                }

                case GetRoleRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getRoleRuleData = new GetRoleRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.RoleID, outputs);
                    SaveNodeItem(getRoleRuleData);
                    break;
                }

                case GenderRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var genderRuleData =
                        new GenderRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(genderRuleData);
                    break;
                }

                case AgeRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var ageRuleData =
                        new AgeRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.Age,
                            outputs);
                    SaveNodeItem(ageRuleData);
                    break;
                }

                case SpouseCountRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var spouseCountRuleData = new SpouseCountRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.Count, node.IncludePassiveCharacters,
                        outputs);
                    SaveNodeItem(spouseCountRuleData);
                    break;
                }

                case ChildCountRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var childCountRuleData = new ChildCountRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.Count, node.IncludePassiveCharacters,
                        outputs);
                    SaveNodeItem(childCountRuleData);
                    break;
                }

                case ParentCountRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var parentCountRuleData = new ParentCountRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.Count, node.IncludePassiveCharacters,
                        outputs);
                    SaveNodeItem(parentCountRuleData);
                    break;
                }

                case SiblingCountRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var siblingCountRuleData = new SiblingCountRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.Count, node.IncludePassiveCharacters,
                        outputs);
                    SaveNodeItem(siblingCountRuleData);
                    break;
                }

                case GrandchildCountRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var grandchildRuleData = new GrandchildCountRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.Count, node.IncludePassiveCharacters,
                        outputs);
                    SaveNodeItem(grandchildRuleData);
                    break;
                }

                case GrandparentCountRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var grandchildRuleData = new GrandparentCountRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.Count, node.IncludePassiveCharacters,
                        outputs);
                    SaveNodeItem(grandchildRuleData);
                    break;
                }

                case SameClanRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var sameClanRuleData =
                        new SameClanRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(sameClanRuleData);
                    break;
                }

                case SameFamilyRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var sameFamilyRuleData =
                        new SameFamilyRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(sameFamilyRuleData);
                    break;
                }

                case SameCultureRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var sameCultureRuleData =
                        new SameCultureRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(sameCultureRuleData);
                    break;
                }

                case SameGenderRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var sameGenderRuleData =
                        new SameGenderRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(sameGenderRuleData);
                    break;
                }

                case GetStateRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var getStateRuleData = new GetStateRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.State, outputs);
                    SaveNodeItem(getStateRuleData);
                    break;
                }

                case IsParentRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var isParentRuleData =
                        new IsParentRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(isParentRuleData);
                    break;
                }

                case HasHeirRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var hasHeirRuleData =
                        new HasHeirRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(hasHeirRuleData);
                    break;
                }

                case IsGrandChildRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var isGrandchildRuleData =
                        new IsGrandchildRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(isGrandchildRuleData);
                    break;
                }

                case IsRelativeRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var isGrandchildRuleData =
                        new IsRelativeRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(isGrandchildRuleData);
                    break;
                }

                case IsSpouseRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var isSpouseRuleData =
                        new IsSpouseRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(isSpouseRuleData);
                    break;
                }

                case IsChildRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var isChildRuleData =
                        new IsChildRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(isChildRuleData);
                    break;
                }

                case IsSiblingRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var isSiblingRuleData =
                        new IsSiblingRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos, outputs);
                    SaveNodeItem(isSiblingRuleData);
                    break;
                }

                case IsGrandParentRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();


                    var isGrandparentRuleData =
                        new IsGrandparentRuleData(node.ID, node.Group.ID,
                            pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            outputs);
                    SaveNodeItem(isGrandparentRuleData);
                    break;
                }

                case ReturnActorRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var returnActorRuleData =
                        new ReturnActorRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.MethodName,
                            outputs);
                    SaveNodeItem(returnActorRuleData);
                    break;
                }

                case ReturnClanRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var returnClanNode =
                        new ReturnClanRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.MethodName,
                            outputs);
                    SaveNodeItem(returnClanNode);
                    break;
                }

                case ReturnFamilyRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var returnFamilyData =
                        new ReturnFamilyRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.MethodName,
                            outputs);
                    SaveNodeItem(returnFamilyData);
                    break;
                }

                case DualActorRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var dualActorData =
                        new DualActorRuleData(node.ID, node.Group.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                            node.DualType,
                            node.MethodName,
                            outputs);
                    SaveNodeItem(dualActorData);
                    break;
                }

                case GetRelationVariableRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var getRelationVariableRuleData = new GetRelationVariableRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableID, node.StringValue, node.IntegerValue, node.FloatValue, node.EnumType, node.Type,
                        outputs);
                    SaveNodeItem(getRelationVariableRuleData);
                    break;
                }

                case InvokeRuleNode node: {
                    node.Outputs ??= new List< OutputData >();
                    var outputs = node.Outputs.CloneNodeOutputs();

                    var invokeNode = new InvokeRuleData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.MethodName,
                        outputs);
                    SaveNodeItem(invokeNode);
                    break;
                }

                #endregion

                #region GHOST

                case GhostClanNode node: {
                    var ghostClanData = new GhostClanData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos);
                    SaveNodeItem(ghostClanData);
                    break;
                }

                case GhostFamilyNode node: {
                    var ghostFamilyData = new GhostFamilyData(node.ID, node.Group.ID,
                        pos.Equals(Vector2.zero) ? savedNode.Pos : pos);
                    SaveNodeItem(ghostFamilyData);
                    break;
                }

                #endregion

                #region UNGROUPED

                case PolicyNode node: {
                    var policyNodeData = new PolicyData(node.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.PolicyName,
                        node.Description, node.Type, node.PolicyIcon);
                    SaveNodeItem(policyNodeData);
                    break;
                }
                case ActorNode node: {
                    var actorData = new ActorData(node.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.ActorName, node.Age,
                        node.Gender, node.State, node.CultureID, node.Portrait, node.IsPlayer);
                    SaveNodeItem(actorData);
                    break;
                }
                case CultureNode node: {
                    var cultureData = new CultureData(node.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.CultureName,
                        node.Description, node.CultureIcon, node.NamesForMale, node.NamesForFemale);
                    SaveNodeItem(cultureData);
                    break;
                }
                case VariableNode node: {
                    var variableData = new VariableData(node.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.VariableName,
                        node.Value, node.Type, node.Variable);
                    SaveNodeItem(variableData);
                    break;
                }
                case RoleNode node: {
                    var roleData = new RoleData(node.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos, node.RoleName,
                        node.Description, node.FilterID, node.RoleSlot, node.Legacy,
                        node.TitleForMale, node.TitleForFemale, node.RoleIcon, node.Priority);
                    SaveNodeItem(roleData);
                    break;
                }
                case HeirFilterNode node: {
                    var filterData = new HeirFilterData(node.ID, pos.Equals(Vector2.zero) ? savedNode.Pos : pos,
                        node.FilterName, node.Gender, node.Age, node.Clan, node.relativeButtons.Values);
                    SaveNodeItem(filterData);
                    break;
                }

                #endregion

                default:
                    break;
            }

            EditorUtility.SetDirty(currentDatabase);
        }

        private static void SavePolicyItem(PolicyNode node) {
            var _policies = currentDatabase.policyCatalog;
            var index = _policies.FindIndex(g => g.ID == node.ID);
            var policy = new Policy(node.ID, node.PolicyName, node.Description, node.Type, node.PolicyIcon);
            if ( index != -1 ) {
                _policies[ index ] = policy;
                return;
            }

            _policies.Add(policy);
        }

        private static void SaveCultureItem(CultureNode node) {
            var _cultures = currentDatabase.culturalProfiles;
            var index = _cultures.FindIndex(g => g.ID == node.ID);
            var culture = new Culture(node.ID, node.CultureName, node.Description, node.CultureIcon,
                node.NamesForFemale,
                node.NamesForMale);
            if ( index != -1 ) {
                _cultures[ index ] = culture;
                return;
            }

            _cultures.Add(culture);
        }

        private static void SaveActorItem(ActorNode node) {
            var _actors = currentDatabase.actorRegistry;
            var index = _actors.FindIndex(g => g.ID == node.ID);
            var actor = new IEActor(node.ID, node.ActorName, node.State, node.CultureID, node.Age, node.Gender,
                node.Portrait, node.IsPlayer);
            if ( index != -1 ) {
                _actors[ index ] = actor;
                return;
            }

            _actors.Add(actor);
        }

        public static void SaveActorItem(ActorData data) {
            var _actors = currentDatabase.actorRegistry;
            var index = _actors.FindIndex(g => g.ID == data.ID);
            var actor = new IEActor(data.ID, data.ActorName, data.State, data.CultureID, data.Age, data.Gender,
                data.Portrait, data.IsPlayer);
            if ( index != -1 ) {
                _actors[ index ] = actor;
                return;
            }

            _actors.Add(actor);
        }

        private static void SaveVariableItem(string key, NVar variable) {
            var index = currentDatabase.variablePool.FindIndex(v => v.name == key);
            if ( index != -1 ) {
                currentDatabase.variablePool[ index ] = variable;
                return;
            }

            currentDatabase.variablePool.Add(variable);
        }

        private static void SaveRoleItem(RoleNode node) {
            var _roles = currentDatabase.roleDefinitions;
            var index = _roles.FindIndex(g => g.ID == node.ID);
            var role = new Role(node.ID, node.RoleName, node.TitleForMale, node.TitleForFemale, node.Description,
                node.FilterID,
                node.RoleSlot, node.Legacy,
                node.RoleIcon, node.Priority);
            if ( index != -1 ) {
                _roles[ index ] = role;
                return;
            }

            _roles.Add(role);
        }

        private static void SaveSchemeItem(SchemeGroup group) {
            var _schemes = currentDatabase.schemeLibrary;
            var index = _schemes.FindIndex(g => g.ID == group.ID);
            var scheme = new Scheme(group.ID, group.title, group.Description, group.RuleID, group.Icon,
                group.HideIfNotCompatible, group.TargetNotRequired, group.HideOnUI);
            if ( index != -1 ) {
                _schemes[ index ] = scheme;
                return;
            }

            _schemes.Add(scheme);
        }

        private static void SaveVariables(VariableNode node) {
            var variable = node.Variable.Duplicate();
            variable.id = node.ID;
            SaveVariableItem(node.VariableName, variable);
        }

        private static void SaveGroups() {
            foreach ( var group in groups ) {
                SaveGroup(group);
            }
        }

        private static void SaveGroupItem(GroupData data) {
            var _groups = currentDatabase.groupDataList;
            var index = _groups.FindIndex(g => g.ID == data.ID);
            if ( index != -1 ) {
                _groups[ index ] = data;
                return;
            }

            _groups.Add(data);
        }

        public static void RemoveGroupItem(IGroup group) {
            var _groups = currentDatabase.groupDataList;
            var data = _groups.Find(d => d.ID == group.ID);
            if ( data != null ) _groups.Remove(data);

            switch ( group ) {
                case SchemeGroup storyGroup:
                    currentDatabase.schemeLibrary.RemoveAll(p => p.SchemeName == storyGroup.title);
                    break;
            }
        }

        private static void SaveGroup(IGroup group) {
            switch ( group ) {
                case SchemeGroup schemeGroup: {
                    var nexusGroupData = new SchemeGroupData(schemeGroup.ID, schemeGroup.GetPosition().position,
                        schemeGroup.title, schemeGroup.Description, schemeGroup.RuleID, schemeGroup.HideIfNotCompatible,
                        schemeGroup.TargetNotRequired, schemeGroup.HideOnUI, schemeGroup.Icon,
                        schemeGroup.Variables);
                    SaveGroupItem(nexusGroupData);
                    SaveSchemeItem(schemeGroup);
                    break;
                }
                case ClanGroup clanGroup: {
                    var clanGroupData = new ClanGroupData(clanGroup.ID, clanGroup.GetPosition().position,
                        clanGroup.title, clanGroup.Description, clanGroup.CultureID,
                        clanGroup.Emblem, clanGroup.Policies);
                    SaveGroupItem(clanGroupData);
                    break;
                }
                case FamilyGroup familyGroup: {
                    var familyGroupData = new FamilyGroupData(familyGroup.ID, familyGroup.GetPosition().position,
                        familyGroup.title, familyGroup.Description, familyGroup.CultureID,
                        familyGroup.Emblem, familyGroup.Policies);
                    SaveGroupItem(familyGroupData);
                    break;
                }
                case RuleGroup ruleGroup: {
                    var ruleGroupData = new RuleGroupData(ruleGroup.ID,
                        ruleGroup.GetPosition().position, ruleGroup.title);
                    SaveGroupItem(ruleGroupData);
                    break;
                }
            }
        }

        private static void GetElementsFromGraphView() {
            _graphView.graphElements.ForEach(graphElement => {
                if ( graphElement is INode { Dirty: true } node ) {
                    nodes.Add(node);
                    node.ClearDirty();
                    return;
                }

                if ( graphElement is IGroup group ) groups.Add(group);
            });
        }
    }
}