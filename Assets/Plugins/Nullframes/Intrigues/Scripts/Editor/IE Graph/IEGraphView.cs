using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class IEGraphView : GraphView
    {
        #region VARIABLES

        private readonly GraphWindow _graphWindow;
        private SchemeSearchMenu schemeSearchMenu;
        private RuleSearchMenu ruleSearchMenu;
        private ClanSearchMenu clanSearchMenu;
        private FamilySearchMenu familySearchMenu;

        private MiniMap miniMap;

        private List<GraphElement> copiedElements;

        protected override bool canCutSelection => false;

        private List<IManipulator> manipulators = new();

        private GenericNodeType copiedPage;

        #endregion

        #region INIT

        public IEGraphView(GraphWindow graphWindow)
        {
            _graphWindow = graphWindow;

            SetupZoom(.1f, 1f, 0.05f, 1f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            AddMiniMap();
            AddGridBackground();

            OnElementDeleted();
            OnGroupElementAdded();
            OnGroupElementRemoved();
            OnGraphViewChanged();

            this.AddStyleSheets("Nullframes/IEGraphViewStyles.uss", "Nullframes/IENodeStyles.uss");
            MiniMapStyle();
            
            serializeGraphElements += CutCopyOperation;
            unserializeAndPaste += PasteOperation;
        }

        #endregion

        #region COPY-PASTE

        private void DuplicateData(INode original, INode duplicated)
        {
            switch (original, duplicated)
            {
                case (ActorNode o, ActorNode d):
                    d.ActorName = o.ActorName;
                    d.Gender = o.Gender;
                    d.Age = o.Age;
                    d.CultureID = o.CultureID;
                    d.Portrait = o.Portrait;
                    d.State = o.State;
                    d.IsPlayer = o.IsPlayer;
                    break;
                case (ObjectiveNode o, ObjectiveNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Objective = o.Objective;
                    break;
                case (AddPolicyNode o, AddPolicyNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.PolicyID = o.PolicyID;
                    break;
                case (AgeRuleNode o, AgeRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Age = o.Age;
                    break;
                case (AgeNode o, AgeNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Age = o.Age;
                    break;
                case (ChanceNode o, ChanceNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Chance = o.Chance;
                    break;
                case (ChanceModifierNode o, ChanceModifierNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.Positive = o.Positive;
                    d.Negative = o.Negative;
                    d.Opposite = o.Opposite;
                    d.Mode = o.Mode;
                    break;
                case (ChildCountRuleNode o, ChildCountRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (ChildCountNode o, ChildCountNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (ClanMemberNode o, ClanMemberNode d):
                    //
                    d.ActorID = o.ActorID;
                    d.RoleID = o.RoleID;
                    break;
                case (ReturnClanNode o, ReturnClanNode d):
                    d.MethodName = o.MethodName;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (ReturnClanRuleNode o, ReturnClanRuleNode d):
                    d.MethodName = o.MethodName;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (ReturnFamilyNode o, ReturnFamilyNode d):
                    d.MethodName = o.MethodName;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (ReturnFamilyRuleNode o, ReturnFamilyRuleNode d):
                    d.MethodName = o.MethodName;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (ReturnActorNode o, ReturnActorNode d):
                    d.MethodName = o.MethodName;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (ReturnActorRuleNode o, ReturnActorRuleNode d):
                    d.MethodName = o.MethodName;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;                
                case (DualActorNode o, DualActorNode d):
                    d.DualType = o.DualType;
                    d.MethodName = o.MethodName;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (DualActorRuleNode o, DualActorRuleNode d):
                    d.DualType = o.DualType;
                    d.MethodName = o.MethodName;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SetConspiratorNode o, SetConspiratorNode d):
                    d.ConspiratorID = o.ConspiratorID;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SetTargetNode o, SetTargetNode d):
                    d.TargetID = o.TargetID;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;                   
                case (SetInheritorNode o, SetInheritorNode d):
                    d.Inheritor = o.Inheritor;
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;                
                case (OnLoadNode o, OnLoadNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (RuleNode o, RuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.RuleID = o.RuleID;
                    break;
                case (CultureNode o, CultureNode d):
                    //
                    d.CultureName = GenerateCultureName();
                    d.Description = o.Description;
                    d.NamesForMale = o.NamesForMale;
                    d.NamesForFemale = o.NamesForFemale;
                    break;
                case (DialogueNode o, DialogueNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Title = o.Title;
                    d.Content = o.Content;
                    d.Break = o.Break;
                    d.Time = o.Time;
                    break;
                case (GlobalMessageNode o, GlobalMessageNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Title = o.Title;
                    d.TypeWriter = o.TypeWriter;
                    d.Background = o.Background;
                    d.Content = o.Content;
                    break;
                case (RemoveSpousesNode o, RemoveSpousesNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (FailSchemeNode o, FailSchemeNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (FamilyMemberNode o, FamilyMemberNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.ActorID = o.ActorID;
                    break;
                case (GenderRuleNode o, GenderRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (GenderNode o, GenderNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (GetActorRuleNode o, GetActorRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.ActorID = o.ActorID;
                    break;
                case (GetActorNode o, GetActorNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.ActorID = o.ActorID;
                    break;
                case (GetClanRuleNode o, GetClanRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.ClanID = o.ClanID;
                    break;
                case (GetClanNode o, GetClanNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.ClanID = o.ClanID;
                    break;                
                case (SetClanNode o, SetClanNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.ClanID = o.ClanID;
                    break;
                case (GetCultureRuleNode o, GetCultureRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.CultureID = o.CultureID;
                    break;
                case (GetCultureNode o, GetCultureNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.CultureID = o.CultureID;
                    break;
                case (GetFamilyRuleNode o, GetFamilyRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.FamilyID = o.FamilyID;
                    break;
                case (GetFamilyNode o, GetFamilyNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.FamilyID = o.FamilyID;
                    break;
                case (GetPolicyRuleNode o, GetPolicyRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.PolicyID = o.PolicyID;
                    break;
                case (GetPolicyNode o, GetPolicyNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.PolicyID = o.PolicyID;
                    break;
                case (GetRoleRuleNode o, GetRoleRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.RoleID = o.RoleID;
                    break;                
                case (KeyHandlerNode o, KeyHandlerNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.KeyCode = o.KeyCode;
                    d.KeyType = o.KeyType;
                    d.TapCount = o.TapCount;
                    d.HoldTime = o.HoldTime;
                    break;
                case (GetRoleNode o, GetRoleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.RoleID = o.RoleID;
                    break;
                case (GetStateRuleNode o, GetStateRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.State = o.State;
                    break;
                case (GetStateNode o, GetStateNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.State = o.State;
                    break;
                case (GetVariableRuleNode o, GetVariableRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Type = o.Type;
                    break;
                case (GetClanVariableRuleNode o, GetClanVariableRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Type = o.Type;
                    break;
                case (GetFamilyVariableRuleNode o, GetFamilyVariableRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Type = o.Type;
                    break;
                case (GetVariableNode o, GetVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Type = o.Type;
                    break;
                case (GetFamilyVariableNode o, GetFamilyVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Type = o.Type;
                    break;
                case (GetClanVariableNode o, GetClanVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Type = o.Type;
                    break;
                case (GetRelationVariableNode o, GetRelationVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Type = o.Type;
                    break;
                case (GetRelationVariableRuleNode o, GetRelationVariableRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Type = o.Type;
                    break;
                case (WaitUntilNode o, WaitUntilNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Type = o.Type;
                    break;
                case (GetTableVariableNode o, GetTableVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.Type = o.Type;
                    break;
                case (GhostClanNode _, GhostClanNode _):
                    //

                    break;
                case (GhostFamilyNode _, GhostFamilyNode _):
                    //

                    break;
                case (GrandchildCountRuleNode o, GrandchildCountRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (GrandchildCountNode o, GrandchildCountNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (GrandparentCountRuleNode o, GrandparentCountRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (GrandparentCountNode o, GrandparentCountNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (InvokeNode o, InvokeNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.MethodName = o.MethodName;
                    break;
                case (InvokeRuleNode o, InvokeRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.MethodName = o.MethodName;
                    break;
                case (SignalNode o, SignalNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Signal = o.Signal;
                    break;
                case (IsAIRuleNode o, IsAIRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsAINode o, IsAINode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsChildRuleNode o, IsChildRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsChildNode o, IsChildNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsGrandChildRuleNode o, IsGrandChildRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsGrandChildNode o, IsGrandChildNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsGrandParentRuleNode o, IsGrandParentRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsGrandParentNode o, IsGrandParentNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsParentRuleNode o, IsParentRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsParentNode o, IsParentNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (HasHeirNode o, HasHeirNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (HasHeirRuleNode o, HasHeirRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsRelativeRuleNode o, IsRelativeRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsRelativeNode o, IsRelativeNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsSiblingRuleNode o, IsSiblingRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsSiblingNode o, IsSiblingNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsSpouseRuleNode o, IsSpouseRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (IsSpouseNode o, IsSpouseNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (LogNode o, LogNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Message = o.Message;
                    break;
                case (LogRuleNode o, LogRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Message = o.Message;
                    break;
                case (AddSpouseNode o, AddSpouseNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.JoinSpouseFamily = o.JoinSpouseFamily;
                    break;
                case (CommentLeftToRightNode o, CommentLeftToRightNode d):
                    d.Note = o.Note;
                    break;
                case (CommentRightToLeftNode o, CommentRightToLeftNode d):
                    d.Note = o.Note;
                    break;
                case (CommentRuleNode o, CommentRuleNode d):
                    d.Note = o.Note;
                    break;
                case (ParentCountRuleNode o, ParentCountRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (ParentCountNode o, ParentCountNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (PolicyNode o, PolicyNode d):
                    d.PolicyName = GeneratePolicyName();
                    d.Description = o.Description;
                    d.PolicyIcon = o.PolicyIcon;
                    d.Type = o.Type;
                    break;
                case (RandomNode o, RandomNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (RemovePolicyNode o, RemovePolicyNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.PolicyID = o.PolicyID;
                    break;
                case (RepeaterNode o, RepeaterNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.RepetitionCount = o.RepetitionCount;
                    break;
                case (BreakRepeaterNode o, BreakRepeaterNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (BreakSequencerNode o, BreakSequencerNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (ContinueNode o, ContinueNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (BreakNode o, BreakNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (PauseNode o, PauseNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (ResumeNode o, ResumeNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (NewFlowNode o, NewFlowNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;                
                case (BackgroundWorkerNode o, BackgroundWorkerNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (ChoiceDataNode o, ChoiceDataNode d):
                    d.Text = o.Text;
                    d.Text2 = o.Text2;
                    break;
                case (JumpNode o, JumpNode d):
                    d.LinkID = o.LinkID;
                    break;
                case (LinkedNode o, LinkedNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Name = o.Name;
                    break;
                case (RoleNode o, RoleNode d):
                    d.RoleName = GenerateRoleName();
                    d.Description = o.Description;
                    d.RoleSlot = o.RoleSlot;
                    d.Legacy = o.Legacy;
                    d.TitleForMale = o.TitleForMale;
                    d.TitleForFemale = o.TitleForFemale;
                    d.RoleIcon = o.RoleIcon;
                    d.Priority = o.Priority;
                    break;                
                case (HeirFilterNode o, HeirFilterNode d):
                    d.FilterName = GenerateFilterName();
                    d.Gender = o.Gender;
                    d.Age = o.Age;
                    d.Relatives = new List<int>(o.relativeButtons.Values);
                    break;
                case (SameClanRuleNode o, SameClanRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SameClanNode o, SameClanNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SameCultureRuleNode o, SameCultureRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SameCultureNode o, SameCultureNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SameFamilyRuleNode o, SameFamilyRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SameFamilyNode o, SameFamilyNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SameGenderRuleNode o, SameGenderRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SameGenderNode o, SameGenderNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SequencerNode o, SequencerNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SkipSequencerNode o, SkipSequencerNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Index = o.Index;
                    break;
                case (SetCultureNode o, SetCultureNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.CultureID = o.CultureID;
                    break;
                case (SetRoleNode o, SetRoleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.RoleID = o.RoleID;
                    break;
                case (SetStateNode o, SetStateNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.State = o.State;
                    break;
                case (SchemeStateNode o, SchemeStateNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.State = o.State;
                    break;
                case (SetVariableNode o, SetVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.ObjectValue = o.ObjectValue;
                    d.Operation = o.Operation;
                    d.VariableType = o.VariableType;
                    break;
                case (SetFamilyVariableNode o, SetFamilyVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.ObjectValue = o.ObjectValue;
                    d.Operation = o.Operation;
                    d.VariableType = o.VariableType;
                    break;
                case (SetClanVariableNode o, SetClanVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.ObjectValue = o.ObjectValue;
                    d.Operation = o.Operation;
                    d.VariableType = o.VariableType;
                    break;
                case (SetRelationVariableNode o, SetRelationVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.ObjectValue = o.ObjectValue;
                    d.Operation = o.Operation;
                    d.VariableType = o.VariableType;
                    break;
                case (SetTableVariableNode o, SetTableVariableNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    d.StringValue = o.StringValue;
                    d.IntegerValue = o.IntegerValue;
                    d.FloatValue = o.FloatValue;
                    d.Operation = o.Operation;
                    d.VariableType = o.VariableType;
                    break;                
                case (SetActorNode o, SetActorNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.VariableID = o.VariableID;
                    break;
                case (SiblingCountRuleNode o, SiblingCountRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (SiblingCountNode o, SiblingCountNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (SoundNode o, SoundNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Clip = o.Clip;
                    d.Volume = o.Volume;
                    d.Pitch = o.Pitch;
                    d.Priority = o.Priority;
                    d.WaitEnd = o.WaitEnd;
                    d.AudioMixerGroup = o.AudioMixerGroup;
                    break;
                case (SoundClassNode o, SoundClassNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Clip = o.Clip;
                    d.Volume = o.Volume;
                    d.FadeOut = o.FadeOut;
                    d.Pitch = o.Pitch;
                    d.Priority = o.Priority;
                    d.Loop = o.Loop;
                    d.StopWhenClosed = o.StopWhenClosed;
                    d.AudioMixerGroup = o.AudioMixerGroup;
                    break;
                case (VoiceClassNode o, VoiceClassNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Clip = o.Clip;
                    d.Volume = o.Volume;
                    d.Pitch = o.Pitch;
                    d.Priority = o.Priority;
                    d.Sync = o.Sync;
                    d.AudioMixerGroup = o.AudioMixerGroup;
                    break;
                case (SpouseCountRuleNode o, SpouseCountRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (SpouseCountNode o, SpouseCountNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.IncludePassiveCharacters = o.IncludePassiveCharacters;
                    d.Count = o.Count;
                    break;
                case (StartRuleNode o, StartRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (StartNode o, StartNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (EndNode o, EndNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SuccessRuleNode o, SuccessRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SuccessSchemeNode o, SuccessSchemeNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    break;
                case (SchemeIsActiveNode o, SchemeIsActiveNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.SchemeID = o.SchemeID;
                    d.VerifyType = o.VerifyType;
                    break;
                case (SchemeIsActiveRuleNode o, SchemeIsActiveRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.SchemeID = o.SchemeID;
                    d.VerifyType = o.VerifyType;
                    break;
                case (ErrorRuleNode o, ErrorRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.ErrorName = GenerateErrorName();
                    d.Error = o.Error;
                    break;                
                case (WarningRuleNode o, WarningRuleNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.WarningName = GenerateWarningName();
                    d.WarningName = o.Warning;
                    break;
                case (TriggerNode o, TriggerNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.TriggerName = o.TriggerName;
                    d.Value = o.Value;
                    break;
                case (VariableNode o, VariableNode d):
                    d.VariableName = GenerateVariableName();
                    d.Value = o.Value;
                    d.Type = o.Type;
                    d.Variable = o.Variable.Duplicate();
                    break;
                case (WaitNode o, WaitNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Delay = o.Delay;
                    break;
                case (WaitRandomNode o, WaitRandomNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.Min = o.Min;
                    d.Max = o.Max;
                    break;
                case (WaitTriggerNode o, WaitTriggerNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.TriggerName = o.TriggerName;
                    d.Timeout = o.Timeout;
                    break;
                case (ValidatorNode o, ValidatorNode d):
                    d.Outputs = o.Outputs.CloneNodeOutputs();
                    d.RuleID = o.RuleID;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(duplicated));
            }
        }

        private void PasteOperation(string operationname, string data)
        {
            if (copiedElements == null) return;

            if (copiedElements.Count < 1 || copiedPage != _graphWindow.CurrentPage) return;

            var copiedGroup = copiedElements.FirstOrDefault(x => x is IGroup) as IGroup;
            var activeGroup = graphElements.FirstOrDefault(x => x is IGroup) as IGroup;

            ClearSelection();

            IGroup duplicatedGroup = null;
            if (copiedGroup != null)
            {
                duplicatedGroup = DuplicateGroup(copiedGroup);
                duplicatedGroup.selected = true;
            }

            var createdNodes = new List<INode>();

            foreach (var graphElement in copiedElements.Where(x => x is INode))
            {
                var node = (INode)graphElement;

                var createdNode = CreateNodeScript(node.GetType());

                if (node.Group != null)
                {
                    if (duplicatedGroup == null)
                    {
                        if (activeGroup != null)
                        {
                            activeGroup.AddElement(createdNode);
                            createdNode.Group = activeGroup;
                        }
                    }
                    else
                    {
                        duplicatedGroup.AddElement(createdNode);
                        createdNode.Group = duplicatedGroup;
                    }
                }

                DuplicateData(node, createdNode);

                InstantiateNode(createdNode, node.GetPosition().position, false, node.ID);

                //Re-position

                // InstantiateNode(createdNode,
                //     copiedElements.Contains(node.Group) ? node.GetPosition().position : pos.position, false, node.ID);

                // Rect pos;
                // if (selectionGroup == null)
                // {
                //     pos = new Rect(node.GetPosition().x + node.GetPosition().width, node.GetPosition().y,
                //         node.GetPosition().width, node.GetPosition().height);
                // }
                // else
                // {
                //     var lastElement = selectionGroup.containedElements.Last();
                //     pos = new Rect(lastElement.GetPosition().x + lastElement.GetPosition().width,
                //         lastElement.GetPosition().y,
                //         lastElement.GetPosition().width, lastElement.GetPosition().height);
                // }

                AddToSelection(createdNode);

                createdNode.SetDirty();

                createdNodes.Add(createdNode);
            }

            var loadedNodes = new Dictionary<string, GeneratedNode>();

            foreach (var graphElement in graphElements.Where(n => n is INode))
            {
                var node = (INode)graphElement;
                // if (node.ElementType != GenericNodeType.Nexus) continue;
                if (loadedNodes.ContainsKey(node.ID)) loadedNodes.Remove(node.ID);

                loadedNodes.Add(node.ID, new GeneratedNode(node));
            }

            var regeneratedNodes = new List<INode>();

            foreach (var node in createdNodes)
            {
                if (!node.IsDrawed) node.Draw();
                foreach (var port in node.GetElements<Port>())
                {
                    if (port.direction != Direction.Output) continue;
                    foreach (var dataCollection in ((OutputData)port.userData).DataCollection)
                        if (loadedNodes.ContainsKey(dataCollection.NextID))
                        {
                            var nextNode = loadedNodes[dataCollection.NextID];
                            if (!nextNode.node.IsDrawed) nextNode.node.Draw();
                            if (node.Group != nextNode.node.Group)
                            {
                                dataCollection.NextID = string.Empty;
                                dataCollection.NextName = string.Empty;
                                continue;
                            }

                            if (createdNodes.Contains(nextNode.node))
                            {
                                if (!nextNode.generated)
                                {
                                    nextNode.node.ReGenerateID();
                                    regeneratedNodes.Add(nextNode.node);
                                }

                                nextNode.generated = true;
                                dataCollection.NextID = nextNode.node.ID;
                            }

                            var nextNodeInputPort = nextNode.node.GetElements<Port>()
                                .First(p => p.portName == dataCollection.NextName);
                            var edge = port.ConnectTo(nextNodeInputPort);
                            AddElement(edge);
                        }
                }

                node.RefreshPorts();
            }

            ActorNode actorNode = null;

            foreach (var createdNode in createdNodes)
            {
                if (regeneratedNodes.Contains(createdNode)) continue;
                createdNode.ReGenerateID();

                if (createdNode is ActorNode a) actorNode = a;
            }

            EditorRoutine.StartRoutine(0.01f, () =>
            {
                GraphSaveUtility.SaveCurrent();

                if (actorNode != null)
                {
                    GraphWindow.instance.SetActorKey(actorNode.ID);
                    GraphSaveUtility.LoadCurrent(this);
                    GraphWindow.instance.GoView();
                }

                if (duplicatedGroup == null) return;
                switch (duplicatedGroup)
                {
                    case SchemeGroup:
                        GraphWindow.instance.SetStoryKey(duplicatedGroup.ID);
                        GraphSaveUtility.LoadCurrent(this);
                        GraphWindow.instance.GoView();
                        break;
                    case RuleGroup:
                        GraphWindow.instance.SetRuleKey(duplicatedGroup.ID);
                        GraphSaveUtility.LoadCurrent(this);
                        GraphWindow.instance.GoView();
                        break;
                }
            });
        }

        private string CutCopyOperation(IEnumerable<GraphElement> elements)
        {
            copiedElements = new List<GraphElement>();
            foreach (var element in elements.Where(x => x is INode or IGroup)) copiedElements.Add(element);

            copiedPage = _graphWindow.CurrentPage;
            return "null";
        }

        #endregion

        #region MINIMAP

        private void AddMiniMap()
        {
            miniMap = new MiniMap()
            {
                anchored = true
            };
            miniMap.SetPosition(new Rect(15, 50, 200, 180));
            Add(miniMap);

            miniMap.visible = false;
        }

        #endregion

        #region SEARCH

        public void AddSearchWindow()
        {
            if (schemeSearchMenu == null)
            {
                schemeSearchMenu = ScriptableObject.CreateInstance<SchemeSearchMenu>();
                schemeSearchMenu.Init(this);
            }

            if (ruleSearchMenu == null)
            {
                ruleSearchMenu = ScriptableObject.CreateInstance<RuleSearchMenu>();
                ruleSearchMenu.Init(this);
            }

            if (clanSearchMenu == null)
            {
                clanSearchMenu = ScriptableObject.CreateInstance<ClanSearchMenu>();
                clanSearchMenu.Init(this);
            }

            if (familySearchMenu == null)
            {
                familySearchMenu = ScriptableObject.CreateInstance<FamilySearchMenu>();
                familySearchMenu.Init(this);
            }

            nodeCreationRequest = context =>
            {
                var page = GraphWindow.instance.CurrentPage;
                if (page == GenericNodeType.Scheme)
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), schemeSearchMenu);
                if (page == GenericNodeType.Rule)
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), ruleSearchMenu);
                if (page == GenericNodeType.Clan)
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), clanSearchMenu);
                if (page == GenericNodeType.Family)
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), familySearchMenu);
            };
        }

        public void RemoveSearchWindow()
        {
            nodeCreationRequest = null;
        }

        #endregion

        #region Overrided Methods

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(p =>
            {
                var startNode = (INode)startPort.node;
                var targetNode = (INode)p.node;

                if (startPort.portName != "[Actor]" && p.portName == "[Actor]" || startPort.portName == "[Actor]" && p.portName != "[Actor]")
                    return;
                
                if (startPort.portName != "[Dual]" && p.portName == "[Dual]" || startPort.portName == "[Dual]" && p.portName != "[Dual]")
                    return;
                
                if (startPort.portName != "[Clan]" && p.portName == "[Clan]" || startPort.portName == "[Clan]" && p.portName != "[Clan]")
                    return;
                
                if (startPort.portName != "[Family]" && p.portName == "[Family]" || startPort.portName == "[Family]" && p.portName != "[Family]")
                    return;
                
                if (startPort.portName != "[Class]" && p.portName == "[Class]" || startPort.portName == "[Class]" && p.portName != "[Class]")
                    return;

                // var startData = (OutputData)startPort.userData;
                // var targetData = (OutputData)p.userData;
                if (startNode?.Group != targetNode?.Group) return;
                switch (startNode)
                {
                    case SuccessSchemeNode when targetNode is FailSchemeNode:
                    case FailSchemeNode when targetNode is SuccessSchemeNode:
                        return;
                }

                switch (startPort.portName)
                {
                    case "Children" when p.portName != "Parent":
                    case "Spouse" when p.portName != "Spouse":
                        return;
                }

                if (startPort.portName == "Display" && p.portName != "Display") return;
                if (startPort.portName != "Display" && p.portName == "Display") return;

                //Chance-ChanceModifier
                if (startPort.portType == typeof(double) && p.portType != typeof(double))
                    return;

                //Chance-ChanceModifier
                if (p.portType == typeof(double) && startPort.portType != typeof(double))
                    return;

                if (startPort.direction == Direction.Output && startPort.portName == "Spouse" &&
                    startPort.direction == Direction.Input && p.portName != "Spouse")
                    return;
                if (startNode is FamilyMemberNode startFamilyNode && targetNode is FamilyMemberNode targetFamilyNode)
                {
                    if (startPort.portName == "Children" && p.portName == "Parent")
                    {
                        if (startFamilyNode.actor?.Age < targetFamilyNode.actor?.Age &&
                            startFamilyNode.actor.CurrentState == Actor.IState.Active) return;
                        if (p.connections.Count() > 1) return;

                        var spouseOutPortData = (OutputData)startFamilyNode.spouseContainer.GetChild<Port>().userData;
                        if (spouseOutPortData.DataCollection.Any(data => data.NextID == targetNode?.ID)) return;

                        var childs = (OutputData)targetFamilyNode.childContainer.GetChild<Port>().userData;
                        if (childs.DataCollection.Any(data => data.NextID == startNode?.ID)) return;
                    }

                    if (startPort.portName == "Parent" && p.portName == "Children")
                    {
                        if (targetFamilyNode.actor?.Age < startFamilyNode.actor?.Age &&
                            startFamilyNode.actor.CurrentState == Actor.IState.Active) return;
                        if (startPort.connections.Count() > 1) return;
                        var characterNode = startFamilyNode;
                        var targetCharacterNode = targetFamilyNode;

                        var spouseOutPortData = (OutputData)characterNode.childContainer.GetChild<Port>().userData;
                        if (spouseOutPortData.DataCollection.Any(data => data.NextID == targetNode?.ID)) return;

                        var childs = (OutputData)targetCharacterNode.spouseContainer.GetChild<Port>().userData;
                        if (childs.DataCollection.Any(data => data.NextID == startNode?.ID)) return;
                    }

                    if (startPort.portName == "Spouse" && p.portName == "Spouse")
                    {
                        var characterNode = startFamilyNode;
                        var targetCharacterNode = targetFamilyNode;
                        var isOutput = startPort.direction == Direction.Output;

                        if (characterNode.actor.Age < STATIC.MARRIAGE_AGE ||
                            targetCharacterNode.actor.Age < STATIC.MARRIAGE_AGE) return;

                        var anySpouseInput =
                            characterNode.spouseInputContainer.GetChild<Port>().connections.Any();

                        // var anySpouseOutput =
                        //     characterNode.spouseContainer.GetChild<Port>().connections.Any();

                        var anyTargetSpouseInput =
                            targetCharacterNode.spouseInputContainer.GetChild<Port>().connections.Any();

                        var anyTargetSpouseOutput =
                            targetCharacterNode.spouseContainer.GetChild<Port>().connections.Any();

                        if (isOutput)
                        {
                            if (anySpouseInput) return;
                            if (anyTargetSpouseInput) return;
                            if (anyTargetSpouseOutput) return;
                        }
                        else
                        {
                            if (anyTargetSpouseInput) return;
                            if (anySpouseInput) return;
                            if (anyTargetSpouseOutput) return;
                        }
                    }
                }

                if (startPort == p)
                    return;

                if (p.connections.Any(edge => startPort.connections.Contains(edge)))
                    return;

                if (startPort.node == p.node)
                    return;

                if (startPort.direction == p.direction)
                    return;

                compatiblePorts.Add(p);
            });
            return compatiblePorts;
        }

        #endregion

        #region Manipulators

        public void AddManipulators(GenericNodeType page)
        {
            ClearManipulators();
            if (page == GenericNodeType.Scheme)
                manipulators = new List<IManipulator>()
                {
                    CreateGroupContextualMenu(),
                    CreateLabelContextualMenu("                    "),
                    CreateNodeContextualMenu<CommentLeftToRightNode>("Comment(LR)"),
                    CreateNodeContextualMenu<CommentRightToLeftNode>("Comment(RL)"),
                    CreateNodeContextualMenu<DialogueNode>("Dialogue"),
                    CreateNodeContextualMenu<WaitNode>("Wait"),
                    CreateNodeContextualMenu<WaitRandomNode>("Wait(Random)"),
                    CreateNodeContextualMenu<SequencerNode>("Sequencer"),
                    CreateNodeContextualMenu<RepeaterNode>("Repeater"),
                    CreateNodeContextualMenu<SoundNode>("Play Sound"),
                    CreateNodeContextualMenu<JumpNode>("Go To"),
                    CreateNodeContextualMenu<LinkedNode>("Link"),
                };

            if (page == GenericNodeType.Clan)
                manipulators = new List<IManipulator>()
                {
                    CreateClanContextualMenu(),
                    CreateLabelContextualMenu("                    "),
                    CreateNodeContextualMenu<RoleNode>("New Role"),
                    CreateNodeContextualMenu<HeirFilterNode>("Heir Filter")
                };
            
            if (page == GenericNodeType.Policy)
                manipulators = new List<IManipulator>()
                {
                    CreateNodeContextualMenu<PolicyNode>("New Policy"),
                };

            if (page == GenericNodeType.Family)
                manipulators = new List<IManipulator>()
                {
                    CreateFamilyContextualMenu(),
                };

            if (page == GenericNodeType.Actor)
                manipulators = new List<IManipulator>()
                {
                    CreateNodeContextualMenu<ActorNode>("New Actor")
                };

            if (page == GenericNodeType.Culture)
                manipulators = new List<IManipulator>()
                {
                    CreateNodeContextualMenu<CultureNode>("New Culture")
                };

            if (page == GenericNodeType.Variable)
                manipulators = new List<IManipulator>()
                {
                    CreateNodeContextualMenu<VariableNode>("New Variable")
                };

            if (page == GenericNodeType.Rule)
                manipulators = new List<IManipulator>()
                {
                    CreateRuleContextualMenu(),
                    CreateLabelContextualMenu("                    "),
                    CreateNodeContextualMenu<CommentRuleNode>("Comment"),
                    CreateNodeContextualMenu<SuccessRuleNode>("Success"),
                    CreateNodeContextualMenu<ErrorRuleNode>("Error"),
                    CreateNodeContextualMenu<WarningRuleNode>("Warning"),
                    CreateLabelContextualMenu("                         "),
                    CreateNodeContextualMenu<GetVariableRuleNode>("Get Variable"),
                    CreateNodeContextualMenu<GetActorRuleNode>("Get Actor"),
                    CreateNodeContextualMenu<GetClanRuleNode>("Get Clan"),
                    CreateNodeContextualMenu<GetFamilyRuleNode>("Get Family"),
                    CreateNodeContextualMenu<GetRoleRuleNode>("Get Role"),
                    CreateNodeContextualMenu<GetCultureRuleNode>("Get Culture"),
                    CreateLabelContextualMenu("                  "),
                    CreateNodeContextualMenu<GetPolicyRuleNode>("Has Policy"),
                    CreateNodeContextualMenu<SameClanRuleNode>("Same Clan"),
                    CreateNodeContextualMenu<SameFamilyRuleNode>("Same Family"),
                    CreateNodeContextualMenu<SameCultureRuleNode>("Same Culture"),
                    CreateNodeContextualMenu<SameGenderRuleNode>("Same Gender"),
                    CreateNodeContextualMenu<AgeRuleNode>("Age"),
                    CreateNodeContextualMenu<GenderRuleNode>("Gender"),
                    CreateNodeContextualMenu<IsAIRuleNode>("Is AI"),
                    CreateNodeContextualMenu<GetStateRuleNode>("Get State"),
                    CreateLabelContextualMenu("                      "),
                    CreateNodeContextualMenu<IsRelativeRuleNode>("Is Relative"),
                    CreateNodeContextualMenu<IsParentRuleNode>("Is Parent"),
                    CreateNodeContextualMenu<IsGrandParentRuleNode>("Is Grandparent"),
                    CreateNodeContextualMenu<IsGrandChildRuleNode>("Is Grandchildren"),
                    CreateNodeContextualMenu<IsSpouseRuleNode>("Is Spouse"),
                    CreateNodeContextualMenu<IsChildRuleNode>("Is Child"),
                    CreateNodeContextualMenu<IsSiblingRuleNode>("Is Sibling"),
                    CreateNodeContextualMenu<HasHeirRuleNode>("Has Heir"),
                    CreateLabelContextualMenu("                     "),
                    CreateNodeContextualMenu<ParentCountRuleNode>("Parent Count"),
                    CreateNodeContextualMenu<GrandparentCountRuleNode>("Grandparent Count"),
                    CreateNodeContextualMenu<GrandchildCountRuleNode>("Grandchildren Count"),
                    CreateNodeContextualMenu<ChildCountRuleNode>("Children Count"),
                    CreateNodeContextualMenu<SpouseCountRuleNode>("Spouse Count"),
                    CreateNodeContextualMenu<SiblingCountRuleNode>("Sibling Count"),
                };

            foreach (var manipulator in manipulators) this.AddManipulator(manipulator);
        }

        private void ClearManipulators()
        {
            foreach (var manipulator in manipulators) this.RemoveManipulator(manipulator);

            manipulators = new List<IManipulator>();
        }

        private IManipulator CreateLabelContextualMenu(string title)
        {
            var contextualMenuManipulator = new ContextualMenuManipulator(evnt =>
            {
                evnt.menu.AppendAction(title, _ => { }, DropdownMenuAction.Status.Disabled);
            });
            return contextualMenuManipulator;
        }

        private IManipulator CreateNodeContextualMenu<T>(string title) where T : INode
        {
            var contextualMenuManipulator = new ContextualMenuManipulator(evnt =>
                evnt.menu.AppendAction(title, action =>
                {
                    switch (_graphWindow.CurrentPage)
                    {
                        case GenericNodeType.Scheme when !graphElements.ToList()
                            .Exists(e => e is SchemeGroup):
                            NDebug.Log("You need a Scheme Group to create a Node.", NLogType.Error);
                            return;
                        // case GenericNodeType.Clan when !graphElements.ToList()
                        //     .Exists(e => e is ClanGroup):
                        //     NDebug.Log("You need a Clan Group to create a Node.", NLogType.Error);
                        //     return;
                        case GenericNodeType.Family when !graphElements.ToList()
                            .Exists(e => e is FamilyGroup):
                            NDebug.Log("You need a Family Group to create a Node.", NLogType.Error);
                            return;
                        case GenericNodeType.Rule when !graphElements.ToList()
                            .Exists(e => e is RuleGroup):
                            NDebug.Log("You need a Rule Group to create a Node.", NLogType.Error);
                            return;
                    }

                    var mousePosition = GetLocalMousePosition(action.eventInfo.localMousePosition);
                    var node = CreateNode<T>(mousePosition, true, true);
                    AddElement(node);
                }));
            return contextualMenuManipulator;
        }

        private IManipulator CreateGroupContextualMenu()
        {
            var menuManipulator = new ContextualMenuManipulator(evnt =>
                evnt.menu.AppendAction("Create Story", action =>
                {
                    var position = GetLocalMousePosition(action.eventInfo.localMousePosition);
                    CreateSchemeGroup(position);
                }));
            return menuManipulator;
        }

        private IManipulator CreateClanContextualMenu()
        {
            var menuManipulator = new ContextualMenuManipulator(evnt =>
                evnt.menu.AppendAction("New Clan", action =>
                {
                    var position = GetLocalMousePosition(action.eventInfo.localMousePosition);
                    CreateClanGroup(position);
                }));
            return menuManipulator;
        }

        private IManipulator CreateRuleContextualMenu()
        {
            var menuManipulator = new ContextualMenuManipulator(evnt =>
                evnt.menu.AppendAction("Create Rule", action =>
                {
                    var position = GetLocalMousePosition(action.eventInfo.localMousePosition);
                    CreateRuleGroup(position);
                }));
            return menuManipulator;
        }

        private IManipulator CreateFamilyContextualMenu()
        {
            var menuManipulator = new ContextualMenuManipulator(evnt =>
                evnt.menu.AppendAction("Create Family Tree", action =>
                {
                    var position = GetLocalMousePosition(action.eventInfo.localMousePosition);
                    CreateFamilyGroup(position);
                }));
            return menuManipulator;
        }

        public void CreateSchemeGroup(Vector2 position)
        {
            var group = CreateSchemeGroup(GenerateGroupName("Intrigue ", GenericNodeType.Scheme), position,
                "Description", string.Empty,
                new List<NexusVariable>(), null);
            group.OnCreated();
            if (!group.containedElements.Cast<INode>().Any(x => x is StartNode))
            {
                var startNode = CreateNodeScript<StartNode>();
                startNode.Group = group;
                InstantiateNode(startNode, position);
            }

            if (!group.containedElements.Cast<INode>().Any(x => x is EndNode))
            {
                var endNode = CreateNodeScript<EndNode>();
                endNode.Group = group;
                InstantiateNode(endNode, new Vector2(position.x, position.y + 100f));
            }

            EditorRoutine.StartRoutine(0.01f, () =>
            {
                GraphSaveUtility.SaveCurrent();
                GraphWindow.instance.SetStoryKey(group.ID);
                GraphSaveUtility.LoadCurrent(this);
                GraphWindow.instance.GoView();
            });
        }

        public void CreateClanGroup(Vector2 position)
        {
            var group = CreateClanGroup(GenerateGroupName("Clan ", GenericNodeType.Clan), position, "Description", null,
                string.Empty, new List<string>());
            group.OnCreated();
            if (!group.containedElements.Cast<INode>().Any(x => x is GhostClanNode))
            {
                var ghostClan = CreateNodeScript<GhostClanNode>();
                ghostClan.Group = group;
                InstantiateNode(ghostClan, position);
                group.AddElement(ghostClan);
            }

            EditorRoutine.StartRoutine(0.01f, () =>
            {
                GraphSaveUtility.SaveCurrent();
                GraphWindow.instance.SetClanKey(group.ID);
                GraphSaveUtility.LoadCurrent(this);
                GraphWindow.instance.GoView();
            });
        }

        public void CreateRuleGroup(Vector2 position)
        {
            var group = CreateRuleGroup(GenerateGroupName("Rule ", GenericNodeType.Rule), position);
            group.OnCreated();
            if (!group.containedElements.Cast<INode>().Any(x => x is StartRuleNode))
            {
                var startRule = CreateNodeScript<StartRuleNode>();
                startRule.Group = group;
                InstantiateNode(startRule, position);
                group.AddElement(startRule);
            }

            EditorRoutine.StartRoutine(0.01f, () =>
            {
                GraphSaveUtility.SaveCurrent();
                GraphWindow.instance.SetRuleKey(group.ID);
                GraphSaveUtility.LoadCurrent(this);
                GraphWindow.instance.GoView();
            });
        }

        public void CreateFamilyGroup(Vector2 position)
        {
            var group = CreateFamilyGroup(GenerateGroupName("Family ", GenericNodeType.Family), position, "Description",
                null, string.Empty, new List<string>());
            group.OnCreated();
            if (!group.containedElements.Cast<INode>().Any(x => x is GhostFamilyNode))
            {
                var ghostFamily = CreateNodeScript<GhostFamilyNode>();
                ghostFamily.Group = group;
                InstantiateNode(ghostFamily, position);
                group.AddElement(ghostFamily);
            }

            EditorRoutine.StartRoutine(0.01f, () =>
            {
                GraphSaveUtility.SaveCurrent();
                GraphWindow.instance.SetFamilyKey(group.ID);
                GraphSaveUtility.LoadCurrent(this);
                GraphWindow.instance.GoView();
            });
        }

        #endregion

        #region Element Creation

        public IGroup CreateSchemeGroup(string title, Vector2 position, string description, string ruleId,
            List<NexusVariable> variables, Sprite icon,
            bool hideIfNotCompatible = true, bool targetNotRequired = false, bool hideOnUI = false)
        {
            var schemeGroup = Activator.CreateInstance<SchemeGroup>();
            schemeGroup.Init(title, position, description, ruleId, variables, hideIfNotCompatible, targetNotRequired, hideOnUI, icon, this);
            schemeGroup.Draw();
            AddElement(schemeGroup);
            return schemeGroup;
        }

        public IGroup CreateClanGroup(string title, Vector2 position, string description, Sprite emblem,
            string cultureId, List<string> policies)
        {
            var clanGroup = Activator.CreateInstance<ClanGroup>();
            clanGroup.Init(title, position, description, cultureId, policies, emblem, this);
            clanGroup.Draw();
            AddElement(clanGroup);
            return clanGroup;
        }

        public IGroup CreateRuleGroup(string title, Vector2 position)
        {
            var ruleGroup = Activator.CreateInstance<RuleGroup>();
            ruleGroup.Init(title, position, this);
            ruleGroup.Draw();
            AddElement(ruleGroup);
            return ruleGroup;
        }

        public IGroup CreateFamilyGroup(string title, Vector2 position, string description, Sprite emblem,
            string cultureId, List<string> policies)
        {
            var familyGroup = Activator.CreateInstance<FamilyGroup>();
            familyGroup.Init(title, position, description, cultureId, policies, emblem, this);
            familyGroup.Draw();
            AddElement(familyGroup);
            return familyGroup;
        }

        private IGroup DuplicateGroup(IGroup originalGroup)
        {
            switch (originalGroup)
            {
                case SchemeGroup schemeGroup:
                {
                    var _schemeGroup = Activator.CreateInstance<SchemeGroup>();
                    _schemeGroup.Init(GenerateGroupName("Intrigue ", GenericNodeType.Scheme),
                        schemeGroup.GetPosition().position,
                        schemeGroup.Description, schemeGroup.RuleID, schemeGroup.Variables, schemeGroup.HideIfNotCompatible, schemeGroup.TargetNotRequired, schemeGroup.HideOnUI,
                        schemeGroup.Icon, this);
                    _schemeGroup.Draw();
                    AddElement(_schemeGroup);
                    return _schemeGroup;
                }
                case ClanGroup clanGroup:
                {
                    var _clanGroup = Activator.CreateInstance<ClanGroup>();
                    _clanGroup.Init(GenerateGroupName("Clan ", GenericNodeType.Clan), clanGroup.GetPosition().position,
                        clanGroup.Description, clanGroup.CultureID, clanGroup.Policies, clanGroup.Emblem, this);
                    _clanGroup.Draw();
                    AddElement(_clanGroup);
                    return _clanGroup;
                }
                case FamilyGroup familyGroup:
                {
                    var _familyGroup = Activator.CreateInstance<FamilyGroup>();
                    _familyGroup.Init(GenerateGroupName("Family ", GenericNodeType.Family),
                        familyGroup.GetPosition().position, familyGroup.Description, familyGroup.CultureID, familyGroup.Policies,
                        familyGroup.Emblem, this);
                    _familyGroup.Draw();
                    AddElement(_familyGroup);
                    return _familyGroup;
                }
                case RuleGroup ruleGroup:
                {
                    var _ruleGroup = Activator.CreateInstance<RuleGroup>();
                    _ruleGroup.Init(GenerateGroupName("Rule ", GenericNodeType.Rule),
                        ruleGroup.GetPosition().position, this);
                    _ruleGroup.Draw();
                    AddElement(_ruleGroup);
                    return _ruleGroup;
                }
                default:
                    return null;
            }
        }

        private T CreateNodeScript<T>() where T : INode
        {
            return Activator.CreateInstance<T>();
        }

        private INode CreateNodeScript(Type type)
        {
            return Activator.CreateInstance(type) as INode;
        }

        public T CreateNode<T>(Vector2 position, bool shouldDraw = true, bool manualCreation = false)
            where T : INode
        {
            INode node = CreateNodeScript<T>();
            switch (node)
            {
                case PolicyNode policyNode:
                    policyNode.PolicyName = GeneratePolicyName();
                    break;
                case CultureNode cultureNode:
                    cultureNode.CultureName = GenerateCultureName();
                    break;
                case VariableNode variableNode:
                    variableNode.VariableName = GenerateVariableName();
                    break;
                case ErrorRuleNode causeRuleNode:
                    causeRuleNode.ErrorName = GenerateErrorName();
                    break;
                case RoleNode roleNode:
                    roleNode.RoleName = GenerateRoleName();
                    break;
                case HeirFilterNode heirFilterNode:
                    heirFilterNode.FilterName = GenerateFilterName();
                    break;
            }

            InstantiateNode(node, position, shouldDraw, null, manualCreation);

            return node as T;
        }

        private void InstantiateNode(INode node, Vector2 position, bool shouldDraw = true, string manualid = null,
            bool manualCreation = false)
        {
            node.SetPosition(new Rect(position, Vector2.zero));
            node.Pos = position;
            if (manualCreation && node.IsGroupable())
            {
                if (IGroup.selectedGroup == null || IGroup.selectedGroup != node.Group)
                {
                    foreach (var selectable in selection)
                    {
                        if (selectable is not Group selectedElement)
                        {
                            if (selectable is INode selectedINode)
                                if (selection.Count == 1 && selectedINode.Group != null)
                                {
                                    selectedINode.Group.AddElement(node);
                                    node.Group = selectedINode.Group;
                                }

                            continue;
                        }

                        selectedElement.AddElement(node);
                        node.Group = (IGroup)selectedElement;
                    }

                    if (node.Group == null)
                    {
                        var lst = new List<INode>();
                        switch (node.GenericType)
                        {
                            case GenericNodeType.Family:
                                lst = graphElements.OfType<INode>().Where(e =>
                                        e.GenericType == GenericNodeType.Family && e.IsGroupable())
                                    .OrderBy(e => Vector2.Distance(position, e.GetPosition().position)).ToList();
                                break;
                            case GenericNodeType.Clan:
                                lst = graphElements.OfType<INode>().Where(e =>
                                        e.GenericType == GenericNodeType.Clan && e.IsGroupable())
                                    .OrderBy(e => Vector2.Distance(position, e.GetPosition().position)).ToList();
                                break;
                            case GenericNodeType.Rule:
                                lst = graphElements.OfType<INode>()
                                    .Where(e => e.GenericType == GenericNodeType.Rule && e.IsGroupable())
                                    .OrderBy(e => Vector2.Distance(position, e.GetPosition().position)).ToList();
                                break;
                            case GenericNodeType.Scheme:
                                lst = graphElements.OfType<INode>()
                                    .Where(e => e.GenericType == GenericNodeType.Scheme && e.IsGroupable())
                                    .OrderBy(e => Vector2.Distance(position, e.GetPosition().position)).ToList();
                                break;
                        }

                        if (lst.Count > 0)
                        {
                            var closestElement = lst[0];
                            var closestNode = closestElement;

                            closestNode.Group.AddElement(node);
                            node.Group = closestNode.Group;
                        }
                    }
                }
                else
                {
                    IGroup.selectedGroup.AddElement(node);
                    node.Group = IGroup.selectedGroup;
                }
            }

            node.Init(this);
            if (!string.IsNullOrEmpty(manualid)) node.ID = manualid;


            if (shouldDraw)
            {
                node.Draw();
                node.OnCreated();
                AddElement(node);
                GraphSaveUtility.SaveCurrent();

                EditorRoutine.StartRoutine(0.01f, () =>
                {
                    if (node is ActorNode)
                    {
                        GraphWindow.instance.SetActorKey(node.ID);
                        GraphSaveUtility.LoadCurrent(this);
                        GraphWindow.instance.GoView();
                    }
                });
                return;
            }

            if (manualid != null)
            {
                AddElement(node);
            }
        }

        #endregion

        #region Element Styles

        private void AddGridBackground()
        {
            var gridBackground = new GridBackground();
            gridBackground.StretchToParentSize();
            Insert(0, gridBackground);
        }

        private void MiniMapStyle()
        {
            miniMap.style.backgroundColor = new StyleColor(new Color32(29, 29, 30, 55));
            miniMap.SetBorderColor(NullUtils.HTMLColor("#353535"));
            miniMap.SetBorderRadius(0f);
            miniMap.SetBorderWidth(2f);
        }

        public void ToggleMiniMap()
        {
            miniMap.visible = !miniMap.visible;
            PlayerPrefs.SetString("IDE_Map_Open_State", miniMap.visible.ToString());
        }

        #endregion

        #region Position Correction

        public Vector2 GetLocalMousePosition(Vector2 mousePosition, bool isWindow = false)
        {
            var worldPosition = mousePosition;
            if (isWindow) worldPosition -= _graphWindow.position.position;

            var localPosition = contentViewContainer.WorldToLocal(worldPosition);
            return localPosition;
        }

        #endregion

        public void Fit(Func<GraphElement, bool> condition = null)
        {
            var rectToFit = condition == null
                ? this.CalculateRectToFitAllEx(contentViewContainer)
                : this.CalculateRectToFitAllEx(contentViewContainer, condition);
            CalculateFrameTransform(rectToFit, layout, 30, out var frameTranslation, out var frameScaling);
            Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);
            UpdateViewTransform(frameTranslation, frameScaling);
        }

        #region Callbacks

        private void OnElementDeleted()
        {
            deleteSelection = (_, _) =>
            {
                var groupsToDelete = new List<IGroup>();
                var edgesToDelete = new List<Edge>();
                var nodesToDelete = new List<INode>();

                ActorNode actorNode = null;

                foreach (var selectable in selection)
                {
                    var element = (GraphElement)selectable;
                    if (element is INode node)
                    {
                        if (node is ActorNode a) actorNode = a;
                        nodesToDelete.Add(node);
                        continue;
                    }

                    if (element is Edge edge)
                    {
                        edgesToDelete.Add(edge);
                        continue;
                    }

                    if (element is not IGroup group) continue;

                    groupsToDelete.Add(group);
                }

                SchemeGroup schemeGroup = null;
                ClanGroup clanGroup = null;
                FamilyGroup familyGroup = null;
                RuleGroup ruleGroup = null;
                foreach (var group in groupsToDelete)
                {
                    RemoveGroup(group);
                    group.OnDestroy();
                    switch (group)
                    {
                        case SchemeGroup s:
                            schemeGroup = s;
                            break;
                        case ClanGroup c:
                            clanGroup = c;
                            break;
                        case FamilyGroup f:
                            familyGroup = f;
                            break;
                        case RuleGroup r:
                            ruleGroup = r;
                            break;
                    }
                }

                DeleteElements(edgesToDelete);

                foreach (var node in nodesToDelete)
                {
                    RemoveNode(node);
                }

                if (schemeGroup != null)
                {
                    GraphWindow.instance.stories.Remove(schemeGroup.ID);
                    GraphWindow.instance.SetLastStoryKey();
                    GraphSaveUtility.LoadCurrent(this);
                    GraphWindow.instance.GoView();
                }

                if (clanGroup != null)
                {
                    GraphWindow.instance.clans.Remove(clanGroup.ID);
                    GraphWindow.instance.SetLastClanKey();
                    GraphSaveUtility.LoadCurrent(this);
                    GraphWindow.instance.GoView();
                }

                if (familyGroup != null)
                {
                    GraphWindow.instance.families.Remove(familyGroup.ID);
                    GraphWindow.instance.SetLastFamilyKey();
                    GraphSaveUtility.LoadCurrent(this);
                    GraphWindow.instance.GoView();
                }

                if (ruleGroup != null)
                {
                    GraphWindow.instance.rules.Remove(ruleGroup.ID);
                    GraphWindow.instance.SetLastRuleKey();
                    GraphSaveUtility.LoadCurrent(this);
                    GraphWindow.instance.GoView();
                }

                if (actorNode != null)
                {
                    GraphWindow.instance.characters.Remove(actorNode.ID);
                    GraphWindow.instance.SetLastActorKey();
                    GraphSaveUtility.LoadCurrent(this);
                    GraphWindow.instance.GoView();
                }
            };
        }

        private void RemoveNode(INode node)
        {
            if (!graphElements.Contains(node)) return;
            node.Group?.RemoveElement(node);

            node.DisconnectAllPorts();
            RemoveElement(node);
            node.OnDestroy();
        }

        private void RemoveGroup(IGroup group)
        {
            var groupNodes = new List<INode>();
            foreach (var groupElement in group.containedElements)
            {
                if (groupElement is not INode groupNode)
                    continue;

                groupNodes.Add(groupNode);
            }

            foreach (var node in groupNodes)
            {
                RemoveNode(node);
            }

            RemoveElement(group);
        }

        private void OnGroupElementAdded()
        {
            elementsAddedToGroup = (group, elements) =>
            {
                foreach (var element in elements)
                {
                    if (element is not INode node)
                        continue;
                    var nodeGroup = (IGroup)group;
                    node.Group = nodeGroup;
                }
            };
        }

        private void OnGroupElementRemoved()
        {
            elementsRemovedFromGroup = (_, elements) =>
            {
                foreach (var element in elements)
                {
                    if (element is not INode node)
                        continue;
                    node.DisconnectAllPorts();
                    RemoveElement(node);
                    // node.OnDestroy();
                }
            };
        }

        private void OnGraphViewChanged()
        {
            graphViewChanged = (changes) => {
                var isChanged = false;
                var actions = new List<Action>();
                
                if (changes.movedElements != null)
                {
                    foreach (var element in changes.movedElements)
                    {
                        if (element is INode node)
                        {
                            node.SetDirty();
                            node.Pos = node.GetPosition().position;
                        }
                    }
                    isChanged = true;
                }

                if (changes.edgesToCreate != null)
                {
                    foreach (var edge in changes.edgesToCreate)
                    {
                        var nextNode = (INode)edge.input.node;
                        var outputNode = (INode)edge.output.node;
                        var outputData = (OutputData)edge.output.userData;
                        var data = new PortData(nextNode.ID, edge.input.portName, edge.input.userData as string ?? string.Empty);
                        actions.Add(() => {
                            outputData.DataCollection.Add(data);
                        });

                        if (GraphWindow.instance.CurrentPage == GenericNodeType.Scheme &&
                            edge.output.portColor != STATIC.DefaultColor && edge.input.portColor == STATIC.DefaultColor)
                            edge.input.portColor = edge.output.portColor;

                        if (outputNode is ChanceNode chanceNode && nextNode is ChoiceDataNode choiceDataNode)
                        {
                            choiceDataNode.ChanceID = chanceNode.ID;
                        }
                        
                        nextNode.SetDirty();
                        outputNode.SetDirty();

                        if (nextNode is DialogueNode dialogueNode)
                        {
                            dialogueNode.RefreshTargetBorder(true);
                        }

                        outputNode.children.Add(nextNode);
                        nextNode.parents.Add(outputNode);
                    }

                    isChanged = true;
                }
                
                if (changes.elementsToRemove != null)
                {
                    foreach (var element in changes.elementsToRemove) {
                        if (!graphElements.Contains(element)) continue;
                        if (element.GetType() != typeof(Edge)) continue;
                        
                        var edge = (Edge)element;
                        var outputData = (OutputData)edge.output.userData;
                        var nextNode = (INode)edge.input.node;
                        var outputNode = (INode)edge.output.node;

                        if (outputNode is ChanceNode && nextNode is ChoiceDataNode choiceDataNode) {
                            choiceDataNode.ChanceID = null;
                        }

                        nextNode.SetDirty();
                        outputNode.SetDirty();

                        actions.Add(() => {
                            var index = outputData.DataCollection.FindIndex(d => d.NextID == nextNode.ID);
                            if(index != -1)
                                outputData.DataCollection.RemoveAt(index);
                        });

                        nextNode.RefreshPorts();

                        if (nextNode is DialogueNode dialogueNode) {
                            dialogueNode.RefreshTargetBorder(true);
                        }
                        
                        outputNode.children.Remove(nextNode);
                        nextNode.parents.Remove(outputNode);
                    }

                    isChanged = true;
                }

                if (isChanged) {
                    Undo.RecordObject(GraphWindow.CurrentDatabase, "IEGraph");
                    foreach (var action in actions) {
                        action?.Invoke();
                    }
                    GraphSaveUtility.SaveCurrent(true);
                }
                
                UpdateNodeIcons();
                
                return changes;
            };
        }

        public void UpdateNodeIcons() {
            var loadedNodes = nodes.Cast<INode>().ToList();
                
            foreach (var iNode in loadedNodes) {
                iNode.HideAllIcons();
            }

            foreach (var iNode in loadedNodes) {
                iNode.Execute();
            }
        }

        public void ClearGraph()
        {
            graphElements.ForEach(RemoveElement);
        }

        #endregion

        #region Methods

        private string GenerateGroupName(string titleText, GenericNodeType type)
        {
            var nameList =
                (from GroupData element in GraphWindow.CurrentDatabase.groupDataList.Where(e => e.GenericType == type)
                    select element.Title).ToList();

            var i = 1;
            while (nameList.Contains($"{titleText}{i}")) i++;

            return $"{titleText}{i}";
        }

        private string GeneratePolicyName()
        {
            var nameList = (from PolicyNode element in graphElements.OfType<PolicyNode>()
                    select element.PolicyName)
                .ToList();

            var policyName = "Policy";
            var i = 1;
            while (nameList.Contains($"{policyName}{i}")) i++;

            return $"{policyName}{i}";
        }

        private string GenerateCultureName()
        {
            var nameList =
                (from CultureNode element in graphElements.OfType<CultureNode>() select element.CultureName)
                .ToList();

            var cultureName = "Culture";
            var i = 1;
            while (nameList.Contains($"{cultureName}{i}")) i++;

            return $"{cultureName}{i}";
        }

        private string GenerateErrorName()
        {
            var nameList =
                (from ErrorRuleNode element in graphElements.OfType<ErrorRuleNode>() select element.ErrorName)
                .ToList();

            var errorName = "Error";
            var i = 1;
            while (nameList.Contains($"{errorName}{i}")) i++;

            return $"{errorName}{i}";
        }
        
        private string GenerateWarningName()
        {
            var nameList =
                (from WarningRuleNode element in graphElements.OfType<WarningRuleNode>() select element.WarningName)
                .ToList();

            var warningName = "Warning";
            var i = 1;
            while (nameList.Contains($"{warningName}{i}")) i++;

            return $"{warningName}{i}";
        }

        private string GenerateVariableName()
        {
            var nameList = (from VariableNode element in graphElements.OfType<VariableNode>()
                    select element.VariableName)
                .ToList();

            var variableName = "Variable";
            var i = 1;
            while (nameList.Contains($"{variableName}{i}")) i++;

            return $"{variableName}{i}";
        }

        private string GenerateRoleName()
        {
            var nameList = (from RoleNode element in graphElements.OfType<RoleNode>() select element.RoleName)
                .ToList();

            var missionName = "Engineer";
            var i = 1;
            while (nameList.Contains($"{missionName}{i}")) i++;

            return $"{missionName}{i}";
        }
        
        private string GenerateFilterName()
        {
            var nameList = (from HeirFilterNode element in graphElements.OfType<HeirFilterNode>() select element.FilterName)
                .ToList();

            var filterName = "Filter";
            var i = 1;
            while (nameList.Contains($"{filterName}{i}")) i++;

            return $"{filterName}{i}";
        }

        #endregion
    }

    public sealed class GeneratedNode
    {
        public readonly INode node;
        public bool generated;

        public GeneratedNode(INode node)
        {
            this.node = node;
        }
    }
}