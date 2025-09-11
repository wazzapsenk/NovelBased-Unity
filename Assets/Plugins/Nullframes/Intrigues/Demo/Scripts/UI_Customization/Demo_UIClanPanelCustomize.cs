using UnityEngine;

namespace Nullframes.Intrigues.UI {
    public class Demo_UIClanPanelCustomize : IUIClanBehaviour {
        public TooltipTrigger clanTooltip;
        public TooltipTrigger clanCultureTooltip;

        public override void OnMenuOpened(Clan clan) {
            // Clan Info
            clanTooltip.header = $"Story of {clan.ClanName}";
            clanTooltip.content = clan.Description;

            // Culture Info
            if (clan.Culture != null)
                clanCultureTooltip.content = clan.Culture.Description;
        }

        public override void OnClanMemberLoaded(Actor actor, Transform Transform) {
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

        public override void OnPolicyLoaded(Policy policy, Transform Transform) {
            TooltipTrigger policyTooltip = Transform.GetComponent<TooltipTrigger>();

            policyTooltip.header = policy.PolicyName;
            policyTooltip.content = policy.Description;
        }
    }
}