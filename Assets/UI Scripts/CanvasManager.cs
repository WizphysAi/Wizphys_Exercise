using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    public GameObject loginCanvas;
    public GameObject dashboardCanvas;
    public GameObject FTUcanvas;
    void Start()
    {
        loginCanvas.SetActive(true);
        dashboardCanvas.SetActive(false);
        FTUcanvas.SetActive(false);
    }
    public void CreateProfile()
    {
        loginCanvas.SetActive(false);
        dashboardCanvas.SetActive(true);
        FTUcanvas.SetActive(false);
    }

    
}
