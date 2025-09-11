using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEngine;

namespace Nullframes.Intrigues
{
    public sealed class Clan
    {
        private readonly List<Policy> policies;
        private readonly List<Actor> members;
        private readonly List<NVar> variables;

        private string _clanName { get; set; }
        private string _description { get; set; }
        
        /// <summary>
        /// Contains private variables associated with the clan.
        /// </summary>
        public IEnumerable<NVar> Variables => variables;
        /// <summary>
        /// Gets the unique ID of the clan.
        /// </summary>
        public string ID { get; private set; }
        /// <summary>
        /// Gets the icon of the clan, e.g., a flag icon.
        /// </summary>
        public Sprite Icon { get; private set; }
        /// <summary>
        /// Gets the culture of the clan.
        /// </summary>
        public Culture Culture { get; private set; }
        /// <summary>
        /// Gets the members of the clan.
        /// </summary>
        public IEnumerable<Actor> Members => members;
        /// <summary>
        /// Gets the policies of the clan.
        /// </summary>
        public IEnumerable<Policy> Policies => policies.Where(policy => policy is { Type: PolicyType.Generic or PolicyType.Clan });
        /// <summary>
        /// Gets the count of members in the clan.
        /// </summary>
        public int MemberCount => members.Count;

        /// <summary>
        /// Gets the name of the clan, e.g., "Arcanum."
        /// </summary>
        public string ClanName {
            get => _clanName.LocaliseText();
            private set => _clanName = value;
        }

        /// <summary>
        /// Gets the description of the clan.
        /// </summary>
        public string Description {
            get => _description.LocaliseText();
            private set => _description = value;
        }
        
        /// <summary>
        /// Gets un-localized definition of policy.
        /// </summary>
        public string DescriptionWithoutLocalisation => _description;

        /// <summary>
        /// Occurs when a policy is accepted by the clan.
        /// </summary>
        public event Action<Policy> onPolicyAccepted;
        /// <summary>
        /// Occurs when a policy is no longer accepted by the clan.
        /// </summary>
        public event Action<Policy> onPolicyUnaccepted;
        /// <summary>
        /// Occurs when the clan's name is changed.
        /// </summary>
        public event Action<string> onClanNameChanged;
        /// <summary>
        /// Occurs when the clan's icon is changed.
        /// </summary>
        public event Action<Sprite> onClanIconChanged;
        /// <summary>
        /// Occurs when the clan's culture is changed.
        /// </summary>
        public event Action<Culture> onClanCultureChanged;
        /// <summary>
        /// Occurs when a new member joins the clan.
        /// </summary>
        public Action<Actor> onMemberJoin;
        /// <summary>
        /// Occurs when a member leaves the clan.
        /// </summary>
        public event Action<Actor> onMemberLeave;
        /// <summary>
        /// Occurs when an actor's role within the clan changes.
        /// </summary>
        public Action<Actor, Role> onActorRoleChanged;
        
        
        /// <summary>
        /// Retrieves all members of the clan that match a specific role.
        /// </summary>
        /// <param name="roleFilter">The role to filter the members by, e.g., 'Engineer'.</param>
        /// <param name="inclusivePassive">If true, includes passive (e.g., deceased) members in the results.</param>
        /// <returns>An enumerable of members matching the role filter.</returns>
        public IEnumerable<Actor> GetMembers(string roleFilter, bool inclusivePassive = false)
        {
            if(inclusivePassive) return Members.Where(m => m.Role?.ID == roleFilter || m.Role?.RoleName == roleFilter);
            return Members.Where(m => m.State == Actor.IState.Active && (m.Role?.ID == roleFilter || m.Role?.RoleName == roleFilter));
        }
        
        /// <summary>
        /// Retrieves a member of the clan based on their role.
        /// </summary>
        /// <param name="roleFilter">The role to filter the members by, e.g., 'Engineer'.</param>
        /// <param name="inclusivePassive">If true, includes passive (e.g., deceased) members in the search.</param>
        /// <returns>The first member found matching the role filter; null if no match is found.</returns>
        /// <remarks>
        /// If multiple members have the specified role, this method returns the first one in the list.
        /// </remarks>
        public Actor GetMember(string roleFilter, bool inclusivePassive = false)
        {
            if(inclusivePassive) return Members.FirstOrDefault(m => m.Role?.ID == roleFilter || m.Role?.RoleName == roleFilter);
            return Members.FirstOrDefault(m => m.State == Actor.IState.Active && (m.Role?.ID == roleFilter || m.Role?.RoleName == roleFilter));
        }

        /// <summary>
        /// Checks if the clan has accepted a specific policy.
        /// </summary>
        /// <param name="policyNameOrId">The name or ID of the policy to check.</param>
        /// <returns>True if the policy is accepted by the clan; otherwise, false.</returns>
        public bool HasPolicy(string policyNameOrId)
        {
            return !string.IsNullOrEmpty(policyNameOrId) && Policies.Any(p => p.PolicyName == policyNameOrId || p.ID == policyNameOrId);
        }

