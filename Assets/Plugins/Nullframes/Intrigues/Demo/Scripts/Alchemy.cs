using Nullframes.Intrigues.UI;
using Nullframes.Intrigues.Attributes;
using UnityEngine;

namespace Nullframes.Intrigues.Demo
{
    public class Alchemy : MonoBehaviour
    {
        //With Policy, we easily select the policy identity (ID) through a Dropdown menu using the inspector.
        [Policy] public string policy;

        //The icon representing a buy choice.
        public Sprite goldIcon;
        
        public Sprite backgroundTemplate;

        //The price of the Poison.
        public int PoisonPrice = 55;

        //The price of the Bomb.
        public int BombPrice = 35;
        
        //This variable, PurchaseSFX, is a public AudioClip that is used to store an audio clip or sound effect associated with a purchase action or event.
        public AudioClip PurchaseSFX;

        /// <summary>
        /// Opens the Alchemy menu.
        /// </summary>
        public void OpenAlchemyMenu() {
            
            // Check if the policy associated with the ownedClan is accepted.
            bool isPolicyAccepted = IM.Player.HasPolicy(policy);

            // Get the demoCharacter instance.
            var demoCharacter = DemoManager.instance.player;

            if (isPolicyAccepted)
            {
                // Text describing the Poison.
                string poison =
                    "\u25CF Venomous Weaver\n\nWith its ominous presence and notorious reputation, the Venomous Weaver possessed a unique power in the form of a venom." +
                    "Flowing through its teeth, this poison was a deadly concoction capable of inflicting unbearable agony and bringing swift death upon its victims." +
                    "In the clan territories where the policy of \"Alchemy\" was nonexistent, commerce was strictly forbidden.";

                // Text describing the Bomb.
                string bomb =
                    "\u25CF The Bomb of Night's Envoys\n\nThe Bomb of Night's Envoys is an explosive device capable of generating an intense shroud of mist." +
                    "Within its core lies a meticulously prepared mixture, containing elements that, upon detonation, swiftly disperse, enveloping the surroundings in a mesmerizing haze, searing the eyes with an ethereal fog." +
                    "In the midst of the eternal struggle between light and darkness, ages ago, a group of enigmatic alchemists toiled to contribute to this battle." +
                    "These alchemists joined their efforts in creating a powerful weapon, harnessing the power of light. Thus, the \"Bomb of Night's Envoys\" was born, an extraordinary device crafted through a concoction of mystical properties.";

                // Open a dialogue with the Alchemy, displaying the descriptive texts about the flowers.
                var dialogue = DialogueManager.OpenDialogue("Alchemy", $"{poison}\n\n{bomb}");

                if(backgroundTemplate != null)
                    dialogue.SetBackground(backgroundTemplate).SetNativeSize();

                // Add a choice to buy the Silver Blossom.
                dialogue.AddChoice($"Buy Venomous Weaver's Venom <size=16>[{PoisonPrice} Gold]</size>", goldIcon,
                    () =>
                    {
                        DemoManager.instance.AddPoison(1);
                        DemoManager.instance.AddCoin(-PoisonPrice);
                        
                        //Play sound
                        IM.SetupAudio(PurchaseSFX).Play();
                    }, () => demoCharacter.currentCoin.Value >= PoisonPrice, false);

                // Add a choice to buy the Witch's Lily.
                dialogue.AddChoice($"Buy The Bomb of Night's Envoys <size=16>[{BombPrice} Gold]</size>", goldIcon,
                    () =>
                    {
                        DemoManager.instance.AddBomb(1);
                        DemoManager.instance.AddCoin(-BombPrice);
                        
                        //Play sound
                        IM.SetupAudio(PurchaseSFX).Play();
                    }, () => demoCharacter.currentCoin.Value >= BombPrice, false);

                // Add a choice to cancel the purchase.
                dialogue.AddChoice("Never-mind.", null, null);
            }
            else
            {
                // Message displayed when the policy is not accepted.
                string message =
                    "Since alchemy is illegal in your clan, you cannot buy alchemical items.";

                // Open a dialogue with the Alchemy, displaying the message.
                var dialogue = DialogueManager.OpenDialogue("Alchemy", message);
                
                if(backgroundTemplate != null)
                    dialogue.SetBackground(backgroundTemplate).SetNativeSize();

                // Add a choice to cancel.
                dialogue.AddChoice($"Never-mind.", null, null);
            }
        }
    }
}