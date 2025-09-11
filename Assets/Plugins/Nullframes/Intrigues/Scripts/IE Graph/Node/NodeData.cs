using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nullframes.Intrigues.UI;
using Nullframes.Intrigues.Utils;
using Nullframes.Threading;
using UnityEngine;

namespace Nullframes.Intrigues.Graph {
    [Serializable]
    public class NodeData {
        [field: SerializeField] public string ID { get; protected set; }

        public virtual List< OutputData > GetOutputs() => null;

        [SerializeField] private string groupId;

        public string GroupId {
            get => groupId;
            protected set => groupId = value;
        }

        [field: SerializeField] public Vector2 Position { get; protected set; }

        public virtual GenericNodeType GenericType => GenericNodeType.Scheme;

        protected NodeInfo GenerateNodeData(NodeData node) => new() { node = node };

        #region SCHEMER

        public virtual Task Run(Schemer schemer, NodeInfo nodeInfo) {
            nodeInfo.SchemerFactory.ExecuteNode(nodeInfo);
            return Task.CompletedTask;
        }

        public virtual void End(NodeInfo nodeInfo) {
            nodeInfo.SchemerFactory.KillNode(nodeInfo);
        }

        protected async Task Next(Schemer schemer, IEnumerable< OutputData > outputs, NodeInfo nodeInfo, int index = 0,
            bool end = true) {
            if ( schemer.ActiveNodeList.Count > STATIC.MAX_FLOW_DEPTH ) {
                Debug.LogError(
                    $"Maximum stack depth exceeded! Possible infinite loop detected. Node: {nodeInfo.node.GetType()}");
                schemer.Kill(true);
                return;
            }

            if ( !nodeInfo.SchemerFactory.Scheme.IsValid ) {
                schemer.Kill(true);
                return;
            }

            if ( nodeInfo.SchemerFactory.Validator != null && !schemer.IsEnded &&
                 nodeInfo.SchemerFactory.ActiveNodeList.All(a => a.validator == null) ) {
                var result = nodeInfo.SchemerFactory.Validator.Invoke();
                if ( result != Schemer.SchemerFactory.ValidatorResult.Success ) {
                    nodeInfo.SchemerFactory.GoValidation(result == Schemer.SchemerFactory.ValidatorResult.FailedBreak);
                    if ( result == Schemer.SchemerFactory.ValidatorResult.FailedBreak )
                        return;
                }
            }

            var output = outputs.ElementAtOrDefault(index);
            if ( output == null ) {
                if ( end ) End(nodeInfo);
                return;
            }

            var executeList = output.DataCollection
                .Select(port => new Tuple< NodeData, string, string >(
                    nodeInfo.SchemerFactory.GetNode(port.NextID),
                    port.NextName,
                    port.ActorID))
                .Where(tuple => tuple.Item1 != null)
                .OrderBy(tuple => tuple.Item1.Position.y)
                .ToList();

            foreach ( var nodeData in executeList ) {
                if ( !nodeInfo.SchemerFactory.ActiveNodeList.Contains(nodeInfo) )
                    break;

                var nextNodeInfo = GenerateNodeData(nodeData.Item1);
                nextNodeInfo.inputName = nodeData.Item2;
                nextNodeInfo.SchemerFactory = nodeInfo.SchemerFactory;
                nextNodeInfo.actor = ( schemer.GetVariable(nodeData.Item3) as NActor )?.Value;
                nextNodeInfo.sequencer = nodeInfo.sequencer;
                nextNodeInfo.repeater = nodeInfo.repeater;
                nextNodeInfo.bgWorker = nodeInfo.bgWorker;
                nextNodeInfo.validator = nodeInfo.validator;

                if ( nextNodeInfo.inputName == "[Actor]" ) nextNodeInfo.actor = nodeInfo.actor;
                else if ( nextNodeInfo.inputName == "[Dual]" ) nextNodeInfo.dualActor = nodeInfo.dualActor;
                else if ( nextNodeInfo.inputName == "[Clan]" ) nextNodeInfo.clan = nodeInfo.clan;
                else if ( nextNodeInfo.inputName == "[Family]" ) nextNodeInfo.family = nodeInfo.family;

                if ( nodeData.Item2.Equals("[STOP]") ) {
                    nodeInfo.SchemerFactory.StopDelay(nodeData.Item1);
                    continue;
                }

                await nodeData.Item1.Run(schemer, nextNodeInfo);

                if ( nodeData.Item1 is ContinueData )
                    break;
            }

            if ( end ) End(nodeInfo);
        }


        protected async Task Next(Schemer schemer, OutputData output, NodeInfo nodeInfo, int index = 0) {
            if ( !nodeInfo.SchemerFactory.Scheme.IsValid ) {
                schemer.Kill(true);
                return;
            }

            if ( nodeInfo.SchemerFactory.Validator != null && !schemer.IsEnded &&
                 nodeInfo.SchemerFactory.ActiveNodeList.All(a => a.validator == null) ) {
                var result = nodeInfo.SchemerFactory.Validator.Invoke();
                if ( result != Schemer.SchemerFactory.ValidatorResult.Success ) {
                    nodeInfo.SchemerFactory.GoValidation(result == Schemer.SchemerFactory.ValidatorResult.FailedBreak);
                    if ( result == Schemer.SchemerFactory.ValidatorResult.FailedBreak )
                        return;
                }
            }

            if ( output == null ) {
                End(nodeInfo);
                return;
            }

            var line = output.DataCollection.ElementAtOrDefault(index);
            if ( line == null ) {
                End(nodeInfo);
                return;
            }

            var nextNode = nodeInfo.SchemerFactory.GetNode(line.NextID);
            if ( nextNode == null ) {
                End(nodeInfo);
                return;
            }

            var nextNodeInfo = GenerateNodeData(nextNode);
            nextNodeInfo.inputName = line.NextName;
            nextNodeInfo.SchemerFactory = nodeInfo.SchemerFactory;
            nextNodeInfo.actor = ( schemer.GetVariable(line.ActorID) as NActor )?.Value;
            nextNodeInfo.sequencer = nodeInfo.sequencer;
            nextNodeInfo.repeater = nodeInfo.repeater;
            nextNodeInfo.bgWorker = nodeInfo.bgWorker;
            nextNodeInfo.validator = nodeInfo.validator;

            if ( line.NextName == "[STOP]" ) {
                nodeInfo.SchemerFactory.StopDelay(nextNode);
                End(nodeInfo);
                return;
            }

            switch ( nextNodeInfo.inputName ) {
                case "[Actor]":
                    nextNodeInfo.actor = nodeInfo.actor;
                    break;
                case "[Dual]":
                    nextNodeInfo.dualActor = nodeInfo.dualActor;
                    break;
                case "[Clan]":
                    nextNodeInfo.clan = nodeInfo.clan;
                    break;
                case "[Family]":
                    nextNodeInfo.family = nodeInfo.family;
                    break;
            }

            await nextNode.Run(schemer, nextNodeInfo);

            End(nodeInfo);
        }

        #endregion

        #region RULER

        public virtual Task Run(Ruler ruler, NodeInfo nodeInfo) {
            nodeInfo.RulerFactory.ExecuteNode(nodeInfo);
            return Task.CompletedTask;
        }

        protected void End(Ruler ruler, NodeInfo nodeInfo) {
            nodeInfo.RulerFactory.KillNode(nodeInfo);
        }

        protected async Task Next(Ruler ruler, IEnumerable< OutputData > outputs, NodeInfo nodeInfo, int index = 0,
            bool end = true) {
            if ( ruler.Conspirator == null ) {
                nodeInfo.RulerFactory.Kill();
                return;
            }

            var output = outputs.ElementAtOrDefault(index);
            if ( output == null ) return;
            var executeList = ( from port in output.DataCollection
                let next = nodeInfo.RulerFactory.GetNode(port.NextID)
                where next != null
                select new Tuple< NodeData, string >(next, port.NextName) ).ToList();

            foreach ( var nodeData in executeList.OrderBy(n => n.Item1.Position.y) ) {
                var nodeInf = GenerateNodeData(nodeData.Item1);
                nodeInf.inputName = nodeData.Item2;
                switch ( nodeInf.inputName ) {
                    case "[Actor]":
                        nodeInf.actor = nodeInfo.actor;
                        break;
                    case "[Dual]":
                        nodeInf.dualActor = nodeInfo.dualActor;
                        break;
                    case "[Clan]":
                        nodeInf.clan = nodeInfo.clan;
                        break;
                    case "[Family]":
                        nodeInf.family = nodeInfo.family;
                        break;
                }

                nodeInf.RulerFactory = nodeInfo.RulerFactory;

                await nodeData.Item1.Run(ruler, nodeInf);
            }

            if ( end )
                End(ruler, nodeInfo);
        }

        #endregion
    }

    #region NEXUS

    [Serializable]
    public class StartData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public StartData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class EndData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public EndData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            //Any
            await Next(schemer, Outputs, generatedNodeInfo, 3, false);

