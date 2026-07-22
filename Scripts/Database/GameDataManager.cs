using UnityEngine;
using UnityEngine.UI;
// using GooglePlayGames;
// using GooglePlayGames.BasicApi;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class GameDataManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject nameSelectionPanel;
    public TMP_InputField nameInputField;
    public TMP_InputField OfflineNameInputField;

    // Variable to store the name the user typed BEFORE logging in
    private string nameToRegister;

    [System.Serializable]
    public class MyPlayerData
    {
        public string playerName;
        public int coins;
        public int playerID;
        public string googleID; // <--- ADD THIS

        public MyPlayerData(string name, int id, string gId)
        {
            this.playerName = name;
            this.coins = 0;
            this.playerID = id;
            this.googleID = gId;
        }
    }

    private FirebaseAuth auth;
    private DatabaseReference dbReference;
    private string currentFirebaseUserId;

    public static GameDataManager Instance;

//database
    // void Awake()
    // {
    //     if (Instance == null)
    //     {
    //         Instance = this;
    //         DontDestroyOnLoad(gameObject);
    //     }
    //     else
    //     {
    //         Destroy(gameObject);
    //         return;
    //     }

    //     PlayGamesPlatform.DebugLogEnabled = true;
    //     PlayGamesPlatform.Activate();
    // }

    void Start()
    {
        // CHANGED: Show the panel immediately so user can type name first
        nameSelectionPanel.SetActive(true);

        // CHANGED: Do NOT initialize Firebase yet. Wait for button click.
    }

    // --- STEP 1: BUTTON CLICK STARTS THE PROCESS ---
//database
    // public void OnSubmitNameButton()
    // {
    //     string chosenName = nameInputField.text;

    //     if (string.IsNullOrEmpty(chosenName))
    //     {
    //         Debug.LogWarning("Name cannot be empty!");
    //         return;
    //     }

    //     // CHANGED: Store the name in a variable to use LATER after login
    //     nameToRegister = chosenName;

    //     // Optional: Update your offline field now if needed
    //     if (OfflineNameInputField != null) OfflineNameInputField.text = chosenName;

    //     // Disable panel to prevent double clicks
    //     nameSelectionPanel.SetActive(false);

    //     // CHANGED: NOW we start the login process
    //     Debug.Log("Name accepted. Starting Login...");
    //     InitializeFirebase();
    // }

    // --- STEP 2: LOGIN ---

    // void InitializeFirebase()
    // {
    //     FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
    //     {
    //         if (task.Result == DependencyStatus.Available)
    //         {
    //             auth = FirebaseAuth.DefaultInstance;
    //             dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    //             SignInWithPlayGames();
    //         }
    //         else
    //         {
    //             Debug.LogError("Firebase Error: " + task.Result);
    //             // Re-enable panel if error so they can try again
    //             nameSelectionPanel.SetActive(true);
    //         }
    //     });
    // }

