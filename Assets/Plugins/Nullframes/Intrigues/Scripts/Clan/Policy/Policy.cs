using System;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEngine;

namespace Nullframes.Intrigues
{
    [Serializable]
    public class Policy
    {
        [SerializeField] private string _policyName;
        [SerializeField] private string _description;
        
        /// <summary>
        /// Gets the unique ID of the policy.
        /// </summary>
        [field: SerializeField] public string ID { get; private set; }

        /// <summary>
        /// Gets the type of the policy, indicating its scope or applicability.
        /// </summary>
        [field: SerializeField] public PolicyType Type { get; private set; }

        /// <summary>
        /// Gets the icon associated with the policy.
        /// </summary>
        [field: SerializeField] public Sprite Icon { get; private set; }
        
        /// <summary>
        /// Gets the name of the policy, e.g., "Economy."
        /// </summary>
        public string PolicyName { get => _policyName.LocaliseText();
            private set => _policyName = value;
        }

        /// <summary>
        /// Gets the description of the policy.
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
        /// Occurs when the policy name is changed.
        /// </summary>
        public event Action<string> onPolicyNameChanged;
        
        /// <summary>
        /// Creates a new policy with specified attributes and adds it to the system.
        /// </summary>
        /// <param name="policyName">The name of the policy.</param>
        /// <param name="description">A brief description of the policy, outlining its purpose and guidelines.</param>
        /// <param name="type">The type of the policy.</param>
        /// <param name="icon">A visual representation (icon) of the policy, used for UI elements or identification.</param>
        /// <returns>If a policy with the same name already exists within the system, the existing policy is returned; otherwise, a new Policy object is created and added to the system.</returns>
        /// <remarks>
        /// This method initiates by checking for the presence of an existing policy with the specified name to ensure the uniqueness of policies within the system. If an existing policy is identified, 
        /// a debug message is logged to notify about the duplicate policy name attempt, and the existing Policy object is returned. If no such policy exists, a new Policy object is instantiated with a generated unique ID along with the provided name, description, and icon, and then it is added to the system. This process facilitates the management and organization of policies, ensuring that they are distinct and properly cataloged.
        /// </remarks>
        public static Policy Create(string policyName, string description, PolicyType type = PolicyType.Generic, Sprite icon = null)
        {
            var targetPolicy = IM.Policies.FirstOrDefault((s) => s.PolicyName == policyName && s.Type == type);
            if (targetPolicy != null)
            {
                NDebug.Log(string.Format(STATIC.DEBUG_POLICY_SAME_TITLE, policyName));
                return targetPolicy;
            }

            var policy = new Policy(NullUtils.GenerateID(), policyName, description, type, icon);
            IM.AddPolicy(policy);
            return policy;
        }
        
        public Policy(string id, string policyName, string description, PolicyType type, Sprite icon)
        {
            ID = id;
            PolicyName = policyName;
            Description = description;
            Type = type;
            Icon = icon;
        }

        /// <summary>
        /// Sets the name of the policy.
        /// </summary>
        /// <param name="policyName">The new name for the policy.</param>
        /// <param name="withoutNotification">If set to true, the name change does not trigger a notification event.</param>
        /// <remarks>
        /// This method changes the policy's name to the specified new name, provided no other policy already has that name.
        /// If 'withoutNotification' is false, and the name is successfully changed, the 'onPolicyNameChanged' event is invoked with the old name.
        /// An error log is generated if there is already another policy with the specified new name.
        /// It's important to ensure unique policy names within the system to avoid conflicts.
        /// </remarks>
        public void SetName(string policyName, bool withoutNotification = false)
        {
            if (IM.Policies.All(c => c.PolicyName != policyName))
            {
                var oldName = PolicyName;
                PolicyName = policyName;
                if(!withoutNotification && oldName != PolicyName)
                    onPolicyNameChanged?.Invoke(oldName);
            }
        }

        /// <summary>
        /// Sets the description of the policy.
        /// </summary>
        /// <param name="description">The new description for the policy.</param>
        public void SetDescription(string description)
        {
            Description = description;
        }

        /// <summary>
        /// Sets the icon of the policy.
        /// </summary>
        /// <param name="icon">The new icon for the policy.</param>
        public void SetIcon(Sprite icon)
        {
            Icon = icon;
        }

        public Policy Duplicate() => new(ID, PolicyName, DescriptionWithoutLocalisation, Type, Icon);
    }
}