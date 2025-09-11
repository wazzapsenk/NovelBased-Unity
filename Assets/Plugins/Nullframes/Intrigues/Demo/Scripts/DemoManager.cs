using Nullframes.Intrigues.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nullframes.Intrigues.Demo
{
    public class DemoManager : MonoBehaviour
    {
        // Holds the player character instance
        public DemoCharacter player { get; private set; }

        // Static instance of this class for singleton pattern
        public static DemoManager instance;
        
        // UI element to display the player's portrait
        public Image playerPortrait;

        [Space(10)]

        // Tooltip and text UI elements for power, spy, coin, and bag information
        public TooltipTrigger powerTooltip;
        public TextMeshProUGUI powerText;
        
        public TooltipTrigger spyTooltip;
        public TextMeshProUGUI spyText;
        
        public TooltipTrigger coinTooltip;
        public TextMeshProUGUI coinText;
        
        public TooltipTrigger bagTooltip;
        public TextMeshProUGUI bagText;

        // Boolean to track if the game is paused
        private bool gameIsPaused;
        
        [Space(10)]

        // GameObject to display key notes
        public GameObject keyNotes;
        
        [Space(10)]
        
        // SpeedControl object to control the game speed
        public SpeedControl speedControl;

        [Space(10)]

        // Unity events triggered when quit menu is opened or closed
        public UnityEvent onQuitMenuOpen; 
        public UnityEvent onQuitMenuClosed;

        // Default portrait to be used if no specific portrait is set
        private Sprite defaultPortrait;
        
        private void Awake()
        {
            // Assign this instance to the static instance property for singleton pattern
            instance = this;
        }

        private void OnEnable()
        {
            // Subscribe to player change event to handle changes in player character
            IM.onPlayerIsChanged += OnPlayerChanged;
        }

        private void OnDisable()
        {
            // Unsubscribe from player change event when this object gets disabled
            IM.onPlayerIsChanged -= OnPlayerChanged;
        }

        private void Start()
        {
            // Initialize player from global player manager and setup portrait UI
            player = IM.Player.GetComponent<DemoCharacter>();

            defaultPortrait = playerPortrait.sprite;
            
            // Use custom portrait from global player if available, else use default
            playerPortrait.sprite = IM.Player.Portrait ?? defaultPortrait;
            
            // Add a click listener to player's portrait button to open actor menu
            playerPortrait.GetComponent<Button>().onClick.AddListener(() =>
            {
                IntrigueSystemUI.instance.OpenActorMenu(IM.Player);
            });
            
            // Sets a lightweight candidate filter to pre-screen potential partners before compatibility evaluation.
            // This avoids unnecessary graph-based rule checks by eliminating clearly invalid candidates early.
            // 
            // Conditions:
            // - Only consider actors who are not currently married (HasSpouse == false)
            // - Only consider actors of a different gender (assumes monogamous heterosexual rules)
            // 
            // This filter significantly improves performance in large-scale simulations (e.g., 300+ actors)
            // by reducing the number of expensive asynchronous compatibility checks.
            // You can customize this filter to allow same-gender or polygamous pairing based on your game rules.

            IM.Instance.MatchEngine.CandidateFilter =
                (self, other) => other.HasSpouse == false && other.Gender != self.Gender;

        }

        private void OnPlayerChanged(Actor oldPlayer, Actor newPlayer)
        {
            // Update player reference and portrait when player changes
            player = newPlayer.GetComponent<DemoCharacter>();
            playerPortrait.sprite = newPlayer.Portrait ?? defaultPortrait;
        }

        /// <summary>
        /// Adds a specified amount of coins to the player's inventory.
        /// </summary>
        /// <param name="amount">The amount of coins to add.</param>
        public void AddCoin(int amount)
        {
            player.currentCoin.Value += amount;
        }
        
        public void AddBomb(int amount)
        {
            NotificationSystem.ShowNotification($"{amount} unit of <color=#FF6262>The Bomb of Night's</color> Envoys has been <color=#9BFF78>added</color> to your inventory.");
            player.currentBomb.Value += amount;
        }

        public void AddPoison(int amount)
        {
            NotificationSystem.ShowNotification($"{amount} unit of <color=#FF6262>Venomous Weaver</color> venom has been <color=#9BFF78>added</color> to your inventory.");
            player.currentPoison.Value += amount;
        }

        public void AddPower(int amount)
        {
            NotificationSystem.ShowNotification($"You have <color=#9BFF78>acquired</color> an additional {amount} points of Scheme <b>Power</b>.");
            player.currentPower.Value += amount;
        }

        public void UpdateBagTooltip()
        {
            bagTooltip.header = "Items in your bag";
            bagTooltip.content = $"Venomous Weaver's Venom: {player.currentPoison.Value}\nThe Bomb of Night's Envoys: {player.currentBomb.Value}";
        }
        
        public void UpdateSpyTooltip()
        {
            spyTooltip.content = $"Available Spy: {player.currentSpy.Value}";
        }        
        
        public void UpdatePower()
        {
            powerTooltip.content = $"Scheme Power: {player.currentPower.Value}";
        }
        
        public void UpdateCoinTooltip()
        {
            coinTooltip.content = $"Current Coins: {player.currentCoin.Value}";
        }

        private void Update()
        {
            // Regularly update UI text elements with current player stats
            
            powerText.text = player.currentPower.Value.ToString();
            spyText.text = player.currentSpy.Value.ToString();
            coinText.text = player.currentCoin.Value.ToString();
            bagText.text = (player.currentPoison.Value + player.currentBomb.Value).ToString();
            
            // Handle key inputs for pause toggle and showing key notes

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            if ( Input.GetKeyDown(KeyCode.Y) ) {
                IM.Player.StartScheme("Family Honor");
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                keyNotes.SetActive(!keyNotes.activeInHierarchy);
            }
        }

        /// <summary>
        /// Toggles the pause state of the game.
        /// </summary>
        /// <remarks>
        /// When called, this method either pauses or resumes the game, updating the UI and game speed accordingly.
        /// It also triggers events related to opening or closing the quit menu.
        /// </remarks>
        public void TogglePause()
        {
            gameIsPaused = !gameIsPaused;
            if (gameIsPaused)
            {
                // When the game is paused, trigger pause related actions and freeze the game time
                
                onQuitMenuOpen.Invoke();
                DialogueManager.HideAll();
                IntrigueSystemUI.instance.Hide();
                Time.timeScale = 0;
                
                speedControl.gameObject.SetActive(false);
            }
            else
            {
                // When un-pausing, resume normal game operations and time flow
                
                onQuitMenuClosed.Invoke();
                DialogueManager.ShowAll();
                IntrigueSystemUI.instance.Show();
                speedControl.gameObject.SetActive(true);

                // Handle the speed control state based on its current setting
                switch (speedControl.State)
                {
                    case SpeedControl.SpeedMode.Paused: speedControl.Pause(); break;
                    case SpeedControl.SpeedMode.Normal: speedControl.Normalize(); break;
                    case SpeedControl.SpeedMode.SpeedUp: speedControl.SpeedUp(); break;
                }
            }
        }

        public void SaveGame()
        {
            SaveSystem.IntrigueSaveSystem.Instance.SaveOverwrite();
            NotificationSystem.ShowNotification("game data is <color=#9BFF78>saved</color>.");
        }
        
        public void StorePage()
        {
            Application.OpenURL("https://u3d.as/2TCR");
        }

        public void GotoMainMenu()
        {
            SceneManager.LoadScene(sceneBuildIndex: 0);
            Time.timeScale = 1;
        }
        
        public void Quit()
        {
            Application.Quit();
        }
    }
}