using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_4EX : MonoBehaviour
{
    public GameObject Dasboard;
    public GameObject GuidanceScreen;
    public GameObject ExerciseList;
    public GameObject ProfileSettings;
    public GameObject ReportsScreen;

    private void Start()
    {
        Dasboard.SetActive(true);
        GuidanceScreen.SetActive(false);
        ExerciseList.SetActive(false);
        ProfileSettings.SetActive(false);
        ReportsScreen.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            SceneManager.LoadScene("Dashboard");

            return;
        }
    }


    public void StartBracing()
    {
        SceneManager.LoadScene("Bracing");
    }
    public void StartSRSE()
    {
        SceneManager.LoadScene("SRSE");
    }
    public void StartSS()
    {
        SceneManager.LoadScene("SS");
    }
    public void StartRhomboids()
    {
        SceneManager.LoadScene("Rhomboids");
    }
    public void StartSA()
    {
        SceneManager.LoadScene("SA");
    }
    public void StartIS()
    {
        SceneManager.LoadScene("IS");
    }
    public void StartLowRow()
    {
        SceneManager.LoadScene("LowRow");
    }
    public void StartPronatedSR()
    {
        SceneManager.LoadScene("PronatedSR");
    }
    public void StartPronated()
    {
        SceneManager.LoadScene("Pronated");
    }
    public void StartBridge()
    {
        SceneManager.LoadScene("Bridge");
    }
    

    public void Reports()
    {
        Dasboard.SetActive(false);
        GuidanceScreen.SetActive(false);
        ExerciseList.SetActive(false);
        ProfileSettings.SetActive(false);
        ReportsScreen.SetActive(true);
    }

    public void ProfileSelection()
    {
        Dasboard.SetActive(false);
        GuidanceScreen.SetActive(false);
        ExerciseList.SetActive(false);
        ProfileSettings.SetActive(true);
        ReportsScreen.SetActive(false);
    }

    public void GuidanceSection()
    {
        Dasboard.SetActive(false);
        GuidanceScreen.SetActive(true);
        ExerciseList.SetActive(false);
        ProfileSettings.SetActive(false);
        ReportsScreen.SetActive(false);
    }
    public void ExerciseListScreen()
    {
        Dasboard.SetActive(false);
        GuidanceScreen.SetActive(false);
        ExerciseList.SetActive(true);
        ProfileSettings.SetActive(false);
        ReportsScreen.SetActive(false);
    }

    public void Home()
    {
        Dasboard.SetActive(true);
        GuidanceScreen.SetActive(false);
        ExerciseList.SetActive(false);
        ProfileSettings.SetActive(false);
        ReportsScreen.SetActive(false);
    }

    public void BackButton()
    {
        
        SceneManager.LoadScene("Dashboard");
        
    }

    public void SkipButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}
