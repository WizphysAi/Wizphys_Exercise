using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QAPageUI : MonoBehaviour
{
    
    [SerializeField] GameObject QA1;
    [SerializeField] GameObject QA2;
    [SerializeField] GameObject QA3;
    [SerializeField] GameObject QA4;
    [SerializeField] GameObject QA5;
    [SerializeField] GameObject QA6;
    [SerializeField] GameObject QA7;
    [SerializeField] GameObject painScale;


    public void Start()
    {

        painScale.gameObject.SetActive(true);
        QA1.gameObject.SetActive(false);
        QA2.gameObject.SetActive(false);
        QA3.gameObject.SetActive(false);
        QA4.gameObject.SetActive(false);
        QA5.gameObject.SetActive(false);
        QA6.gameObject.SetActive(false);
        QA7.gameObject.SetActive(false);

        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

    }

    public void qA2()
    {
        QA1.gameObject.SetActive(false);
        QA2.gameObject.SetActive(true);
        QA3.gameObject.SetActive(false);
        QA4.gameObject.SetActive(false);
        QA5.gameObject.SetActive(false);
        QA6.gameObject.SetActive(false);
        QA7.gameObject.SetActive(false);
    }
    public void qA3()
    {
        QA1.gameObject.SetActive(false);
        QA2.gameObject.SetActive(false);
        QA3.gameObject.SetActive(true);
        QA4.gameObject.SetActive(false);
        QA5.gameObject.SetActive(false);
        QA6.gameObject.SetActive(false);
        QA7.gameObject.SetActive(false);
    }
    public void qA4()
    {
        QA1.gameObject.SetActive(false);
        QA2.gameObject.SetActive(false);
        QA3.gameObject.SetActive(false);
        QA4.gameObject.SetActive(true);
        QA5.gameObject.SetActive(false);
        QA6.gameObject.SetActive(false);
        QA7.gameObject.SetActive(false);
    }
    public void qA5()
    {
        QA1.gameObject.SetActive(false);
        QA2.gameObject.SetActive(false);
        QA3.gameObject.SetActive(false);
        QA4.gameObject.SetActive(false);
        QA5.gameObject.SetActive(true);
        QA6.gameObject.SetActive(false);
        QA7.gameObject.SetActive(false);
    }
    public void qA6()
    {
        QA1.gameObject.SetActive(false);
        QA2.gameObject.SetActive(false);
        QA3.gameObject.SetActive(false);
        QA4.gameObject.SetActive(false);
        QA5.gameObject.SetActive(false);
        QA6.gameObject.SetActive(true);
        QA7.gameObject.SetActive(false);
    }
    public void qA7()
    {
        QA1.gameObject.SetActive(false);
        QA2.gameObject.SetActive(false);
        QA3.gameObject.SetActive(false);
        QA4.gameObject.SetActive(false);
        QA5.gameObject.SetActive(false);
        QA6.gameObject.SetActive(false);
        QA7.gameObject.SetActive(true);
       
    }



    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
           
            SceneManager.LoadScene("Report");
        }
    }

    public void LoadReports()
    {
        SceneManager.LoadScene("login");
    }

    public void loadQa1()
    {
        QA1.gameObject.SetActive(true);
        painScale.gameObject.SetActive(false);

    }

}
