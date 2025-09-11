using Nullframes.Intrigues.UI;
using Nullframes.Intrigues.Attributes;
using Nullframes.Intrigues.SaveSystem;
using UnityEngine;

namespace Nullframes.Intrigues.Demo {
    public class MapInteract : MonoBehaviour {
        [Clan] public string clanId; // Clan ID
        [Role] public string leaderId; // Leader Role ID

        public SpriteRenderer leaderPortrait;
        public SpriteRenderer clanIcon;
        public TooltipTrigger clanTooltip;

        public Transform PolicyLayout;
        public GameObject PolicyItem;

        private Clan clan;

        private void Start() {
            clan = IM.GetClan(clanId);

            if (clan == null) return;

            UpDate();

            clanTooltip.header = clan.ClanName;
            clanTooltip.content = clan.Description;

            //Listeners
            clan.onActorRoleChanged += OnActorRoleChanged;
            clan.onPolicyAccepted += OnPolicyAccepted;
            clan.onPolicyUnaccepted += OnPolicyUnaccepted;

            IntrigueSaveSystem.onLoad += OnLoad;
        }

        private void OnDestroy() {
            clan.onActorRoleChanged -= OnActorRoleChanged;
            clan.onPolicyAccepted -= OnPolicyAccepted;
            clan.onPolicyUnaccepted -= OnPolicyUnaccepted;
            IntrigueSaveSystem.onLoad -= OnLoad;
        }

        private void UpDate() {
            UpdateLeader(clan.GetMember(leaderId));
            UpdateClanIcon();
            UpdatePolicies();
        }

        private void OnActorRoleChanged(Actor actor, Role oldRole) {
            Actor leader = clan.GetMember(leaderId);
            UpdateLeader(leader);
        }

        private void UpdateClanIcon() {
            clanIcon.sprite = clan.Icon;
        }

        private void UpdateLeader(Actor actor) {
            if (actor == null) return;
            //..
            leaderPortrait.sprite = actor.Portrait;
        }

        private void OnPolicyAccepted(Policy policy) {
            UpdatePolicies();
        }

        private void OnPolicyUnaccepted(Policy policy) {
            UpdatePolicies();
        }

        private void OnLoad() {
            UpDate();
        }

        private void UpdatePolicies() {
            foreach (Transform t in PolicyLayout) {
                Destroy(t.gameObject);
            }

            foreach (var policy in clan.Policies) {
                var policyRenderer = PolicyItem.Duplicate<SpriteRenderer>(PolicyLayout);

                if (policy.Icon != null) {
                    policyRenderer.sprite = policy.Icon;
                }

                TooltipTrigger tooltipTrigger = policyRenderer.GetComponent<TooltipTrigger>();

                tooltipTrigger.header = policy.PolicyName;
                tooltipTrigger.content = policy.Description;
            }
        }

        public void OpenLeaderMenu() {
            if (clan == null) return;
            Actor leader = clan.GetMember(leaderId);

            IntrigueSystemUI.instance.OpenActorMenu(leader);
        }

        public void OpenClanMenu() {
            if (clan == null) return;
            IntrigueSystemUI.instance.OpenClanMenu(clan);
        }
    }
}