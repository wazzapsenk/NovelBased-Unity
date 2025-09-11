using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nullframes.Intrigues.Graph;
using Nullframes.Threading;
using UnityEngine;

namespace Nullframes.Intrigues {
    public class Ruler : MonoBehaviour {
        public class RulerFactory {
            private Ruler Ruler { get; }
            public List<NodeData> NodeList { get; set; }
            public HashSet<NodeInfo> ActiveNodeList { get; set; }
            public List<string> ErrorList { get; set; }
            public List<string> ErrorNameList { get; set; }
            public List<string> WarningList { get; set; }
            public List<string> WarningNameList { get; set; }
            public bool IsAsync { get; set; }
            public readonly TaskCompletionSource<bool> _completionSource = new();

            public NodeData GetNode(string nodeId) => NodeList.FirstOrDefault(n => n.ID == nodeId);

            public void ExecuteNode(NodeInfo nodeInfo) => ActiveNodeList.Add(nodeInfo);

            public void KillNode(NodeInfo nodeInfo) {
                ActiveNodeList.Remove(nodeInfo);

                if (ActiveNodeList.Count < 1) {
                    _completionSource.SetResult(true);
                    Dispatcher.Invoke(() => DestroyImmediate(Ruler));
                }
            }

            public void SetConditionalState(RuleState state) => Ruler.Result = state;

            public void Kill() {
                ActiveNodeList.Clear();
                Dispatcher.Invoke(() => DestroyImmediate(Ruler));
            }

            public RulerFactory(Ruler ruler) {
                Ruler = ruler;
            }
        }

        public Actor Conspirator { get; private set; }
        public Actor Target { get; private set; }
        private RuleState Result { get; set; }

        private RulerFactory rulerFactory;

        private void Awake() {
            Init();
            hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            // NDebug.Log("Rule executed.");
        }

        private void Init() {
            rulerFactory = new RulerFactory(this);

            Result = RuleState.Failed;
            rulerFactory.NodeList = new List<NodeData>();
            rulerFactory.ActiveNodeList = new HashSet<NodeInfo>();
            rulerFactory.ErrorList = new List<string>();
            rulerFactory.ErrorNameList = new List<string>();
            rulerFactory.WarningList = new List<string>();
            rulerFactory.WarningNameList = new List<string>();
        }

        public static RuleResult StartGraph(string ruleNameOrId, Actor conspirator, Actor target) {
            if (conspirator == null) {
                return (new RuleResult(RuleState.Failed));
            }

            var conditionGroup =
                (RuleGroupData)IM.IEDatabase.groupDataList.Find(g =>
                    g.Title == ruleNameOrId || g.ID == ruleNameOrId);
            if (conditionGroup == null) {
                return (new RuleResult(RuleState.Success));
            }

            var conditional = SpawnRule();

            foreach (var node in IM.IEDatabase.nodeDataList.Where(n => n.GroupId == conditionGroup.ID)) {
                conditional.rulerFactory.NodeList.Add(node);
            }

            conditional.Conspirator = conspirator;
            conditional.Target = target;

            conditional.GoStart();

            if (conditional.rulerFactory.ErrorList.Count > 0) {
                conditional.Result = RuleState.Failed;
            }

            return (new RuleResult(conditional.Result, conditional.rulerFactory.ErrorNameList,
                conditional.rulerFactory.ErrorList, conditional.rulerFactory.WarningNameList,
                conditional.rulerFactory.WarningList));
        }
        
        public static async Task<RuleResult> StartGraphAsync(string ruleNameOrId, Actor conspirator, Actor target) {
            return await Task.Run(async () => {
                if (conspirator == null) {
                    return (new RuleResult(RuleState.Failed));
                }

                var conditionGroup =
                    (RuleGroupData)IM.IEDatabase.groupDataList.Find(g =>
                        g.Title == ruleNameOrId || g.ID == ruleNameOrId);
                if (conditionGroup == null) {
                    return (new RuleResult(RuleState.Success));
                }

                var conditional = await SpawnRuleAsync();

                conditional.rulerFactory.IsAsync = true;
                
                foreach (var node in IM.IEDatabase.nodeDataList.Where(n => n.GroupId == conditionGroup.ID)) {
                    conditional.rulerFactory.NodeList.Add(node);
                }

                conditional.Conspirator = conspirator;
                conditional.Target = target;

                await conditional.GoStartAsync();

                await conditional.rulerFactory._completionSource.Task;
                
                if (conditional.rulerFactory.ErrorList.Count > 0) {
                    conditional.Result = RuleState.Failed;
                }

                return (new RuleResult(conditional.Result, conditional.rulerFactory.ErrorNameList,
                    conditional.rulerFactory.ErrorList, conditional.rulerFactory.WarningNameList,
                    conditional.rulerFactory.WarningList));
            });
        }

        private void GoStart() {
            var startNode = rulerFactory.NodeList.FirstOrDefault(n => n is StartRuleData);
            _ = startNode ?? throw new ArgumentException("Start node cannot be null.");
            startNode.Run(this, new NodeInfo { RulerFactory = rulerFactory });
        }
        
        private async Task GoStartAsync() {
            await Task.Run(() => {
                var startNode = rulerFactory.NodeList.FirstOrDefault(n => n is StartRuleData);
                _ = startNode ?? throw new ArgumentException("Start node cannot be null.");
                startNode.Run(this, new NodeInfo { RulerFactory = rulerFactory });
            });
        }

        private static Task<Ruler> SpawnRuleAsync() {
            return Dispatcher.InvokeAsync(() => {
                var rulerObject = new GameObject("Ruler") {
                    hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
                };
                return rulerObject.AddComponent<Ruler>();
            });
        }

        private static Ruler SpawnRule() {
            var rulerObject = new GameObject("Ruler") {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
            };
            return rulerObject.AddComponent<Ruler>();
        }
    }

    public struct RuleResult {
        public RuleState Result { get; }
        public List<string> ErrorList { get; }
        public List<string> WarningList { get; }
        public List<string> ErrorNameList { get; }
        public List<string> WarningNameList { get; }

        public RuleResult(RuleState result, IEnumerable<string> errorNameList, IEnumerable<string> errorList,
            IEnumerable<string> warningNameList, IEnumerable<string> warningList) {
            Result = result;
            ErrorNameList = new List<string>(errorNameList);
            ErrorList = new List<string>(errorList);
            WarningNameList = new List<string>(warningNameList);
            WarningList = new List<string>(warningList);
        }

        public RuleResult(RuleState result) {
            Result = result;
            ErrorNameList = new List<string>();
            ErrorList = new List<string>();
            WarningNameList = new List<string>();
            WarningList = new List<string>();
        }

        public static implicit operator bool(RuleResult d) => d.Result == RuleState.Success;
    }
}