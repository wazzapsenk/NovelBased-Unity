using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nullframes.Intrigues.Utils;
using Nullframes.Intrigues.Graph;
using UnityEngine;

namespace Nullframes.Intrigues.SaveSystem {
    [DefaultExecutionOrder(-350)]
    [AddComponentMenu("Intrigues/Save System")]
    public class IntrigueSaveSystem : MonoBehaviour {
        /// <summary>
        /// Indicates whether to pretty print the JSON data for better readability.
        /// </summary>
        public bool PrettyPrint;

        /// <summary>
        /// If true, the save file will be encrypted.
        /// </summary>
        public bool Encrypt;

        /// <summary>
        /// The password used to encrypt the save file.
        /// </summary>
        public string encryptionPassword = "My Password";

        /// <summary>
        /// Returns the directory where the save files are stored.
        /// </summary>
        private static string Path => $"{Application.persistentDataPath}/";

        /// <summary>
        /// Generates and returns a name for the save file using a unique ID with a .null extension.
        /// </summary>
        private static string FileNameExtension => "null";

        /// <summary>
        /// Retrieves the most recently saved file based on its last write time.
        /// </summary>
        public SaveFileInfo GET_LATEST_SAVE {
            get {
                var fileInfo = new DirectoryInfo(Path).GetFiles(@$"*.{FileNameExtension}")
                    .OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                return fileInfo == null
                    ? null
                    : new SaveFileInfo(System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name), fileInfo);
            }
        }

