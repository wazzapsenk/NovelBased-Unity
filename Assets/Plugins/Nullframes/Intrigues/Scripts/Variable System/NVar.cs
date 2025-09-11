using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nullframes.Intrigues
{
    [Serializable]
    public abstract class NVar
    {
        [SerializeReference] [HideInInspector] public string name;
        [SerializeReference] [HideInInspector] public string id;
        [field: SerializeReference] public virtual NType Type { get; set; }

        public Action onValueChanged;

        public object value
        {
            set
            {
                switch (this, value)
                {
                    case (NString nString, string @string):
                        nString.Value = @string;
                        onValueChanged?.Invoke();
                        break;
                    case (NInt nInt, int @int):
                        nInt.Value = @int;
                        onValueChanged?.Invoke();
                        break;
                    case (NFloat nFloat, float @float):
                        nFloat.Value = @float;
                        onValueChanged?.Invoke();
                        break;
                    case (NObject nObject, Object @object):
                        nObject.Value = @object;
                        onValueChanged?.Invoke();
                        break;
                    case (NBool nBool, bool @bool):
                        nBool.Value = @bool;
                        onValueChanged?.Invoke();
                        break;
                    case (NEnum nEnum, int index):
                        nEnum.Index = index;
                        onValueChanged?.Invoke();
                        break;
                    case (NEnum nEnumVol2, string enumString):
                        nEnumVol2.value = enumString;
                        onValueChanged?.Invoke();
                        break;
                    case (NActor nActor, Actor actor):
                        nActor.Value = actor;
                        onValueChanged?.Invoke();
                        break;                    
                    case (NActor nActor, string actorID):
                        nActor.ActorID = actorID;
                        onValueChanged?.Invoke();
                        break;
                    case (NClan nClan, Clan clan):
                        nClan.Value = clan;
                        onValueChanged?.Invoke();
                        break;                    
                    case (NClan nClan, string clanID):
                        nClan.ClanID = clanID;
                        onValueChanged?.Invoke();
                        break;
                    case (NFamily nFamily, Family family):
                        nFamily.Value = family;
                        onValueChanged?.Invoke();
                        break;                    
                    case (NFamily nFamily, string familyID):
                        nFamily.FamilyID = familyID;
                        onValueChanged?.Invoke();
                        break;
                }
            }
            get
            {
                return this switch
                {
                    NString @string => @string.Value,
                    NInt @int => @int.Value,
                    NFloat @float => @float.Value,
                    NObject @object => @object.value,
                    NBool @bool => @bool.Value,
                    NEnum @enum => @enum.value,
                    NActor actor => actor.Value,
                    NClan clan => clan.Value,
                    NFamily family => family.Value,
                    _ => null
                };
            }
        }

        protected NVar(string name, object value)
        {
            id = NullUtils.GenerateID();
            this.name = name;
            this.value = value;
        }

        protected NVar(string name)
        {
            id = NullUtils.GenerateID();
            this.name = name;
        }

        public NVar Duplicate()
        {
            return this switch
            {
                NString @string => new NString(name, @string.Value)
                {
                    id = id,
                    Type = NType.String,
                },
                NInt @int => new NInt(name, @int.Value)
                {
                    id = id,
                    Type = NType.Integer,
                },
                NFloat @float => new NFloat(name, @float.Value)
                {
                    id = id,
                    Type = NType.Float,
                },
                NObject @object => new NObject(name, @object.Value)
                {
                    id = id,
                    Type = NType.Object,
                },
                NBool @bool => new NBool(name, @bool.Value)
                {
                    id = id,
                    Type = NType.Bool,
                },
                NEnum @enum => new NEnum(name, @enum.Values.ToList(), @enum.Index)
                {
                    id = id,
                    Type = NType.Enum,
                },
                NActor actor => new NActor(name, actor.Value) {
                    id = id,
                    Type = NType.Actor,
                },
                NClan clan => new NClan(name, clan.Value) {
                    id = id,
                    Type = NType.Clan,
                },
                NFamily family => new NFamily(name, family.Value) {
                    id = id,
                    Type = NType.Family,
                },
                _ => null
            };
        }

        public static NVar CreateWithType(string name, NType type)
        {
            return type switch
            {
                NType.String => new NString(name),
                NType.Integer => new NInt(name),
                NType.Float => new NFloat(name),
                NType.Object => new NObject(name),
                NType.Bool => new NBool(name),
                NType.Enum => new NEnum(name),
                NType.Actor => new NActor(name),
                NType.Clan => new NClan(name),
                NType.Family => new NFamily(name),
                _ => null
            };
        }

        public static NVar Create(string id, string name, object value, NType type) {
            if (type == NType.String) {
                var nStr = new NString(name) {
                    id = id,
                    value = value
                };
                return nStr;
            }
            
            if (type == NType.Integer) {
                var nInt = new NInt(name) {
                    id = id,
                    value = value
                };
                return nInt;
            }

            if (type == NType.Float) {
                var nFloat = new NFloat(name) {
                    id = id,
                    value = value
                };
                return nFloat;
            }
            
            if (type == NType.Object) {
                var nObject = new NObject(name) {
                    id = id,
                    value = value
                };
                return nObject;
            }
            

            if (type == NType.Bool) {
                var nBool = new NBool(name) {
                    id = id,
                    value = value
                };
                return nBool;
            }
            
            if (type == NType.Enum) {
                var nEnum = new NEnum(name) {
                    id = id,
                    Index = (int)value
                };
                return nEnum;
            }
            
            if (type == NType.Actor) {
                var nActor = new NActor(name) {
                    id = id,
                    value = value
                };
                return nActor;
            }
            
            if (type == NType.Clan) {
                var nClan = new NClan(name) {
                    id = id,
                    value = value
                };
                return nClan;
            }
            
            if (type == NType.Family) {
                var nFamily = new NFamily(name) {
                    id = id,
                    value = value
                };
                return nFamily;
            }
            return null;
        }

        public static explicit operator List<string>(NVar variable)
        {
            if (variable is not NEnum @enum) return null;
            return (List<string>)@enum.Values;
        }

        public static explicit operator string(NVar variable)
        {
            return variable switch
            {
                NString @string => @string.Value,
                NInt @int => @int.value.ToString(),
                NFloat @float => @float.Value.ToString(CultureInfo.InvariantCulture),
                NObject @object => @object.value != null ? @object.Value.name : "Null",
                NBool @bool => @bool.value.ToString(),
                NEnum @enum => @enum.value,
                NActor actor => actor.Value.ID,
                NClan clan => clan.Value.ID,
                NFamily family => family.Value.ID,
                _ => null
            };
        }

        public static explicit operator int(NVar variable)
        {
            return variable switch
            {
                NInt @int => @int.Value,
                NEnum @enum => @enum.Index,
                NBool @bool => @bool.Value ? 1 : 0,
                _ => -1
            };
        }

        public static explicit operator Actor(NVar variable) {
            return ((NActor)variable).Value;
        }
        
        public static explicit operator Clan(NVar variable) {
            return ((NClan)variable).Value;
        }
        
        public static explicit operator Family(NVar variable) {
            return ((NFamily)variable).Value;
        }

        public static explicit operator float(NVar variable)
        {
            return ((NFloat)variable).Value;
        }

        public static explicit operator Object(NVar variable)
        {
            return ((NObject)variable).Value;
        }

        public static explicit operator bool(NVar variable)
        {
            return ((NBool)variable).Value;
        }
    }

    [Serializable]
    public class NString : NVar
    {
        [SerializeReference] private new string value;
        public string Value
        {
            get => value;
            set
            {
                this.value = value;
                onValueChanged?.Invoke();
            }
        }
        public override NType Type => NType.String;

        public NString(string name, string value) : base(name, value) { }
        public NString(string name) : base(name) { }
    }

    [Serializable]
    public class NInt : NVar
    {
        [SerializeReference] private new int value;
        public int Value
        {
            get => value;
            set
            {
                this.value = value;
                onValueChanged?.Invoke();
            }
        }
        public override NType Type => NType.Integer;

        public NInt(string name, int value) : base(name, value) { }
        public NInt(string name) : base(name) { }
    }

    [Serializable]
    public class NFloat : NVar
    {
        [SerializeReference] private new float value;
        public float Value
        {
            get => value;
            set
            {
                this.value = value;
                onValueChanged?.Invoke();
            }
        }
        public override NType Type => NType.Float;

        public NFloat(string name, float value) : base(name, value) { }
        public NFloat(string name) : base(name) { }
    }

    [Serializable]
    public class NObject : NVar
    {
        [SerializeReference] private new Object value;
        public Object Value
        {
            get => value;
            set
            {
                this.value = value;
                onValueChanged?.Invoke();
            }
        }
        public override NType Type => NType.Object;

        public NObject(string name, Object value) : base(name, value) { }
        public NObject(string name) : base(name) { }
    }

    [Serializable]
    public class NBool : NVar
    {
        [SerializeReference] private new bool value;
        public bool Value
        {
            get => value;
            set
            {
                this.value = value;
                onValueChanged?.Invoke();
            }
        }
        public override NType Type => NType.Bool;

        public NBool(string name, bool value) : base(name, value) { }
        public NBool(string name) : base(name) { }
    }
    
    [Serializable]
    public class NActor : NVar
    {
        [SerializeReference] private new string value;

        public Actor Value {
            get => IM.Actors.FirstOrDefault(a => a.ID == value);
            set => this.value = value.ID;
        }
        
        public string ActorID {
            get => value;
            set => this.value = value;
        }

        public override NType Type => NType.Actor;

        public NActor(string name, Actor value) : base(name, value) { }
        public NActor(string name, string value) : base(name, value) { }
        public NActor(string name) : base(name) { }
    }
    
    [Serializable]
    public class NClan : NVar
    {
        [SerializeReference] private new string value;

        public Clan Value {
            get => IM.Clans.FirstOrDefault(a => a.ID == value);
            set => this.value = value.ID;
        }
        
        public string ClanID {
            get => value;
            set => this.value = value;
        }

        public override NType Type => NType.Clan;

        public NClan(string name, Clan value) : base(name, value) { }
        public NClan(string name, string value) : base(name, value) { }
        public NClan(string name) : base(name) { }
    }
    
    [Serializable]
    public class NFamily : NVar
    {
        [SerializeReference] private new string value;

        public Family Value {
            get => IM.Families.FirstOrDefault(a => a.ID == value);
            set => this.value = value.ID;
        }
        
        public string FamilyID {
            get => value;
            set => this.value = value;
        }

        public override NType Type => NType.Family;

        public NFamily(string name, Family value) : base(name, value) { }
        public NFamily(string name, string value) : base(name, value) { }
        public NFamily(string name) : base(name) { }
    }

    [Serializable]
    public class NEnum : NVar
    {
        [SerializeReference] private List<string> values;
        public override NType Type => NType.Enum;

        public IEnumerable<string> Values => values;

        public new string value
        {
            get => values.ElementAtOrDefault(index);
            set => Index = values.IndexOf(value);
        }

        [SerializeReference] private int index;
        public int Index
        {
            get => index;
            set
            {
                index = value;
                onValueChanged?.Invoke();
            }
        }

        public void AddItems(string item)
        {
            if (values.Contains(item)) return;
            values.Add(item);
        }

        public void AddItem(params string[] items)
        {
            foreach (var item in items)
            {
                if (values.Contains(item)) continue;
                values.AddRange(items);
            }
        }

        public NEnum(string name, IEnumerable<string> value, int index) : base(name)
        {
            values = new List<string>(value.ToHashSet());
            this.index = index;
        }

        public NEnum(string name, IEnumerable<string> value) : base(name)
        {
            values = new List<string>(value.ToHashSet());
            index = 0;
        }

        public NEnum(string name, string[] value) : base(name)
        {
            values = new List<string>(value.ToHashSet());
            index = 0;
        }

        public NEnum(string name) : base(name)
        {
            values = new List<string>();
            index = 0;
        }
    }
}