using System.Collections.Generic;
using Nullframes.Intrigues.Utils;
using UnityEngine;

namespace Nullframes.Intrigues {
    [System.Serializable]
    public class AssetDb {
        [SerializeField] internal SerializableDictionary< Sprite, List< string > > Sprites = new();
    }
}