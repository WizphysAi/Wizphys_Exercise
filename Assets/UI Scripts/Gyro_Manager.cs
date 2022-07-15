using UnityEngine;
using UnityEngine.UI;

public class Gyro_Manager : MonoBehaviour
{
    //[SerializeField] Text AngleValue;
    [SerializeField] Slider gyroSlider;
    [SerializeField] Gradient gradient;
    public float gyrovalues;
    
    void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }
    }

    void Update()
    {
        Quaternion deviceRotation = new Quaternion(0.5f, 0.5f, -0.5f, 0.5f) * Input.gyro.attitude * new Quaternion(0, 0, 1, 0);
        float x = (deviceRotation).x;
        float y = (deviceRotation).y;
        float z = (deviceRotation).z;
        float w = (deviceRotation).w;
        float pitch = Mathf.Atan2(2 * x * w - 2 * y * z, 1 - 2 * x * x - 2 * z * z);
        float yaw  = Mathf.Atan2(2*y*w - 2*x*z, 1 - 2*y*y - 2*z*z);
        float roll =  Mathf.Asin(2*x*y + 2*z*w);
        // Debug.Log("pitch: "+pitch);
        // Debug.Log("yaw: "+yaw);
        // Debug.Log("roll: "+roll);
        //AngleValue.text = "AngleValue:" + (gyrovalues*10).ToString("n2");
        gyrovalues = Mathf.Atan2(2 * x * w - 2 * y * z, 1 - 2 * x * x - 2 * z * z);
        gyroSlider.value = gyrovalues*10;
    }
}
