using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseINIT : MonoBehaviour
{
    void Start()
    {
        CheckIfReady();
    }

    public static void CheckIfReady()
    {

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            Firebase.DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {

                Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
                SceneManager.LoadScene("Login");
                Debug.Log("Firebase is ready for use.");
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            }
        });
    }
}
