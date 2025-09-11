using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEngine;

namespace Nullframes.Intrigues
{
    public sealed class Family
    {
        private readonly List<Policy> policies;
        private readonly List<Actor> members;
        private readonly List<NVar> variables;
        
        private string _familyName { get; set; }
        private string _description { get; set; }
        
        /// <summary>
        /// Contains private variables associated with the family.
        /// </summary>
        public IEnumerable<NVar> Variables => variables;
        
        /// <summary>
        /// Gets the unique ID of the family.
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// Gets the name of the family.
        /// </summary>
        public string FamilyName { 
            get => _familyName.LocaliseText();
            private set => _familyName = value;
        }

        /// <summary>
        /// Gets the description of the family.
        /// </summary>
        public string Description {
            get => _description.LocaliseText();
            private set => _description = value;
        }
        
        /// <summary>
        /// Gets un-localized definition of family.
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
        /// Gets the emblem of the family.
        /// </summary>
        public Sprite Icon { get; private set; }

        /// <summary>
        /// Gets the culture associated with the family.
        /// </summary>
        public Culture Culture { get; private set; }

        /// <summary>
        /// Gets the members of the family.
        /// </summary>
        public IEnumerable<Actor> Members => members;
        
        /// <summary>
        /// Gets the policies of the clan.
        /// </summary>
        public IEnumerable<Policy> Policies => policies.Where(policy => policy is { Type: PolicyType.Generic or PolicyType.Family });

        /// <summary>
        /// Gets the count of members in the family.
        /// </summary>
        public int MemberCount => members.Count;
        
        /// <summary>
        /// Occurs when the family's name is changed.
        /// </summary>
        public event Action<string> onFamilyNameChanged;

        /// <summary>
        /// Occurs when the family's icon is changed.
        /// </summary>
        public event Action<Sprite> onFamilyIconChanged;

        /// <summary>
        /// Occurs when the family's culture is changed.
        /// </summary>
        public event Action<Culture> onFamilyCultureChanged;

        /// <summary>
        /// Occurs when a new member joins the family.
        /// </summary>
        public Action<Actor> onMemberJoin;

        /// <summary>
        /// Occurs when a member leaves the family.
        /// </summary>
        public event Action<Actor> onMemberLeave;
        
        /// <summary>
        /// Checks if the family has accepted a specific policy.
        /// </summary>
        /// <param name="policyNameOrId">The name or ID of the policy to check.</param>
        /// <returns>True if the policy is accepted by the family; otherwise, false.</returns>
        public bool HasPolicy(string policyNameOrId)
        {
            return !string.IsNullOrEmpty(policyNameOrId) && Policies.Any(p => p.PolicyName == policyNameOrId || p.ID == policyNameOrId);
        }
        
        /// <summary>
        /// Checks if the family has accepted any policy.
        /// </summary>
        /// <returns>True if the clan has any accepted policy; otherwise, false.</returns>
        public bool AnyPolicy() => Policies.Any();
        
        /// <summary>
        /// Creates a new family with specified attributes and adds it to the system.
        /// </summary>
        /// <param name="familyName">The name of the family.</param>
        /// <param name="description">A description of the family's history, values, or notable characteristics.</param>
        /// <param name="icon">A visual representation (banner) of the family.</param>
        /// <param name="culture">The culture associated with the family, defining its traditions and practices.</param>
        /// <param name="policies">A collection of policies the clan adheres to, defining its rules and governance.</param>
        /// <returns>If a family with the same name already exists, the existing family is returned; otherwise, a new Family object is created and added to the system.</returns>
        /// <remarks>
        /// Before creating a new family, this method checks for the existence of a family with the provided name to ensure that each family within the system is unique. If a matching family is found,
        /// a debug message is generated, and the existing Family object is returned to prevent duplication. If no matching family is found, a new Family object is instantiated with a generated unique ID and the provided attributes, then added to the system. This ensures that family names are distinct and correctly managed within the system.
        /// </remarks>
        public static Family Create(string familyName, string description, Culture culture = null, Sprite icon = null, params Policy[] policies)
        {
            var targetFamily = IM.Families.FirstOrDefault((s) => s.FamilyName == familyName);
            if (targetFamily != null)
            {
                NDebug.Log(string.Format(STATIC.DEBUG_FAMILY_SAME_TITLE, familyName));
                return targetFamily;
            }

            var family = new Family(NullUtils.GenerateID(), familyName, description, icon, culture, policies.Where(policy => policy is { Type: PolicyType.Generic or PolicyType.Family }), null);
            IM.AddFamily(family);
            return family;
        }
        
        /// <summary>
        /// Creates a new family with specified attributes and adds it to the system.
        /// </summary>
        /// <param name="familyName">The name of the family.</param>
        /// <param name="description">A description of the family's history, values, or notable characteristics.</param>
        /// <param name="icon">A visual representation (banner) of the family.</param>
        /// <param name="cultureNameOrId">The culture name/id associated with the family, defining its traditions and practices.</param>
        /// <param name="policyNameOrId">A collection of policies the clan adheres to, defining its rules and governance.</param>
        /// <returns>If a family with the same name already exists, the existing family is returned; otherwise, a new Family object is created and added to the system.</returns>
        /// <remarks>
        /// Before creating a new family, this method checks for the existence of a family with the provided name to ensure that each family within the system is unique. If a matching family is found,
        /// a debug message is generated, and the existing Family object is returned to prevent duplication. If no matching family is found, a new Family object is instantiated with a generated unique ID and the provided attributes, then added to the system. This ensures that family names are distinct and correctly managed within the system.
        /// </remarks>
        public static Family Create(string familyName, string description, string cultureNameOrId, Sprite icon, params string[] policyNameOrId)
        {
            var targetFamily = IM.Families.FirstOrDefault((s) => s.FamilyName == familyName);
            if (targetFamily != null)
            {
                NDebug.Log(string.Format(STATIC.DEBUG_FAMILY_SAME_TITLE, familyName));
                return targetFamily;
            }

            var family = new Family(NullUtils.GenerateID(), familyName, description, icon, IM.GetCulture(cultureNameOrId), policyNameOrId.Select(IM.GetPolicy).Where(policy => policy is { Type: PolicyType.Generic or PolicyType.Family }), null);
            IM.AddFamily(family);
            return family;
        }

