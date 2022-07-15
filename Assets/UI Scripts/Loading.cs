using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
    public Slider slider;
    public GameObject loadingScreen;

    public void LoadLevel(string SceneName)
    {
        loadingScreen.SetActive(true);
        StartCoroutine(LoadingScene("Scene1"));
    }
    IEnumerator LoadingScene(string SceneName)
    {
        yield return null;
        AsyncOperation operation = SceneManager.LoadSceneAsync("Scene1");
        operation.allowSceneActivation = false;
        while (!operation.isDone)
        {
            float running = Mathf.Clamp01(operation.progress / .9f);
            slider.value = running;
            if (running == 1) { operation.allowSceneActivation = true; }
            yield return null;
        }
    }
}