        /// <summary>
        /// Retrieves all save files sorted by their last write time in descending order.
        /// </summary>
        public IEnumerable<SaveFileInfo> GET_SAVE_FILES {
            get {
                return new DirectoryInfo(Path).GetFiles(@$"*.{FileNameExtension}")
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => new SaveFileInfo(System.IO.Path.GetFileNameWithoutExtension(f.Name), f)).ToList();
            }
        }

        /// <summary>
        /// Returns a FileInfo object for a given save file name.
        /// </summary>
        /// <param name="saveFileName">Name of the save file.</param>
        public FileInfo SaveFileInfo(string saveFileName) => new DirectoryInfo(Path)
            .GetFiles(@$"*.{FileNameExtension}")
            .FirstOrDefault(f => System.IO.Path.GetFileNameWithoutExtension(f.Name) == saveFileName);

        /// <summary>
        /// Checks if a save file with the given name exists.
        /// </summary>
        /// <param name="saveFileName">Name of the save file to check.</param>
        public bool SaveFileExists(string saveFileName) => SaveFileInfo(saveFileName) != null;

        private SaveData loadedData;

        private string currentSaveFile;

        public static IntrigueSaveSystem Instance;

        /// <summary>
        /// Checks if any save file exists.
        /// </summary>
        public bool AnySaveFileExists => GET_LATEST_SAVE != null;

        /// <summary>
        /// Provides access to the loaded save data.
        /// </summary>
        public SaveData Data => loadedData;

        /// <summary>
        /// Returns the name of the currently active save file.
        /// </summary>
        public string ActiveSaveFile => currentSaveFile;

        /// <summary>
        /// Event triggered when all Runtime Actors have been loaded.
        /// </summary>
        public static event Action onRuntimeActorsLoaded;

        /// <summary>
        /// Event triggered when data is saved.
        /// </summary>
        public static event Action onSave;

        /// <summary>
        /// Event triggered when data is loaded.
        /// </summary>
        public static event Action onLoad;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(this);
                return;
            }

            Destroy(this);
        }

        /// <summary>
        /// Clears the loaded save data. Typically used when starting a new game or similar scenarios.
        /// </summary>
        public void NewGame() {
            loadedData = null;
            IM.Load(null);
        }

        /// <summary>
        /// Saves the game data. If a file name is provided, it will use that; otherwise, it might use a default naming mechanism.
        /// </summary>
        /// <param name="fileName">Optional name for the save file.</param>
        public string Save(string fileName) {
            if (string.IsNullOrEmpty(fileName)) {
                fileName = null;
            }

            loadedData = new SaveData();

            //Save Actor Data
            foreach (var actor in IM.ActorDictionary.Values) {
                var actorData = new ActorData();
                actorData.Init(actor);
                loadedData.actorData.Add(actorData);
            }

            //Save Game Data
            var gameData = new GameData();
            gameData.Init();
            loadedData.gameData = gameData;

            var jsonString = JsonUtility.ToJson(loadedData, PrettyPrint);

            var fName = fileName ?? NullUtils.GenerateID();
            File.WriteAllText(Path + fName + "." + FileNameExtension,
                Encrypt ? Encryption.Encrypt(jsonString, encryptionPassword) : jsonString);

            currentSaveFile = fName;

            NDebug.Log($"Save successful. Save name: '{currentSaveFile}'");

            onSave?.Invoke();

            return fName;
        }

        /// <summary>
        /// Saves the game data. If a file name is provided, it will use that; otherwise, it might use a default naming mechanism.
        /// </summary>
        public void Save() => Save(null);

        /// <summary>
        /// Overwrites the currently active save file. If there's no active save file, it creates a new save.
        /// </summary>
        public void SaveOverwrite() {
            if (string.IsNullOrEmpty(currentSaveFile)) {
                Save();
                return;
            }

            NDebug.Log($"The save file named '{currentSaveFile}' was overwritten.");

            Save(currentSaveFile);
        }

        /// <summary>
        /// Loads the game data from the specified save file.
        /// </summary>
        /// <param name="fileName">The name of the save file to load.</param>
        public void Load(string fileName) {
            if (string.IsNullOrEmpty(fileName)) return;

            fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);

            if (!SaveFileExists(fileName)) return;

            string content = File.ReadAllText(Path + fileName + "." + FileNameExtension);
            loadedData = JsonUtility.FromJson<SaveData>(Encryption.Decrypt(content, encryptionPassword));
            if (loadedData == null) return;

            IM.Load(loadedData);

            currentSaveFile = fileName;

            IRuntimeLoader.Load();

            foreach (var actor in IM.ActorDictionary.Values) {
                var actorSaveData = loadedData.actorData.Find(a => a.id == actor.ID);
                if (actorSaveData == null) continue;
                actor.LoadActor(actorSaveData);
            }

            NDebug.Log($"The save named '{currentSaveFile}' was successfully loaded.");

            onLoad?.Invoke();
        }

        /// <summary>
        /// Loads the runtime actors.
        /// </summary>
        public void LoadRuntimeActors() {
            if (loadedData == null) return;

            //Create Runtime Actors
            foreach (var runtimeActor in loadedData.actorData) // Where(a => a.classType == 1)
            {
                if (IM.Actors.Any(a => a.ID == runtimeActor.id)) continue;
                RuntimeActor.LoadRuntimeActor(runtimeActor);
            }

            onRuntimeActorsLoaded?.Invoke();

            NDebug.Log("RuntimeActors were successfully loaded.");
        }

        /// <summary>
        /// Loads the most recent save based on its last write time.
        /// </summary>
        public void LoadLatest() {
            Load(GET_LATEST_SAVE.Name);
        }

        public void Delete(string saveFileName) {
            if (SaveFileExists(saveFileName)) {
                File.Delete(Path + saveFileName + "." + FileNameExtension);
                NDebug.Log($"The save file named '{saveFileName}' was deleted.");
            }
        }

        public void DeleteCurrent() {
            if (SaveFileExists(currentSaveFile)) {
                File.Delete(Path + currentSaveFile + "." + FileNameExtension);
                NDebug.Log($"The save file named '{currentSaveFile}' was deleted.");
            }
        }
    }

    [Serializable]
    public class SaveData {
        public List<ActorData> actorData = new();
        public GameData gameData;
    }

    [Serializable]
    public class CultureData {
        public string id;
        public string name;
        public Sprite icon;
        public string description;
        public List<string> femaleNameList;
        public List<string> maleNameList;

        public void Init(Culture culture) {
            id = culture.ID;
            name = culture.CultureName;
            icon = culture.Icon;
            description = culture.DescriptionWithoutLocalisation;
            femaleNameList = new List<string>(culture.FemaleNames);
            maleNameList = new List<string>(culture.MaleNames);
        }
    }
    
    [Serializable]
    public class FamilyData {
        public string id;
        public string name;
        public Sprite icon;
        public string description;
        public string cultureId;
        public List<string> policies;
        public List<NVarData> variableData;

        public void Init(Family family) {
            id = family.ID;
            name = family.FamilyName;
            icon = family.Icon;
            description = family.DescriptionWithoutLocalisation;
            cultureId = family.Culture?.ID;
            policies = new List<string>();
            variableData = new List<NVarData>();
            
            foreach (var variable in family.Variables) {
                var varData = new NVarData();
                varData.Init(variable);
                variableData.Add(varData);
            }  
            
            foreach (var policy in family.Policies) {
                policies.Add(policy.ID);
            }
        }
    }
    
    [Serializable]
    public class ClanData {
        public string id;
        public string name;
        public Sprite icon;
        public string description;
        public string cultureId;
        public List<string> policies;
        public List<NVarData> variableData;

        public void Init(Clan clan) {
            id = clan.ID;
            name = clan.ClanName;
            icon = clan.Icon;
            description = clan.DescriptionWithoutLocalisation;
            cultureId = clan.Culture?.ID;
            policies = new List<string>();
            variableData = new List<NVarData>();

            foreach ( var variable in clan.Variables ) {
                var varData = new NVarData();
                varData.Init(variable);
                variableData.Add(varData);
            }

            foreach (var policy in clan.Policies) {
                policies.Add(policy.ID);
            }
        }
    }

    [Serializable]
    public class PolicyData {
        public string id;
        public string name;
        public string description;
        public PolicyType type;
        public Sprite icon;

        public void Init(Policy policy) {
            id = policy.ID;
            name = policy.PolicyName;
            description = policy.DescriptionWithoutLocalisation;
            type = policy.Type;
            icon = policy.Icon;
        }
    }

    [Serializable]
    public class RoleData {
        public string id;
        public string name;
        public string description;
        public string titleForFemale;
        public string titleForMale;
        public string heirFilterId;
        public bool inheritance;
        public int capacity;
        public int priority;
        public Sprite icon;

        public void Init(Role role) {
            id = role.ID;
            name = role.RoleName;
            description = role.DescriptionWithoutLocalisation;
            titleForFemale = role.TitleForFemale;
            titleForMale = role.TitleForMale;
            heirFilterId = role.FilterID;
            capacity = role.Capacity;
            inheritance = role.Inheritance;
            priority = role.Priority;
            icon = role.Icon;
        }
    }

    [Serializable]
    public class GameData {
        public List<NVarData> variableData;
        public List<CultureData> cultureData;
        public List<PolicyData> policyData;
        public List<FamilyData> familyData;
        public List<ClanData> clanData;
        public List<RoleData> roleData;

        public void Init() {
            variableData = new List<NVarData>();
            cultureData = new List<CultureData>();
            policyData = new List<PolicyData>();
            roleData = new List<RoleData>();
            familyData = new List<FamilyData>();
            clanData = new List<ClanData>();

            foreach (var variable in IM.Variables) {
                var data = new NVarData();
                data.Init(variable);
                variableData.Add(data);
            }

            foreach (var culture in IM.Cultures) {
                var data = new CultureData();
                data.Init(culture);
                cultureData.Add(data);
            }

            foreach (var policy in IM.Policies) {
                var data = new PolicyData();
                data.Init(policy);
                policyData.Add(data);
            }

            foreach (var role in IM.Roles) {
                var data = new RoleData();
                data.Init(role);
                roleData.Add(data);
            }
            
            foreach (var family in IM.Families) {
                var data = new FamilyData();
                data.Init(family);
                familyData.Add(data);
            }
            
            foreach (var clan in IM.Clans) {
                var data = new ClanData();
                data.Init(clan);
                clanData.Add(data);
            }
        }
    }

    [Serializable]
    public class ActorData {
        public string id;
        public int classType;
        public bool isPlayer;
        public bool inheritor;
        public string name;
        public int age;
        public string culture;
        public string clan;
        public string role;
        public Actor.IGender gender;
        public Actor.IState state;
        public List<string> parents;
        public List<string> children;
        public List<string> spouses;
        public string family;
        public string familyOrigin;
        public string overrideHeir;
        public Sprite portrait;
        public List<NVarData> variableData;
        public List<SchemeData> schemeData;
        public List<RelationVariableData> relationVariableData;

        public void Init(Actor actor) {
            id = actor.ID;
            name = actor.Name;
            age = actor.Age;
            gender = actor.Gender;
            inheritor = actor.Inheritor;
            culture = actor.Culture?.ID;
            clan = actor.Clan?.ID;
            role = actor.Role?.ID;
            state = actor.State;
            overrideHeir = actor.HeirOverridden ? actor.Heir.ID : null;
            portrait = actor.Portrait;
            familyOrigin = actor.Origin?.ID;
            family = actor.Family?.ID;
            isPlayer = actor.IsPlayer;

            classType = actor switch {
                InitialActor => 0,
                RuntimeActor => 1,
                _ => classType
            };

            parents = new List<string>();
            children = new List<string>();
            spouses = new List<string>();
            variableData = new List<NVarData>();
            relationVariableData = new List<RelationVariableData>();
            schemeData = new List<SchemeData>();

            foreach (var parent in actor.Parents()) {
                parents.Add(parent.ID);
            }

            foreach (var child in actor.Children()) {
                children.Add(child.ID);
            }

            foreach (var spouse in actor.Spouses()) {
                spouses.Add(spouse.ID);
            }

            foreach (var variable in actor.Variables) {
                var varData = new NVarData();
                varData.Init(variable);
                variableData.Add(varData);
            }            
            
            foreach (var specificVariable in actor.RelationVariables) {
                var specificData = new RelationVariableData() {
                    actorId = specificVariable.Key.ID,
                    varData = new List<NVarData>()
                };

                foreach (var variable in specificVariable.Value) {
                    var varData = new NVarData();
                    varData.Init(variable);
                    specificData.varData.Add(varData);
                }
                
                relationVariableData.Add(specificData);
            }

            foreach (var scheme in actor.Schemes) {
                if (scheme.Schemer == null) continue;
                var _schemeData = new SchemeData();
                _schemeData.Init(scheme);
                this.schemeData.Add(_schemeData);
            }
        }
    }

    [Serializable]
    public class RelationVariableData {
        public string actorId;
        public List<NVarData> varData;
    }

    [Serializable]
    public class NVarData {
        public string id;
        public string name;
        public NType type;
        public string stringValue;
        public int integerValue;
        public bool boolValue;
        public float floatValue;
        public string actorId;
        public UnityEngine.Object objectValue;
        public int enumIndex;

        public void Init(NVar variable) {
            id = variable.id;
            name = variable.name;
            type = variable.Type;
            switch (variable) {
                case NString nString:
                    stringValue = nString.Value;
                    break;
                case NInt nInt:
                    integerValue = nInt.Value;
                    break;
                case NBool nBool:
                    boolValue = nBool.Value;
                    break;
                case NFloat nFloat:
                    floatValue = nFloat.Value;
                    break;
                case NObject nObject:
                    objectValue = nObject.Value;
                    break;
                case NEnum nEnum:
                    enumIndex = nEnum.Index;
                    break;
                case NActor nActor:
                    actorId = nActor.ActorID;
                    break;
            }
        }
    }

    [Serializable]
    public class SchemeData {
        public string id;
        public string targetId;
        public bool isPaused;
        public string objective;
        public bool isEnded;
        public List<NVarData> variables;
        public List<NodeInfoData> activeNodes;

        public void Init(Scheme scheme) {
            id = scheme.ID;
            targetId = scheme.Schemer.Target != null ? scheme.Schemer.Target.ID : null;
            objective = scheme.CurrentObjective;
            if (scheme.Schemer != null) {
                isPaused = scheme.Schemer.IsPaused;
                isEnded = scheme.Schemer.IsEnded;
            }

            activeNodes = new List<NodeInfoData>();
            foreach (var nodeInfo in scheme.Schemer.ActiveNodeList) {
                var nInfo = new NodeInfoData();
                nInfo.Init(nodeInfo);
                activeNodes.Add(nInfo);
            }

            variables = new List<NVarData>();
            foreach (var _nVar in scheme.Schemer.Variables) {
                var nVar = new NVarData();
                nVar.Init(_nVar);
                nVar.id = _nVar.name;
                variables.Add(nVar);
            }
        }
    }

    [Serializable]
    public class NodeInfoData {
        public string id;
        public string nodeId;
        public bool bgWorker;
        public string inputName;
        public string actorId;
        public string actorId1;
        public string actorId2;
        public string clanId;
        public string familyId;
        public float time;
        public int index;
        public string sequencerId;
        public string repeaterId;
        public string validatorId;
        public int repeatCount;
        public List<string> delays;
        public List<int> indexes;

        public void Init(NodeInfo nodeInfo) {
            id = nodeInfo.id;
            nodeId = nodeInfo.node.ID;
            bgWorker = nodeInfo.bgWorker;
            actorId = nodeInfo.actor != null ? nodeInfo.actor.ID : null;
            actorId1 = nodeInfo.dualActor.Item1 != null ? nodeInfo.dualActor.Item1.ID : null;
            actorId2 = nodeInfo.dualActor.Item2 != null ? nodeInfo.dualActor.Item2.ID : null;
            clanId = nodeInfo.clan?.ID;
            familyId = nodeInfo.family?.ID;
            inputName = nodeInfo.inputName;
            time = nodeInfo.time;
            index = nodeInfo.index;
            sequencerId = nodeInfo.sequencer?.id;
            repeaterId = nodeInfo.repeater?.id;
            validatorId = nodeInfo.validator?.id;
            repeatCount = nodeInfo.repeatCount;
            delays = new List<string>(nodeInfo.delays);
            indexes = new List<int>(nodeInfo.indexes);
        }
    }

    public class SaveFileInfo {
        public string Name { get; }
        public FileInfo FileInfo { get; }

        public SaveFileInfo(string name, FileInfo fileInfo) {
            Name = name;
            FileInfo = fileInfo;
        }
    }
}