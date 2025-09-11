using System;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEngine;

namespace Nullframes.Intrigues
{
    [Serializable]
    public class Role
    {
        [SerializeField] private string _roleName;

        /// <summary>
        /// Gets the unique ID of the role.
        /// </summary>
        [field: SerializeField] public string ID { get; private set; }
        
        [SerializeField] private string _description;
        [SerializeField] private string _titleForMale;
        [SerializeField] private string _titleForFemale;
        
        [field: SerializeField] public string FilterID { get; private set; }

        /// <summary>
        /// Gets the capacity of the role, e.g., maximum number of people who can hold the role in a clan.
        /// </summary>
        [field: SerializeField] public int Capacity { get; private set; }
        /// <summary>
        /// Indicates whether the role is inheritable.
        /// </summary>
        [field: SerializeField] public bool Inheritance { get; private set; }
        /// <summary>
        /// Gets the icon associated with the role.
        /// </summary>
        [field: SerializeField] public Sprite Icon { get; private set; }
        /// <summary>
        /// Gets the priority of the role, which can be used to create a hierarchy among roles.
        /// </summary>
        [field: SerializeField] public int Priority { get; private set; }

        public HeirFilter HeirFilter { get; set; }
        
        /// <summary>
        /// Gets the name of the role, e.g., "Doctor."
        /// </summary>
        public string RoleName { 
            get => _roleName.LocaliseText();
            private set => _roleName = value;
        }
        
        /// <summary>
        /// Gets the description of the role.
        /// </summary>
        public string Description {
            get => _description.LocaliseText();
            private set => _description = value;
        }
        
        /// <summary>
        /// Gets un-localized definition of role.
        /// </summary>
        public string DescriptionWithoutLocalisation => _description;
        
        /// <summary>
        /// Gets the title of the role for male actors, e.g., "King" for a male "Leader."
        /// </summary>
        public string TitleForMale {
            get => _titleForMale.LocaliseText();
            private set => _titleForMale = value;
        }        
        
        /// <summary>
        /// Gets the title of the role for female actors, e.g., "Queen" for a female "Leader."
        /// </summary>
        public string TitleForFemale {
            get => _titleForFemale.LocaliseText();
            private set => _titleForFemale = value;
        }
        
        /// <summary>
        /// Occurs when the role name is changed.
        /// </summary>
        public event Action<string> onRoleNameChanged;
        
        /// <summary>
        /// Retrieves the title of the role based on the specified gender.
        /// </summary>
        /// <param name="gender">The gender for which to retrieve the title.</param>
        /// <returns>The title of the role for the specified gender.</returns>
        public string Title(Actor.IGender gender) => gender == Actor.IGender.Female ? TitleForFemale : TitleForMale;
        
        /// <summary>
        /// Dynamically creates and registers a new role within the system based on the provided attributes.
        /// </summary>
        /// <param name="roleName">Specifies the name of the role being created.</param>
        /// <param name="titleForMale">Defines the title of the role for male actors.</param>
        /// <param name="titleForFemale">Defines the title of the role for female actors.</param>
        /// <param name="description">Gives a brief description of the role's duties or significance.</param>
        /// <param name="filterNameOrId">Associates the role with a specific heir filter NAME/ID, facilitating inheritance logic.</param>
        /// <param name="capacity">Limits the number of actors that can hold this role within any given clan.</param>
        /// <param name="inheritance">Indicates if the role is inheritable, allowing it to be passed down through generations.</param>
        /// <param name="icon">Associates an icon with the role for visual representation in the UI.</param>
        /// <param name="priority">Assigns a priority level to the role, aiding in establishing a hierarchy among roles.</param>
        /// <returns>If a role with the same name already exists, the existing role is returned; otherwise, a new Role object is instantiated and returned.</returns>
        /// <remarks>
        /// This method ensures the uniqueness of role names within the system, preventing the creation of duplicate roles.
        /// It generates a debug message if an attempt is made to create a role with a name that already exists. New roles are added to the system with a generated ID and the specified attributes.
        /// </remarks>
        public static Role Create(string roleName, string description, Sprite icon = null, int capacity = 1, string titleForMale = null, string titleForFemale = null,
            bool inheritance = false, int priority = 0, string filterNameOrId = null)
        {
            var targetRole = IM.Roles.FirstOrDefault((s) => s.RoleName == roleName);
            if (targetRole != null)
            {
                NDebug.Log(string.Format(STATIC.DEBUG_ROLE_SAME_TITLE, roleName));
                return targetRole;
            }

            var role = new Role(NullUtils.GenerateID(), roleName, titleForMale, titleForFemale, description, filterNameOrId, capacity, inheritance, icon, priority)
                {
                    HeirFilter = IM.HeirFilter(filterNameOrId)
                };
            IM.AddRole(role);
            return role;
        }

        public Role(string id, string roleName, string titleForMale, string titleForFemale, string description,
            string filterId, int capacity, bool inheritance, Sprite icon, int priority)
        {
            ID = id;
            RoleName = roleName;
            TitleForMale = titleForMale;
            TitleForFemale = titleForFemale;
            Description = description;
            FilterID = filterId;
            Capacity = capacity;
            Inheritance = inheritance;
            Icon = icon;
            Priority = priority;
        }

        /// <summary>
        /// Sets the name of the role.
        /// </summary>
        /// <param name="roleName">The new name for the role.</param>
        /// <param name="withoutNotification">If set to true, the name change does not trigger a notification event.</param>
        /// <remarks>
        /// This method changes the role's name to the specified new name, provided no other role already has that name.
        /// If 'withoutNotification' is false and the name is successfully changed, the 'onRoleNameChanged' event is invoked with the old name.
        /// An error log is generated if there is already another role with the specified new name.
        /// Ensuring unique role names within the system is important to avoid conflicts.
        /// </remarks>
        public void SetName(string roleName, bool withoutNotification = false)
        {
            if (IM.Roles.All(c => c.RoleName != roleName))
            {
                var oldName = RoleName;
                RoleName = roleName;
                if(!withoutNotification && oldName != RoleName)
                    onRoleNameChanged?.Invoke(oldName);
            }
        }

        /// <summary>
        /// Sets the description of the role.
        /// </summary>
        /// <param name="description">The new description for the role.</param>
        public void SetDescription(string description)
        {
            Description = description;
        }

        /// <summary>
        /// Sets the capacity of the role.
        /// </summary>
        /// <param name="capacity">The new capacity for the role.</param>
        public void SetCapacity(int capacity) {
            if (capacity < 1) return;
            Capacity = capacity;
        }

        /// <summary>
        /// Sets the icon of the role.
        /// </summary>
        /// <param name="icon">The new icon for the role.</param>
        public void SetIcon(Sprite icon)
        {
            Icon = icon;
        }
        
        public Role Duplicate()
        {
            return new Role(ID, RoleName, TitleForMale, TitleForFemale, DescriptionWithoutLocalisation, FilterID, Capacity,
                Inheritance, Icon, Priority);
        }

        public static implicit operator string(Role role) => role.ID;
    }
}