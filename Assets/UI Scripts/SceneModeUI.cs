using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneModeUI : MonoBehaviour
{
    public GameObject quitPanel;
    public Image loadingAnim;
    private Button yes;
    private Button no;
    private int Counter = 0;
    void Start()
    {
        quitPanel.gameObject.SetActive(false);
        loadingAnim.gameObject.SetActive(false);


        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            quitPanel.gameObject.SetActive(true);
        }
    }

    public void LoadDashboard(string SceneName)
    {
        SceneManager.LoadScene("Dashboard");
        loadingAnim.gameObject.SetActive(true);
    }

    public void ss(string SceneName)
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("SS");
    }
    public void IS(string SceneName)
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("IS");
    }
    public void Bracing(string SceneName)
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("Bracing");
    }
    public void Loading(string SceneName)
    {
        SceneManager.LoadScene("Loading");
    }

    public void closeQuitPanel()
    {
        quitPanel.gameObject.SetActive(false);
    }

    public void LoadReports()
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("Reports");
    }
    public void Pronated(string SceneName)
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("Pronated");
    }
    public void LoadQA()
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("QA Page");
    }

    public void LoadNextScene()
    {
        if(Counter >= 10)
        {
            loadingAnim.gameObject.SetActive(true);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex +1);
    }

}
