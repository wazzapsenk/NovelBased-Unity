using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nullframes.Intrigues.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nullframes.Intrigues
{
    [Serializable]
    public sealed class Culture
    {
        [SerializeField] private List<string> maleNames;
        [SerializeField] private List<string> femaleNames;
        
        [SerializeField] private string _cultureName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        
        /// <summary>
        /// Gets the unique ID of the culture.
        /// </summary>
        [field: SerializeField] public string ID { get; private set; }
        /// <summary>
        /// Gets the list of male names common in the culture.
        /// </summary>
        public IEnumerable<string> MaleNames => maleNames;
        /// <summary>
        /// Gets the list of female names common in the culture.
        /// </summary>
        public IEnumerable<string> FemaleNames => femaleNames;
        
        /// <summary>
        /// Occurs when the culture's name is changed.
        /// </summary>
        public event Action<string> onCultureNameChanged;
        
        /// <summary>
        /// Occurs when the culture's icon is changed.
        /// </summary>
        public event Action<Sprite> onCultureIconChanged;

        public Sprite Icon => _icon;
        
        /// <summary>
        /// Gets the name of the culture.
        /// </summary>
        public string CultureName { get => _cultureName.LocaliseText();
            private set => _cultureName = value;
        }

        /// <summary>
        /// Gets the description of the culture.
        /// </summary>
        public string Description {
            get => _description.LocaliseText();
            private set => _description = value;
        }
        
        /// <summary>
        /// Gets un-localized definition of culture.
        /// </summary>
        public string DescriptionWithoutLocalisation => _description;

        /// <summary>
        /// Sets the name of the culture.
        /// </summary>
        /// <param name="cultureName">The new name for the culture.</param>
        /// <param name="withoutNotification">If set to true, the name change does not trigger a notification event.</param>
        /// <remarks>
        /// This method changes the culture's name to the specified new name, provided no other culture already has that name.
        /// If 'withoutNotification' is false and the name is successfully changed, the 'onCultureNameChanged' event is invoked with the old name.
        /// An error log is generated if there is already another culture with the specified new name.
        /// It's important to ensure unique culture names within the system to avoid conflicts.
        /// </remarks>
        public void SetName(string cultureName, bool withoutNotification = false)
        {
            if (IM.Cultures.All(c => c.CultureName != cultureName))
            {
                var oldName = CultureName;
                CultureName = cultureName;
                if(!withoutNotification && oldName != CultureName)
                    onCultureNameChanged?.Invoke(oldName);
            }
        }
        
        /// <summary>
        /// Sets the description of the culture.
        /// </summary>
        /// <param name="description">The new description for the culture.</param>
        /// <returns>The updated Culture instance.</returns>
        public Culture SetDescription(string description)
        {
            Description = description;
            return this;
        }

        /// <summary>
        /// Generates a random name appropriate for the specified gender, according to the culture.
        /// </summary>
        /// <param name="gender">The gender for which to generate a name.</param>
        /// <returns>A culturally appropriate random name.</returns>
        public string GenerateName(Actor.IGender gender) => gender == Actor.IGender.Male ? maleNames[Random.Range(0, maleNames.Count)] : femaleNames[Random.Range(0, femaleNames.Count)];

        public string GetCultureName()
        {
            var name = CultureName;
            foreach (Match descLocalise in Regex.Matches(name, STATIC.LOCALISATION))
                if (descLocalise.Success)
                    name = Regex.Replace(name, "{l:" + descLocalise.Value + "}", IM.GetText(descLocalise.Value));

            return name;
        }

        public string GetDescription()
        {
            var desc = Description;
            foreach (Match descLocalise in Regex.Matches(desc, STATIC.LOCALISATION))
                if (descLocalise.Success)
                    desc = Regex.Replace(desc, "{l:" + descLocalise.Value + "}", IM.GetText(descLocalise.Value));

            return desc;
        }

        /// <summary>
        /// Adds a new name to the list of names for the specified gender in the culture.
        /// </summary>
        /// <param name="name">The name to add.</param>
        /// <param name="gender">The gender for which the name is appropriate.</param>
        /// <returns>The updated Culture instance.</returns>
        public Culture AddName(string name, Actor.IGender gender)
        {
            switch (gender)
            {
                case Actor.IGender.Male:
                    maleNames.Add(name);
                    break;
                case Actor.IGender.Female:
                    femaleNames.Add(name);
                    break;
            }

            return this;
        }

        /// <summary>
        /// Removes a name from the list of names for the specified gender in the culture.
        /// </summary>
        /// <param name="name">The name to remove.</param>
        /// <param name="gender">The gender for which the name is appropriate.</param>
        /// <returns>The updated Culture instance.</returns>
        public Culture RemoveName(string name, Actor.IGender gender)
        {
            switch (gender)
            {
                case Actor.IGender.Male:
                    maleNames.Remove(name);
                    break;
                case Actor.IGender.Female:
                    femaleNames.Remove(name);
                    break;
            }

            return this;
        }
        
        /// <summary>
        /// Sets the icon of the culture.
        /// </summary>
        /// <param name="icon">The new icon for the culture.</param>
        public void SetIcon(Sprite icon)
        {
            var oldIcon = Icon;
            _icon = icon;
            onCultureIconChanged?.Invoke(oldIcon);
        }
        
        /// <summary>
        /// Creates a new culture with specified attributes and adds it to the system.
        /// </summary>
        /// <param name="cultureName">The name of the culture.</param>
        /// <param name="description">A brief description of the culture.</param>
        /// <param name="icon">A visual representation (icon) of the culture.</param>
        /// <param name="femaleNames">A collection of common female names within the culture.</param>
        /// <param name="maleNames">A collection of common male names within the culture.</param>
        /// <returns>If a culture with the same name already exists, the existing culture is returned; otherwise, a new Culture object is created and added to the system.</returns>
        /// <remarks>
        /// Before creating a new culture, this method checks for the existence of a culture with the provided name to ensure that each culture within the system is unique. If a matching culture is found, 
        /// a debug message is generated, and the existing Culture object is returned to prevent duplication. If no matching culture is found, a new Culture object is instantiated with a generated unique ID and the provided attributes, then added to the system. This process ensures that cultural identities are distinct and properly managed within the system.
        /// </remarks>
        public static Culture Create(string cultureName, string description, Sprite icon = null, IEnumerable<string> femaleNames = null,
            IEnumerable<string> maleNames = null)
        {
            var targetCulture = IM.Cultures.FirstOrDefault((s) => s.CultureName == cultureName);
            if (targetCulture != null)
            {
                NDebug.Log(string.Format(STATIC.DEBUG_CULTURE_SAME_TITLE, cultureName));
                return targetCulture;
            }

            var culture = new Culture(NullUtils.GenerateID(), cultureName, description, icon, femaleNames, maleNames);
            IM.AddCulture(culture);
            return culture;
        }

        public Culture(string id, string cultureName, string description, Sprite icon, IEnumerable<string> femaleNames,
            IEnumerable<string> maleNames)
        {
            ID = id;
            CultureName = cultureName;
            Description = description;
            _icon = icon;
            this.femaleNames = new List<string>();
            this.maleNames = new List<string>();
            foreach (var name in femaleNames) this.femaleNames.Add(name);
            foreach (var name in maleNames) this.maleNames.Add(name);
        }

        public Culture Duplicate() => new(ID, CultureName, DescriptionWithoutLocalisation, Icon, FemaleNames, MaleNames);
    }
}