        /// <summary>
        /// Checks if the clan has accepted any policy.
        /// </summary>
        /// <returns>True if the clan has any accepted policy; otherwise, false.</returns>
        public bool AnyPolicy() => Policies.Any();

        /// <summary>
        /// Creates a new clan with the specified attributes and adds it to the system.
        /// </summary>
        /// <param name="clanName">The name of the clan.</param>
        /// <param name="description">A description of the clan.</param>
        /// <param name="icon">An icon or banner representing the clan visually.</param>
        /// <param name="culture">The culture associated with the clan, defining its traditions and practices.</param>
        /// <param name="policies">A collection of policies the clan adheres to, defining its rules and governance.</param>
        /// <returns>If a clan with the same name already exists within the system, the existing clan is returned; otherwise, a new Clan object is created and returned.</returns>
        /// <remarks>
        /// This method first checks for the existence of a clan with the provided name to ensure uniqueness within the system. If a clan with the same name is found, 
        /// a debug message is logged, and the existing Clan object is returned, avoiding the creation of duplicate clans. If no such clan exists, a new Clan object is instantiated 
        /// with a generated unique ID and the specified attributes, and then added to the system. This approach ensures that each clan is uniquely identified and properly integrated into the system.
        /// </remarks>
        public static Clan Create(string clanName, string description, Culture culture = null,
            Sprite icon = null, params Policy[] policies)
        {
            var targetClan = IM.Clans.FirstOrDefault((s) => s.ClanName == clanName);
            if (targetClan != null)
            {
                NDebug.Log(string.Format(STATIC.DEBUG_CLAN_SAME_TITLE, clanName));
                return targetClan;
            }

            var clan = new Clan(NullUtils.GenerateID(), clanName, description, icon, culture, policies.Where(policy => policy is { Type: PolicyType.Generic or PolicyType.Clan }), null);
            IM.AddClan(clan);
            return clan;
        }
        
                /// <summary>
        /// Creates a new clan with the specified attributes and adds it to the system.
        /// </summary>
        /// <param name="clanName">The name of the clan.</param>
        /// <param name="description">A description of the clan.</param>
        /// <param name="icon">An icon or banner representing the clan visually.</param>
        /// <param name="cultureNameOrId">The culture name/id associated with the clan, defining its traditions and practices.</param>
        /// <param name="policyNameOrId">A collection of policies the clan adheres to, defining its rules and governance.</param>
        /// <returns>If a clan with the same name already exists within the system, the existing clan is returned; otherwise, a new Clan object is created and returned.</returns>
        /// <remarks>
        /// This method first checks for the existence of a clan with the provided name to ensure uniqueness within the system. If a clan with the same name is found, 
        /// a debug message is logged, and the existing Clan object is returned, avoiding the creation of duplicate clans. If no such clan exists, a new Clan object is instantiated 
        /// with a generated unique ID and the specified attributes, and then added to the system. This approach ensures that each clan is uniquely identified and properly integrated into the system.
        /// </remarks>
        public static Clan Create(string clanName, string description, string cultureNameOrId,
            Sprite icon = null, params string[] policyNameOrId)
        {
            var targetClan = IM.Clans.FirstOrDefault((s) => s.ClanName == clanName);
            if (targetClan != null)
            {
                NDebug.Log(string.Format(STATIC.DEBUG_CLAN_SAME_TITLE, clanName));
                return targetClan;
            }

            var clan = new Clan(NullUtils.GenerateID(), clanName, description, icon, IM.GetCulture(cultureNameOrId), policyNameOrId.Select(IM.GetPolicy).Where(policy => policy is { Type: PolicyType.Generic or PolicyType.Clan }).ToList(), null);
            IM.AddClan(clan);
            return clan;
        }

        public Clan(string id, string clanName, string story, Sprite icon, Culture culture, IEnumerable<Policy> policies, IEnumerable<NVar> vars)
        {
            ID = id;
            members = new List<Actor>();
            variables = new List<NVar>(vars);
            this.policies = policies != null ? new List<Policy>(policies) : new List<Policy>();

            ClanName = clanName;
            Description = story;
            Icon = icon;
            Culture = culture;
        }

        /// <summary>
        /// Checks if an actor is a member of the clan.
        /// </summary>
        /// <param name="member">The actor to check.</param>
        /// <returns>True if the actor is a member of the clan; otherwise, false.</returns>
        public bool IsMember(Actor member)
        {
            return members.Contains(member);
        }

        public bool Join(Actor member, bool withoutNotification = false)
        {
            if (members.Contains(member)) return false;

            members.Add(member);
            if (withoutNotification) return true;
            onMemberJoin?.Invoke(member);
            return true;
        }

        public bool Quit(Actor member, bool showNotification = true)
        {
            if (!members.Remove(member)) return false;
            if(showNotification)
                onMemberLeave?.Invoke(member);
            return true;
        }

