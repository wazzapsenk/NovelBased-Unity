using Nullframes.Intrigues.SaveSystem;
using UnityEngine;

namespace Nullframes.Intrigues
{
    public class RuntimeActor : Actor
    {
        protected override void OnAwake()
        {
            Init();
            LoadData();
            RegisterEvents();
        }

        protected override void OnStart() { }
        
        protected override void OnActorDeath() {
            base.OnActorDeath();   
        }

        #region RUNTIME

        /// <summary>
        /// Creates a new RuntimeActor and its corresponding GameObject to represent a new actor in the game.
        /// </summary>
        /// <param name="name">The name of the new actor.</param>
        /// <param name="state">The initial state of the new actor.</param>
        /// <param name="age">The age of the new actor.</param>
        /// <param name="culture">The cultural background of the new actor.</param>
        /// <param name="gender">The gender of the new actor.</param>
        /// <param name="portrait">The sprite used for the new actor's portrait.</param>
        /// <param name="runtimeActor">Out parameter that returns the created RuntimeActor instance.</param>
        /// <param name="gameObj">Out parameter that returns the GameObject associated with the new actor.</param>
        /// <remarks>
        /// This method is used to create new characters within the game, such as when a child is born or a new character is introduced. 
        /// It initializes a RuntimeActor with the provided details and also creates a corresponding GameObject.
        /// </remarks>
        public static void CreateActor(string name, IState state, int age, Culture culture, IGender gender,
            Sprite portrait, out RuntimeActor runtimeActor, out GameObject gameObj)
        {
            var actor = CreateActor(name, state, age, culture, gender, portrait);

            runtimeActor = actor;
            gameObj = runtimeActor.gameObject;
        }

        /// <summary>
        /// Creates a new RuntimeActor and its corresponding GameObject to represent a new actor in the game.
        /// </summary>
        /// <param name="name">The name of the new actor.</param>
        /// <param name="state">The initial state of the new actor.</param>
        /// <param name="age">The age of the new actor.</param>
        /// <param name="culture">The cultural background of the new actor.</param>
        /// <param name="gender">The gender of the new actor.</param>
        /// <param name="portrait">The sprite used for the new actor's portrait.</param>
        /// <remarks>
        /// This method is used to create new characters within the game, such as when a child is born or a new character is introduced. 
        /// It initializes a RuntimeActor with the provided details and also creates a corresponding GameObject.
        /// </remarks>
        public static RuntimeActor CreateActor(string name, IState state, int age, Culture culture, IGender gender,
            Sprite portrait)
        {
            var gameObj = new GameObject($"{name}(Runtime)");
            var runtimeActor = gameObj.AddComponent<RuntimeActor>();
            runtimeActor.ID = NullUtils.GenerateID();
            runtimeActor.Name = name;
            runtimeActor.State = state;
            runtimeActor.Age = age;
            runtimeActor.Culture = culture;
            runtimeActor.Gender = gender;
            runtimeActor.Portrait = portrait;

            IM.AddActor(runtimeActor);

            runtimeActor.UpdateVariables();
            runtimeActor.UpdateActor();

            IM.onRuntimeActorCreated?.Invoke(runtimeActor, gameObj);
            return runtimeActor;
        }

        public static void LoadRuntimeActor(ActorData data)
        {
            if (IM.ActorDictionary.ContainsKey(data.id)) return;

            var gameObject = new GameObject($"{data.name}(Runtime)");
            var actor = gameObject.AddComponent<RuntimeActor>();

            actor.ID = data.id;
            actor.Data = data;
            actor.UpdateVariables();
            actor.UpdateActor();

            IM.AddActor(actor);

            IntrigueSaveSystem.onRuntimeActorsLoaded += Load;

            void Load()
            {
                actor.UpdateFamily();
                actor.UpdateClan();

                actor.SpawnFamilyMember();

                IM.onRuntimeActorCreated?.Invoke(actor, gameObject);

                IntrigueSaveSystem.onRuntimeActorsLoaded -= Load;
            }
        }

        #endregion
    }
}