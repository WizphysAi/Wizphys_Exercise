using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReportScreenUI : MonoBehaviour
{
    public void LoadDashboard(string SceneName)
    {
        SceneManager.LoadScene("Dashboard");
    }

    private void Start()
    {
        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            SceneManager.LoadScene("Dashboard");
        }
    }
}
