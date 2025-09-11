using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.SaveSystem;
using Nullframes.Intrigues.UI;
using Nullframes.Intrigues.Utils;
using UnityEngine;

namespace Nullframes.Intrigues.Graph {
    public class Schemer : MonoBehaviour {
        public class SchemerFactory {
            public enum ValidatorResult {
                Success,
                FailedContinue,
                FailedBreak,
            }

            public List< string > Delays { get; set; }
            public HashSet< Dialogue > Dialogues { get; set; }
            private Dictionary< string, List< float > > ChanceBonus { get; set; }
            public Func< ValidatorResult > Validator { get; set; }
            public List< string > ErrorNameList { get; set; }
            public List< string > WarningNameList { get; set; }
            public float ValidatePerSecond => 2f;

            public List< NVar > Variables { get; set; }

            public HashSet< NodeInfo > ActiveNodeList { get; set; }

            public Scheme Scheme { get; set; }
            public List< NodeData > NodeList { get; set; }
            public Dictionary< NodeData, List< NodeData > > ClassList { get; set; }
            public Coroutine ValidateRoutine { get; set; }

            public void KillNode(NodeInfo nodeInfo) {
                ActiveNodeList.Remove(nodeInfo);

                StopDelay(nodeInfo.node);

                if ( nodeInfo.repeater != null ) {
                    if ( ActiveNodeList.Count(n => n.repeater == nodeInfo.repeater) < 1 ) {
                        var rep = ActiveNodeList.FirstOrDefault(n => n == nodeInfo.repeater);
                        if ( rep != null ) {
                            rep.repeatCount++;
                            rep.node.Run(Scheme.Schemer, rep);
                        }
                    }
                }

                if ( nodeInfo.sequencer != null ) {
                    if ( ActiveNodeList.Count(n => n.sequencer == nodeInfo.sequencer) < 1 ) {
                        var seq = ActiveNodeList.FirstOrDefault(n => n == nodeInfo.sequencer);
                        if ( seq != null ) {
                            seq.index++;
                            seq.node.Run(Scheme.Schemer, seq);
                        }
                    }
                }

                if ( nodeInfo.validator != null ) {
                    if ( ActiveNodeList.Count(n => n.validator == nodeInfo.validator) < 1 ) {
                        ActiveNodeList.Remove(nodeInfo.validator);
                    }
                }

                if ( ActiveNodeList.Count(n => !n.bgWorker) < 1 ) {
                    if ( Scheme.Schemer.IsEnded ) {
                        Scheme.Schemer.EndScheme(Scheme.Schemer.Result);
                        Destroy(Scheme.Schemer);
                    } else {
                        Scheme.Schemer.IsEnded = true;
                        Scheme.Schemer.StopValidateRoutine();
                        Scheme.Schemer.GoEnd();
                    }
                }
            }

            public NodeData GetNode(string nodeId) => NodeList.FirstOrDefault(n => n.ID == nodeId);

            public void ExecuteNode(NodeInfo nodeInfo) => ActiveNodeList.Add(nodeInfo);

            public void StopDelay(NodeData waitNode) {
                if ( waitNode is not WaitData ) return;
                List< NodeInfo > nodeInfos = ActiveNodeList.Where(n => n.node == waitNode).Select(n => n)
                    .ToList();

                foreach ( var nodeInfo in nodeInfos ) {
                    foreach ( var delayId in nodeInfo.delays ) {
                        NullUtils.StopCall(delayId);
                        nodeInfo.node.End(nodeInfo);
                    }
                }
            }

            public void Continue(NodeInfo node, bool closeDialogue = true) {
                if ( closeDialogue ) Scheme.Schemer.CloseDialogues();
                Scheme.Schemer.StopDelay();

                if ( closeDialogue ) {
                    // ActiveNodeList.RemoveWhere(n => n != node);
                    var nodeInfos = ActiveNodeList.Where(n => n != node).ToDictionary(nodeInfo => nodeInfo.node);

                    foreach ( var nodeInfo in nodeInfos ) {
                        nodeInfo.Key.End(nodeInfo.Value);
                    }
                } else {
                    // ActiveNodeList.RemoveWhere(n => n != node && n.node is not DialogueData);
                    var nodeInfos = ActiveNodeList.Where(n => n != node && n.node is not DialogueData)
                        .ToDictionary(nodeInfo => nodeInfo.node);

                    foreach ( var nodeInfo in nodeInfos ) {
                        nodeInfo.Key.End(nodeInfo.Value);
                    }
                }
            }

