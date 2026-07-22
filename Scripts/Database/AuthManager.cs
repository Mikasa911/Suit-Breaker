// using UnityEngine;
// using GooglePlayGames;
// using GooglePlayGames.BasicApi;
// using Firebase.Auth;
// using Firebase.Extensions;

// public class AuthManager : MonoBehaviour
// {
//     FirebaseAuth auth;

//     void Start()
//     {
//         auth = FirebaseAuth.DefaultInstance;
//     }

//     public void SignInWithPlayGames()
//     {
//         // 1. Authenticate (Standard v2 way)
//         PlayGamesPlatform.Instance.Authenticate((SignInStatus status) =>
//         {
//             if (status == SignInStatus.Success)
//             {
//                 Debug.Log("GPGS: Login Success. Requesting Server Auth Code...");

//                 // 2. Request the Server Auth Code manually here
//                 // 'true' forces a refresh to ensure we get a valid code
//                 PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
//                 {
//                     if (string.IsNullOrEmpty(code))
//                     {
//                         Debug.LogError("GPGS: Failed to get Server Auth Code. Check Client ID in Android Setup.");
//                         return;
//                     }

//                     Debug.Log("GPGS: Got Auth Code! Signing into Firebase...");
//                     FirebaseSignIn(code);
//                 });
//             }
//             else
//             {
//                 Debug.LogError("GPGS: Login Failed. Status: " + status);
//             }
//         });
//     }

//     private void FirebaseSignIn(string serverAuthCode)
//     {
//         Credential credential = PlayGamesAuthProvider.GetCredential(serverAuthCode);

//         auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
//         {
//             if (task.IsFaulted)
//             {
//                 Debug.LogError("Firebase Error: " + task.Exception);
//                 return;
//             }

//             Debug.Log("Firebase Login Success! User: " + task.Result.DisplayName);
//         });
//     }
// }