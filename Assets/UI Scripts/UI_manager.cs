using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_manager : MonoBehaviour
{
    //public GameObject signIn;
    public GameObject profileScreen;
    public GameObject dashboardScreen;
    public GameObject shoulderPlan;
    public GameObject kneePlan;
    public GameObject programSelectionScreen;


    void Start()
    {
        //signIn.SetActive(true);
        profileScreen.SetActive(false);
        dashboardScreen.SetActive(false);
        shoulderPlan.SetActive(false);
        kneePlan.SetActive(false); 
        programSelectionScreen.SetActive(false);
    }

    public void EditProfile()
    {
        //signIn.SetActive(false);
        profileScreen.SetActive(true);
        dashboardScreen.SetActive(false);
        shoulderPlan.SetActive(false);
        kneePlan.SetActive(false);
        programSelectionScreen.SetActive(false);
    }

    public void StartShoulder()
    {
        SceneManager.LoadScene("Bracing");
    }
    public void StartKnee()
    {
        SceneManager.LoadScene("Bracing");
    }
    public void ExerciseProgram()
    {
        //signIn.SetActive(false);
        profileScreen.SetActive(false);
        dashboardScreen.SetActive(false);
        shoulderPlan.SetActive(false);
        kneePlan.SetActive(false);
        programSelectionScreen.SetActive(true);
    }

    public void ShoulderPlan()
    {
        //signIn.SetActive(false);
        profileScreen.SetActive(false);
        dashboardScreen.SetActive(false);
        shoulderPlan.SetActive(true);
        kneePlan.SetActive(false);
        programSelectionScreen.SetActive(false);
    }

    public void KneePlan()
    {
        //signIn.SetActive(false);
        profileScreen.SetActive(false);
        dashboardScreen.SetActive(false);
        shoulderPlan.SetActive(false);
        kneePlan.SetActive(true);
        programSelectionScreen.SetActive(false);
    }
    public void Reports()
    {
        Debug.Log("Reported Button Clicked");
    }

    public void CreateProfile()
    {
        SceneManager.LoadScene("Dashboard");
    }

}
