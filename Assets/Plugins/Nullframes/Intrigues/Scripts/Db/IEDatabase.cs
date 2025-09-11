using System.Collections.Generic;
using Nullframes.Intrigues.Graph;
using Nullframes.Intrigues.Utils;
using UnityEngine;
using System.Runtime.CompilerServices;
using UnityEngine.Serialization;

[assembly: InternalsVisibleTo("Nullframes.Intrigues.Editor")]

namespace Nullframes.Intrigues {
    [CreateAssetMenu(menuName = "Nullframes/Database", fileName = "New Database")]
    public class IEDatabase : ScriptableObject {
        #region FIELDS

        [FormerlySerializedAs("Groups")] [SerializeReference, HideInInspector]
        internal List< GroupData > groupDataList = new();

        [FormerlySerializedAs("Nodes")] [SerializeReference, HideInInspector]
        internal List< NodeData > nodeDataList = new();

        [FormerlySerializedAs("Variables")] [SerializeReference, HideInInspector]
        internal List< NVar > variablePool = new();

        [FormerlySerializedAs("Schemes")] [SerializeField, HideInInspector]
        internal List< Scheme > schemeLibrary = new();

        [FormerlySerializedAs("Cultures")] [SerializeField, HideInInspector]
        internal List< Culture > culturalProfiles = new();

        [FormerlySerializedAs("Policies")] [SerializeField, HideInInspector]
        internal List< Policy > policyCatalog = new();

        [FormerlySerializedAs("Roles")] [SerializeField, HideInInspector]
        internal List< Role > roleDefinitions = new();

        [FormerlySerializedAs("Texts")] [SerializeField, HideInInspector]
        internal SerializableDictionary< string, SerializableDictionary< string, string > > localisationTexts = new();

        [FormerlySerializedAs("Actors")] [SerializeField, HideInInspector]
        internal List< IEActor > actorRegistry = new();

        [SerializeField] internal SerializableDictionary< string, AssetDb > spriteDatabase = new();

        [FormerlySerializedAs("AgeSuffixes")] [SerializeField, HideInInspector]
        internal List< AgeSuffix > ageClassificationTable = new() {
            new AgeSuffix("baby", 0, 3),
            new AgeSuffix("child", 4, 12),
            new AgeSuffix("teen", 13, 18),
            new AgeSuffix("adult", 19, 39),
            new AgeSuffix("middleage", 40, 54),
            new AgeSuffix("old", 55, 64),
            new AgeSuffix("veryold", 65, 90)
        };

        #endregion

        #region PROPERTIES (Read-Only)

        public IReadOnlyList< GroupData > GroupsReadOnly => groupDataList;
        public IReadOnlyList< NodeData > NodesReadOnly => nodeDataList;
        public IReadOnlyList< NVar > VariablesReadOnly => variablePool;
        public IReadOnlyList< Scheme > SchemesReadOnly => schemeLibrary;
        public IReadOnlyList< Culture > CulturesReadOnly => culturalProfiles;
        public IReadOnlyList< Policy > PoliciesReadOnly => policyCatalog;
        public IReadOnlyList< Role > RolesReadOnly => roleDefinitions;

        public IReadOnlyDictionary< string, SerializableDictionary< string, string > > TextsReadOnly =>
            localisationTexts;

        public IReadOnlyList< IEActor > ActorsReadOnly => actorRegistry;
        public IReadOnlyDictionary< string, AssetDb > SpriteDbReadOnly => spriteDatabase;
        public IReadOnlyList< AgeSuffix > AgeSuffixesReadOnly => ageClassificationTable;

        #endregion

        #region METHODS

        public void Reset() {
            groupDataList.Clear();
            nodeDataList.Clear();
            variablePool.Clear();
            schemeLibrary.Clear();
            culturalProfiles.Clear();
            policyCatalog.Clear();
            roleDefinitions.Clear();
            localisationTexts.Clear();
            actorRegistry.Clear();
            spriteDatabase.Clear();
        }

        #endregion
    }
}