        /// <summary>
        /// Adds a policy to the clan.
        /// </summary>
        /// <param name="policyNameOrId">The name or ID of the policy to add.</param>
        public void AddPolicy(string policyNameOrId)
        {
            var policy = IM.Policies.FirstOrDefault(p => p.PolicyName == policyNameOrId || p.ID == policyNameOrId);
            if (policy == null) return;
            if (Policies.Contains(policy)) return;
            policies.Add(policy);
            onPolicyAccepted?.Invoke(policy);
        }
        
        /// <summary>
        /// Adds a policy to the clan.
        /// </summary>
        /// <param name="policy">The policy to add.</param>
        public void AddPolicy(Policy policy)
        {
            if (policy == null) return;
            if (Policies.Contains(policy)) return;
            policies.Add(policy);
            onPolicyAccepted?.Invoke(policy);
        }

        /// <summary>
        /// Removes a policy from the clan.
        /// </summary>
        /// <param name="policyNameOrId">The name or ID of the policy to remove.</param>
        public void RemovePolicy(string policyNameOrId)
        {
            var policy = Policies.FirstOrDefault(p => p.PolicyName == policyNameOrId || p.ID == policyNameOrId);
            if (policy == null) return;
            if (policies.Remove(policy)) onPolicyUnaccepted?.Invoke(policy);
        }
        
        /// <summary>
        /// Removes a policy from the clan.
        /// </summary>
        /// <param name="policy">The policy to remove.</param>
        public void RemovePolicy(Policy policy)
        {
            if (policy == null) return;
            if (policies.Remove(policy)) onPolicyUnaccepted?.Invoke(policy);
        }

        /// <summary>
        /// Sets the culture of the clan.
        /// </summary>
        /// <param name="cultureNameOrId">The name or ID of the culture to set.</param>
        public void SetCulture(string cultureNameOrId)
        {
            var culture = IM.Cultures.FirstOrDefault(p => p.CultureName == cultureNameOrId || p.ID == cultureNameOrId);
            if (culture == null) return;
            var oldCulture = Culture;
            Culture = culture;
            onClanCultureChanged?.Invoke(oldCulture);
        }
        
        /// <summary>
        /// Sets the culture of the clan.
        /// </summary>
        /// <param name="culture">The culture to set.</param>
        public void SetCulture(Culture culture)
        {
            if (culture == null) return;
            var oldCulture = Culture;
            Culture = culture;
            onClanCultureChanged?.Invoke(oldCulture);
        }

        /// <summary>
        /// Sets the icon of the clan.
        /// </summary>
        /// <param name="icon">The new icon for the clan.</param>
        public void SetIcon(Sprite icon)
        {
            var oldEmblem = Icon;
            Icon = icon;
            onClanIconChanged?.Invoke(oldEmblem);
        }

        /// <summary>
        /// Sets the name of the clan.
        /// </summary>
        /// <param name="clanName">The new name for the clan.</param>
        /// <param name="withoutNotification">If set to true, the name change does not trigger a notification event.</param>
        /// <remarks>
        /// This method changes the clan's name to the specified new name, provided no other clan already has that name.
        /// If 'withoutNotification' is false, and the name is successfully changed, the 'onClanNameChanged' event is invoked with the old name.
        /// An error log is generated if there is already another clan with the specified new name.
        /// It's important to ensure unique policy names within the system to avoid conflicts.
        /// </remarks>
        public void SetName(string clanName, bool withoutNotification = false)
        {
            if (IM.Clans.All(c => c.ClanName != clanName))
            {
                var oldName = ClanName;
                ClanName = clanName;
                if(!withoutNotification && oldName != ClanName)
                    onClanNameChanged?.Invoke(oldName);
                return;
            }

            NDebug.Log($"There is already another clan with this name. / {clanName}", NLogType.Error);
        }

        /// <summary>
        /// Sets the description of the clan.
        /// </summary>
        /// <param name="description">The new description for the clan.</param>
        public void SetDescription(string description)
        {
            Description = description;
        }
        
        /// <summary>
        /// Retrieves a private variable from the clan based on its name or ID.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the desired variable.</param>
        /// <returns>Returns the corresponding variable from the clan.</returns>
        public NVar GetVariable(string variableNameOrId) => variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);

        /// <summary>
        /// Retrieves a private variable from the clan based on its name or ID and casts it to the specified type.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the desired variable.</param>
        /// <returns>Returns the corresponding variable of type T from the clan.</returns>
        public T GetVariable<T>(string variableNameOrId) where T : NVar => variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId) as T;

        /// <summary>
        /// Sets the value of a private variable for the clan.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the variable to set.</param>
        /// <param name="value">New value for the clan's variable.</param>
        public void SetVariable(string variableNameOrId, object value) {
            var localVariable = variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
            if (localVariable == null) return;
            localVariable.value = value;
        }

        /// <summary>
        /// Sets the value of a private variable for the clan and returns the updated value cast to the specified type.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the variable to set.</param>
        /// <param name="value">New value for the clan's variable.</param>
        /// <returns>Returns the updated variable of type T from the clan.</returns>
        public T SetVariable<T>(string variableNameOrId, object value) where T : NVar {
            var localVariable = variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
            if (localVariable == null) return null;
            localVariable.value = value;
            return localVariable as T;
        }
    }
}