            public void GoValidation(bool breaks) {
                if ( breaks ) {
                    Scheme.Schemer.CloseDialogues();
                    Scheme.Schemer.StopDelay();
                    ActiveNodeList.Clear();
                }

                Scheme.Schemer.GoValidator();
            }

            public void InitializeStaticNodes() {
                ChanceBonus = new Dictionary< string, List< float > >();

                foreach ( var chanceModifierData in NodeList.OfType< ChanceModifierData >() ) {
                    foreach ( var outputData in chanceModifierData.Outputs ) {
                        foreach ( var node in outputData.DataCollection.Select(portData => GetNode(portData.NextID)) ) {
                            if ( node is not ChanceData chanceData ) continue;

                            NFloat bonus;

                            var table = Variables.FirstOrDefault(n =>
                                n.id == chanceModifierData.VariableID);

                            if ( table == null ) {
                                var target = outputData.Name switch {
                                    "Conspirator" => Scheme.Schemer.Conspirator,
                                    "Target" => Scheme.Schemer.Target,
                                    _ => null
                                };

                                bonus = target == null
                                    ? IM.GetVariable< NFloat >(chanceModifierData.VariableID)
                                    : target.GetVariable< NFloat >(chanceModifierData.VariableID);
                            } else {
                                bonus = (NFloat)table;
                            }

                            float negativeValue = -Mathf.Abs(bonus.Value);
                            float positiveValue = Mathf.Abs(bonus.Value);
                            bool isPositive = bonus.Value > 0;

                            float value = chanceModifierData.Negative ? negativeValue :
                                chanceModifierData.Positive ? positiveValue :
                                chanceModifierData.Opposite ? isPositive ? negativeValue : positiveValue : bonus.Value;

                            value = chanceModifierData.Mode == 0 ? value : chanceData.Chance.CalculatePercentage(value);

                            if ( ChanceBonus.ContainsKey(node.ID) ) {
                                if ( ChanceBonus[ node.ID ] != null ) {
                                    ChanceBonus[ node.ID ].Add(value);
                                } else {
                                    ChanceBonus[ node.ID ] = new List< float > { value };
                                }
                            } else {
                                ChanceBonus.Add(node.ID, new List< float > { value });
                            }
                        }
                    }
                }
            }

            public float GetBonus(string id) =>
                !ChanceBonus.ContainsKey(id) ? 0f : ChanceBonus[ id ].Sum();

            public void SetSchemeState(SchemeResult result) => Scheme.Schemer.Result = result;
        }

        /// <summary>
        /// Gets the conspirator of the scheme.
        /// </summary>
        public Actor Conspirator { get; private set; }

        /// <summary>
        /// Gets the target of the scheme.
        /// </summary>
        public Actor Target { get; private set; }

