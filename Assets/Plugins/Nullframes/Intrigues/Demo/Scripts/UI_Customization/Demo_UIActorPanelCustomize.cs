using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nullframes.Intrigues.UI {
    public class Demo_UIActorPanelCustomize : IUIActorBehaviour {
        public TooltipTrigger actorTooltip;
        public TooltipTrigger roleTooltip;
        public TooltipTrigger cultureTooltip;
        
        public TooltipTrigger clanTooltip;
        public GameObject clanObject;

        // public GameObject schemesTab;

        public Button swapButton;

        public override void OnMenuOpened(Actor actor) {
            UpdateInfo(actor);
        }

        public override void OnPageUpdated() {
            UpdateInfo(Actor);
        }

        public override void OnFamilyMemberLoaded(Actor actor, Transform Transform) {
            TooltipTrigger tooltipTrigger = Transform.GetComponent<TooltipTrigger>();

            tooltipTrigger.header = actor.FullName;
            tooltipTrigger.content = $"\n\u25CF {IntrigueSystemUI.GetRelationName(actor) ?? "Stranger"}";
            if (actor.Clan != null)
                tooltipTrigger.content += $"\n\u25CF Clan: {actor.Clan.ClanName}";

            if (actor.Culture != null)
                tooltipTrigger.content += $"\n\u25CF Culture: {actor.Culture.CultureName}";

            tooltipTrigger.content += $"\n\u25CF Age: {actor.Age}";
            tooltipTrigger.content += $"\n\u25CF Gender: {actor.Gender}";
            if (actor.Role is { Inheritance: true } && actor.Heir) {
                tooltipTrigger.content +=
                    $"\n\u25CF Heir: {actor.Heir.FullName} ({IntrigueSystemUI.GetRelationName(actor.Heir) ?? "Stranger"})";
            }

            NEnum hooked = IM.Player.GetRelationVariable<NEnum>("Hooked", actor);
            if (hooked == null) return;
            // Hook (Custom Variable Info)
            if (hooked.Index > 0) {
                tooltipTrigger.content += "\n\n\u25CF <color=#ed4a68>You hold leverage against this character.</color>";
            }
        }

        public override void OnSchemeButtonTriggered(Transform Transform) {
            if (Scheme.TargetNotRequired) {
                var dialogueForNoTarget =
                    DialogueManager.OpenDialogue(Scheme.SchemeName,
                        $"My Lord, \n\nI am ready to initiate the scheme.\nBefore we proceed, my Lord, you should review the potential consequences of our actions.\n\n" +
                        "The possible outcomes of this plan, designed to achieve our objective, are as follows:\n\n" +
                        $"{Scheme.Description.SchemeFormat(Conspirator, Target)}\n\nConsidering these consequences, are you sure you want to proceed with this scheme?");

                dialogueForNoTarget.AddChoice("Very well, begin the preparations to execute the scheme.",
                    IM.Approval_Icon, () => {
                        dialogueForNoTarget = DialogueManager.OpenDialogue(Scheme.SchemeName,
                            "Understood, my Lord. \n\nI shall commence the necessary preparations without delay. Rest assured, every aspect will be handled meticulously to ensure the success of our scheme.\nPlace your trust in my abilities, and together, we shall navigate the intricate web of schemes with finesse and precision.");

                        dialogueForNoTarget.AddChoice("Good.");
                        StartScheme();
                        IntrigueSystemUI.instance.Refresh();
                    }, () => Scheme.IsCompatible(Conspirator, Target));

                dialogueForNoTarget.AddChoice("No, hold on. The risks are too great. We must reconsider our approach.",
                    IM.Cancel_Icon);
                return;
            }
            
            var dialogueForTarget =
                DialogueManager.OpenDialogue(Scheme.SchemeName,
                    $"My Lord, \n\nI am ready to initiate the scheme against our target.\nBefore we proceed, my Lord, you should review the potential consequences of our actions.\n\n" +
                    "The possible outcomes of this plan, designed to achieve our objective, are as follows:\n\n" +
                    $"{Scheme.Description.SchemeFormat(Conspirator, Target)}\n\nConsidering these consequences, are you sure you want to proceed with this scheme?");

            dialogueForTarget.AddChoice("Very well, begin the preparations to execute the scheme.",
                IM.Approval_Icon, () => {
                    dialogueForTarget = DialogueManager.OpenDialogue(Scheme.SchemeName,
                        "Understood, my Lord. \n\nI shall commence the necessary preparations without delay. Rest assured, every aspect will be handled meticulously to ensure the success of our scheme.\nPlace your trust in my abilities, and together, we shall navigate the intricate web of schemes with finesse and precision.");

                    dialogueForTarget.AddChoice("Good.");
                    StartScheme();
                    IntrigueSystemUI.instance.Refresh();
                }, () => Scheme.IsCompatible(Conspirator, Target));

            dialogueForTarget.AddChoice("No, hold on. The risks are too great. We must reconsider our approach.",
                IM.Cancel_Icon);
        }

        public override void OnAvailableSchemeLoaded(RuleResult result, Scheme scheme, Transform Transform) {
            TooltipTrigger tooltipTrigger = Transform.GetComponent<TooltipTrigger>();

            if (result.Result == RuleState.Failed) {
                if (result.ErrorList.Any()) // If any Info;
                {
                    tooltipTrigger.header = "You cannot initiate this scheme for the following reasons;";
                    tooltipTrigger.content = string.Join("\n", result.ErrorList);
                    tooltipTrigger.content += "\n";
                }
                else {
                    tooltipTrigger.header += "You cannot initiate this scheme against them.";
                }
            }

            if (result.WarningList.Any()) {
                tooltipTrigger.content += "<color=#a69e6c>WARNING</color>\n";
                tooltipTrigger.content += string.Join("\n", result.WarningList);
            }
        }
        
        
        public override void OnPolicyLoaded(Policy policy, Transform Transform) {
            TooltipTrigger policyTooltip = Transform.GetComponent<TooltipTrigger>();

            policyTooltip.header = policy.PolicyName;
            policyTooltip.content = policy.Description;
        }
        
        private void UpdateInfo(Actor actor) {
            // Actor Info
            actorTooltip.header = actor.FullName;
            actorTooltip.content = $"\n\u25CF {IntrigueSystemUI.GetRelationName(actor) ?? "Stranger"}";
            if (actor.Clan != null)
                actorTooltip.content += $"\n\u25CF Clan: {actor.Clan.ClanName}";

            if (actor.Culture != null)
                actorTooltip.content += $"\n\u25CF Culture: {actor.Culture.CultureName}";

            actorTooltip.content += $"\n\u25CF Age: {actor.Age}";
            actorTooltip.content += $"\n\u25CF Gender: {actor.Gender}";
            if (actor.Role is { Inheritance: true } && actor.Heir) {
                actorTooltip.content +=
                    $"\n\u25CF Heir: {actor.Heir.FullName} ({IntrigueSystemUI.GetRelationName(actor.Heir) ?? "Stranger"})";
            }

            // Role Info
            if (actor.Role != null)
                roleTooltip.content = actor.Role.Description;

            // Culture Info
            if (actor.Culture != null)
                cultureTooltip.content = actor.Culture.Description;

            // Schemes Tab
            // if (IM.IsPlayer(actor)) {
            //     schemesTab.SetActive(false);
            // }

            // Swap Actor
            if (!IM.IsPlayer(actor)) {
                swapButton.onClick.AddListener(() => {
                    IM.SetPlayer(actor);
                    IntrigueSystemUI.instance.Refresh();
                    swapButton.gameObject.SetActive(false);
                });
            }
            else {
                swapButton.gameObject.SetActive(false);
            }

            if (actor.Clan == null) {
                clanObject.SetActive(false);
            }
            else {
                // Clan Info
                clanTooltip.header = $"Story of {actor.Clan.ClanName}";
                clanTooltip.content = actor.Clan.Description;
            }
            
            NEnum hooked = IM.Player.GetRelationVariable<NEnum>("Hooked", actor);
            if (hooked == null) return;
            // Hook (Custom Variable Info)
            if (hooked.Index > 0) {
                actorTooltip.content += "\n\n\u25CF <color=#ed4a68>You hold leverage against this character.</color>";
            }
        }
    }
}