            //Result
            await Next(schemer, Outputs, generatedNodeInfo, (int)schemer.Result, false);

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class DialogueData : NodeData {
        [SerializeReference] public string Title;

        [TextArea] [SerializeReference] public string Content;

        [SerializeReference] public bool Break;
        [SerializeReference] public float Time;
        [SerializeReference] public bool TypeWriter;
        [SerializeReference] public Sprite Background;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public DialogueData(string id, string groupId, Vector2 position, string title, string content, bool @break,
            bool typeWriter,
            float time, Sprite background, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Title = title;
            Content = content;
            Break = @break;
            TypeWriter = typeWriter;
            Time = time;
            Background = background;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            generatedNodeInfo.SchemerFactory.InitializeStaticNodes();

            var target = generatedNodeInfo.actor;
            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var _outputs = new List< OutputData >();
            for ( int i = 1; i < Outputs.Count; i++ ) {
                _outputs.Add(Outputs[ i ]);
            }

            float time = generatedNodeInfo.time > 0 ? generatedNodeInfo.time : Time;

            //Choice Data
            foreach ( var outputData in _outputs ) {
                foreach ( var node in outputData.DataCollection.Select(portData =>
                             generatedNodeInfo.SchemerFactory.GetNode(portData.NextID)) ) {
                    ChanceData linkedChanceData = null;

                    if ( node is not ChanceData chanceData ) {
                        if ( node is JumpData jumpData ) {
                            var linked = (LinkedData)generatedNodeInfo.SchemerFactory.GetNode(jumpData.LinkID);
                            foreach ( var linkData in linked.Outputs ) {
                                foreach ( var nd in linkData.DataCollection.Select(portData =>
                                             generatedNodeInfo.SchemerFactory.GetNode(portData.NextID)) ) {
                                    if ( nd is not ChanceData cData ) continue;
                                    linkedChanceData = cData;
                                    break;
                                }
                            }
                        }

                        if ( linkedChanceData == null && node is RepeaterData repeaterData ) {
                            foreach ( var nd in repeaterData.Outputs[ 0 ].DataCollection.Select(portData =>
                                         generatedNodeInfo.SchemerFactory.GetNode(portData.NextID)) ) {
                                if ( nd is not ChanceData cData ) continue;
                                linkedChanceData = cData;
                                break;
                            }
                        }

                        //

                        if ( node is ChoiceTextData choiceTextData ) {
                            if ( linkedChanceData == null && !string.IsNullOrEmpty(choiceTextData.ChanceID) ) {
                                var chNode =
                                    (ChanceData)generatedNodeInfo.SchemerFactory.GetNode(choiceTextData.ChanceID);
                                if ( chNode != null )
                                    linkedChanceData = chNode;
                            }

                            if ( outputData.ChoiceData != null ) {
                                outputData.ChoiceData.Text1 =
                                    choiceTextData.Text.SchemeFormat(schemer.Conspirator, schemer.Target);
                                outputData.ChoiceData.Text2 =
                                    choiceTextData.Text2.SchemeFormat(schemer.Conspirator, schemer.Target);
                            } else {
                                outputData.ChoiceData = new ChoiceData() {
                                    Text1 = choiceTextData.Text.SchemeFormat(schemer.Conspirator,
                                        schemer.Target),
                                    Text2 = choiceTextData.Text2.SchemeFormat(schemer.Conspirator,
                                        schemer.Target)
                                };
                            }
                        }

                        if ( linkedChanceData != null ) {
                            var chanceWithBonus = linkedChanceData.ChanceWithBonus(generatedNodeInfo.SchemerFactory);
                            if ( outputData.ChoiceData != null ) {
                                outputData.ChoiceData.Rate = new Rate()
                                    { FailRate = 100f - chanceWithBonus, SuccessRate = chanceWithBonus };
                            } else {
                                outputData.ChoiceData = new ChoiceData() {
                                    Rate = new Rate()
                                        { FailRate = 100f - chanceWithBonus, SuccessRate = chanceWithBonus }
                                };
                            }

                            // break;
                        }

                        continue;
                    }

                    var bonus = chanceData.ChanceWithBonus(generatedNodeInfo.SchemerFactory);
                    if ( outputData.ChoiceData != null ) {
                        outputData.ChoiceData.Rate = new Rate() { FailRate = 100f - bonus, SuccessRate = bonus };
                    } else {
                        outputData.ChoiceData = new ChoiceData()
                            { Rate = new Rate() { FailRate = 100f - bonus, SuccessRate = bonus } };
                    }

                    // break;
                }
            }

            var title = Title;
            var content = Content;
            var choices = _outputs.Select(output => output.Name).ToList();

            #region PATTERNS

            //Localisation Patterns
            foreach ( Match titleLocalisation in Regex.Matches(title, STATIC.LOCALISATION) )
                if ( titleLocalisation.Success )
                    title = Regex.Replace(title, "{l:" + titleLocalisation.Value + "}",
                        IM.GetText(titleLocalisation.Value));

            foreach ( Match contentLocalisation in Regex.Matches(content, STATIC.LOCALISATION) )
                if ( contentLocalisation.Success )
                    content = Regex.Replace(content, "{l:" + contentLocalisation.Value + "}",
                        IM.GetText(contentLocalisation.Value));

            for ( var i = 0; i < choices.Count; i++ )
                foreach ( Match choiceLocalisation in Regex.Matches(choices[ i ], STATIC.LOCALISATION) )
                    if ( choiceLocalisation.Success )
                        choices[ i ] = Regex.Replace(choices[ i ], "{l:" + choiceLocalisation.Value + "}",
                            IM.GetText(choiceLocalisation.Value));

            //Table Patterns
            foreach ( Match titlePattern in Regex.Matches(title, STATIC.TABLE_VARIABLE) )
                if ( titlePattern.Success )
                    title = Regex.Replace(title, "{table:" + titlePattern.Value + "}",
                        (string)schemer.GetVariable(titlePattern.Value) ?? string.Empty);

            foreach ( Match contentPattern in Regex.Matches(content, STATIC.TABLE_VARIABLE) )
                if ( contentPattern.Success )
                    content = Regex.Replace(content, "{table:" + contentPattern.Value + "}",
                        (string)schemer.GetVariable(contentPattern.Value) ?? string.Empty);

            for ( var i = 0; i < choices.Count; i++ )
                foreach ( Match choicePattern in Regex.Matches(choices[ i ], STATIC.TABLE_VARIABLE) )
                    if ( choicePattern.Success )
                        choices[ i ] = Regex.Replace(choices[ i ], "{table:" + choicePattern.Value + "}",
                            (string)schemer.GetVariable(choicePattern.Value) ?? string.Empty);

            //Actor Default Patterns
            title = title.SchemeFormat(schemer.Conspirator, schemer.Target);
            content = content.SchemeFormat(schemer.Conspirator, schemer.Target);
            content = content.ManipulateString(nodeInfo.SchemerFactory.Scheme);

            for ( var i = 0; i < choices.Count; i++ )
                choices[ i ] = choices[ i ].SchemeFormat(schemer.Conspirator, schemer.Target);

            //Global Variable Patterns
            foreach ( Match titlePattern in Regex.Matches(title, STATIC.GLOBAL_VARIABLE) )
                if ( titlePattern.Success )
                    title = Regex.Replace(title, "{g:" + titlePattern.Value + "}",
                        (string)IM.GetVariable(titlePattern.Value));

            foreach ( Match contentPattern in Regex.Matches(content, STATIC.GLOBAL_VARIABLE) )
                if ( contentPattern.Success )
                    content = Regex.Replace(content, "{g:" + contentPattern.Value + "}",
                        (string)IM.GetVariable(contentPattern.Value));

            for ( var i = 0; i < choices.Count; i++ )
                foreach ( Match choicePattern in Regex.Matches(choices[ i ], STATIC.GLOBAL_VARIABLE) )
                    if ( choicePattern.Success )
                        choices[ i ] = Regex.Replace(choices[ i ], "{g:" + choicePattern.Value + "}",
                            (string)IM.GetVariable(choicePattern.Value));

            #endregion

            // bool isTarget = target == schemer.Target;

            void TimeOut() {
                if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                    return;
                }

                _ = Next(schemer, Outputs[ 0 ], generatedNodeInfo);
            }

            //---
            if ( IM.IsPlayer(target) ) {
                if ( _outputs.Count > 1 ) {
                    var dialogue = DialogueManager.OpenDialogue(title, content, Break || _outputs.Count > 1 ? time : 0,
                        TypeWriter, f => generatedNodeInfo.time = f, true);

                    if ( Background != null ) {
                        dialogue.SetBackground(Background).SetNativeSize();
                    }

                    dialogue.onTimeout += TimeOut;

                    nodeInfo.SchemerFactory.Dialogues.Add(dialogue);

                    // if (!isTarget) dialogue.AddProfile(schemer.Target);

                    var i = 0;
                    do {
                        var l = i;
                        dialogue.AddChoice(choices[ l ].ClearChoicePatterns(), _outputs[ l ].Sprite,
                            () => _ = Next(schemer, _outputs, generatedNodeInfo, l),
                            choices[ l ].PatternExists() ? () => choices[ l ].If(nodeInfo.SchemerFactory.Scheme) : null,
                            true,
                            _outputs[ l ].HideIfDisable,
                            _outputs[ l ].ChoiceData);
                        i++;
                    } while ( i < choices.Count );

                    bool voiceCalled = false;

                    if ( nodeInfo.SchemerFactory.ClassList.ContainsKey(this) ) {
                        foreach ( var sc in nodeInfo.SchemerFactory.ClassList[ this ] ) {
                            switch ( sc ) {
                                case SoundClassData soundClassData: {
                                    var (soundId, soundSource) = dialogue.AddSound(soundClassData);

                                    if ( soundSource == null ) break;

                                    dialogue.onDialogueHide += () => {
                                        if ( soundSource != null ) {
                                            soundSource.Pause();
                                        }
                                    };

                                    dialogue.onDialogueShow += () => {
                                        if ( soundSource != null ) {
                                            soundSource.UnPause();
                                        }
                                    };

                                    if ( soundClassData.StopWhenClosed || soundClassData.Loop ) {
                                        dialogue.onDialogueClose += () => {
                                            if ( soundSource != null ) {
                                                IM.AudioFade(soundSource, soundClassData.FadeOut, soundSource.volume,
                                                    0f, () => {
                                                        if ( soundSource != null ) {
                                                            IM.RemoveAudio(soundId);
                                                        }
                                                    });
                                            }
                                        };
                                    }

                                    break;
                                }
                                case VoiceClassData voiceClassData: {
                                    if ( voiceCalled ) break;

                                    voiceCalled = true;

                                    var (soundId, voiceSource) = dialogue.AddSound(voiceClassData);

                                    if ( voiceSource == null ) break;

                                    if ( voiceClassData.Sync ) {
                                        dialogue.SetTypeWriterDuration(voiceSource.clip.length);
                                    }

                                    dialogue.onDialogueHide += () => {
                                        if ( voiceSource != null ) {
                                            voiceSource.Pause();
                                        }
                                    };

                                    dialogue.onDialogueShow += () => {
                                        if ( voiceSource != null ) {
                                            voiceSource.UnPause();
                                        }
                                    };

                                    dialogue.onDialogueClose += () => {
                                        IM.RemoveAudio(soundId, true);
                                        if ( voiceSource != null ) {
                                            IM.AudioFade(voiceSource, 1f, voiceSource.volume,
                                                0f, () => {
                                                    if ( voiceSource != null ) {
                                                        UnityEngine.Object.Destroy(voiceSource.gameObject);
                                                    }
                                                });
                                        }
                                    };
                                    break;
                                }
                            }
                        }
                    }
                } else {
                    switch ( Break ) {
                        case true: {
                            var dialogue = DialogueManager.OpenDialogue(title, content,
                                    Break || _outputs.Count > 1 ? time : 0, TypeWriter, f => generatedNodeInfo.time = f,
                                    true)
                                .AddChoice(
                                    choices[ 0 ].ClearChoicePatterns(), _outputs[ 0 ].Sprite,
                                    () => { _ = Next(schemer, _outputs, generatedNodeInfo); },
                                    choices[ 0 ].PatternExists()
                                        ? () => choices[ 0 ].If(nodeInfo.SchemerFactory.Scheme)
                                        : null, true,
                                    _outputs[ 0 ].HideIfDisable,
                                    _outputs[ 0 ].ChoiceData);

                            if ( Background != null ) {
                                dialogue.SetBackground(Background).SetNativeSize();
                            }

                            dialogue.onTimeout += TimeOut;

                            nodeInfo.SchemerFactory.Dialogues.Add(dialogue);

                            // if (!isTarget) dialogue.AddProfile(schemer.Target);

                            bool voiceCalled = false;

                            if ( nodeInfo.SchemerFactory.ClassList.ContainsKey(this) ) {
                                foreach ( var sc in nodeInfo.SchemerFactory.ClassList[ this ] ) {
                                    switch ( sc ) {
                                        case SoundClassData soundClassData: {
                                            var (soundId, soundSource) = dialogue.AddSound(soundClassData);

                                            if ( soundSource == null ) break;

                                            dialogue.onDialogueHide += () => {
                                                if ( soundSource != null ) {
                                                    soundSource.Pause();
                                                }
                                            };

                                            dialogue.onDialogueShow += () => {
                                                if ( soundSource != null ) {
                                                    soundSource.UnPause();
                                                }
                                            };

                                            if ( soundClassData.StopWhenClosed || soundClassData.Loop ) {
                                                dialogue.onDialogueClose += () => {
                                                    if ( soundSource != null ) {
                                                        IM.AudioFade(soundSource, soundClassData.FadeOut,
                                                            soundSource.volume,
                                                            0f, () => {
                                                                if ( soundSource != null ) {
                                                                    IM.RemoveAudio(soundId);
                                                                }
                                                            });
                                                    }
                                                };
                                            }

                                            break;
                                        }
                                        case VoiceClassData voiceClassData: {
                                            if ( voiceCalled ) break;

                                            voiceCalled = true;

                                            var (soundId, voiceSource) = dialogue.AddSound(voiceClassData);

                                            if ( voiceSource == null ) break;

                                            if ( voiceClassData.Sync ) {
                                                dialogue.SetTypeWriterDuration(voiceSource.clip.length);
                                            }

                                            dialogue.onDialogueHide += () => {
                                                if ( voiceSource != null ) {
                                                    voiceSource.Pause();
                                                }
                                            };

                                            dialogue.onDialogueShow += () => {
                                                if ( voiceSource != null ) {
                                                    voiceSource.UnPause();
                                                }
                                            };

                                            dialogue.onDialogueClose += () => {
                                                IM.RemoveAudio(soundId, true);
                                                if ( voiceSource != null ) {
                                                    IM.AudioFade(voiceSource, 1f, voiceSource.volume,
                                                        0f, () => {
                                                            if ( voiceSource != null ) {
                                                                UnityEngine.Object.Destroy(voiceSource.gameObject);
                                                            }
                                                        });
                                                }
                                            };
                                            break;
                                        }
                                    }
                                }
                            }

                            break;
                        }
                        case false: {
                            var dialogue = DialogueManager.OpenDialogue(title, content,
                                    Break || _outputs.Count > 1 ? time : 0, TypeWriter, f => generatedNodeInfo.time = f,
                                    true)
                                .AddChoice(choices[ 0 ].ClearChoicePatterns(), _outputs[ 0 ].Sprite, null,
                                    choices[ 0 ].PatternExists()
                                        ? () => choices[ 0 ].If(nodeInfo.SchemerFactory.Scheme)
                                        : null, true,
                                    _outputs[ 0 ].HideIfDisable, _outputs[ 0 ].ChoiceData);

                            if ( Background != null ) {
                                dialogue.SetBackground(Background).SetNativeSize();
                            }

                            dialogue.onTimeout += TimeOut;

                            nodeInfo.SchemerFactory.Dialogues.Add(dialogue);

                            // if (!isTarget) dialogue.AddProfile(schemer.Target);

                            await Next(schemer, _outputs, generatedNodeInfo);

                            bool voiceCalled = false;

                            if ( nodeInfo.SchemerFactory.ClassList.ContainsKey(this) ) {
                                foreach ( var sc in nodeInfo.SchemerFactory.ClassList[ this ] ) {
                                    switch ( sc ) {
                                        case SoundClassData soundClassData: {
                                            var (soundId, soundSource) = dialogue.AddSound(soundClassData);

                                            if ( soundSource == null ) break;

                                            dialogue.onDialogueHide += () => {
                                                if ( soundSource != null ) {
                                                    soundSource.Pause();
                                                }
                                            };

                                            dialogue.onDialogueShow += () => {
                                                if ( soundSource != null ) {
                                                    soundSource.UnPause();
                                                }
                                            };

                                            if ( soundClassData.StopWhenClosed || soundClassData.Loop ) {
                                                dialogue.onDialogueClose += () => {
                                                    if ( soundSource != null ) {
                                                        IM.AudioFade(soundSource, soundClassData.FadeOut,
                                                            soundSource.volume,
                                                            0f, () => {
                                                                if ( soundSource != null ) {
                                                                    IM.RemoveAudio(soundId);
                                                                }
                                                            });
                                                    }
                                                };
                                            }

                                            break;
                                        }
                                        case VoiceClassData voiceClassData: {
                                            if ( voiceCalled ) break;

                                            voiceCalled = true;

                                            var (soundId, voiceSource) = dialogue.AddSound(voiceClassData);

                                            if ( voiceSource == null ) break;

                                            if ( voiceClassData.Sync ) {
                                                dialogue.SetTypeWriterDuration(voiceSource.clip.length);
                                            }

                                            dialogue.onDialogueHide += () => {
                                                if ( voiceSource != null ) {
                                                    voiceSource.Pause();
                                                }
                                            };

                                            dialogue.onDialogueShow += () => {
                                                if ( voiceSource != null ) {
                                                    voiceSource.UnPause();
                                                }
                                            };

                                            dialogue.onDialogueClose += () => {
                                                IM.RemoveAudio(soundId, true);
                                                if ( voiceSource != null ) {
                                                    IM.AudioFade(voiceSource, 1f, voiceSource.volume,
                                                        0f, () => {
                                                            if ( voiceSource != null ) {
                                                                UnityEngine.Object.Destroy(voiceSource.gameObject);
                                                            }
                                                        });
                                                }
                                            };
                                            break;
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    }
                }

                return;
            }

            var _outputsWithCondition = _outputs.Where(o => o.Name.If(nodeInfo.SchemerFactory.Scheme)).ToList();
            var primaries = _outputsWithCondition.Where(o => o.Primary).ToList();

            if ( primaries.Count > 0 ) {
                var rand = UnityEngine.Random.Range(0, primaries.Count);

                await Next(schemer, _outputs, generatedNodeInfo, _outputs.IndexOf(primaries[ rand ]));
            } else {
                var rand = UnityEngine.Random.Range(0, _outputsWithCondition.Count);

                await Next(schemer, _outputs, generatedNodeInfo, _outputs.IndexOf(_outputsWithCondition[ rand ]));
            }

            NDebug.Log(STATIC.DEBUG_AI_SELECTED_DIALOGUE);
        }
    }

    [Serializable]
    public class GlobalMessageData : NodeData {
        [SerializeReference] public string Title;

        [TextArea] [SerializeReference] public string Content;
        [SerializeReference] public bool TypeWriter;
        [SerializeReference] public Sprite Background;

        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GlobalMessageData(string id, string groupId, Vector2 position, string title, string content,
            bool typeWriter, Sprite background,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Title = title;
            Content = content;
            TypeWriter = typeWriter;
            Background = background;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            foreach ( var outputData in Outputs ) {
                foreach ( var node in outputData.DataCollection.Select(portData =>
                             generatedNodeInfo.SchemerFactory.GetNode(portData.NextID)) ) {
                    if ( node is ChoiceTextData choiceTextData ) {
                        if ( outputData.ChoiceData != null ) {
                            outputData.ChoiceData.Text1 =
                                choiceTextData.Text.SchemeFormat(schemer.Conspirator, schemer.Target);
                            outputData.ChoiceData.Text2 =
                                choiceTextData.Text2.SchemeFormat(schemer.Conspirator, schemer.Target);
                        } else {
                            outputData.ChoiceData = new ChoiceData() {
                                Text1 = choiceTextData.Text.SchemeFormat(schemer.Conspirator,
                                    schemer.Target),
                                Text2 = choiceTextData.Text2.SchemeFormat(schemer.Conspirator,
                                    schemer.Target)
                            };
                        }

                        break;
                    }
                }
            }

            var title = Title;
            var content = Content;
            var choices = Outputs.Select(output => output.Name).ToList();

            #region PATTERNS

            //Localisation Patterns
            foreach ( Match titleLocalisation in Regex.Matches(title, STATIC.LOCALISATION) )
                if ( titleLocalisation.Success )
                    title = Regex.Replace(title, "{l:" + titleLocalisation.Value + "}",
                        IM.GetText(titleLocalisation.Value));

            foreach ( Match contentLocalisation in Regex.Matches(content, STATIC.LOCALISATION) )
                if ( contentLocalisation.Success )
                    content = Regex.Replace(content, "{l:" + contentLocalisation.Value + "}",
                        IM.GetText(contentLocalisation.Value));

            for ( var i = 0; i < choices.Count; i++ )
                foreach ( Match choiceLocalisation in Regex.Matches(choices[ i ], STATIC.LOCALISATION) )
                    if ( choiceLocalisation.Success )
                        choices[ i ] = Regex.Replace(choices[ i ], "{l:" + choiceLocalisation.Value + "}",
                            IM.GetText(choiceLocalisation.Value));

            //Table Patterns
            foreach ( Match titlePattern in Regex.Matches(title, STATIC.TABLE_VARIABLE) )
                if ( titlePattern.Success )
                    title = Regex.Replace(title, "{table:" + titlePattern.Value + "}",
                        (string)schemer.GetVariable(titlePattern.Value) ?? string.Empty);

            foreach ( Match contentPattern in Regex.Matches(content, STATIC.TABLE_VARIABLE) )
                if ( contentPattern.Success )
                    content = Regex.Replace(content, "{table:" + contentPattern.Value + "}",
                        (string)schemer.GetVariable(contentPattern.Value) ?? string.Empty);

            for ( var i = 0; i < choices.Count; i++ )
                foreach ( Match choicePattern in Regex.Matches(choices[ i ], STATIC.TABLE_VARIABLE) )
                    if ( choicePattern.Success )
                        choices[ i ] = Regex.Replace(choices[ i ], "{table:" + choicePattern.Value + "}",
                            (string)schemer.GetVariable(choicePattern.Value) ?? string.Empty);

            //Actor Default Patterns
            title = title.SchemeFormat(schemer.Conspirator, schemer.Target);
            content = content.SchemeFormat(schemer.Conspirator, schemer.Target);

            for ( var i = 0; i < choices.Count; i++ )
                choices[ i ] = choices[ i ].SchemeFormat(schemer.Conspirator, schemer.Target);

            //Global Variable Patterns
            foreach ( Match titlePattern in Regex.Matches(title, STATIC.GLOBAL_VARIABLE) )
                if ( titlePattern.Success )
                    title = Regex.Replace(title, "{g:" + titlePattern.Value + "}",
                        (string)IM.GetVariable(titlePattern.Value));

            foreach ( Match contentPattern in Regex.Matches(content, STATIC.GLOBAL_VARIABLE) )
                if ( contentPattern.Success )
                    content = Regex.Replace(content, "{g:" + contentPattern.Value + "}",
                        (string)IM.GetVariable(contentPattern.Value));

            for ( var i = 0; i < choices.Count; i++ )
                foreach ( Match choicePattern in Regex.Matches(choices[ i ], STATIC.GLOBAL_VARIABLE) )
                    if ( choicePattern.Success )
                        choices[ i ] = Regex.Replace(choices[ i ], "{g:" + choicePattern.Value + "}",
                            (string)IM.GetVariable(choicePattern.Value));

            #endregion

            var dialogue =
                DialogueManager.OpenDialogue(title, content, 0f, true, null, true);

            if ( Background != null ) {
                dialogue.SetBackground(Background);
            }

            bool voiceCalled = false;

            if ( generatedNodeInfo.SchemerFactory.ClassList.ContainsKey(this) ) {
                foreach ( var sc in generatedNodeInfo.SchemerFactory.ClassList[ this ] ) {
                    switch ( sc ) {
                        case SoundClassData soundClassData: {
                            var (soundId, soundSource) = dialogue.AddSound(soundClassData);

                            if ( soundSource == null ) break;

                            dialogue.onDialogueHide += () => {
                                if ( soundSource != null ) {
                                    soundSource.Pause();
                                }
                            };

                            dialogue.onDialogueShow += () => {
                                if ( soundSource != null ) {
                                    soundSource.UnPause();
                                }
                            };

                            if ( soundClassData.StopWhenClosed || soundClassData.Loop ) {
                                dialogue.onDialogueClose += () => {
                                    if ( soundSource != null ) {
                                        IM.AudioFade(soundSource, soundClassData.FadeOut, soundSource.volume,
                                            0f, () => {
                                                if ( soundSource != null ) {
                                                    IM.RemoveAudio(soundId);
                                                }
                                            });
                                    }
                                };
                            }

                            break;
                        }
                        case VoiceClassData voiceClassData: {
                            if ( voiceCalled ) break;

                            voiceCalled = true;

                            var (soundId, voiceSource) = dialogue.AddSound(voiceClassData);

                            if ( voiceSource == null ) break;

                            if ( voiceClassData.Sync ) {
                                dialogue.SetTypeWriterDuration(voiceSource.clip.length);
                            }

                            dialogue.onDialogueHide += () => {
                                if ( voiceSource != null ) {
                                    voiceSource.Pause();
                                }
                            };

                            dialogue.onDialogueShow += () => {
                                if ( voiceSource != null ) {
                                    voiceSource.UnPause();
                                }
                            };

                            dialogue.onDialogueClose += () => {
                                IM.RemoveAudio(soundId, true);
                                if ( voiceSource != null ) {
                                    IM.AudioFade(voiceSource, 1f, voiceSource.volume,
                                        0f, () => {
                                            if ( voiceSource != null ) {
                                                UnityEngine.Object.Destroy(voiceSource.gameObject);
                                            }
                                        });
                                }
                            };
                            break;
                        }
                    }
                }
            }

            generatedNodeInfo.SchemerFactory.Dialogues.Add(dialogue);

            for ( int j = 0; j < choices.Count; j++ ) {
                dialogue.AddChoice(choices[ j ], Outputs[ j ].Sprite, null);
            }

            // dialogue.AddProfile(schemer.Target);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class ObjectiveData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public string Objective;

        public ObjectiveData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs,
            string objective) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);

            Objective = objective;
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);
            IM.SystemMethods[ "Scheme_SetObjective" ]
                .Invoke(generatedNodeInfo.SchemerFactory.Scheme, new object[ ] { Objective });
            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class ClanMemberData : NodeData {
        [SerializeReference] public string ActorID;
        [SerializeReference] public string RoleID;
        public override GenericNodeType GenericType => GenericNodeType.Clan;

        public ClanMemberData(string id, string groupId, Vector2 position, string actorID, string roleId) {
            ID = id;
            GroupId = groupId;
            Position = position;
            ActorID = actorID;
            RoleID = roleId;
        }
    }

    [Serializable]
    public class FamilyMemberData : NodeData {
        [SerializeReference] public string ActorID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Family;

        public FamilyMemberData(string id, string groupId, Vector2 position, string actorID,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            ActorID = actorID;
            Outputs = new List< OutputData >(outputs);
        }
    }

    [Serializable]
    public class WaitData : NodeData {
        [SerializeReference] public float Delay;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public WaitData(string id, string groupId, Vector2 position, float delay, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Delay = delay;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            float delay = generatedNodeInfo.time > 0 ? generatedNodeInfo.time : Delay;

            NDebug.Log($"Waiting {delay} seconds..");

            var id = NullUtils.GenerateID();
            generatedNodeInfo.delays.Add(id);
            generatedNodeInfo.SchemerFactory.Delays.Add(id);
            NullUtils.DelayedCall(new DelayedCallParams {
                DelayName = id,
                WaitUntil = () => !schemer.IsPaused,
                Delay = delay,
                Call = () => {
                    generatedNodeInfo.SchemerFactory.Delays.Remove(id);
                    _ = Next(schemer, Outputs, generatedNodeInfo);
                },
                OnUpdate = (f) => {
                    generatedNodeInfo.time = f;
                }
            });
        }
    }

    [Serializable]
    public class WaitRandomData : NodeData {
        [SerializeReference] public float Min;
        [SerializeReference] public float Max;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public WaitRandomData(string id, string groupId, Vector2 position, float min, float max,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Min = min;
            Max = max;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            float delay = generatedNodeInfo.time > 0 ? generatedNodeInfo.time : UnityEngine.Random.Range(Min, Max);

            NDebug.Log($"Waiting {delay:F1} seconds..");

            var id = NullUtils.GenerateID();
            generatedNodeInfo.delays.Add(id);
            generatedNodeInfo.SchemerFactory.Delays.Add(id);
            NullUtils.DelayedCall(new DelayedCallParams {
                DelayName = id,
                WaitUntil = () => !schemer.IsPaused,
                Delay = delay,
                Call = () => {
                    generatedNodeInfo.SchemerFactory.Delays.Remove(id);
                    _ = Next(schemer, Outputs, generatedNodeInfo);
                },
                OnUpdate = (f) => {
                    generatedNodeInfo.time = f;
                }
            });
            // NullUtils.DelayedCall(id, () => !schemer.IsPaused, delay, () =>
            // {
            //     generatedNodeInfo.SchemerFactory.Delays.Remove(id);
            //     await Next(schemer, Outputs, generatedNodeInfo);
            // }, f => { generatedNodeInfo.time = f; });
        }
    }

    [Serializable]
    public class ChanceData : NodeData {
        [SerializeReference] private float chance;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public float Chance => Mathf.Clamp(chance, 0f, 100f);

        public float ChanceWithBonus(Schemer.SchemerFactory factory) =>
            Mathf.Clamp(chance + factory.GetBonus(ID), 0f, 100f);

        public ChanceData(string id, string groupId, Vector2 position, float chance,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            this.chance = chance;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            generatedNodeInfo.SchemerFactory.InitializeStaticNodes();

            float randomValue = UnityEngine.Random.Range(0f, 100f);

            var index = randomValue <= ChanceWithBonus(generatedNodeInfo.SchemerFactory) ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class ChanceModifierData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public bool Positive;
        [SerializeReference] public bool Negative;
        [SerializeReference] public bool Opposite;
        [SerializeReference] public int Mode;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;


        public ChanceModifierData(string id, string groupId, Vector2 position, string variableId, bool positive,
            bool negative, bool opposite, int mode, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            Positive = positive;
            Negative = negative;
            Opposite = opposite;
            Mode = mode;
            Outputs = new List< OutputData >(outputs);
        }

        public override Task Run(Schemer schemer, NodeInfo nodeInfo) {
            //
            return Task.CompletedTask;
        }
    }

    [Serializable]
    public class RandomData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public RandomData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);
            var index = UnityEngine.Random.Range(0, Outputs[ 0 ].DataCollection.Count);
            await Next(schemer, Outputs[ 0 ], generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class SuccessData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SuccessData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);
            generatedNodeInfo.SchemerFactory.SetSchemeState(SchemeResult.Success);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class FailData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public FailData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);
            generatedNodeInfo.SchemerFactory.SetSchemeState(SchemeResult.Failed);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class InvokeData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public InvokeData(string id, string groupId, Vector2 position, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            IResult result = generatedNodeInfo.SchemerFactory.Scheme.Invoke(MethodName);

            await Next(schemer, Outputs, generatedNodeInfo, (int)result);
        }
    }

    [Serializable]
    public class LinkedData : NodeData {
        [SerializeReference] public string Name;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public LinkedData(string id, string groupId, Vector2 position, string name,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Name = name;
            Outputs = new List< OutputData >(outputs);
        }

        public override Task Run(Schemer schemer, NodeInfo nodeInfo) {
            //
            return Task.CompletedTask;
        }
    }

    [Serializable]
    public class JumpData : NodeData {
        [SerializeReference] public string LinkID;

        public JumpData(string id, string groupId, Vector2 position, string linkId) {
            ID = id;
            GroupId = groupId;
            Position = position;
            LinkID = linkId;
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            //
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var next = (LinkedData)generatedNodeInfo.SchemerFactory.GetNode(LinkID);

            if ( next == null ) {
                End(generatedNodeInfo);
                return;
            }

            await Next(schemer, next.Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SignalData : NodeData {
        [SerializeReference] public Signal Signal;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SignalData(string id, string groupId, Vector2 position, Signal signal,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Signal = signal;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);
            IM.Signal(Signal);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class TriggerData : NodeData {
        [SerializeReference] public string TriggerName;
        [SerializeReference] public bool Value;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public TriggerData(string id, string groupId, Vector2 position, string triggerName, bool value,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            TriggerName = triggerName;
            Value = value;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator,
                "Target" => schemer.Target,
                _ => null
            };

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( target == null )
                generatedNodeInfo.SchemerFactory.Scheme.Trigger(TriggerName, Value);
            else
                target.Trigger(TriggerName, Value);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class WaitTriggerData : NodeData {
        [SerializeReference] public string TriggerName;
        [SerializeReference] public float Timeout;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public WaitTriggerData(string id, string groupId, Vector2 position, string triggerName, float timeout,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            TriggerName = triggerName;
            Timeout = timeout;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var id = NullUtils.GenerateID();

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator,
                "Target" => schemer.Target,
                _ => null
            };

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( target == null )
                IM.onTrigger += OnTrigger;
            else
                target.OnTrigger += OnActorTrigger;

            void OnTrigger(Scheme scheme, string s, bool value) {
                if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                    IM.onTrigger -= OnTrigger;
                    return;
                }

                if ( schemer.IsPaused ) return;
                if ( scheme != null && scheme != generatedNodeInfo.SchemerFactory.Scheme ) return;

                if ( s != TriggerName ) return;

                NullUtils.StopCall(id);
                generatedNodeInfo.SchemerFactory.Delays.Remove(id);

                int index = value ? 0 : 1;
                _ = Next(schemer, Outputs, generatedNodeInfo, index);
                IM.onTrigger -= OnTrigger;
            }

            void OnActorTrigger(string s, bool value) {
                if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                    target.OnTrigger -= OnActorTrigger;
                    return;
                }

                if ( schemer.IsPaused ) return;
                if ( s != TriggerName ) return;

                NullUtils.StopCall(id);
                generatedNodeInfo.SchemerFactory.Delays.Remove(id);

                int index = value ? 0 : 1;
                _ = Next(schemer, Outputs, generatedNodeInfo, index);
                target.OnTrigger -= OnActorTrigger;
            }

            if ( !( Timeout > 0 ) ) return;

            generatedNodeInfo.SchemerFactory.Delays.Add(id);

            NullUtils.DelayedCall(new DelayedCallParams {
                DelayName = id,
                WaitUntil = () => !schemer.IsPaused,
                Delay = Timeout,
                Call = () => {
                    IM.onTrigger -= OnTrigger;
                    if ( target != null )
                        target.OnTrigger -= OnActorTrigger;

                    if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                        return;
                    }

                    generatedNodeInfo.SchemerFactory.Delays.Remove(id);
                    _ = Next(schemer, Outputs, generatedNodeInfo, 2);
                },
            });

            // NullUtils.DelayedCall(id, () => !schemer.IsPaused, Timeout, () =>
            // {
            //     IM.onTrigger -= OnTrigger;
            //     if (target != null)
            //         target.OnTrigger -= OnActorTrigger;
            //
            //     if (!schemer.ActiveNodeList.Contains(generatedNodeInfo))
            //     {
            //         return;
            //     }
            //
            //     generatedNodeInfo.SchemerFactory.Delays.Remove(id);
            //     await Next(schemer, Outputs, generatedNodeInfo, 2);
            // });
        }
    }

    [Serializable]
    public class GetVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public EnumType EnumType;

        public GetVariableData(string id, string groupId, Vector2 position, string variableId, string stringValue,
            int integerValue, float floatValue, EnumType enumType,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            EnumType = enumType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when schemer.Target == null:
                    End(generatedNodeInfo);
                    return;
                case "[Actor]" when generatedNodeInfo.actor == null:
                    End(generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator.GetVariable(VariableID),
                "Global" => IM.GetVariable(VariableID),
                "Target" => schemer.Target.GetVariable(VariableID),
                "[Actor]" => generatedNodeInfo.actor.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( IM.Variables.Any(v => v.id == VariableID) == false ) {
                NDebug.Log($"Variable({VariableType}) is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Enum ) {
                //Work in progress.
                if ( EnumType == EnumType.Is )
                    await Next(schemer, Outputs, generatedNodeInfo, (int)variable, false);
                else {
                    await Next(schemer, Outputs, generatedNodeInfo, (int)variable, false);
                }
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetClanVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public List< OutputData > Outputs;
        [SerializeReference] public EnumType EnumType;

        public GetClanVariableData(string id, string groupId, Vector2 position, string variableId, string stringValue,
            int integerValue, float floatValue, EnumType enumType,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            EnumType = enumType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when schemer.Target == null || schemer.Target.Clan == null:
                    End(generatedNodeInfo);
                    return;
                case "Conspirator" when schemer.Conspirator == null || schemer.Conspirator.Clan == null:
                    End(generatedNodeInfo);
                    return;
                case "[Clan]" when generatedNodeInfo.clan == null:
                    End(generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator.Clan.GetVariable(VariableID),
                "Target" => schemer.Target.Clan.GetVariable(VariableID),
                "[Clan]" => generatedNodeInfo.clan.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( IM.Variables.Any(v => v.id == VariableID) == false ) {
                NDebug.Log($"Variable({VariableType}) is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Enum ) {
                //Work in progress.
                if ( EnumType == EnumType.Is )
                    await Next(schemer, Outputs, generatedNodeInfo, (int)variable, false);
                else {
                    await Next(schemer, Outputs, generatedNodeInfo, (int)variable, false);
                }
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetFamilyVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public List< OutputData > Outputs;
        [SerializeReference] public EnumType EnumType;

        public GetFamilyVariableData(string id, string groupId, Vector2 position, string variableId, string stringValue,
            int integerValue, float floatValue, EnumType enumType,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            EnumType = enumType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);

            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when schemer.Target == null || schemer.Target.Family == null:
                    End(generatedNodeInfo);
                    return;
                case "Conspirator" when schemer.Conspirator == null || schemer.Conspirator.Family == null:
                    End(generatedNodeInfo);
                    return;
                case "[Family]" when generatedNodeInfo.family == null:
                    End(generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator.Family.GetVariable(VariableID),
                "Target" => schemer.Target.Family.GetVariable(VariableID),
                "[Family]" => generatedNodeInfo.family.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( IM.Variables.Any(v => v.id == VariableID) == false ) {
                NDebug.Log($"Variable({VariableType}) is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Enum ) {
                //Work in progress.
                if ( EnumType == EnumType.Is )
                    await Next(schemer, Outputs, generatedNodeInfo, (int)variable, false);
                else {
                    await Next(schemer, Outputs, generatedNodeInfo, (int)variable, false);
                }
            }

            NDebug.Log(variable.value);

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetFamilyVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public string StringValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public MathOperation Operation;
        [SerializeReference] public UnityEngine.Object ObjectValue;
        [SerializeReference] public List< OutputData > Outputs;


        public SetFamilyVariableData(string id, string groupId, Vector2 position, string variableId, string stringValue,
            int integerValue, float floatValue,
            UnityEngine.Object objectValue, MathOperation operation, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            ObjectValue = objectValue;
            Operation = operation;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when schemer.Target == null || schemer.Target.Family == null:
                    End(generatedNodeInfo);
                    return;
                case "Conspirator" when schemer.Conspirator == null || schemer.Conspirator.Family == null:
                    End(generatedNodeInfo);
                    return;
                case "[Family]" when generatedNodeInfo.family == null:
                    End(generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator.Family.GetVariable(VariableID),
                "Target" => schemer.Target.Family.GetVariable(VariableID),
                "[Family]" => generatedNodeInfo.family.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType == NType.String ) {
                var value = StringValue;

                #region PATTERNS

                //Localisation Patterns
                foreach ( Match titleLocalisation in Regex.Matches(value, STATIC.LOCALISATION) )
                    if ( titleLocalisation.Success )
                        value = Regex.Replace(value, "{l:" + titleLocalisation.Value + "}",
                            IM.GetText(titleLocalisation.Value));

                //Table Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.TABLE_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{table:" + titlePattern.Value + "}",
                            (string)schemer.GetVariable(titlePattern.Value) ?? string.Empty);

                //Actor Default Patterns
                value = value.SchemeFormat(schemer.Conspirator, schemer.Target);

                //Global Variable Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.GLOBAL_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{g:" + titlePattern.Value + "}",
                            (string)IM.GetVariable(titlePattern.Value));

                #endregion

                variable.value = StringValue;
            }

            if ( variableType == NType.Integer )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NInt)variable ).Value = IntegerValue;
                        break;
                    case MathOperation.Add:
                        ( (NInt)variable ).Value += IntegerValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NInt)variable ).Value -= IntegerValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NInt)variable ).Value *= IntegerValue;
                        break;
                }

            if ( variableType == NType.Bool ) {
                if ( bool.TryParse(StringValue, out var parseValue) ) {
                    variable.value = parseValue;
                } else {
                    NDebug.Log("Bool Parse Error", NLogType.Error);
                    End(generatedNodeInfo);
                    return;
                }
            }

            if ( variableType == NType.Float )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NFloat)variable ).Value = FloatValue;
                        break;
                    case MathOperation.Add:
                        ( (NFloat)variable ).Value += FloatValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NFloat)variable ).Value -= FloatValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NFloat)variable ).Value *= FloatValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if ( variableType == NType.Enum ) ( (NEnum)variable ).value = StringValue;

            if ( variableType == NType.Object ) ( (NObject)variable ).Value = ObjectValue;

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetClanVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public string StringValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public MathOperation Operation;
        [SerializeReference] public UnityEngine.Object ObjectValue;
        [SerializeReference] public List< OutputData > Outputs;


        public SetClanVariableData(string id, string groupId, Vector2 position, string variableId, string stringValue,
            int integerValue, float floatValue,
            UnityEngine.Object objectValue, MathOperation operation, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            ObjectValue = objectValue;
            Operation = operation;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when schemer.Target == null || schemer.Target.Clan == null:
                    End(generatedNodeInfo);
                    return;
                case "Conspirator" when schemer.Conspirator == null || schemer.Conspirator.Clan == null:
                    End(generatedNodeInfo);
                    return;
                case "[Family]" when generatedNodeInfo.clan == null:
                    End(generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator.Clan.GetVariable(VariableID),
                "Target" => schemer.Target.Clan.GetVariable(VariableID),
                "[Family]" => generatedNodeInfo.clan.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType == NType.String ) {
                var value = StringValue;

                #region PATTERNS

                //Localisation Patterns
                foreach ( Match titleLocalisation in Regex.Matches(value, STATIC.LOCALISATION) )
                    if ( titleLocalisation.Success )
                        value = Regex.Replace(value, "{l:" + titleLocalisation.Value + "}",
                            IM.GetText(titleLocalisation.Value));

                //Table Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.TABLE_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{table:" + titlePattern.Value + "}",
                            (string)schemer.GetVariable(titlePattern.Value) ?? string.Empty);

                //Actor Default Patterns
                value = value.SchemeFormat(schemer.Conspirator, schemer.Target);

                //Global Variable Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.GLOBAL_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{g:" + titlePattern.Value + "}",
                            (string)IM.GetVariable(titlePattern.Value));

                #endregion

                variable.value = StringValue;
            }

            if ( variableType == NType.Integer )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NInt)variable ).Value = IntegerValue;
                        break;
                    case MathOperation.Add:
                        ( (NInt)variable ).Value += IntegerValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NInt)variable ).Value -= IntegerValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NInt)variable ).Value *= IntegerValue;
                        break;
                }

            if ( variableType == NType.Bool ) {
                if ( bool.TryParse(StringValue, out var parseValue) ) {
                    variable.value = parseValue;
                } else {
                    NDebug.Log("Bool Parse Error", NLogType.Error);
                    End(generatedNodeInfo);
                    return;
                }
            }

            if ( variableType == NType.Float )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NFloat)variable ).Value = FloatValue;
                        break;
                    case MathOperation.Add:
                        ( (NFloat)variable ).Value += FloatValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NFloat)variable ).Value -= FloatValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NFloat)variable ).Value *= FloatValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if ( variableType == NType.Enum ) ( (NEnum)variable ).value = StringValue;

            if ( variableType == NType.Object ) ( (NObject)variable ).Value = ObjectValue;

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetRelationVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public EnumType EnumType;

        public GetRelationVariableData(string id, string groupId, Vector2 position, string variableId,
            string stringValue,
            int integerValue, float floatValue, EnumType enumType,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            EnumType = enumType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            var variable =
                generatedNodeInfo.dualActor.Item1.GetRelationVariable(VariableID, generatedNodeInfo.dualActor.Item2);

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( IM.Variables.Any(v => v.id == VariableID) == false ) {
                NDebug.Log($"Variable({VariableType}) is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Enum ) {
                //Work in progress.
                if ( EnumType == EnumType.Is )
                    await Next(schemer, Outputs, generatedNodeInfo, (int)variable, false);
                else {
                    await Next(schemer, Outputs, generatedNodeInfo, (int)variable, false);
                }
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class WaitUntilData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;


        public WaitUntilData(string id, string groupId, Vector2 position, string variableId, string stringValue,
            int integerValue, float floatValue,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            generatedNodeInfo.index = 0;

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when schemer.Target == null:
                    End(generatedNodeInfo);
                    return;
                case "[Actor]" when generatedNodeInfo.actor == null:
                    End(generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator.GetVariable(VariableID),
                "Global" => IM.GetVariable(VariableID),
                "Target" => schemer.Target.GetVariable(VariableID),
                "[Actor]" => generatedNodeInfo.actor.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( IM.Variables.Any(v => v.id == VariableID) == false ) {
                NDebug.Log($"Variable({VariableType}) is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                if ( Outputs[ 0 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(0) ) {
                    if ( (string)variable == StringValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                        generatedNodeInfo.indexes.Add(0);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( (string)variable != StringValue ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 0, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(0);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 1 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(1) ) {
                    if ( (string)variable != StringValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                        generatedNodeInfo.indexes.Add(1);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( (string)variable == StringValue ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 1, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(1);

                            Execute();
                        }
                    }
                }
            }

            if ( variableType == NType.Integer ) {
                var value = ( (int)variable );

                if ( Outputs[ 0 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(0) ) {
                    if ( value.Equals(IntegerValue) ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                        generatedNodeInfo.indexes.Add(0);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( (int)variable ).Equals(IntegerValue) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 0, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(0);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 1 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(1) ) {
                    if ( !value.Equals(IntegerValue) ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                        generatedNodeInfo.indexes.Add(1);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( ( (int)variable ).Equals(IntegerValue) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 1, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(1);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 2 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(2) ) {
                    if ( value > IntegerValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                        generatedNodeInfo.indexes.Add(2);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( ( (int)variable ) > IntegerValue ) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 2, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(2);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 3 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(3) ) {
                    if ( value < IntegerValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                        generatedNodeInfo.indexes.Add(3);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( ( (int)variable ) < IntegerValue ) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 3, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(3);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 4 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(4) ) {
                    if ( value >= IntegerValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                        generatedNodeInfo.indexes.Add(4);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( ( (int)variable ) >= IntegerValue ) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 4, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(4);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 5 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(5) ) {
                    if ( value <= IntegerValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                        generatedNodeInfo.indexes.Add(5);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( ( (int)variable ) <= IntegerValue ) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 5, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(5);

                            Execute();
                        }
                    }
                }
            }

            if ( variableType == NType.Bool ) {
                if ( Outputs[ 0 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(0) ) {
                    if ( (int)variable == 1 ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                        generatedNodeInfo.indexes.Add(0);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( (int)variable != 1 ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 0, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(0);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 1 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(1) ) {
                    if ( (int)variable != 1 ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                        generatedNodeInfo.indexes.Add(1);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( (int)variable == 1 ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 1, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(1);

                            Execute();
                        }
                    }
                }
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( Outputs[ 0 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(0) ) {
                    if ( value.Equals(FloatValue) ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                        generatedNodeInfo.indexes.Add(0);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( (float)variable ).Equals(FloatValue) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 0, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(0);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 1 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(1) ) {
                    if ( !value.Equals(FloatValue) ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                        generatedNodeInfo.indexes.Add(1);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( ( (float)variable ).Equals(FloatValue) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 1, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(1);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 2 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(2) ) {
                    if ( value > FloatValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                        generatedNodeInfo.indexes.Add(2);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( ( (float)variable ) > FloatValue ) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 2, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(2);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 3 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(3) ) {
                    if ( value < FloatValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                        generatedNodeInfo.indexes.Add(3);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( ( (float)variable ) < FloatValue ) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 3, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(3);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 4 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(4) ) {
                    if ( value >= FloatValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                        generatedNodeInfo.indexes.Add(4);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( ( (float)variable ) >= FloatValue ) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 4, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(4);

                            Execute();
                        }
                    }
                }

                if ( Outputs[ 5 ].DataCollection.Count > 0 && !generatedNodeInfo.indexes.Contains(5) ) {
                    if ( value <= FloatValue ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                        generatedNodeInfo.indexes.Add(5);
                    } else {
                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( !( ( (float)variable ) <= FloatValue ) ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, 5, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(5);

                            Execute();
                        }
                    }
                }
            }

            if ( variableType == NType.Enum ) {
                for ( int i = 0; i < Outputs.Count; i++ ) {
                    if ( Outputs[ i ].DataCollection.Count < 1 ) continue;
                    if ( generatedNodeInfo.indexes.Contains(i) ) continue;

                    if ( (int)variable == i ) {
                        await Next(schemer, Outputs, generatedNodeInfo, i, false);
                        generatedNodeInfo.indexes.Add(i);
                    } else {
                        var index = i;

                        variable.onValueChanged += OnValueChanged;
                        generatedNodeInfo.index++;

                        void OnValueChanged() {
                            if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                                variable.onValueChanged -= OnValueChanged;
                                return;
                            }

                            if ( (int)variable != index ) return;
                            _ = Next(schemer, Outputs, generatedNodeInfo, index, false);
                            variable.onValueChanged -= OnValueChanged;
                            generatedNodeInfo.index--;

                            generatedNodeInfo.indexes.Add(index);

                            Execute();
                        }
                    }
                }
            }

            Execute();

            void Execute() {
                if ( generatedNodeInfo.index < 1 ) {
                    End(generatedNodeInfo);
                }
            }
        }
    }

    [Serializable]
    public class GetTableVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;


        public GetTableVariableData(string id, string groupId, Vector2 position, string variableId,
            string stringValue, int integerValue, float floatValue,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            VariableType = type;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var variable = schemer.GetVariable(VariableID);

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetSchemeTableVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public string SchemeID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;


        public GetSchemeTableVariableData(string id, string groupId, Vector2 position, string variableId,
            string schemeId,
            string stringValue, int integerValue, float floatValue,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            SchemeID = schemeId;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            VariableType = type;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var actor = generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;
            var target = generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Target
                : schemer.Conspirator;

            if ( target == null || actor == null ) {
                End(generatedNodeInfo);
                return;
            }

            var scheme = actor.GetScheme(SchemeID, target);
            if ( scheme == null ) {
                End(generatedNodeInfo);
                return;
            }

            var variable = schemer.GetVariable(VariableID);

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(schemer, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, index, false);
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public string StringValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public MathOperation Operation;
        [SerializeReference] public UnityEngine.Object ObjectValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;


        public SetVariableData(string id, string groupId, Vector2 position, string variableId, string stringValue,
            int integerValue, float floatValue,
            UnityEngine.Object objectValue, MathOperation operation, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            ObjectValue = objectValue;
            Operation = operation;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when schemer.Target == null:
                    End(generatedNodeInfo);
                    return;
                case "[Actor]" when generatedNodeInfo.actor == null:
                    End(generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator.GetVariable(VariableID),
                "Global" => IM.GetVariable(VariableID),
                "Target" => schemer.Target.GetVariable(VariableID),
                "[Actor]" => generatedNodeInfo.actor.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType == NType.String ) {
                var value = StringValue;

                #region PATTERNS

                //Localisation Patterns
                foreach ( Match titleLocalisation in Regex.Matches(value, STATIC.LOCALISATION) )
                    if ( titleLocalisation.Success )
                        value = Regex.Replace(value, "{l:" + titleLocalisation.Value + "}",
                            IM.GetText(titleLocalisation.Value));

                //Table Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.TABLE_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{table:" + titlePattern.Value + "}",
                            (string)schemer.GetVariable(titlePattern.Value) ?? string.Empty);

                //Actor Default Patterns
                value = value.SchemeFormat(schemer.Conspirator, schemer.Target);

                //Global Variable Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.GLOBAL_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{g:" + titlePattern.Value + "}",
                            (string)IM.GetVariable(titlePattern.Value));

                #endregion

                variable.value = StringValue;
            }

            if ( variableType == NType.Integer )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NInt)variable ).Value = IntegerValue;
                        break;
                    case MathOperation.Add:
                        ( (NInt)variable ).Value += IntegerValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NInt)variable ).Value -= IntegerValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NInt)variable ).Value *= IntegerValue;
                        break;
                }

            if ( variableType == NType.Bool ) {
                if ( bool.TryParse(StringValue, out var parseValue) ) {
                    variable.value = parseValue;
                } else {
                    NDebug.Log("Bool Parse Error", NLogType.Error);
                    End(generatedNodeInfo);
                    return;
                }
            }

            if ( variableType == NType.Float )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NFloat)variable ).Value = FloatValue;
                        break;
                    case MathOperation.Add:
                        ( (NFloat)variable ).Value += FloatValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NFloat)variable ).Value -= FloatValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NFloat)variable ).Value *= FloatValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if ( variableType == NType.Enum ) ( (NEnum)variable ).value = StringValue;

            if ( variableType == NType.Object ) ( (NObject)variable ).Value = ObjectValue;

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetRelationVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public string StringValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public MathOperation Operation;
        [SerializeReference] public UnityEngine.Object ObjectValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SetRelationVariableData() { }

        public SetRelationVariableData(string id, string groupId, Vector2 position, string variableId,
            string stringValue,
            int integerValue, float floatValue,
            UnityEngine.Object objectValue, MathOperation operation, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            ObjectValue = objectValue;
            Operation = operation;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            var variable =
                generatedNodeInfo.dualActor.Item1.GetRelationVariable(VariableID, generatedNodeInfo.dualActor.Item2);

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType == NType.String ) {
                var value = StringValue;

                #region PATTERNS

                //Localisation Patterns
                foreach ( Match titleLocalisation in Regex.Matches(value, STATIC.LOCALISATION) )
                    if ( titleLocalisation.Success )
                        value = Regex.Replace(value, "{l:" + titleLocalisation.Value + "}",
                            IM.GetText(titleLocalisation.Value));

                //Table Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.TABLE_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{table:" + titlePattern.Value + "}",
                            (string)schemer.GetVariable(titlePattern.Value) ?? string.Empty);

                //Actor Default Patterns
                value = value.SchemeFormat(schemer.Conspirator, schemer.Target);

                //Global Variable Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.GLOBAL_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{g:" + titlePattern.Value + "}",
                            (string)IM.GetVariable(titlePattern.Value));

                #endregion

                variable.value = StringValue;
            }

            if ( variableType == NType.Integer )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NInt)variable ).Value = IntegerValue;
                        break;
                    case MathOperation.Add:
                        ( (NInt)variable ).Value += IntegerValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NInt)variable ).Value -= IntegerValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NInt)variable ).Value *= IntegerValue;
                        break;
                }

            if ( variableType == NType.Bool ) {
                if ( bool.TryParse(StringValue, out var parseValue) ) {
                    variable.value = parseValue;
                } else {
                    NDebug.Log("Bool Parse Error", NLogType.Error);
                    End(generatedNodeInfo);
                    return;
                }
            }

            if ( variableType == NType.Float )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NFloat)variable ).Value = FloatValue;
                        break;
                    case MathOperation.Add:
                        ( (NFloat)variable ).Value += FloatValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NFloat)variable ).Value -= FloatValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NFloat)variable ).Value *= FloatValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if ( variableType == NType.Enum ) ( (NEnum)variable ).value = StringValue;

            if ( variableType == NType.Object ) ( (NObject)variable ).Value = ObjectValue;

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetTableVariableData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public MathOperation Operation;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;


        public SetTableVariableData(string id, string groupId, Vector2 position, string variableId,
            string stringValue, int integerValue, float floatValue,
            NType type, MathOperation operation, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            Operation = operation;
            FloatValue = floatValue;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);
            var variable = schemer.GetVariable(VariableID);

            if ( variable == null ) {
                NDebug.Log("Table Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType == NType.String ) {
                var value = StringValue;

                #region PATTERNS

                //Localisation Patterns
                foreach ( Match titleLocalisation in Regex.Matches(value, STATIC.LOCALISATION) )
                    if ( titleLocalisation.Success )
                        value = Regex.Replace(value, "{l:" + titleLocalisation.Value + "}",
                            IM.GetText(titleLocalisation.Value));

                //Table Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.TABLE_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{table:" + titlePattern.Value + "}",
                            (string)schemer.GetVariable(titlePattern.Value) ?? string.Empty);

                //Actor Default Patterns
                value = value.SchemeFormat(schemer.Conspirator, schemer.Target);

                //Global Variable Patterns
                foreach ( Match titlePattern in Regex.Matches(value, STATIC.GLOBAL_VARIABLE) )
                    if ( titlePattern.Success )
                        value = Regex.Replace(value, "{g:" + titlePattern.Value + "}",
                            (string)IM.GetVariable(titlePattern.Value));

                #endregion

                variable.value = value;
            }

            if ( variableType == NType.Integer )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NInt)variable ).Value = IntegerValue;
                        break;
                    case MathOperation.Add:
                        ( (NInt)variable ).Value += IntegerValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NInt)variable ).Value -= IntegerValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NInt)variable ).Value *= IntegerValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if ( variableType == NType.Bool ) {
                if ( string.IsNullOrEmpty(StringValue) )
                    StringValue = "True";
                if ( bool.TryParse(StringValue, out var parseValue) ) {
                    variable.value = parseValue;
                } else {
                    NDebug.Log(
                        $"Invalid bool value. Please refer to the SetVariable(Table) Node within the Intrigue group named {generatedNodeInfo.SchemerFactory.Scheme.SchemeName}.");
                    End(generatedNodeInfo);
                    return;
                }
            }

            if ( variableType == NType.Float )
                switch ( Operation ) {
                    case MathOperation.Set:
                        ( (NFloat)variable ).Value = FloatValue;
                        break;
                    case MathOperation.Add:
                        ( (NFloat)variable ).Value += FloatValue;
                        break;

                    case MathOperation.Subtract:
                        ( (NFloat)variable ).Value -= FloatValue;
                        break;

                    case MathOperation.Multiply:
                        ( (NFloat)variable ).Value *= FloatValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetActorData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;


        public SetActorData(string id, string groupId, Vector2 position, string variableId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);
            var variable = schemer.GetVariable(VariableID);

            if ( variable == null ) {
                NDebug.Log("Table Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            variable.value = generatedNodeInfo.actor;

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SequencerData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SequencerData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            // var list =
            //     (from output in Outputs
            //         from port in output.DataCollection
            //         select new { node = generatedNodeInfo.SchemerFactory.GetNode(port.NextID), iName = port.NextName })
            //     .OrderBy(n => n.node.Position.y).ToDictionary(d => d.node, d => d.iName);

            var list =
                ( from output in Outputs
                    from port in output.DataCollection
                    select new Tuple< NodeData, string, string >(generatedNodeInfo.SchemerFactory.GetNode(port.NextID),
                        port.NextName, port.ActorID) ).OrderBy(n => n.Item1.Position.y).ToList();

            var isExists = list.ElementAtOrDefault(generatedNodeInfo.index) != null;

            if ( !isExists ) {
                End(generatedNodeInfo);
                return;
            }

            var next = list.ElementAt(generatedNodeInfo.index);

            var nodeInf = GenerateNodeData(next.Item1);
            nodeInf.sequencer = generatedNodeInfo;
            nodeInf.inputName = next.Item2;
            var act = (NActor)schemer.GetVariable(next.Item3);
            if ( act != null )
                nodeInf.actor = act.Value;
            nodeInf.repeater = generatedNodeInfo.repeater;
            nodeInf.validator = generatedNodeInfo.validator;
            nodeInf.bgWorker = generatedNodeInfo.bgWorker;
            nodeInf.SchemerFactory = generatedNodeInfo.SchemerFactory;

            await next.Item1.Run(schemer, nodeInf);
        }
    }

    [Serializable]
    public class SkipSequencerData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public int Index;

        public SkipSequencerData(string id, string groupId, Vector2 position, int index,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Index = index;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.sequencer != null ) {
                generatedNodeInfo.sequencer.index = Index - 1;
            }

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class BreakSequencerData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public BreakSequencerData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.sequencer != null ) {
                End(generatedNodeInfo.sequencer);

                foreach ( var nodeInf in generatedNodeInfo.SchemerFactory.ActiveNodeList.Where(s =>
                             s.sequencer == generatedNodeInfo.sequencer) ) {
                    nodeInf.sequencer = null;
                }
            }

            NDebug.Log("Sequencer is breaks..");

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class BreakRepeaterData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public BreakRepeaterData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.repeater != null ) {
                End(generatedNodeInfo.repeater);

                foreach ( var nodeInf in generatedNodeInfo.SchemerFactory.ActiveNodeList.Where(s =>
                             s.repeater == generatedNodeInfo.repeater) ) {
                    nodeInf.repeater = null;
                }
            }

            NDebug.Log("Repeater is breaks..");

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class ContinueData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public ContinueData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            generatedNodeInfo.SchemerFactory.Continue(generatedNodeInfo);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class BreakData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public BreakData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            generatedNodeInfo.SchemerFactory.Continue(generatedNodeInfo, false);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class PauseData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public PauseData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            schemer.Pause();

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class ResumeData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public ResumeData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            schemer.Resume();

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class NewFlowData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public NewFlowData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class BackgroundWorkerData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public BackgroundWorkerData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            generatedNodeInfo.bgWorker = true;

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class RepeaterData : NodeData {
        [SerializeReference] public int RepetitionCount;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;


        public RepeaterData(string id, string groupId, Vector2 position, int repetitionCount,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            RepetitionCount = repetitionCount;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.repeatCount >= RepetitionCount && RepetitionCount >= 0 ) {
                End(generatedNodeInfo);
                return;
            }

            var list = Outputs[ 0 ].DataCollection
                .Select(port => new Tuple< NodeData, string, string >(
                    generatedNodeInfo.SchemerFactory.GetNode(port.NextID),
                    port.NextName,
                    port.ActorID))
                .Where(t => t.Item1 != null)
                .OrderBy(t => t.Item1.Position.y)
                .ToList();

            if ( !list.Any() ) {
                End(generatedNodeInfo);
                return;
            }

            if ( schemer.IsPaused )
                await NullUtils.WaitUntilAsync(() => !schemer.IsPaused);

            if ( generatedNodeInfo.repeatCount >= RepetitionCount && RepetitionCount < 0 )
                await Task.Yield();

            foreach ( var nodeData in list ) {
                var nodeInf = GenerateNodeData(nodeData.Item1);
                nodeInf.inputName = nodeData.Item2;

                var act = (NActor)schemer.GetVariable(nodeData.Item3);
                if ( act != null )
                    nodeInf.actor = act.Value;

                nodeInf.SchemerFactory = generatedNodeInfo.SchemerFactory;
                nodeInf.repeater = generatedNodeInfo;
                nodeInf.sequencer = generatedNodeInfo.sequencer;
                nodeInf.bgWorker = generatedNodeInfo.bgWorker;
                nodeInf.validator = generatedNodeInfo.validator;

                await nodeData.Item1.Run(schemer, nodeInf);
            }
        }
    }

    [Serializable]
    public class SoundData : NodeData {
        [SerializeReference] public AudioClip Clip;
        [SerializeReference] public float Volume;
        [SerializeReference] public float Pitch;
        [SerializeReference] public int Priority;
        [SerializeReference] public bool WaitEnd;
        [SerializeReference] public bool Loop;
        [SerializeReference] public UnityEngine.Audio.AudioMixerGroup AudioMixerGroup;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SoundData(string id, string groupId, Vector2 position, AudioClip clip, float volume, float pitch,
            int priority, bool waitEnd, bool loop, UnityEngine.Audio.AudioMixerGroup mixerGroup,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Clip = clip;
            Volume = volume;
            Pitch = pitch;
            Priority = priority;
            WaitEnd = waitEnd;
            Loop = loop;
            AudioMixerGroup = mixerGroup;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName switch {
                "Conspirator" => schemer.Conspirator,
                "Target" => schemer.Target,
                "Global" => null,
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            bool isTargetOrGlobal = target == null || IM.IsPlayer(target);

            if ( Clip != null && isTargetOrGlobal ) {
                var audioSource = IM.SetupAudio(null, Clip, Volume, 0f, false, AudioMixerGroup);
                audioSource.priority = Priority;
                audioSource.pitch = Pitch;
                audioSource.Play();
                if ( WaitEnd ) {
                    var id = NullUtils.GenerateID();
                    generatedNodeInfo.SchemerFactory.Delays.Add(id);
                    NullUtils.DelayedCall(new DelayedCallParams {
                        DelayName = id,
                        WaitUntil = () => audioSource == null || !audioSource.isPlaying,
                        Call = () => {
                            generatedNodeInfo.SchemerFactory.Delays.Remove(id);
                            _ = Next(schemer, Outputs, generatedNodeInfo);
                        }
                    });
                    // NullUtils.DelayedCall(id, () => audioSource == null || !audioSource.isPlaying, () =>
                    // {
                    //     generatedNodeInfo.SchemerFactory.Delays.Remove(id);
                    //     await Next(schemer, Outputs, generatedNodeInfo);
                    // });
                    return;
                }
            }

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SoundClassData : NodeData {
        [SerializeReference] public AudioClip Clip;
        [SerializeReference] public float Volume;
        [SerializeReference] public float Pitch;
        [SerializeReference] public int Priority;
        [SerializeReference] public bool Loop;
        [SerializeReference] public bool StopWhenClosed;
        [SerializeReference] public float FadeOut;
        [SerializeReference] public UnityEngine.Audio.AudioMixerGroup AudioMixerGroup;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SoundClassData(string id, string groupId, Vector2 position, AudioClip clip, float volume, float pitch,
            float fadeOut,
            int priority, bool loop, bool stopWhenClosed, UnityEngine.Audio.AudioMixerGroup mixerGroup,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Clip = clip;
            Volume = volume;
            Pitch = pitch;
            Priority = priority;
            Loop = loop;
            StopWhenClosed = stopWhenClosed;
            FadeOut = fadeOut;
            AudioMixerGroup = mixerGroup;
            Outputs = new List< OutputData >(outputs);
        }
    }

    [Serializable]
    public class VoiceClassData : NodeData {
        [SerializeReference] public AudioClip Clip;
        [SerializeReference] public float Volume;
        [SerializeReference] public float Pitch;
        [SerializeReference] public int Priority;
        [SerializeReference] public bool Sync;
        [SerializeReference] public UnityEngine.Audio.AudioMixerGroup AudioMixerGroup;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public VoiceClassData(string id, string groupId, Vector2 position, AudioClip clip, float volume, float pitch,
            int priority, bool sync, UnityEngine.Audio.AudioMixerGroup mixerGroup,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Clip = clip;
            Volume = volume;
            Pitch = pitch;
            Priority = priority;
            Sync = sync;
            AudioMixerGroup = mixerGroup;
            Outputs = new List< OutputData >(outputs);
        }
    }

    [Serializable]
    public class NoteData : NodeData {
        [SerializeReference] public string Note;

        public NoteData(string id, string groupId, Vector2 position, string note) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Note = note;
        }
    }

    [Serializable]
    public class NoteDataRL : NodeData {
        [SerializeReference] public string Note;

        public NoteDataRL(string id, string groupId, Vector2 position, string note) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Note = note;
        }
    }

    [Serializable]
    public class LogData : NodeData {
        [SerializeReference] public string Log;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public LogData(string id, string groupId, Vector2 position, string log, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Log = log;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            NDebug.Log(Log.SchemeFormat(schemer.Conspirator, schemer.Target), NLogType.Log, true);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class ChoiceTextData : NodeData {
        [SerializeReference] public string Text;
        [SerializeReference] public string Text2;
        [SerializeReference] public string ChanceID;

        public ChoiceTextData(string id, string groupId, Vector2 position, string text, string text2, string chanceID) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Text = text;
            Text2 = text2;
            ChanceID = chanceID;
        }

        public override Task Run(Schemer schemer, NodeInfo nodeInfo) {
            //
            return Task.CompletedTask;
        }
    }

    [Serializable]
    public class SchemeIsActiveData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public string SchemeID;
        [SerializeReference] public Actor.VerifyType VerifyType;

        public SchemeIsActiveData(string id, string groupId, Vector2 position, string schemeId,
            Actor.VerifyType verifyType,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            SchemeID = schemeId;
            VerifyType = verifyType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var actor = generatedNodeInfo.inputName == "Conspirator" ? schemer.Conspirator :
                generatedNodeInfo.inputName == "Target" ? schemer.Target : null;
            var target = generatedNodeInfo.inputName == "Conspirator" ? schemer.Target :
                generatedNodeInfo.inputName == "Target" ? schemer.Conspirator : null;

            if ( actor != null ) {
                await Next(schemer, Outputs, generatedNodeInfo,
                    actor.SchemeIsActive(SchemeID, VerifyType, target) ? 0 : 1);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            await Next(schemer, Outputs, generatedNodeInfo,
                generatedNodeInfo.dualActor.Item1.SchemeIsActive(SchemeID, VerifyType,
                    generatedNodeInfo.dualActor.Item2)
                    ? 0
                    : 1);
        }
    }

    [Serializable]
    public class IsAIData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public IsAIData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var index = IM.IsPlayer(target) ? 1 : 0;
            await Next(schemer, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class GenderData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GenderData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var index = target.Gender == Actor.IGender.Male ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class AgeData : NodeData {
        [SerializeReference] public int Age;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public AgeData(string id, string groupId, Vector2 position, int age, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Age = age;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var value = target.Age;

            if ( value == Age ) {
                await Next(schemer, Outputs, generatedNodeInfo, 0, false);
            }

            if ( value != Age ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1, false);
            }

            if ( value > Age ) {
                await Next(schemer, Outputs, generatedNodeInfo, 2, false);
            }

            if ( value < Age ) {
                await Next(schemer, Outputs, generatedNodeInfo, 3, false);
            }

            if ( value >= Age ) {
                await Next(schemer, Outputs, generatedNodeInfo, 4, false);
            }

            if ( value <= Age ) {
                await Next(schemer, Outputs, generatedNodeInfo, 5, false);
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class SpouseCountData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SpouseCountData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var value = target.Spouses(IncludePassiveCharacters).Count();

            if ( value == Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 0, false);
            }

            if ( value != Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1, false);
            }

            if ( value > Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 2, false);
            }

            if ( value < Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 3, false);
            }

            if ( value >= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 4, false);
            }

            if ( value <= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 5, false);
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class ChildCountData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public ChildCountData(string id, string groupId, Vector2 position, int count, bool includePassiveCharacters,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var value = target.Children(IncludePassiveCharacters).Count();

            if ( value == Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 0, false);
            }

            if ( value != Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1, false);
            }

            if ( value > Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 2, false);
            }

            if ( value < Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 3, false);
            }

            if ( value >= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 4, false);
            }

            if ( value <= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 5, false);
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class ParentCountData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public ParentCountData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var value = target.Parents(IncludePassiveCharacters).Count();

            if ( value == Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 0, false);
            }

            if ( value != Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1, false);
            }

            if ( value > Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 2, false);
            }

            if ( value < Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 3, false);
            }

            if ( value >= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 4, false);
            }

            if ( value <= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 5, false);
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class GrandparentCountData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GrandparentCountData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var value = target.Grandparents(IncludePassiveCharacters).Count();

            if ( value == Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 0, false);
            }

            if ( value != Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1, false);
            }

            if ( value > Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 2, false);
            }

            if ( value < Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 3, false);
            }

            if ( value >= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 4, false);
            }

            if ( value <= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 5, false);
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class SiblingCountData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SiblingCountData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var value = target.Siblings(IncludePassiveCharacters).Count();

            if ( value == Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 0, false);
            }

            if ( value != Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1, false);
            }

            if ( value > Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 2, false);
            }

            if ( value < Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 3, false);
            }

            if ( value >= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 4, false);
            }

            if ( value <= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 5, false);
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class GrandchildCountData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GrandchildCountData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var value = target.Grandchildren(IncludePassiveCharacters).Count();

            if ( value == Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 0, false);
            }

            if ( value != Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1, false);
            }

            if ( value > Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 2, false);
            }

            if ( value < Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 3, false);
            }

            if ( value >= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 4, false);
            }

            if ( value <= Count ) {
                await Next(schemer, Outputs, generatedNodeInfo, 5, false);
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetActorData : NodeData {
        [SerializeReference] public string ActorID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GetActorData(string id, string groupId, Vector2 position, string actorID,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            ActorID = actorID;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var index = target.ID == ActorID ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class ReturnActorData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public ReturnActorData(string id, string groupId, Vector2 position, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var actor = generatedNodeInfo.SchemerFactory.Scheme.Invoke_GetActor(MethodName);

            if ( actor == null ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
            } else {
                generatedNodeInfo.actor = actor;
                await Next(schemer, Outputs, generatedNodeInfo);
            }
        }
    }

    [Serializable]
    public class DualActorData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public DualType DualType;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public DualActorData(string id, string groupId, Vector2 position, DualType dualType, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            DualType = dualType;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( DualType ) {
                case DualType.GetActors: {
                    var actor = generatedNodeInfo.SchemerFactory.Scheme.Invoke_GetDualActor(MethodName);

                    if ( actor.Item1 == null && actor.Item2 == null ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 1);
                    } else if ( actor.Item1 == null && actor.Item2 != null ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 2);
                    } else if ( actor.Item1 != null && actor.Item1 == null ) {
                        await Next(schemer, Outputs, generatedNodeInfo, 3);
                    } else if ( actor.Item1 != null && actor.Item2 != null ) {
                        generatedNodeInfo.dualActor = ( actor.Item1, actor.Item2 );
                        await Next(schemer, Outputs, generatedNodeInfo);
                    }

                    break;
                }
                case DualType.Conspirator_Target when schemer.Conspirator == null && schemer.Target == null:
                    await Next(schemer, Outputs, generatedNodeInfo, 1);
                    break;
                case DualType.Conspirator_Target when schemer.Conspirator == null && schemer.Target != null:
                    await Next(schemer, Outputs, generatedNodeInfo, 2);
                    break;
                case DualType.Conspirator_Target when schemer.Conspirator != null && schemer.Target == null:
                    await Next(schemer, Outputs, generatedNodeInfo, 3);
                    break;
                case DualType.Conspirator_Target: {
                    if ( schemer.Conspirator != null && schemer.Target != null ) {
                        generatedNodeInfo.dualActor = ( schemer.Conspirator, schemer.Target );
                        await Next(schemer, Outputs, generatedNodeInfo);
                    }

                    break;
                }
                case DualType.Target_Conspirator when schemer.Conspirator == null && schemer.Target == null:
                    await Next(schemer, Outputs, generatedNodeInfo, 1);
                    break;
                case DualType.Target_Conspirator when schemer.Target == null && schemer.Conspirator != null:
                    await Next(schemer, Outputs, generatedNodeInfo, 2);
                    break;
                case DualType.Target_Conspirator when schemer.Target != null && schemer.Conspirator == null:
                    await Next(schemer, Outputs, generatedNodeInfo, 3);
                    break;
                case DualType.Target_Conspirator: {
                    if ( schemer.Conspirator != null && schemer.Target != null ) {
                        generatedNodeInfo.dualActor = ( schemer.Target, schemer.Conspirator );
                        await Next(schemer, Outputs, generatedNodeInfo);
                    }

                    break;
                }
            }
        }
    }

    [Serializable]
    public class ReturnClanData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public ReturnClanData(string id, string groupId, Vector2 position, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var clan = generatedNodeInfo.SchemerFactory.Scheme.Invoke_GetClan(MethodName);

            if ( clan == null ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
            } else {
                generatedNodeInfo.clan = clan;
                await Next(schemer, Outputs, generatedNodeInfo);
            }
        }
    }

    [Serializable]
    public class ReturnFamilyData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public List< OutputData > Outputs;

        public ReturnFamilyData(string id, string groupId, Vector2 position, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var family = generatedNodeInfo.SchemerFactory.Scheme.Invoke_GetFamily(MethodName);

            if ( family == null ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
            } else {
                generatedNodeInfo.family = family;
                await Next(schemer, Outputs, generatedNodeInfo);
            }
        }
    }

    [Serializable]
    public class SetConspiratorData : NodeData {
        [SerializeReference] public string ConspiratorID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SetConspiratorData(string id, string groupId, Vector2 position, string conspiratorId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            ConspiratorID = conspiratorId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            schemer.SetConspirator(generatedNodeInfo.actor);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetTargetData : NodeData {
        [SerializeReference] public string TargetID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SetTargetData(string id, string groupId, Vector2 position, string targetId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            TargetID = targetId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            schemer.SetTarget(generatedNodeInfo.actor);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class OnLoadData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public OnLoadData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetFamilyData : NodeData {
        [SerializeReference] public string FamilyID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GetFamilyData(string id, string groupId, Vector2 position, string familyId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            FamilyID = familyId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( string.IsNullOrEmpty(FamilyID) ) {
                var i = target.Family == null ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( target.Family == null ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = target.Family.ID == FamilyID ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class GetClanData : NodeData {
        [SerializeReference] public string ClanID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GetClanData(string id, string groupId, Vector2 position, string clanId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            ClanID = clanId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( string.IsNullOrEmpty(ClanID) ) {
                var i = target.Clan == null ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( target.Clan == null ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = target.Clan.ID == ClanID ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class KeyHandlerData : NodeData {
        [SerializeReference] public KeyCode KeyCode;
        [SerializeReference] public KeyType KeyType;
        [SerializeReference] public int TapCount;
        [SerializeReference] public float HoldTime;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public KeyHandlerData(string id, string groupId, Vector2 position, KeyCode keyCode, KeyType keyType,
            int tapCount, float holdTime,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            KeyCode = keyCode;
            KeyType = keyType;
            TapCount = tapCount;
            HoldTime = holdTime;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( IM.IsPlayer(target) ) {
                IM.keyHandler += KeyHandler;

                void KeyHandler(KeyCode keyCode, KeyType keyType, float time) {
                    if ( !schemer.ActiveNodeList.Contains(generatedNodeInfo) ) {
                        IM.keyHandler -= KeyHandler;
                        return;
                    }

                    if ( KeyCode == keyCode && KeyType == keyType ) {
                        _ = Next(schemer, Outputs, generatedNodeInfo);
                        IM.keyHandler -= KeyHandler;
                    }
                }

                return;
            }

            await Next(schemer, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class SetClanData : NodeData {
        [SerializeReference] public string ClanID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SetClanData(string id, string groupId, Vector2 position, string clanId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            ClanID = clanId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            var clan = IM.GetClan(ClanID);

            if ( clan == null ) {
                target.QuitClan();
            } else {
                target.JoinClan(clan);
            }

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetRoleData : NodeData {
        [SerializeReference] public string RoleID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GetRoleData(string id, string groupId, Vector2 position, string roleId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            RoleID = roleId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( string.IsNullOrEmpty(RoleID) ) {
                var i = target.Role == null ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( target.Role == null ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = target.Role.ID == RoleID ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class GetCultureData : NodeData {
        [SerializeReference] public string CultureID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GetCultureData(string id, string groupId, Vector2 position, string cultureId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            CultureID = cultureId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( string.IsNullOrEmpty(CultureID) ) {
                var i = target.Culture == null ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( target.Culture == null ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = target.Culture.ID == CultureID ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class SetCultureData : NodeData {
        [SerializeReference] public string CultureID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SetCultureData(string id, string groupId, Vector2 position, string cultureId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            CultureID = cultureId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            target.SetCulture(CultureID);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetRoleData : NodeData {
        [SerializeReference] public string RoleID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SetRoleData(string id, string groupId, Vector2 position, string roleId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            RoleID = roleId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( string.IsNullOrEmpty(RoleID) ) {
                target.RoleDismiss();
            } else {
                target.SetRole(RoleID);
            }

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SameClanData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SameClanData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( schemer.Conspirator == null || schemer.Target == null ) {
                    End(generatedNodeInfo);
                    return;
                }

                if ( schemer.Conspirator.Clan != null &&
                     schemer.Conspirator.Clan == schemer.Target.Clan ) {
                    await Next(schemer, Outputs, generatedNodeInfo);
                } else {
                    await Next(schemer, Outputs, generatedNodeInfo, 1);
                }

                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.Clan != null &&
                 generatedNodeInfo.dualActor.Item1.Clan == generatedNodeInfo.dualActor.Item2.Clan ) {
                await Next(schemer, Outputs, generatedNodeInfo);
            } else {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class SameFamilyData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SameFamilyData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( schemer.Conspirator == null || schemer.Target == null ) {
                    End(generatedNodeInfo);
                    return;
                }

                if ( schemer.Conspirator.Family != null &&
                     schemer.Conspirator.Family == schemer.Target.Family ) {
                    await Next(schemer, Outputs, generatedNodeInfo);
                } else {
                    await Next(schemer, Outputs, generatedNodeInfo, 1);
                }

                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.Family != null &&
                 generatedNodeInfo.dualActor.Item1.Family == generatedNodeInfo.dualActor.Item2.Family ) {
                await Next(schemer, Outputs, generatedNodeInfo);
            } else {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class SameCultureData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SameCultureData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( schemer.Conspirator == null || schemer.Target == null ) {
                    End(generatedNodeInfo);
                    return;
                }

                if ( schemer.Conspirator.Culture != null &&
                     schemer.Conspirator.Culture == schemer.Target.Culture ) {
                    await Next(schemer, Outputs, generatedNodeInfo);
                } else {
                    await Next(schemer, Outputs, generatedNodeInfo, 1);
                }

                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.Culture != null &&
                 generatedNodeInfo.dualActor.Item1.Culture == generatedNodeInfo.dualActor.Item2.Culture ) {
                await Next(schemer, Outputs, generatedNodeInfo);
            } else {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class SameGenderData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SameGenderData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( schemer.Conspirator == null || schemer.Target == null ) {
                    End(generatedNodeInfo);
                    return;
                }

                if ( schemer.Conspirator.Gender == schemer.Target.Gender ) {
                    await Next(schemer, Outputs, generatedNodeInfo);
                } else {
                    await Next(schemer, Outputs, generatedNodeInfo, 1);
                }

                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.Gender == generatedNodeInfo.dualActor.Item2.Gender ) {
                await Next(schemer, Outputs, generatedNodeInfo);
            } else {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class IsParentData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public IsParentData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target": {
                    if ( schemer.Conspirator == null || schemer.Target == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( schemer.Target.IsParent(schemer.Conspirator) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "Conspirator": {
                    if ( schemer.Conspirator == null || schemer.Target == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( schemer.Conspirator.IsParent(schemer.Target) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "[Dual]": {
                    if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( generatedNodeInfo.dualActor.Item2.IsParent(generatedNodeInfo.dualActor.Item1) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
            }

            await Next(schemer, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class HasHeirData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public HasHeirData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            int index = target.Heir != null ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class IsGrandparentData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public IsGrandparentData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target": {
                    if ( schemer.Conspirator == null || schemer.Target == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( schemer.Target.IsGrandparent(schemer.Conspirator) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "Conspirator": {
                    if ( schemer.Conspirator == null || schemer.Target == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( schemer.Conspirator.IsGrandparent(schemer.Target) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "[Dual]": {
                    if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( generatedNodeInfo.dualActor.Item2.IsGrandparent(generatedNodeInfo.dualActor.Item1) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
            }

            await Next(schemer, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class IsGrandchildData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public IsGrandchildData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target": {
                    if ( schemer.Conspirator == null || schemer.Target == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( schemer.Target.IsGrandchild(schemer.Conspirator) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "Conspirator": {
                    if ( schemer.Conspirator == null || schemer.Target == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( schemer.Conspirator.IsGrandchild(schemer.Target) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "[Dual]": {
                    if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( generatedNodeInfo.dualActor.Item2.IsGrandchild(generatedNodeInfo.dualActor.Item1) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
            }

            await Next(schemer, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class IsSpouseData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public IsSpouseData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( schemer.Conspirator == null || schemer.Target == null ) {
                    End(generatedNodeInfo);
                    return;
                }

                await Next(schemer, Outputs, generatedNodeInfo,
                    schemer.Conspirator.IsSpouse(schemer.Target) ? 0 : 1);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            await Next(schemer, Outputs, generatedNodeInfo,
                generatedNodeInfo.dualActor.Item1.IsSpouse(generatedNodeInfo.dualActor.Item2) ? 0 : 1);
        }
    }

    [Serializable]
    public class IsSiblingData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public IsSiblingData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( schemer.Conspirator == null || schemer.Target == null ) {
                    End(generatedNodeInfo);
                    return;
                }

                await Next(schemer, Outputs, generatedNodeInfo,
                    schemer.Conspirator.IsSibling(schemer.Target) ? 0 : 1);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            await Next(schemer, Outputs, generatedNodeInfo,
                generatedNodeInfo.dualActor.Item1.IsSibling(generatedNodeInfo.dualActor.Item2) ? 0 : 1);
        }
    }

    [Serializable]
    public class IsChildData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public IsChildData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target": {
                    if ( schemer.Conspirator == null || schemer.Target == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( schemer.Target.IsChild(schemer.Conspirator) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "Conspirator": {
                    if ( schemer.Conspirator == null || schemer.Target == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( schemer.Conspirator.IsChild(schemer.Target) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "[Dual]": {
                    if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                        End(generatedNodeInfo);
                        return;
                    }

                    if ( generatedNodeInfo.dualActor.Item2.IsChild(generatedNodeInfo.dualActor.Item1) ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
            }

            await Next(schemer, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class IsRelativeData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public IsRelativeData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( schemer.Conspirator == null || schemer.Target == null ) {
                    End(generatedNodeInfo);
                    return;
                }

                if ( schemer.Conspirator.IsRelative(schemer.Target) ) {
                    await Next(schemer, Outputs, generatedNodeInfo);
                    return;
                }

                await Next(schemer, Outputs, generatedNodeInfo, 1);

                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.IsRelative(generatedNodeInfo.dualActor.Item2) ) {
                await Next(schemer, Outputs, generatedNodeInfo);
                return;
            }

            await Next(schemer, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class GetPolicyData : NodeData {
        [SerializeReference] public string PolicyID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GetPolicyData(string id, string groupId, Vector2 position, string policyId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            PolicyID = policyId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var clan = generatedNodeInfo.clan;

            if ( generatedNodeInfo.inputName.Equals("Target Clan") && schemer.Target == null ) {
                End(generatedNodeInfo);
                return;
            }

            clan ??= generatedNodeInfo.inputName == "Conspirator Clan"
                ? schemer.Conspirator.Clan
                : schemer.Target.Clan;

            if ( string.IsNullOrEmpty(PolicyID) ) {
                var i = clan == null ? 0 : 1;
                await Next(schemer, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( clan == null ) {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = clan.Policies.Any(p => p.ID == PolicyID) ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class AddSpouseData : NodeData {
        [SerializeReference] public bool JoinSpouseFamily;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public AddSpouseData(string id, string groupId, Vector2 position, bool joinSpouseFamily,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            JoinSpouseFamily = joinSpouseFamily;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(generatedNodeInfo);
                return;
            }

            generatedNodeInfo.dualActor.Item1.AddSpouse(generatedNodeInfo.dualActor.Item2, JoinSpouseFamily);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetInheritorData : NodeData {
        [SerializeReference] public bool Inheritor;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SetInheritorData(string id, string groupId, Vector2 position, bool inheritor,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Inheritor = inheritor;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            target.SetInheritor(Inheritor);

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class RemoveSpousesData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public RemoveSpousesData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            target.RemoveSpouses();

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SetStateData : NodeData {
        [SerializeReference] public int State;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SetStateData(string id, string groupId, Vector2 position, int state,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            State = state;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            target.SetState(State == 0 ? Actor.IState.Active : Actor.IState.Passive, State == 2);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetStateData : NodeData {
        [SerializeReference] public Actor.IState State;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public GetStateData(string id, string groupId, Vector2 position, Actor.IState state,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            State = state;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator"
                ? schemer.Conspirator
                : schemer.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(generatedNodeInfo);
                return;
            }

            if ( target.State == State ) {
                await Next(schemer, Outputs, generatedNodeInfo);
            } else {
                await Next(schemer, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class SchemeStateData : NodeData {
        [SerializeReference] public int State;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public SchemeStateData(string id, string groupId, Vector2 position, int state,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            State = state;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            switch ( State ) {
                case 0: {
                    if ( schemer.IsEnded ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                    } else {
                        await Next(schemer, Outputs, generatedNodeInfo, 1);
                    }

                    return;
                }
                case 1: {
                    if ( schemer.Result == SchemeResult.None ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                    } else {
                        await Next(schemer, Outputs, generatedNodeInfo, 1);
                    }

                    return;
                }
                case 2: {
                    if ( schemer.Result == SchemeResult.Success ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                    } else {
                        await Next(schemer, Outputs, generatedNodeInfo, 1);
                    }

                    return;
                }
                case 3: {
                    if ( schemer.Result == SchemeResult.Failed ) {
                        await Next(schemer, Outputs, generatedNodeInfo);
                    } else {
                        await Next(schemer, Outputs, generatedNodeInfo, 1);
                    }

                    return;
                }
                default:
                    End(generatedNodeInfo);
                    break;
            }
        }
    }

    [Serializable]
    public class AddPolicyData : NodeData {
        [SerializeReference] public string PolicyID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public AddPolicyData(string id, string groupId, Vector2 position, string policyId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            PolicyID = policyId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var clan = generatedNodeInfo.clan;

            if ( generatedNodeInfo.inputName.Equals("Target Clan") && schemer.Target == null ) {
                End(generatedNodeInfo);
                return;
            }

            clan ??= generatedNodeInfo.inputName == "Conspirator Clan"
                ? schemer.Conspirator.Clan
                : schemer.Target.Clan;

            clan?.AddPolicy(PolicyID);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class RemovePolicyData : NodeData {
        [SerializeReference] public string PolicyID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public RemovePolicyData(string id, string groupId, Vector2 position, string policyId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            PolicyID = policyId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            var clan = generatedNodeInfo.clan;

            if ( generatedNodeInfo.inputName.Equals("Target Clan") && schemer.Target == null ) {
                End(generatedNodeInfo);
                return;
            }

            clan ??= generatedNodeInfo.inputName == "Conspirator Clan"
                ? schemer.Conspirator.Clan
                : schemer.Target.Clan;

            clan?.RemovePolicy(PolicyID);

            await Next(schemer, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class RuleData : NodeData {
        [SerializeReference] public string RuleID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public RuleData(string id, string groupId, Vector2 position, string ruleId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            RuleID = ruleId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);
            if ( !generatedNodeInfo.SchemerFactory.Scheme.IsValid ) {
                NDebug.Log("Invalid Scheme Actors.", NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var index = await Ruler.StartGraphAsync(RuleID, schemer.Conspirator, schemer.Target) ? 0 : 1;
            await Next(schemer, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class ValidatorData : NodeData {
        [SerializeReference] public string RuleID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public ValidatorData(string id, string groupId, Vector2 position, string ruleId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            RuleID = ruleId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Schemer schemer, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(schemer, generatedNodeInfo);

            if ( generatedNodeInfo.SchemerFactory.ErrorNameList.Count < 1 &&
                 generatedNodeInfo.SchemerFactory.WarningNameList.Count < 1 ) {
                End(generatedNodeInfo);
                return;
            }

            foreach ( var list in from outputData in Outputs
                     where outputData.ValidatorMode != ValidatorMode.Passive
                     where generatedNodeInfo.SchemerFactory.ErrorNameList.Contains(outputData.Name) ||
                           generatedNodeInfo.SchemerFactory.WarningNameList.Contains(outputData.Name)
                     select ( from port in outputData.DataCollection
                             select new Tuple< NodeData, string, string >(
                                 generatedNodeInfo.SchemerFactory.GetNode(port.NextID), port.NextName, port.ActorID) )
                         .ToList().OrderBy(n => n.Item1.Position.y) ) {
                foreach ( var nodeData in list ) {
                    var nodeInf = GenerateNodeData(nodeData.Item1);
                    nodeInf.inputName = nodeData.Item2;
                    var act = (NActor)schemer.GetVariable(nodeData.Item3);
                    if ( act != null )
                        nodeInf.actor = act.Value;
                    nodeInf.validator = generatedNodeInfo;
                    nodeInf.repeater = generatedNodeInfo.repeater;
                    nodeInf.sequencer = generatedNodeInfo.sequencer;
                    nodeInf.SchemerFactory = generatedNodeInfo.SchemerFactory;
                    nodeInf.bgWorker = generatedNodeInfo.bgWorker;

                    await nodeData.Item1.Run(schemer, nodeInf);
                }

                break;
            }
        }
    }

    #endregion

    #region CONDITION

    [Serializable]
    public class StartRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public StartRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            await Next(ruler, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class CommentRuleData : NodeData {
        [SerializeReference] public string Note;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public CommentRuleData(string id, string groupId, Vector2 position, string note) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Note = note;
        }
    }

    [Serializable]
    public class GetSchemeTableVariableRuleData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public string SchemeID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public override GenericNodeType GenericType => GenericNodeType.Rule;


        public GetSchemeTableVariableRuleData(string id, string groupId, Vector2 position, string variableId,
            string schemeId,
            string stringValue, int integerValue, float floatValue,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            SchemeID = schemeId;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            VariableType = type;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var actor = generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;
            var target = generatedNodeInfo.inputName == "Conspirator" ? ruler.Target : ruler.Conspirator;

            if ( target == null || actor == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var scheme = actor.GetScheme(SchemeID, target);
            if ( scheme == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var variable = scheme.Schemer.GetVariable(VariableID);

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(ruler, generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            End(ruler, generatedNodeInfo);
        }
    }

    [Serializable]
    public class SuccessRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public SuccessRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            generatedNodeInfo.RulerFactory.SetConditionalState(RuleState.Success);

            End(ruler, generatedNodeInfo);
        }
    }

    [Serializable]
    public class CauseRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public string ErrorName;
        [SerializeReference] public string Error;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public CauseRuleData(string id, string groupId, Vector2 position, string errorName, string error,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            ErrorName = errorName;
            Error = error;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            generatedNodeInfo.RulerFactory.ErrorList.Add(Error.SchemeFormat(ruler.Conspirator, ruler.Target));
            generatedNodeInfo.RulerFactory.ErrorNameList.Add(ErrorName);

            await Next(ruler, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class WarningRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public string WarningName;
        [SerializeReference] public string Warning;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public WarningRuleData(string id, string groupId, Vector2 position, string warningName, string warning,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            WarningName = warningName;
            Warning = warning;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            generatedNodeInfo.RulerFactory.WarningList.Add(Warning.SchemeFormat(ruler.Conspirator, ruler.Target));
            generatedNodeInfo.RulerFactory.WarningNameList.Add(WarningName);

            await Next(ruler, Outputs, generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetPolicyRuleData : NodeData {
        [SerializeReference] public string PolicyID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public GetPolicyRuleData(string id, string groupId, Vector2 position, string policyId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            PolicyID = policyId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var clan = generatedNodeInfo.clan;

            if ( generatedNodeInfo.inputName.Equals("Target Clan") && ruler.Target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            clan ??= generatedNodeInfo.inputName == "Conspirator Clan"
                ? ruler.Conspirator.Clan
                : ruler.Target.Clan;

            if ( string.IsNullOrEmpty(PolicyID) ) {
                var i = clan == null ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( clan == null ) {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = clan.Policies.Any(p => p.ID == PolicyID) ? 0 : 1;
            await Next(ruler, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class GetVariableRuleData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public EnumType EnumType;
        public override GenericNodeType GenericType => GenericNodeType.Rule;


        public GetVariableRuleData(string id, string groupId, Vector2 position, string variableId,
            string stringValue, int integerValue, float floatValue, EnumType enumType, NType type,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            EnumType = enumType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when ruler.Target == null:
                    End(ruler, generatedNodeInfo);
                    return;
                case "[Actor]" when generatedNodeInfo.actor == null:
                    End(ruler, generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => ruler.Conspirator.GetVariable(VariableID),
                "Global" => IM.GetVariable(VariableID),
                "Target" => ruler.Target.GetVariable(VariableID),
                "[Actor]" => generatedNodeInfo.actor.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(ruler, generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( IM.Variables.Any(v => v.id == VariableID) == false ) {
                NDebug.Log($"Variable({VariableType}) is missing.",
                    NLogType.Error);
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Enum ) {
                //Work in progress.
                if ( EnumType == EnumType.Is )
                    await Next(ruler, Outputs, generatedNodeInfo, (int)variable, false);
                else {
                    await Next(ruler, Outputs, generatedNodeInfo, (int)variable, false);
                }
            }

            End(ruler, generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetClanVariableRuleData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public List< OutputData > Outputs;
        [SerializeReference] public EnumType EnumType;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public GetClanVariableRuleData(string id, string groupId, Vector2 position, string variableId,
            string stringValue,
            int integerValue, float floatValue, EnumType enumType,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            EnumType = enumType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when ruler.Target == null || ruler.Target.Clan == null:
                    End(generatedNodeInfo);
                    return;
                case "Conspirator" when ruler.Conspirator == null || ruler.Conspirator.Clan == null:
                    End(generatedNodeInfo);
                    return;
                case "[Clan]" when generatedNodeInfo.clan == null:
                    End(generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => ruler.Conspirator.Clan.GetVariable(VariableID),
                "Target" => ruler.Target.Clan.GetVariable(VariableID),
                "[Clan]" => generatedNodeInfo.clan.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( IM.Variables.Any(v => v.id == VariableID) == false ) {
                NDebug.Log($"Variable({VariableType}) is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Enum ) {
                //Work in progress.
                if ( EnumType == EnumType.Is )
                    await Next(ruler, Outputs, generatedNodeInfo, (int)variable, false);
                else {
                    await Next(ruler, Outputs, generatedNodeInfo, (int)variable, false);
                }
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class GetFamilyVariableRuleData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public List< OutputData > Outputs;
        [SerializeReference] public EnumType EnumType;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public GetFamilyVariableRuleData(string id, string groupId, Vector2 position, string variableId,
            string stringValue,
            int integerValue, float floatValue, EnumType enumType,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            EnumType = enumType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target" when ruler.Target == null || ruler.Target.Family == null:
                    End(generatedNodeInfo);
                    return;
                case "Conspirator" when ruler.Conspirator == null || ruler.Conspirator.Family == null:
                    End(generatedNodeInfo);
                    return;
                case "[Family]" when generatedNodeInfo.family == null:
                    End(generatedNodeInfo);
                    return;
            }

            var variable = generatedNodeInfo.inputName switch {
                "Conspirator" => ruler.Conspirator.Family.GetVariable(VariableID),
                "Target" => ruler.Target.Family.GetVariable(VariableID),
                "[Family]" => generatedNodeInfo.family.GetVariable(VariableID),
                _ => throw new ArgumentOutOfRangeException()
            };

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( IM.Variables.Any(v => v.id == VariableID) == false ) {
                NDebug.Log($"Variable({VariableType}) is missing.",
                    NLogType.Error);
                End(generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Enum ) {
                //Work in progress.
                if ( EnumType == EnumType.Is )
                    await Next(ruler, Outputs, generatedNodeInfo, (int)variable, false);
                else {
                    await Next(ruler, Outputs, generatedNodeInfo, (int)variable, false);
                }
            }

            End(generatedNodeInfo);
        }
    }

    [Serializable]
    public class SchemeIsActiveRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public string SchemeID;
        [SerializeReference] public Actor.VerifyType VerifyType;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public SchemeIsActiveRuleData(string id, string groupId, Vector2 position, string schemeId,
            Actor.VerifyType verifyType,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            SchemeID = schemeId;
            VerifyType = verifyType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var actor = generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator :
                generatedNodeInfo.inputName == "Target" ? ruler.Target : null;
            var target = generatedNodeInfo.inputName == "Conspirator" ? ruler.Target :
                generatedNodeInfo.inputName == "Target" ? ruler.Conspirator : null;

            if ( actor != null ) {
                await Next(ruler, Outputs, generatedNodeInfo,
                    actor.SchemeIsActive(SchemeID, VerifyType, target) ? 0 : 1);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            await Next(ruler, Outputs, generatedNodeInfo,
                generatedNodeInfo.dualActor.Item1.SchemeIsActive(SchemeID, VerifyType,
                    generatedNodeInfo.dualActor.Item2)
                    ? 0
                    : 1);
        }
    }

    [Serializable]
    public class IsAIRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public IsAIRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var index = IM.IsPlayer(target) ? 1 : 0;
            await Next(ruler, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class GetActorRuleData : NodeData {
        [SerializeReference] public string ActorID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;


        public GetActorRuleData(string id, string groupId, Vector2 position, string actorID,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            ActorID = actorID;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var index = target.ID == ActorID ? 0 : 1;
            await Next(ruler, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class GetFamilyRuleData : NodeData {
        [SerializeReference] public string FamilyID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;


        public GetFamilyRuleData(string id, string groupId, Vector2 position, string familyId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            FamilyID = familyId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( string.IsNullOrEmpty(FamilyID) ) {
                var i = target.Family == null ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( target.Family == null ) {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = target.Family.ID == FamilyID ? 0 : 1;
            await Next(ruler, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class GetClanRuleData : NodeData {
        [SerializeReference] public string ClanID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;


        public GetClanRuleData(string id, string groupId, Vector2 position, string clanId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            ClanID = clanId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( string.IsNullOrEmpty(ClanID) ) {
                var i = target.Clan == null ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( target.Clan == null ) {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = target.Clan.ID == ClanID ? 0 : 1;
            await Next(ruler, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class GetCultureRuleData : NodeData {
        [SerializeReference] public string CultureID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;


        public GetCultureRuleData(string id, string groupId, Vector2 position, string cultureId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            CultureID = cultureId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( string.IsNullOrEmpty(CultureID) ) {
                var i = target.Culture == null ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( target.Culture == null ) {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = target.Culture.ID == CultureID ? 0 : 1;
            await Next(ruler, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class GetRoleRuleData : NodeData {
        [SerializeReference] public string RoleID;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;


        public GetRoleRuleData(string id, string groupId, Vector2 position, string roleId,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            RoleID = roleId;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( string.IsNullOrEmpty(RoleID) ) {
                var i = target.Role == null ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, i);
                return;
            }

            if ( target.Role == null ) {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
                return;
            }

            var j = target.Role.ID == RoleID ? 0 : 1;
            await Next(ruler, Outputs, generatedNodeInfo, j);
        }
    }

    [Serializable]
    public class GenderRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;


        public GenderRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( ruler.Conspirator == null || ruler.Target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            var index = target.Gender == Actor.IGender.Male ? 0 : 1;
            await Next(ruler, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class AgeRuleData : NodeData {
        [SerializeReference] public int Age;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public AgeRuleData(string id, string groupId, Vector2 position, int age, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Age = age;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var value = target.Age;

            if ( value == Age )
                await Next(ruler, Outputs, generatedNodeInfo);

            if ( value != Age )
                await Next(ruler, Outputs, generatedNodeInfo, 1);

            if ( value > Age )
                await Next(ruler, Outputs, generatedNodeInfo, 2);

            if ( value < Age )
                await Next(ruler, Outputs, generatedNodeInfo, 3);

            if ( value >= Age )
                await Next(ruler, Outputs, generatedNodeInfo, 4);

            if ( value <= Age )
                await Next(ruler, Outputs, generatedNodeInfo, 5);
        }
    }

    [Serializable]
    public class SpouseCountRuleData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public SpouseCountRuleData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var value = target.Spouses(IncludePassiveCharacters).Count();

            if ( value == Count )
                await Next(ruler, Outputs, generatedNodeInfo);

            if ( value != Count )
                await Next(ruler, Outputs, generatedNodeInfo, 1);

            if ( value > Count )
                await Next(ruler, Outputs, generatedNodeInfo, 2);

            if ( value < Count )
                await Next(ruler, Outputs, generatedNodeInfo, 3);

            if ( value >= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 4);

            if ( value <= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 5);
        }
    }

    [Serializable]
    public class ChildCountRuleData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public ChildCountRuleData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var value = target.Children(IncludePassiveCharacters).Count();

            if ( value == Count )
                await Next(ruler, Outputs, generatedNodeInfo);

            if ( value != Count )
                await Next(ruler, Outputs, generatedNodeInfo, 1);

            if ( value > Count )
                await Next(ruler, Outputs, generatedNodeInfo, 2);

            if ( value < Count )
                await Next(ruler, Outputs, generatedNodeInfo, 3);

            if ( value >= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 4);

            if ( value <= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 5);
        }
    }

    [Serializable]
    public class ParentCountRuleData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public ParentCountRuleData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var value = target.Parents(IncludePassiveCharacters).Count();

            if ( value == Count )
                await Next(ruler, Outputs, generatedNodeInfo);

            if ( value != Count )
                await Next(ruler, Outputs, generatedNodeInfo, 1);

            if ( value > Count )
                await Next(ruler, Outputs, generatedNodeInfo, 2);

            if ( value < Count )
                await Next(ruler, Outputs, generatedNodeInfo, 3);

            if ( value >= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 4);

            if ( value <= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 5);
        }
    }

    [Serializable]
    public class SiblingCountRuleData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public SiblingCountRuleData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var value = target.Siblings(IncludePassiveCharacters).Count();

            if ( value == Count )
                await Next(ruler, Outputs, generatedNodeInfo);

            if ( value != Count )
                await Next(ruler, Outputs, generatedNodeInfo, 1);

            if ( value > Count )
                await Next(ruler, Outputs, generatedNodeInfo, 2);

            if ( value < Count )
                await Next(ruler, Outputs, generatedNodeInfo, 3);

            if ( value >= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 4);

            if ( value <= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 5);
        }
    }

    [Serializable]
    public class GrandchildCountRuleData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public GrandchildCountRuleData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var value = target.Grandchildren(IncludePassiveCharacters).Count();

            if ( value == Count )
                await Next(ruler, Outputs, generatedNodeInfo);

            if ( value != Count )
                await Next(ruler, Outputs, generatedNodeInfo, 1);

            if ( value > Count )
                await Next(ruler, Outputs, generatedNodeInfo, 2);

            if ( value < Count )
                await Next(ruler, Outputs, generatedNodeInfo, 3);

            if ( value >= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 4);

            if ( value <= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 5);
        }
    }

    [Serializable]
    public class GrandparentCountRuleData : NodeData {
        [SerializeReference] public int Count;
        [SerializeReference] public bool IncludePassiveCharacters;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public GrandparentCountRuleData(string id, string groupId, Vector2 position, int count,
            bool includePassiveCharacters, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Count = count;
            IncludePassiveCharacters = includePassiveCharacters;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var value = target.Grandparents(IncludePassiveCharacters).Count();

            if ( value == Count )
                await Next(ruler, Outputs, generatedNodeInfo);

            if ( value != Count )
                await Next(ruler, Outputs, generatedNodeInfo, 1);

            if ( value > Count )
                await Next(ruler, Outputs, generatedNodeInfo, 2);

            if ( value < Count )
                await Next(ruler, Outputs, generatedNodeInfo, 3);

            if ( value >= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 4);

            if ( value <= Count )
                await Next(ruler, Outputs, generatedNodeInfo, 5);
        }
    }

    [Serializable]
    public class SameClanRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public SameClanRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( ruler.Conspirator == null || ruler.Target == null ) {
                    End(ruler, generatedNodeInfo);
                    return;
                }

                if ( ruler.Conspirator.Clan != null &&
                     ruler.Conspirator.Clan == ruler.Target.Clan ) {
                    await Next(ruler, Outputs, generatedNodeInfo);
                } else {
                    await Next(ruler, Outputs, generatedNodeInfo, 1);
                }

                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.Clan != null &&
                 generatedNodeInfo.dualActor.Item1.Clan == generatedNodeInfo.dualActor.Item2.Clan ) {
                await Next(ruler, Outputs, generatedNodeInfo);
            } else {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class SameFamilyRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public SameFamilyRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( ruler.Conspirator == null || ruler.Target == null ) {
                    End(ruler, generatedNodeInfo);
                    return;
                }

                if ( ruler.Conspirator.Family != null &&
                     ruler.Conspirator.Family == ruler.Target.Family ) {
                    await Next(ruler, Outputs, generatedNodeInfo);
                } else {
                    await Next(ruler, Outputs, generatedNodeInfo, 1);
                }

                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.Family != null &&
                 generatedNodeInfo.dualActor.Item1.Family == generatedNodeInfo.dualActor.Item2.Family ) {
                await Next(ruler, Outputs, generatedNodeInfo);
            } else {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class SameCultureRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public SameCultureRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( ruler.Conspirator == null || ruler.Target == null ) {
                    End(ruler, generatedNodeInfo);
                    return;
                }

                if ( ruler.Conspirator.Culture != null &&
                     ruler.Conspirator.Culture == ruler.Target.Culture ) {
                    await Next(ruler, Outputs, generatedNodeInfo);
                } else {
                    await Next(ruler, Outputs, generatedNodeInfo, 1);
                }

                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.Culture != null &&
                 generatedNodeInfo.dualActor.Item1.Culture == generatedNodeInfo.dualActor.Item2.Culture ) {
                await Next(ruler, Outputs, generatedNodeInfo);
            } else {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class SameGenderRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public SameGenderRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( ruler.Conspirator == null || ruler.Target == null ) {
                    End(ruler, generatedNodeInfo);
                    return;
                }

                if ( ruler.Conspirator.Gender == ruler.Target.Gender ) {
                    await Next(ruler, Outputs, generatedNodeInfo);
                } else {
                    await Next(ruler, Outputs, generatedNodeInfo, 1);
                }

                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.Gender == generatedNodeInfo.dualActor.Item2.Gender ) {
                await Next(ruler, Outputs, generatedNodeInfo);
            } else {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class GetStateRuleData : NodeData {
        [SerializeReference] public Actor.IState State;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public GetStateRuleData(string id, string groupId, Vector2 position, Actor.IState state,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            State = state;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( target.State == State ) {
                await Next(ruler, Outputs, generatedNodeInfo);
            } else {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
            }
        }
    }

    [Serializable]
    public class IsParentRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public IsParentRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target": {
                    if ( ruler.Conspirator == null || ruler.Target == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( ruler.Target.IsParent(ruler.Conspirator) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "Conspirator": {
                    if ( ruler.Conspirator == null || ruler.Target == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( ruler.Conspirator.IsParent(ruler.Target) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "[Dual]": {
                    if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( generatedNodeInfo.dualActor.Item2.IsParent(generatedNodeInfo.dualActor.Item1) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
            }

            await Next(ruler, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class HasHeirRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public HasHeirRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var target = generatedNodeInfo.actor;

            target ??= generatedNodeInfo.inputName == "Conspirator" ? ruler.Conspirator : ruler.Target;

            if ( generatedNodeInfo.inputName.Equals("Target") && target == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            int index = target.Heir != null ? 0 : 1;
            await Next(ruler, Outputs, generatedNodeInfo, index);
        }
    }

    [Serializable]
    public class IsGrandchildRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public IsGrandchildRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target": {
                    if ( ruler.Conspirator == null || ruler.Target == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( ruler.Target.IsGrandchild(ruler.Conspirator) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "Conspirator": {
                    if ( ruler.Conspirator == null || ruler.Target == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( ruler.Conspirator.IsGrandchild(ruler.Target) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "[Dual]": {
                    if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( generatedNodeInfo.dualActor.Item2.IsGrandchild(generatedNodeInfo.dualActor.Item1) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
            }

            await Next(ruler, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class IsRelativeRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public IsRelativeRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( ruler.Conspirator == null || ruler.Target == null ) {
                    End(ruler, generatedNodeInfo);
                    return;
                }

                if ( ruler.Conspirator.IsRelative(ruler.Target) ) {
                    await Next(ruler, Outputs, generatedNodeInfo);
                    return;
                }

                await Next(ruler, Outputs, generatedNodeInfo, 1);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1.IsRelative(generatedNodeInfo.dualActor.Item2) ) {
                await Next(ruler, Outputs, generatedNodeInfo);
                return;
            }

            await Next(ruler, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class IsSpouseRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public IsSpouseRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( ruler.Conspirator == null || ruler.Target == null ) {
                    End(ruler, generatedNodeInfo);
                    return;
                }

                await Next(ruler, Outputs, generatedNodeInfo, ruler.Conspirator.IsSpouse(ruler.Target) ? 0 : 1);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            await Next(ruler, Outputs, generatedNodeInfo,
                generatedNodeInfo.dualActor.Item1.IsSpouse(generatedNodeInfo.dualActor.Item2) ? 0 : 1);
        }
    }

    [Serializable]
    public class IsChildRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public IsChildRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target": {
                    if ( ruler.Conspirator == null || ruler.Target == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( ruler.Target.IsChild(ruler.Conspirator) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "Conspirator": {
                    if ( ruler.Conspirator == null || ruler.Target == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( ruler.Conspirator.IsChild(ruler.Target) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "[Dual]": {
                    if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( generatedNodeInfo.dualActor.Item2.IsChild(generatedNodeInfo.dualActor.Item1) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
            }

            await Next(ruler, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class IsSiblingRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public IsSiblingRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( generatedNodeInfo.inputName == "In" ) {
                if ( ruler.Conspirator == null || ruler.Target == null ) {
                    End(ruler, generatedNodeInfo);
                    return;
                }

                await Next(ruler, Outputs, generatedNodeInfo, ruler.Conspirator.IsSibling(ruler.Target) ? 0 : 1);
                return;
            }

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            await Next(ruler, Outputs, generatedNodeInfo,
                generatedNodeInfo.dualActor.Item1.IsSibling(generatedNodeInfo.dualActor.Item2) ? 0 : 1);
        }
    }

    [Serializable]
    public class IsGrandparentRuleData : NodeData {
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public IsGrandparentRuleData(string id, string groupId, Vector2 position, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            switch ( generatedNodeInfo.inputName ) {
                case "Target": {
                    if ( ruler.Conspirator == null || ruler.Target == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( ruler.Target.IsGrandparent(ruler.Conspirator) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "Conspirator": {
                    if ( ruler.Conspirator == null || ruler.Target == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( ruler.Conspirator.IsGrandparent(ruler.Target) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
                case "[Dual]": {
                    if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                        End(ruler, generatedNodeInfo);
                        return;
                    }

                    if ( generatedNodeInfo.dualActor.Item2.IsGrandparent(generatedNodeInfo.dualActor.Item1) ) {
                        await Next(ruler, Outputs, generatedNodeInfo);
                        return;
                    }

                    break;
                }
            }

            await Next(ruler, Outputs, generatedNodeInfo, 1);
        }
    }

    [Serializable]
    public class ReturnActorRuleData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public ReturnActorRuleData(string id, string groupId, Vector2 position, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var actor = await Dispatcher.InvokeAsync(() =>
                IInvoke.Invoke_GetActor(ruler.Conspirator, ruler.Target, MethodName));

            if ( actor == null ) {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
            } else {
                generatedNodeInfo.actor = actor;
                await Next(ruler, Outputs, generatedNodeInfo);
            }
        }
    }

    [Serializable]
    public class ReturnClanRuleData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public ReturnClanRuleData(string id, string groupId, Vector2 position, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var clan = await Dispatcher.InvokeAsync(() =>
                IInvoke.Invoke_GetClan(ruler.Conspirator, ruler.Target, MethodName));

            if ( clan == null ) {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
            } else {
                generatedNodeInfo.clan = clan;
                await Next(ruler, Outputs, generatedNodeInfo);
            }
        }
    }

    [Serializable]
    public class ReturnFamilyRuleData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public List< OutputData > Outputs;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public ReturnFamilyRuleData(string id, string groupId, Vector2 position, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            var family = await Dispatcher.InvokeAsync(() =>
                IInvoke.Invoke_GetFamily(ruler.Conspirator, ruler.Target, MethodName));

            if ( family == null ) {
                await Next(ruler, Outputs, generatedNodeInfo, 1);
            } else {
                generatedNodeInfo.family = family;
                await Next(ruler, Outputs, generatedNodeInfo);
            }
        }
    }

    [Serializable]
    public class DualActorRuleData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public DualType DualType;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public DualActorRuleData(string id, string groupId, Vector2 position, DualType dualType, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            DualType = dualType;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            switch ( DualType ) {
                case DualType.GetActors: {
                    var actor = await Dispatcher.InvokeAsync(() =>
                        IInvoke.Invoke_GetDualActor(ruler.Conspirator, ruler.Target, MethodName));

                    if ( actor.Item1 == null && actor.Item2 == null ) {
                        await Next(ruler, Outputs, generatedNodeInfo, 1);
                    } else if ( actor.Item1 == null && actor.Item2 != null ) {
                        await Next(ruler, Outputs, generatedNodeInfo, 2);
                    } else if ( actor.Item1 != null && actor.Item1 == null ) {
                        await Next(ruler, Outputs, generatedNodeInfo, 3);
                    } else if ( actor.Item1 != null && actor.Item2 != null ) {
                        generatedNodeInfo.dualActor = ( actor.Item1, actor.Item2 );
                        await Next(ruler, Outputs, generatedNodeInfo);
                    }

                    break;
                }
                case DualType.Conspirator_Target when ruler.Conspirator == null && ruler.Target == null:
                    await Next(ruler, Outputs, generatedNodeInfo, 1);
                    break;
                case DualType.Conspirator_Target when ruler.Conspirator == null && ruler.Target != null:
                    await Next(ruler, Outputs, generatedNodeInfo, 2);
                    break;
                case DualType.Conspirator_Target when ruler.Conspirator != null && ruler.Target == null:
                    await Next(ruler, Outputs, generatedNodeInfo, 3);
                    break;
                case DualType.Conspirator_Target: {
                    if ( ruler.Conspirator != null && ruler.Target != null ) {
                        generatedNodeInfo.dualActor = ( ruler.Conspirator, ruler.Target );
                        await Next(ruler, Outputs, generatedNodeInfo);
                    }

                    break;
                }
                case DualType.Target_Conspirator when ruler.Conspirator == null && ruler.Target == null:
                    await Next(ruler, Outputs, generatedNodeInfo, 1);
                    break;
                case DualType.Target_Conspirator when ruler.Target == null && ruler.Conspirator != null:
                    await Next(ruler, Outputs, generatedNodeInfo, 2);
                    break;
                case DualType.Target_Conspirator when ruler.Target != null && ruler.Conspirator == null:
                    await Next(ruler, Outputs, generatedNodeInfo, 3);
                    break;
                case DualType.Target_Conspirator: {
                    if ( ruler.Conspirator != null && ruler.Target != null ) {
                        generatedNodeInfo.dualActor = ( ruler.Target, ruler.Conspirator );
                        await Next(ruler, Outputs, generatedNodeInfo);
                    }

                    break;
                }
            }
        }
    }

    [Serializable]
    public class GetRelationVariableRuleData : NodeData {
        [SerializeReference] public string VariableID;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public string StringValue;
        [SerializeReference] public float FloatValue;
        [SerializeReference] public int IntegerValue;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;
        [SerializeReference] public EnumType EnumType;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public GetRelationVariableRuleData() {  }

        public GetRelationVariableRuleData(string id, string groupId, Vector2 position, string variableId,
            string stringValue,
            int integerValue, float floatValue, EnumType enumType,
            NType type, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            VariableID = variableId;
            VariableType = type;
            StringValue = stringValue;
            IntegerValue = integerValue;
            FloatValue = floatValue;
            EnumType = enumType;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( generatedNodeInfo.dualActor.Item1 == null || generatedNodeInfo.dualActor.Item2 == null ) {
                End(ruler, generatedNodeInfo);
                return;
            }

            var variable =
                generatedNodeInfo.dualActor.Item1.GetRelationVariable(VariableID, generatedNodeInfo.dualActor.Item2);

            if ( variable == null ) {
                NDebug.Log("Variable is missing.",
                    NLogType.Error);
                End(ruler, generatedNodeInfo);
                return;
            }

            var variableType = variable.Type;

            if ( variableType != VariableType ) {
                NDebug.Log(
                    $"Variable type mismatch. Valid Type: {VariableType}",
                    NLogType.Error);
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( IM.Variables.Any(v => v.id == VariableID) == false ) {
                NDebug.Log($"Variable({VariableType}) is missing.",
                    NLogType.Error);
                End(ruler, generatedNodeInfo);
                return;
            }

            if ( variableType == NType.String ) {
                var index = (string)variable == StringValue ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Integer ) {
                var value = (int)variable;

                if ( value == IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( value != IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= IntegerValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Bool ) {
                var index = (int)variable == 1 ? 0 : 1;
                await Next(ruler, Outputs, generatedNodeInfo, index, false);
            }

            if ( variableType == NType.Float ) {
                var value = (float)variable;

                if ( value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 0, false);
                }

                if ( !value.Equals(FloatValue) ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 1, false);
                }

                if ( value > FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 2, false);
                }

                if ( value < FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 3, false);
                }

                if ( value >= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 4, false);
                }

                if ( value <= FloatValue ) {
                    await Next(ruler, Outputs, generatedNodeInfo, 5, false);
                }
            }

            if ( variableType == NType.Enum ) {
                //Work in progress.
                if ( EnumType == EnumType.Is )
                    await Next(ruler, Outputs, generatedNodeInfo, (int)variable, false);
                else {
                    await Next(ruler, Outputs, generatedNodeInfo, (int)variable, false);
                }
            }

            End(ruler, generatedNodeInfo);
        }
    }

    [Serializable]
    public class InvokeRuleData : NodeData {
        [SerializeReference] public string MethodName;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public InvokeRuleData(string id, string groupId, Vector2 position, string methodName,
            IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            MethodName = methodName;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            IResult result = IResult.Null;
            if ( generatedNodeInfo.RulerFactory.IsAsync ) {
                await Dispatcher.InvokeAsync(() => {
                    result = IInvoke.Invoke(ruler.Conspirator, ruler.Target, MethodName);
                });
            } else {
                result = IInvoke.Invoke(ruler.Conspirator, ruler.Target, MethodName);
            }

            await Next(ruler, Outputs, generatedNodeInfo, (int)result);
        }
    }

    [Serializable]
    public class LogRuleData : NodeData {
        [SerializeReference] public string Log;
        [SerializeReference] public List< OutputData > Outputs;
        public override List< OutputData > GetOutputs() => Outputs;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public LogRuleData(string id, string groupId, Vector2 position, string log, IEnumerable< OutputData > outputs) {
            ID = id;
            GroupId = groupId;
            Position = position;
            Log = log;
            Outputs = new List< OutputData >(outputs);
        }

        public override async Task Run(Ruler ruler, NodeInfo nodeInfo) {
            var generatedNodeInfo = nodeInfo ?? GenerateNodeData(this);
            await base.Run(ruler, generatedNodeInfo);

            if ( generatedNodeInfo.RulerFactory.IsAsync ) {
                await Dispatcher.InvokeAsync(() => {
                    NDebug.Log(Log.SchemeFormat(ruler.Conspirator, ruler.Target), NLogType.Log, true);
                });
            } else {
                NDebug.Log(Log.SchemeFormat(ruler.Conspirator, ruler.Target), NLogType.Log, true);
            }

            await Next(ruler, Outputs, generatedNodeInfo);
        }
    }

    #endregion

    #region GHOST

    [Serializable]
    public class GhostClanData : NodeData {
        public override GenericNodeType GenericType => GenericNodeType.Clan;

        public GhostClanData(string id, string groupId, Vector2 position) {
            ID = id;
            GroupId = groupId;
            Position = position;
        }
    }

    [Serializable]
    public class GhostFamilyData : NodeData {
        public override GenericNodeType GenericType => GenericNodeType.Family;

        public GhostFamilyData(string id, string groupId, Vector2 position) {
            ID = id;
            GroupId = groupId;
            Position = position;
        }
    }

    #endregion

    #region UNGROUPED

    [Serializable]
    public class PolicyData : NodeData {
        [SerializeReference] public string PolicyName;
        [SerializeReference] public string Description;
        [SerializeReference] public Sprite Icon;
        [SerializeReference] public PolicyType Type;
        public override GenericNodeType GenericType => GenericNodeType.Clan;

        public PolicyData(string id, Vector2 position, string policyName, string description, PolicyType type,
            Sprite icon) {
            ID = id;
            Position = position;
            PolicyName = policyName;
            Description = description;
            Type = type;
            Icon = icon;
        }
    }

    [Serializable]
    public class ActorData : NodeData {
        [SerializeReference] public string ActorName;
        [SerializeReference] public int Age;
        [SerializeReference] public Actor.IGender Gender;
        [SerializeReference] public Actor.IState State;
        [SerializeReference] public string CultureID;
        [SerializeReference] public Sprite Portrait;
        [SerializeReference] public bool IsPlayer;
        public override GenericNodeType GenericType => GenericNodeType.Actor;

        public ActorData(string id, Vector2 position, string actorName, int age, Actor.IGender gender,
            Actor.IState state, string cultureId, Sprite portrait, bool isPlayer) {
            ID = id;
            Position = position;
            ActorName = actorName;
            Age = age;
            Gender = gender;
            State = state;
            CultureID = cultureId;
            Portrait = portrait;
            IsPlayer = isPlayer;
        }
    }

    [Serializable]
    public class CultureData : NodeData {
        [SerializeReference] public string CultureName;
        [SerializeReference] public string Description;
        [SerializeReference] public Sprite Icon;
        [SerializeReference] public List< string > MaleNames;
        [SerializeReference] public List< string > FemaleNames;
        public override GenericNodeType GenericType => GenericNodeType.Culture;

        public CultureData(string id, Vector2 position, string cultureName, string description,
            Sprite icon, IEnumerable< string > maleNames, IEnumerable< string > femaleNames) {
            ID = id;
            Position = position;
            CultureName = cultureName;
            Description = description;
            Icon = icon;
            MaleNames = new List< string >(maleNames.ToList());
            FemaleNames = new List< string >(femaleNames.ToList());
        }
    }

    [Serializable]
    public class VariableData : NodeData {
        [SerializeReference] public string VariableName;
        [SerializeReference] public NType VariableType;
        [SerializeReference] public NVar Variable;
        [SerializeReference] public string EnumValue;
        public override GenericNodeType GenericType => GenericNodeType.Variable;

        public VariableData(string id, Vector2 position, string variableName, string enumValue, NType type,
            NVar variable) {
            ID = id;
            Position = position;
            VariableName = variableName;
            VariableType = type;
            Variable = variable.Duplicate();
            EnumValue = enumValue;
        }
    }

    [Serializable]
    public class RoleData : NodeData {
        [SerializeReference] public string RoleName;
        [SerializeReference] public string Description;
        [SerializeReference] public string FilterID;
        [SerializeReference] public int RoleSlot;
        [SerializeReference] public bool Legacy;
        [SerializeReference] public string TitleForMale;
        [SerializeReference] public string TitleForFemale;
        [SerializeReference] public Sprite RoleIcon;
        [SerializeReference] public int Priority;
        public override GenericNodeType GenericType => GenericNodeType.Clan;

        public RoleData(string id, Vector2 position, string roleName, string description, string filterId, int roleSlot,
            bool legacy,
            string titleForMale, string titleForFemale, Sprite roleIcon, int priority) {
            ID = id;
            Position = position;
            RoleName = roleName;
            Description = description;
            FilterID = filterId;
            RoleSlot = roleSlot;
            Legacy = legacy;
            RoleIcon = roleIcon;
            TitleForMale = titleForMale;
            TitleForFemale = titleForFemale;
            Priority = priority;
        }
    }

    [Serializable]
    public class HeirFilterData : NodeData {
        [SerializeReference] public string FilterName;
        [SerializeReference] public int Gender;
        [SerializeReference] public int Age;
        [SerializeReference] public int Clan;
        [SerializeReference] public List< int > Relatives;
        public override GenericNodeType GenericType => GenericNodeType.Clan;

        public HeirFilterData(string id, Vector2 position, string filterName, int gender, int age, int clan,
            IEnumerable< int > relatives) {
            ID = id;
            Position = position;
            FilterName = filterName;
            Gender = gender;
            Age = age;
            Clan = clan;
            Relatives = new List< int >(relatives);
        }
    }

    #endregion
    
    #region ADAPTER
    
    [Serializable]
    public class SetSpecificVariableData : SetRelationVariableData { }

    [Serializable]
    public class GetSpecificVariableRuleData : GetRelationVariableRuleData { }

    
    #endregion
}