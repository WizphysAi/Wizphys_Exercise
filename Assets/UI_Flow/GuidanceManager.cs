using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GuidanceManager : MonoBehaviour
{

    public void IS()
    {
        SceneManager.LoadScene("Guidance_IS");
    }

    public void SS()
    {
        SceneManager.LoadScene("Guidance_SS");
    }

    public void Bracing()
    {
        SceneManager.LoadScene("Guidance_Bracing");
    }

    public void Pronated()
    {
        SceneManager.LoadScene("Guidance_Pronated");
    }

    
}
