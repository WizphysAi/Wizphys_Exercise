using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DashboardUIManager : MonoBehaviour
{

    //public Image redButton;
    //public Image greenButton;

    public GameObject dashboardUI;
    public GameObject exitApp;
    //public Slider slider;
    //public GameObject loadingScreen;
    public Image loadingAnim;

    void Start()
    {
        //redButton.gameObject.SetActive(true);
        //greenButton.gameObject.SetActive(false);
        dashboardUI.gameObject.SetActive(true);
        loadingAnim.gameObject.SetActive(false);
        exitApp.gameObject.SetActive(false);
        //loadingScreen.gameObject.SetActive(false);

        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
   
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //Application.Quit();
            //SceneManager.LoadScene("Dashboard");

            exitApp.gameObject.SetActive(true);
        }

    }

    public void LoadDashboard(string SceneName)
    {
        SceneManager.LoadScene("Dashboard");       
    }

    public void LoadProfile(string SceneName)
    {
        SceneManager.LoadScene("Profile");
    }

    public void LoadSignInPage(string SceneName)
    {
        SceneManager.LoadScene("Login Page");
    }

    public void LoadRegister(string SceneName)
    {
        SceneManager.LoadScene("Register");
    }
    public void Login_Page(string SceneName)
    {
        SceneManager.LoadScene("Login Page");
    }
    public void PersonalInfo(string SceneName)
    {
        SceneManager.LoadScene("Personal info");
    }

    public void LoadStartExercises(string SceneName)
    {
        SceneManager.LoadScene("Bracing");
        dashboardUI.gameObject.SetActive(false);
        //loadingScreen.gameObject.SetActive(true);
        loadingAnim.gameObject.SetActive(true);
    }

    public void LoadWeekPlan(string SceneName)
    {
        SceneManager.LoadScene("Weekwise Plan");
    }

    public void LoadReports()
    {
        SceneManager.LoadScene("Reports");
    }




    public void LoadScene1()
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("Bracing");
    }
    public void LoadScene2()
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("SS");
    }

    public void LoadScene3()
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("IS");
    }

    public void LoadScene4()
    {
        loadingAnim.gameObject.SetActive(true);
        SceneManager.LoadScene("Pronated");
    }

    //public void LoadNextScene()
    //{
    //    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex +1);
    //}
     public void Yes()
    {
        Application.Quit();
    }

    public void No()
    {
        exitApp.gameObject.SetActive(false);
    }
    

}
