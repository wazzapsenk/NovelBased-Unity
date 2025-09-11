using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ValidatorNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Validator_Node";

        public string RuleID;

        public override bool IsCopiable() => false;

        protected override void OnOutputInit()
        {
            var errorList = GraphWindow.CurrentDatabase.nodeDataList.Where(n => n.GroupId == RuleID && n is CauseRuleData)
                .Cast<CauseRuleData>().ToList();
            var warningList = GraphWindow.CurrentDatabase.nodeDataList.Where(n => n.GroupId == RuleID && n is WarningRuleData)
                .Cast<WarningRuleData>().ToList();

            foreach (var cause in errorList)
            {
                AddOutput(cause.ErrorName).Primary = true;
            }

            foreach (var cause in warningList)
            {
                AddOutput(cause.WarningName).Primary = false;
            }

            var lst = Outputs.Where(outputData =>
                !errorList.Exists(c => c.ErrorName == outputData.Name) &&
                !warningList.Exists(w => w.WarningName == outputData.Name)).ToList();

            foreach (var outputData in lst)
            {
                Outputs.Remove(outputData);
                SetDirty();
            }

            Outputs = Outputs.OrderByDescending(o => o.ValidatorMode == ValidatorMode.Break).ThenByDescending(o => o.ValidatorMode == ValidatorMode.Active).ThenByDescending(o => o.Primary).ToList();
        }

        private void CreateOutputs()
        {
            outputContainer.Clear();
            Outputs.Clear();

            var errorList = GraphWindow.CurrentDatabase.nodeDataList.Where(n => n.GroupId == RuleID && n is CauseRuleData)
                .Cast<CauseRuleData>();
            var warningList = GraphWindow.CurrentDatabase.nodeDataList.Where(n => n.GroupId == RuleID && n is WarningRuleData)
                .Cast<WarningRuleData>();

            foreach (var error in errorList)
            {
                AddOutput(error.ErrorName).Primary = true;
            }

            foreach (var warning in warningList)
            {
                AddOutput(warning.WarningName).Primary = false;
            }

            CreatePorts();

            RefreshExpandedState();
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-jean-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Validator"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var rules = GraphWindow.CurrentDatabase.groupDataList.OfType<RuleGroupData>().Select(s => new { s.ID, s.Title })
                .ToDictionary(d => d.ID, d => d.Title);
            var ruleList = IEGraphUtility.CreateDropdown(null);
            ruleList.style.minWidth = 120f;

            ruleList.choices = new List<string>(rules.Values);

            ruleList.choices.Insert(0, "NULL");

            var index = rules.Keys.ToList().IndexOf(RuleID);
            ruleList.index = index == -1 ? 0 : index + 1;

            var dropdownChild = ruleList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            ruleList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                DisconnectAllPorts();

                if (ruleList.index < 1)
                {
                    RuleID = string.Empty;
                    CreateOutputs();

                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                RuleID = rules.Keys.ElementAt(ruleList.index - 1);
                CreateOutputs();

                GraphSaveUtility.SaveCurrent();
            });
            ruleList.style.marginLeft = 0f;
            ruleList.style.marginBottom = 1f;
            ruleList.style.marginTop = 1f;
            ruleList.style.marginRight = 3f;
            ruleList.AddClasses("uis-rule-dropdown");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, ruleList);

            CreatePorts();

            RefreshExpandedState();
        }

        private void CreatePorts()
        {
            foreach (var output in Outputs)
            {
                var portField = new VisualElement()
                {
                    style =
                    {
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                        justifyContent = new StyleEnum<Justify>(Justify.SpaceBetween),
                        paddingLeft = 4f
                    }
                };
                var cPort = this.CreatePort(output.Name, Port.Capacity.Multi);
                cPort.userData = output;

                cPort.portColor = output.Primary ? STATIC.RedPort : STATIC.YellowPort;
                // cPort.GetChild<Label>().style.color = STATIC.YellowPort;
                cPort.tooltip = output.Primary ? "Error" : "Warning";

                var disableBtn = IEGraphUtility.CreateButton("X");
                disableBtn.AddClasses("ide-disable_btn");

                disableBtn.RegisterCallback<ClickEvent>(_ =>
                {
                    output.ValidatorMode = output.ValidatorMode switch
                    {
                        ValidatorMode.Passive => ValidatorMode.Active,
                        ValidatorMode.Active => ValidatorMode.Break,
                        _ => ValidatorMode.Passive
                    };

                    if (output.ValidatorMode == ValidatorMode.Passive)
                    {
                        cPort.Disable();
                        disableBtn.text = "P";
                        disableBtn.tooltip = "Passive";
                        disableBtn.style.backgroundColor = NullUtils.HTMLColor("#4B4B4B");
                    }
                    else if (output.ValidatorMode == ValidatorMode.Active)
                    {
                        cPort.Enable();
                        disableBtn.text = "A";
                        disableBtn.tooltip = "Active";
                        disableBtn.style.backgroundColor = NullUtils.HTMLColor("#3F4B59");
                    }
                    else if (output.ValidatorMode == ValidatorMode.Break)
                    {
                        cPort.Enable();
                        disableBtn.text = "B";
                        disableBtn.tooltip = "Break";
                        disableBtn.style.backgroundColor = NullUtils.HTMLColor("#8E3E3A");
                    }

                    SetDirty();

                    GraphSaveUtility.SaveCurrent();
                });

                portField.Add(disableBtn);
                portField.Add(cPort);

                outputContainer.Add(portField);

                if (output.ValidatorMode == ValidatorMode.Passive)
                {
                    cPort.Disable();
                    disableBtn.text = "P";
                    disableBtn.tooltip = "Passive";
                    disableBtn.style.backgroundColor = NullUtils.HTMLColor("#4B4B4B");
                }
                else if (output.ValidatorMode == ValidatorMode.Active)
                {
                    cPort.Enable();
                    disableBtn.text = "A";
                    disableBtn.tooltip = "Active";
                    disableBtn.style.backgroundColor = NullUtils.HTMLColor("#3F4B59");
                }
                else if (output.ValidatorMode == ValidatorMode.Break)
                {
                    cPort.Enable();
                    disableBtn.text = "B";
                    disableBtn.tooltip = "Break";
                    disableBtn.style.backgroundColor = NullUtils.HTMLColor("#8E3E3A");
                }
            }
        }
    }
}