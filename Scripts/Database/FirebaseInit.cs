using Firebase;
using Firebase.Analytics;
using UnityEngine;
using UnityEngine.Events;

public class FirebaseInit : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this);
    }
    public UnityEvent OnFirebaseInitialized=new();
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(continuationAction: task =>
        {
            if(task.Exception!=null)
            {
                Debug.LogError("Failed to Initialize Firebaswe with " +task.Exception);
                return;
            }
            OnFirebaseInitialized.Invoke();
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        });
    }
}