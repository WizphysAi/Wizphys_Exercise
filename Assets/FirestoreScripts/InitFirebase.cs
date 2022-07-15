using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using UnityEngine.Events;

public class InitFirebase : MonoBehaviour
{
    public UnityEvent OnFirebaseInitalized = new UnityEvent();

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.Log($"failed to init firebase with{task.Exception}");
                return;
            }

            OnFirebaseInitalized.Invoke();
        });
    }
}
