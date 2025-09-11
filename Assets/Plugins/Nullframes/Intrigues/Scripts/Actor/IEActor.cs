using System;
using UnityEngine;

namespace Nullframes.Intrigues
{
    [Serializable]
    public sealed class IEActor
    {
        [field: SerializeField] public string ID { get; private set; }
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public bool IsPlayer { get; private set; }
        [field: SerializeField] public Actor.IState CurrentState { get; private set; }
        [field: SerializeField] public int Age { get; private set; }
        [field: SerializeField] public string CultureID { get; private set; }
        [field: SerializeField] public Actor.IGender Gender { get; private set; }
        [field: SerializeField] public Sprite Portrait { get; private set; }

        public IEActor(string id, string name, Actor.IState currentState, string cultureId, int age,
            Actor.IGender gender, Sprite portrait, bool isPlayer)
        {
            ID = id;
            Name = name;
            CurrentState = currentState;
            CultureID = cultureId;
            Age = age;
            Gender = gender;
            Portrait = portrait;
            IsPlayer = isPlayer;
        }
    }
}