        public SchemeResult Result { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the scheme is running in 'End Node' Flow.
        /// </summary>
        public bool IsEnded { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the scheme is paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Occurs when the conspirator actor of the scheme changes.
        /// </summary>
        public static event Action< Scheme, Actor > onConspiratorChanged;

        /// <summary>
        /// Occurs when the target actor of the scheme changes.
        /// </summary>
        public static event Action< Scheme, Actor > onTargetChanged;

        private SchemerFactory schemerFactory;

        public IReadOnlyCollection< NodeInfo > ActiveNodeList => schemerFactory.ActiveNodeList;
        public IReadOnlyCollection< NVar > Variables => schemerFactory.Variables;

        private void Awake() {
            Init();
            hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }

        private void OnDestroy() {
            NDebug.Log($"The Scheme has been ended. ({Result.ToString()})");
            StopValidateRoutine();
            StopDelay();
            // CloseDialogues();
        }

        private void Init() {
            schemerFactory = new SchemerFactory();

            Result = SchemeResult.None;
            schemerFactory.NodeList = new List< NodeData >();
            schemerFactory.ActiveNodeList = new HashSet< NodeInfo >();
            schemerFactory.Variables = new List< NVar >();
            schemerFactory.Delays = new List< string >();
            schemerFactory.Dialogues = new HashSet< Dialogue >();
        }

        public static void StartGraph(Scheme scheme, Actor conspirator, Actor target) {
            var schemeGroup = (SchemeGroupData)IM.IEDatabase.groupDataList.Find(g => g.ID == scheme.ID);
            if ( schemeGroup == null ) return;

            var schemer = SpawnSchemer();

            schemer.Conspirator = conspirator;
            schemer.Target = target;

            IM.SystemMethods[ "Scheme_Init" ].Invoke(scheme, new object[ ] { schemer });
            schemer.schemerFactory.Scheme = scheme;

            foreach ( var node in IM.IEDatabase.nodeDataList.Where(n => n.GroupId == schemeGroup.ID) ) {
                schemer.schemerFactory.NodeList.Add(node);

                if ( node is ValidatorData validatorData ) {
                    schemer.SetupValidator(validatorData);
                }
            }

            foreach ( var variable in schemeGroup.Variables ) {
                var nVar = NVar.CreateWithType(variable.name, variable.type);
                nVar.id = variable.id;
                schemer.schemerFactory.Variables.Add(nVar);
            }

            schemer.LoadClassNodes();
            schemer.GoStart();
        }

        public static void LoadGraph(Scheme scheme, Actor conspirator, Actor target, SchemeData data) {
            var schemeGroup = (SchemeGroupData)IM.IEDatabase.groupDataList.Find(g => g.ID == scheme.ID);
            if ( schemeGroup == null ) return;

            var schemer = SpawnSchemer();

            schemer.Conspirator = conspirator;
            schemer.Target = target;

            schemer.IsPaused = data.isPaused;
            schemer.IsEnded = data.isEnded;

            IM.SystemMethods[ "Scheme_Init" ].Invoke(scheme, new object[ ] { schemer });
            IM.SystemMethods[ "Scheme_SetObjective" ].Invoke(scheme, new object[ ] { data.objective });

            schemer.schemerFactory.Scheme = scheme;

            foreach ( var node in IM.IEDatabase.nodeDataList.Where(n => n.GroupId == schemeGroup.ID) ) {
                schemer.schemerFactory.NodeList.Add(node);

                if ( node is ValidatorData validatorData && !schemer.IsEnded ) {
                    schemer.SetupValidator(validatorData);
                }
            }

            foreach ( var variable in schemeGroup.Variables ) {
                var nVar = NVar.CreateWithType(variable.name, variable.type);
                nVar.id = variable.id;
                schemer.schemerFactory.Variables.Add(nVar);
            }

            foreach ( var variableData in data.variables ) {
                var nVar = schemer.GetVariable(variableData.id);
                if ( nVar == null ) continue;
                nVar.value = nVar switch {
                    NString => variableData.stringValue,
                    NInt => variableData.integerValue,
                    NBool => variableData.boolValue,
                    NFloat => variableData.floatValue,
                    NObject => variableData.objectValue,
                    NEnum => variableData.enumIndex,
                    NActor => variableData.actorId,
                    _ => nVar.value
                };
            }

            foreach ( var activeNode in data.activeNodes ) {
                schemer.schemerFactory.ActiveNodeList.Add(new NodeInfo(activeNode.id) {
                    index = activeNode.index, node = schemer.schemerFactory.GetNode(activeNode.nodeId),
                    time = activeNode.time,
                    repeatCount = activeNode.repeatCount, inputName = activeNode.inputName,
                    delays = new List< string >(activeNode.delays), indexes = new List< int >(activeNode.indexes),
                    bgWorker = activeNode.bgWorker,
                    actor = IM.ActorDictionary.ContainsKey(activeNode.actorId)
                        ? IM.ActorDictionary[ activeNode.actorId ]
                        : null,
                    dualActor = (
                        IM.ActorDictionary.ContainsKey(activeNode.actorId1)
                            ? IM.ActorDictionary[ activeNode.actorId1 ]
                            : null,
                        IM.ActorDictionary.ContainsKey(activeNode.actorId2)
                            ? IM.ActorDictionary[ activeNode.actorId2 ]
                            : null ),
                    clan = IM.GetClan(activeNode.clanId),
                    family = IM.GetFamily(activeNode.familyId),
                    SchemerFactory = schemer.schemerFactory
                });
            }

            foreach ( var activeNode in data.activeNodes ) {
                if ( !string.IsNullOrEmpty(activeNode.sequencerId) ) {
                    var node = schemer.schemerFactory.ActiveNodeList.FirstOrDefault(n => n.id == activeNode.id);
                    var seq = schemer.schemerFactory.ActiveNodeList.FirstOrDefault(n => n.id == activeNode.sequencerId);
                    if ( node != null && seq != null ) {
                        node.sequencer = seq;
                    }
                }

                if ( !string.IsNullOrEmpty(activeNode.repeaterId) ) {
                    var node = schemer.schemerFactory.ActiveNodeList.FirstOrDefault(n => n.id == activeNode.id);
                    var rep = schemer.schemerFactory.ActiveNodeList.FirstOrDefault(n => n.id == activeNode.repeaterId);
                    if ( node != null && rep != null ) {
                        node.repeater = rep;
                    }
                }

                if ( !string.IsNullOrEmpty(activeNode.validatorId) ) {
                    var node = schemer.schemerFactory.ActiveNodeList.FirstOrDefault(n => n.id == activeNode.id);
                    var validator =
                        schemer.schemerFactory.ActiveNodeList.FirstOrDefault(n => n.id == activeNode.validatorId);
                    if ( node != null && validator != null ) {
                        node.validator = validator;
                    }
                }
            }

            var nodeInfos = schemer.schemerFactory.ActiveNodeList.ToList();

            nodeInfos.RemoveAll(n =>
                n.node is RepeaterData rep &&
                schemer.schemerFactory.ActiveNodeList.Count(a => a.repeater?.node == rep) > 0);

            nodeInfos.RemoveAll(n =>
                n.node is SequencerData rep &&
                schemer.schemerFactory.ActiveNodeList.Count(a => a.sequencer?.node == rep) > 0);

            schemer.LoadClassNodes();

            foreach ( var activeNode in nodeInfos ) {
                activeNode.node.Run(schemer, activeNode);
            }

            foreach ( var loadNode in schemer.schemerFactory.NodeList.Where(n => n is OnLoadData) ) {
                loadNode.Run(schemer, new NodeInfo { SchemerFactory = schemer.schemerFactory });
            }
        }

        private void SetupValidator(ValidatorData validatorData) {
            schemerFactory.Validator = () => {
                if ( IsEnded ) return SchemerFactory.ValidatorResult.Success;

                var ruleResult = Ruler.StartGraph(validatorData.RuleID, Conspirator, Target);
                schemerFactory.ErrorNameList = new List< string >(ruleResult.ErrorNameList);
                schemerFactory.WarningNameList = new List< string >(ruleResult.WarningNameList);

                bool failed = false;

                if ( validatorData.Outputs.Any() ) {
                    foreach ( var output in validatorData.Outputs.Where(o => o.ValidatorMode != ValidatorMode.Passive)
                                 .OrderByDescending(o => o.ValidatorMode == ValidatorMode.Break) ) {
                        if ( schemerFactory.ErrorNameList.Contains(output.Name) ||
                             schemerFactory.WarningNameList.Contains(output.Name) ) {
                            if ( output.ValidatorMode == ValidatorMode.Break ) {
                                return SchemerFactory.ValidatorResult.FailedBreak;
                            }

                            failed = true;
                        }
                    }
                } else {
                    failed = true;
                }

                return !failed || ruleResult
                    ? SchemerFactory.ValidatorResult.Success
                    : SchemerFactory.ValidatorResult.FailedContinue;
            };

            StartValidateRoutine();
        }

        public NVar GetVariable(string variableNameOrId) =>
            Variables.FirstOrDefault(v => v.name == variableNameOrId || v.id == variableNameOrId);

        private void StartValidateRoutine() {
            if ( schemerFactory.ValidateRoutine != null ) return;
            schemerFactory.ValidateRoutine = StartCoroutine(Validate());
        }

        private void StopValidateRoutine() {
            schemerFactory.Validator = null;
            if ( schemerFactory.ValidateRoutine == null ) return;
            StopCoroutine(schemerFactory.ValidateRoutine);
        }

        private IEnumerator Validate() {
            while ( !IsEnded ) {
                yield return new WaitForEndOfFrame();
                if ( schemerFactory.Scheme is not { IsValid: true } ) {
                    Kill(true);
                    yield return null;
                }

                if ( schemerFactory.ActiveNodeList.Any(a => a.validator != null) ) continue;
                var result = schemerFactory.Validator.Invoke();

                if ( result != SchemerFactory.ValidatorResult.Success ) {
                    schemerFactory.GoValidation(result == SchemerFactory.ValidatorResult.FailedBreak);
                }
            }
        }

        /// <summary>
        /// Ends the scheme with a specified result.
        /// </summary>
        /// <param name="result">The result of the scheme (e.g., None, Success, Failed).</param>
        public void EndScheme(SchemeResult result) {
            Conspirator.SendSchemeEndEvent(schemerFactory.Scheme, result);
            Kill();
        }

        /// <summary>
        /// Sets a new conspirator actor for the scheme.
        /// </summary>
        /// <param name="actor">The actor to be set as the new conspirator.</param>
        public void SetConspirator(Actor actor) {
            if ( actor == null || actor == Target || actor == Conspirator ) return;

            var prevConspirator = Conspirator;
            Conspirator = actor;
            onConspiratorChanged?.Invoke(schemerFactory.Scheme, prevConspirator);
        }

        /// <summary>
        /// Sets a new target actor for the scheme.
        /// </summary>
        /// <param name="actor">The actor to be set as the new target.</param>
        public void SetTarget(Actor actor) {
            if ( actor == null || actor == Conspirator || actor == Target ) return;

            var prevTarget = Target;
            Target = actor;
            onTargetChanged?.Invoke(schemerFactory.Scheme, prevTarget);
        }

        /// <summary>
        /// Terminates the scheme.
        /// </summary>
        /// <param name="dontGoEnd">If true, prevents the scheme from reaching its end node.</param>
        public void Kill(bool dontGoEnd = false) {
            StopDelay();

            if ( IsEnded || dontGoEnd ) {
#if UNITY_EDITOR
                if ( !Application.isPlaying )
                    DestroyImmediate(gameObject);
                else
                    Destroy(gameObject);
#else
                    Destroy(gameObject);
#endif

                Conspirator.SendSchemeEndEvent(schemerFactory.Scheme, SchemeResult.Null);
            } else {
                schemerFactory.ActiveNodeList.Clear();
                IsEnded = true;
                StopValidateRoutine();
                GoEnd();
            }
        }

        /// <summary>
        /// Pauses the scheme.
        /// </summary>
        public void Pause() {
            IsPaused = true;
            PauseDelays();
        }

        /// <summary>
        /// Resumes the paused scheme.
        /// </summary>
        public void Resume() {
            IsPaused = false;
            ResumeDelays();
        }

        private void StopDelay() {
            foreach ( var delay in schemerFactory.Delays ) {
                NullUtils.StopCall(delay);
            }
        }

        private void PauseDelays() {
            foreach ( var delay in schemerFactory.Delays ) {
                NullUtils.PauseCall(delay);
            }
        }

        private void ResumeDelays() {
            foreach ( var delay in schemerFactory.Delays ) {
                NullUtils.ResumeCall(delay);
            }
        }

        private void CloseDialogues() {
            foreach ( var dialogue in schemerFactory.Dialogues.Where(dialogue => dialogue != null) ) {
                dialogue.Close(true);
            }
        }

        private void LoadClassNodes() {
            schemerFactory.ClassList = new Dictionary< NodeData, List< NodeData > >();

            foreach ( var soundClassData in schemerFactory.NodeList.OfType< SoundClassData >() ) {
                foreach ( var dialogueData in from output in soundClassData.Outputs
                         from outputData in output.DataCollection
                         select schemerFactory.GetNode(outputData.NextID) ) {
                    if ( schemerFactory.ClassList.ContainsKey(dialogueData) ) {
                        schemerFactory.ClassList[ dialogueData ].Add(soundClassData);
                    } else {
                        schemerFactory.ClassList.Add(dialogueData, new List< NodeData > { soundClassData });
                    }
                }
            }

            foreach ( var voiceClassData in schemerFactory.NodeList.OfType< VoiceClassData >() ) {
                foreach ( var dialogueData in from output in voiceClassData.Outputs
                         from outputData in output.DataCollection
                         select schemerFactory.GetNode(outputData.NextID) ) {
                    if ( schemerFactory.ClassList.ContainsKey(dialogueData) &&
                         schemerFactory.ClassList[ dialogueData ].Any(c => c is VoiceClassData) ) break;

                    if ( schemerFactory.ClassList.ContainsKey(dialogueData) ) {
                        schemerFactory.ClassList[ dialogueData ].Add(voiceClassData);
                    } else {
                        schemerFactory.ClassList.Add(dialogueData, new List< NodeData > { voiceClassData });
                    }
                }
            }
        }

        private void GoStart() {
            var startNode = schemerFactory.NodeList.FirstOrDefault(n => n is StartData);
            _ = startNode ?? throw new ArgumentException("Start node not found.");
            startNode.Run(this, new NodeInfo { SchemerFactory = schemerFactory });
        }

        private void GoEnd() {
            var endNode = schemerFactory.NodeList.FirstOrDefault(n => n is EndData);
            _ = endNode ?? throw new ArgumentException("End node not found.");
            endNode.Run(this, new NodeInfo { SchemerFactory = schemerFactory });
        }

        private void GoValidator() {
            var validatorNode = schemerFactory.NodeList.FirstOrDefault(n => n is ValidatorData);
            _ = validatorNode ?? throw new ArgumentException("Validator node not found.");
            validatorNode.Run(this, new NodeInfo { SchemerFactory = schemerFactory });
        }

        private static Schemer SpawnSchemer() {
            var schemerObject = new GameObject("Schemer") {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
            };
            return schemerObject.AddComponent< Schemer >();
        }

        // Progress
    }
}