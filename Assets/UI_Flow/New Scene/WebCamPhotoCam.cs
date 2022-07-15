using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using TensorFlowLite;

public class WebCamPhotoCam : MonoBehaviour
{
    WebCamTexture webCamTexture;
    public RawImage display;
    public AspectRatioFitter fit;

    [SerializeField, WebCamName] private string editorCameraName;
    [SerializeField] private bool isFrontFacing = false;
    private WebCamDevice[] devices;
    private int deviceIndex;
    [SerializeField] private WebCamKind preferKind = WebCamKind.WideAngle;


    void Start()
    {
        devices = WebCamTexture.devices;
        string cameraName = Application.isEditor
            ? editorCameraName
            : WebCamUtil.FindName(preferKind, isFrontFacing);

        WebCamDevice device = default;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].name == cameraName)
            {
                device = devices[i];
                deviceIndex = i;
                break;
            }
        }
        //webCamTexture = new WebCamTexture();
        //webCamTexture.Play();
        StartCamera(device);
        //

    }

    void Update()
    {
        GetComponent<RawImage>().texture = webCamTexture;

        float ratio = (float)webCamTexture.width / (float)webCamTexture.height;
        fit.aspectRatio = ratio;


        float ScaleY = webCamTexture.videoVerticallyMirrored ? -1f : 1f;
        display.rectTransform.localScale = new Vector3(1f, ScaleY, 1f);

        int orient = -webCamTexture.videoRotationAngle;
        display.rectTransform.localEulerAngles = new Vector3(0, 0, orient);

        //display.texture = webCamTexture;
    }

    private void StopCamera()
    {
        if (webCamTexture == null)
        {
            return;
        }
        webCamTexture.Stop();
        Destroy(webCamTexture);
    }
    public void ToggleCamera()
    {
        deviceIndex = (deviceIndex + 1) % devices.Length;
        StartCamera(devices[deviceIndex]);
    }
    private void StartCamera(WebCamDevice device)
    {
        StopCamera();
        isFrontFacing = device.isFrontFacing;
        webCamTexture = new WebCamTexture();
        webCamTexture.Play();  //camera view

    }

    private void OnDestroy()
    {
        StopCamera();
    }

    public void PhotoClick()
    {
        StartCoroutine(TakePhoto());
    }

    IEnumerator TakePhoto() // Start this Coroutine on some button click
    {

        // NOTE - you almost certainly have to do this here:

        yield return new WaitForEndOfFrame();

        // Unity Doc
        // http://docs.unity3d.com/ScriptReference/WaitForEndOfFrame.html

        Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height);
        photo.SetPixels(webCamTexture.GetPixels());
        photo.Apply();

        //Encode to a PNG
        byte[] bytes = photo.EncodeToPNG();
        //Write out the PNG. Of course you have to substitute your_path for something sensible
        File.WriteAllBytes(Application.persistentDataPath + "photo.png", bytes);
    }
}