//database
    // public void SignInWithPlayGames()
    // {
    //     PlayGamesPlatform.Instance.Authenticate((SignInStatus status) =>
    //     {
    //         if (status == SignInStatus.Success)
    //         {
    //             PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
    //             {
    //                 if (!string.IsNullOrEmpty(code)) ExchangeAuthCodeForFirebase(code);
    //             });
    //         }
    //         else
    //         {
    //             Debug.LogError("GPGS Login Failed");
    //             // Login failed, show panel again to retry
    //             nameSelectionPanel.SetActive(true);
    //         }
    //     });
    // }

    // private void ExchangeAuthCodeForFirebase(string authCode)
    // {
    //     Credential credential = PlayGamesAuthProvider.GetCredential(authCode);
    //     auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
    //     {
    //         if (task.IsFaulted)
    //         {
    //             nameSelectionPanel.SetActive(true); // Retry on fail
    //             return;
    //         }

    //         FirebaseUser newUser = task.Result;
    //         currentFirebaseUserId = newUser.UserId;

    //         CheckIfUserExists();
    //     });
    // }

    // --- STEP 3: CHECK & CREATE ---

    // private void CheckIfUserExists()
    // {
    //     dbReference.Child("players").Child(currentFirebaseUserId).GetValueAsync().ContinueWithOnMainThread(task =>
    //     {
    //         if (task.IsFaulted) return;

    //         DataSnapshot snapshot = task.Result;

    //         if (snapshot.Exists)
    //         {
    //             // RETURNING USER: Load game directly
    //             // Note: We ignore 'nameToRegister' because they already have an account
    //             string loadedName = snapshot.Child("playerName").Value.ToString();
    //             Debug.Log("Welcome back, " + loadedName);
    //         }
    //         else
    //         {
    //             // NEW USER: We don't need to ask for name, we already have it!
    //             Debug.Log("New user detected! Creating account with name: " + nameToRegister);

    //             // CHANGED: Use the variable we saved at the start
    //             GenerateIdAndCreateUser(nameToRegister);
    //         }
    //     });
    // }

    // // --- STEP 4: CREATE ACCOUNT ---

    // private void GenerateIdAndCreateUser(string customName)
    // {
    //     DatabaseReference counterRef = dbReference.Child("metadata").Child("lastPlayerID");

    //     counterRef.RunTransaction(mutableData =>
    //     {
    //         int currentId = 1000;
    //         if (mutableData.Value != null)
    //         {
    //             int.TryParse(mutableData.Value.ToString(), out currentId);
    //         }
    //         mutableData.Value = currentId + 1;
    //         return TransactionResult.Success(mutableData);
    //     })
    //     .ContinueWithOnMainThread(task =>
    //     {
    //         if (task.IsCompleted)
    //         {
    //             DataSnapshot snapshot = task.Result;
    //             int newUniqueId = int.Parse(snapshot.Value.ToString());

    //             CreateUserObject(customName, newUniqueId);
    //         }
    //     });
    // }

    // private void CreateUserObject(string name, int id)
    // {
    //     // Get the Google Play Games ID securely
    //     string myGoogleID = PlayGamesPlatform.Instance.GetUserId();

    //     MyPlayerData newData = new MyPlayerData(name, id, myGoogleID);
    //     string json = JsonUtility.ToJson(newData);

    //     dbReference.Child("players").Child(currentFirebaseUserId).SetRawJsonValueAsync(json)
    //         .ContinueWithOnMainThread(task =>
    //         {
    //             Debug.Log("Account Created with Google ID: " + myGoogleID);
    //             // Load Scene...
    //         });
    // }
    // --- STEP 5: GAMEPLAY METHODS ---

    /// <summary>
    /// Updates the coin count for a specific player ID (e.g., 1001).
    /// Usage: GameDataManager.Instance.UpdateCoins(1001, 500);
    /// </summary>
    /// <summary>
    /// Adds or subtracts coins from the player's existing balance.
    /// Usage: 
    /// Add 100 coins:  GameDataManager.Instance.AddCoins(1001, 100);
    /// Deduct 50 coins: GameDataManager.Instance.AddCoins(1001, -50);
    /// </summary>
    public void AddCoins(int targetPlayerId, int coinsToAdd)
    {
        // 1. Find the user with this specific playerID (e.g., 1001)
        Query query = dbReference.Child("players").OrderByChild("playerID").EqualTo(targetPlayerId);

        query.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error finding player: " + task.Exception);
                return;
            }

            if (!task.Result.Exists)
            {
                Debug.LogError("Player ID " + targetPlayerId + " not found!");
                return;
            }

            // 2. Process the result (usually just one user found)
            foreach (DataSnapshot child in task.Result.Children)
            {
                // A. Read the current balance safely
                int currentBalance = 0;
                if (child.Child("coins").Value != null)
                {
                    int.TryParse(child.Child("coins").Value.ToString(), out currentBalance);
                }

                // B. Calculate the new total
                int newTotal = currentBalance + coinsToAdd;

                // Prevent negative balance (Optional - remove if you allow debt)
                if (newTotal < 0) newTotal = 0;
                                                                                                                
                // C. Write the new total back to the database
                // child.Reference is a shortcut to the specific user's folder
                child.Reference.Child("coins").SetValueAsync(newTotal);

                Debug.Log($"Transaction: Player {targetPlayerId} | {currentBalance} + ({coinsToAdd}) = {newTotal}");
            }
        });
    }
}