        public Family(string id, string familyName, string description, Sprite icon, Culture culture, IEnumerable<Policy> policies, IEnumerable<NVar> vars)
        {
            members = new List<Actor>();
            variables = new List<NVar>(vars);
            this.policies = policies != null ? new List<Policy>(policies) : new List<Policy>();

            ID = id; 
            FamilyName = familyName;
            Description = description;
            Icon = icon;
            Culture = culture;
        }

        /// <summary>
        /// Checks if an actor is a member of the family.
        /// </summary>
        /// <param name="member">The actor to check.</param>
        /// <returns>True if the actor is a member of the family; otherwise, false.</returns>
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
        /// Removes a policy from the family.
        /// </summary>
        /// <param name="policyNameOrId">The name or ID of the policy to remove.</param>
        public void RemovePolicy(string policyNameOrId)
        {
            var policy = Policies.FirstOrDefault(p => p.PolicyName == policyNameOrId || p.ID == policyNameOrId);
            if (policy == null) return;
            if (policies.Remove(policy)) onPolicyUnaccepted?.Invoke(policy);
        }
        
        /// <summary>
        /// Removes a policy from the family.
        /// </summary>
        /// <param name="policy">The policy to remove.</param>
        public void RemovePolicy(Policy policy)
        {
            if (policy == null) return;
            if (policies.Remove(policy)) onPolicyUnaccepted?.Invoke(policy);
        }
        
        /// <summary>
        /// Sets the name of the family.
        /// </summary>
        /// <param name="familyName">The new name for the family.</param>
        /// <param name="withoutNotification">If set to true, the name change does not trigger a notification event.</param>
        /// <remarks>
        /// This method changes the family's name to the specified new name, provided no other family already has that name.
        /// If 'withoutNotification' is false and the name is successfully changed, the 'onFamilyNameChanged' event is invoked with the old name.
        /// An error log is generated if there is already another family with the specified new name.
        /// Ensuring unique family names within the system is crucial to avoid conflicts.
        /// </remarks>
        public void SetName(string familyName, bool withoutNotification = false)
        {
            if (IM.Families.All(c => c.FamilyName != familyName))
            {
                var oldName = FamilyName;
                FamilyName = familyName;
                if(!withoutNotification && oldName != FamilyName)
                    onFamilyNameChanged?.Invoke(oldName);
                return;
            }

            NDebug.Log($"There is already another family with this name. / {familyName}", NLogType.Error);
        }

        /// <summary>
        /// Sets the icon of the family.
        /// </summary>
        /// <param name="icon">The new icon for the family.</param>
        public void SetIcon(Sprite icon)
        {
            var oldEmblem = Icon;
            Icon = icon;
            onFamilyIconChanged?.Invoke(oldEmblem);
        }

        /// <summary>
        /// Sets the culture of the family.
        /// </summary>
        /// <param name="cultureName">The new culture for the family.</param>
        public void SetCulture(string cultureName)
        {
            var culture = IM.Cultures.FirstOrDefault(p => p.CultureName == cultureName);
            if (culture == null) return;
            var oldCulture = Culture;
            Culture = culture;
            onFamilyCultureChanged?.Invoke(oldCulture);
        }

        /// <summary>
        /// Sets the description of the family.
        /// </summary>
        /// <param name="description">The new description for the family.</param>
        public void SetDescription(string description)
        {
            Description = description;
        }
        
        /// <summary>
        /// Retrieves a private variable from the family based on its name or ID.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the desired variable.</param>
        /// <returns>Returns the corresponding variable from the family.</returns>
        public NVar GetVariable(string variableNameOrId) => variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);

        /// <summary>
        /// Retrieves a private variable from the family based on its name or ID and casts it to the specified type.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the desired variable.</param>
        /// <returns>Returns the corresponding variable of type T from the family.</returns>
        public T GetVariable<T>(string variableNameOrId) where T : NVar => variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId) as T;

        /// <summary>
        /// Sets the value of a private variable for the family.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the variable to set.</param>
        /// <param name="value">New value for the family's variable.</param>
        public void SetVariable(string variableNameOrId, object value) {
            var localVariable = variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
            if (localVariable == null) return;
            localVariable.value = value;
        }

        /// <summary>
        /// Sets the value of a private variable for the family and returns the updated value cast to the specified type.
        /// </summary>
        /// <param name="variableNameOrId">Name or ID of the variable to set.</param>
        /// <param name="value">New value for the family's variable.</param>
        /// <returns>Returns the updated variable of type T from the family.</returns>
        public T SetVariable<T>(string variableNameOrId, object value) where T : NVar {
            var localVariable = variables.Find(v => v.name == variableNameOrId || v.id == variableNameOrId);
            if (localVariable == null) return null;
            localVariable.value = value;
            return localVariable as T;
        }
    }
}