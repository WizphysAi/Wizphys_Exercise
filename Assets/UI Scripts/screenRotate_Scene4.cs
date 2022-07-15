using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class screenRotate_Scene4 : MonoBehaviour
{
    // Start is called before the first frame update
    public void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
