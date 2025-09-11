using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nullframes.Intrigues.Graph
{
    [Serializable]
    public class GroupData
    {
        [field: SerializeField] public string ID { get; protected set; }
        [field: SerializeField] public string Title { get; protected set; }
        [field: SerializeField] public Vector2 Position { get; protected set; }
        public virtual GenericNodeType GenericType => GenericNodeType.Scheme;
    }

    [Serializable]
    public class SchemeGroupData : GroupData
    {
        [field: SerializeReference] public string RuleID { get; private set; }
        [field: SerializeReference] public string Description { get; private set; }
        [field: SerializeReference] public bool HideIfNotCompatible { get; private set; }
        [field: SerializeReference] public bool TargetNotRequired { get; private set; }
        [field: SerializeReference] public bool HideOnUI { get; private set; }
        [field: SerializeReference] public Sprite Icon { get; private set; }
        [field: SerializeReference] public List<NexusVariable> Variables { get; private set; }

        public SchemeGroupData(string id, Vector2 position, string title, string description, string ruleId,
            bool hideIfNotCompatible, bool targetNotRequired, bool hideOnUI, Sprite icon, IEnumerable<NexusVariable> variables)
        {
            ID = id;
            Position = position;
            Title = title;
            Description = description;
            RuleID = ruleId;
            HideIfNotCompatible = hideIfNotCompatible;
            TargetNotRequired = targetNotRequired;
            HideOnUI = hideOnUI;
            Icon = icon;
            Variables = new List<NexusVariable>(variables);
        }
    }

    [Serializable]
    public class ClanGroupData : GroupData
    {
        [field: SerializeReference] public string Story { get; private set; }
        [field: SerializeReference] public string CultureID { get; private set; }
        [field: SerializeReference] public Sprite Emblem { get; private set; }
        [field: SerializeReference] public List<string> Policies { get; private set; }
        public override GenericNodeType GenericType => GenericNodeType.Clan;

        public ClanGroupData(string id, Vector2 position, string title, string story, string cultureId, Sprite emblem,
            IEnumerable<string> policies)
        {
            ID = id;
            Position = position;
            Title = title;
            Story = story;
            CultureID = cultureId;
            Emblem = emblem;
            Policies = new List<string>(policies.ToList());
        }
    }

    [Serializable]
    public class FamilyGroupData : GroupData
    {
        [field: SerializeReference] public string Story { get; private set; }
        [field: SerializeReference] public string CultureID { get; private set; }
        [field: SerializeReference] public Sprite Emblem { get; private set; }
        [field: SerializeReference] public List<string> Policies { get; private set; }
        public override GenericNodeType GenericType => GenericNodeType.Family;

        public FamilyGroupData(string id, Vector2 position, string title, string story, string cultureId,
            Sprite emblem, IEnumerable<string> policies)
        {
            ID = id;
            Position = position;
            Title = title;
            Story = story;
            CultureID = cultureId;
            Emblem = emblem;
            Policies = new List<string>(policies.ToList());
        }
    }

    [Serializable]
    public class RuleGroupData : GroupData
    {
        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public RuleGroupData(string id, Vector2 position, string title)
        {
            ID = id;
            Position = position;
            Title = title;
        }
    }
}