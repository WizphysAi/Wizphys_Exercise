using System.Threading;
using Cysharp.Threading.Tasks;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(WebCamInput))]
public sealed class BlazePoseBridge : MonoBehaviour
{
    [SerializeField]
    private BlazePose.Options options = default;

    [SerializeField]
    private RectTransform containerView = null;
    [SerializeField]
    private RawImage debugView = null;
    [SerializeField]
    private RawImage segmentationView = null;

    [SerializeField]
    private Canvas canvas = null;
    [SerializeField]
    private bool runBackground;
    [SerializeField, Range(0f, 1f)]
    private float visibilityThreshold = 0.5f;


    private BlazePose pose;
    private PoseDetect.Result poseResult;
    private PoseLandmarkDetect.Result landmarkResult;
    private BlazePoseDrawer drawer;

    [Header("Text")]
    [SerializeField] Text StandStill;
    [SerializeField] Text Exercise;
    [SerializeField] Text Raise_Up;
    [SerializeField] Text Counter;


    [Header("Audio Souce")]
    [SerializeField]
    AudioClip pronated_Audio;

    [SerializeField] AudioClip Counter_Audio;
    public AudioSource CounterAudio;

    [Header("Error Object")]
    [SerializeField]
    AudioClip pronated_Object;
    [SerializeField]
    AudioClip neckUp_Object;
    [SerializeField]
    AudioClip pbLeft_Object;
    [SerializeField]
    AudioClip pbRight_Object;

    [Header("Exercise Start/Stop Signal")]
    [SerializeField] Image greenSignal;
    [SerializeField] Image redSignal;

    //Check StillPronated
    int Frame;
    int StillFrame;
    int StillCount;
    bool StillFlag = false;
    float NormalizingFactor;
    float PrevShoulderLX = 0;
    float PrevShoulderRX = 0;
    float PrevHipLX = 0;
    float PrevHipRX = 0;
    float PrevElbowX = 0;
    bool PronatedRight = false;


    //Check Movement Variables
    int MoveCount = 0;
    float PrevHipLY = 0;
    float PrevHipRY = 0;

    //Bridge variables
    int BridgeCounter = 0;
    int BridgeCount = 0;
    int RaiseUpCount = 0;
    bool prevBridgeFlag = false;
    bool BridgeFlag = false;

    public Slider slider;
    float sliderCount = 0;
    float sliderValue = 0;


    private UniTask<bool> task;
    private CancellationToken cancellationToken;

    private void Start()
    {
        slider.value = 0;
        Exercise.gameObject.SetActive(false);
        Raise_Up.gameObject.SetActive(false);

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);


        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }

        pose = new BlazePose(options);

        drawer = new BlazePoseDrawer(Camera.main, gameObject.layer, containerView);

        cancellationToken = this.GetCancellationTokenOnDestroy();
        CounterAudio.clip = Counter_Audio;

        GetComponent<WebCamInput>().OnTextureUpdate.AddListener(OnTextureUpdate);

        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void OnDestroy()
    {
        GetComponent<WebCamInput>().OnTextureUpdate.RemoveListener(OnTextureUpdate);
        pose?.Dispose();
        drawer?.Dispose();
    }

    private void OnTextureUpdate(Texture texture)
    {
        if (runBackground)
        {
            if (task.Status.IsCompleted())
            {
                task = InvokeAsync(texture);
            }
        }
        else
        {
            Invoke(texture);
        }
    }

    private void Update()
    {
        Exercise.gameObject.SetActive(false);
        Raise_Up.gameObject.SetActive(false);

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);

        if (landmarkResult != null && landmarkResult.score > 0.2f)
        {
            //float TorsoSlope = (landmarkResult.viewportLandmarks[23][1] - landmarkResult.viewportLandmarks[11][1]) / (landmarkResult.viewportLandmarks[23][0] - landmarkResult.viewportLandmarks[11][0]);
            //Debug.Log("TorsoSlope" + TorsoSlope*10);

            if (StillFlag == true)
            {
                CheckMovementLeft();
                
                if (StillFlag == true)
                {

                    greenSignal.gameObject.SetActive(true);
                    redSignal.gameObject.SetActive(false);

                    CheckBridge();
                }
            }
            else
            {
                Exercise.gameObject.SetActive(false);
                Raise_Up.gameObject.SetActive(false);

                greenSignal.gameObject.SetActive(false);
                redSignal.gameObject.SetActive(true);

                CheckStillPronated();
            }

            drawer.DrawLandmarkResult(landmarkResult, visibilityThreshold, canvas.planeDistance);

            Frame = Frame + 1;
        }
        else
        {
            StillFlag = false;
            StandStill.text = "Please stand in the frame";
            StandStill.gameObject.SetActive(true);
        }

        //Main Audio Player
        AudioSource audioSource = GetComponent<AudioSource>();

    }

    private Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    private void Invoke(Texture texture)
    {
        Debug.Log("Texture" + texture);
        landmarkResult = pose.Invoke(texture);
        poseResult = pose.PoseResult;
        if (pose.LandmarkInputTexture != null)
        {
            debugView.texture = pose.LandmarkInputTexture;
        }
        if (landmarkResult != null && landmarkResult.SegmentationTexture != null)
        {
            segmentationView.texture = landmarkResult.SegmentationTexture;
        }
    }

    private async UniTask<bool> InvokeAsync(Texture texture)
    {
        landmarkResult = await pose.InvokeAsync(texture, cancellationToken);
        poseResult = pose.PoseResult;
        if (pose.LandmarkInputTexture != null)
        {
            debugView.texture = pose.LandmarkInputTexture;
        }
        return landmarkResult != null;
    }

    private void CheckStillPronated()
    {
        if ((landmarkResult.viewportLandmarks[12][0] + landmarkResult.viewportLandmarks[11][0]) / 2 > (landmarkResult.viewportLandmarks[24][0] + landmarkResult.viewportLandmarks[23][0]) / 2)
        {
            PronatedRight = true;
        }
        else
        {
            PronatedRight = false;
        }

        Debug.Log("PronatedRight: "+PronatedRight);

        if (PronatedRight == true)
        {
            float d1 = landmarkResult.viewportLandmarks[12][0] - PrevShoulderLX;
            float d2 = landmarkResult.viewportLandmarks[24][0] - PrevHipLX;
            float d3 = landmarkResult.viewportLandmarks[14][0] - PrevElbowX;

            float delta = Math.Abs((d1 + d2) * 100);
            Debug.Log("delta" + delta);
            float TorsoSlope = (landmarkResult.viewportLandmarks[24][1] - landmarkResult.viewportLandmarks[12][1]) / (landmarkResult.viewportLandmarks[24][0] - landmarkResult.viewportLandmarks[12][0]);
            Debug.Log("TorsoSlope" + TorsoSlope);

            StillCount = (Math.Abs(delta) < 2 && Math.Abs(TorsoSlope * 10) < 2.1) ? (StillCount + 1) : (StillCount = 0);

            if (StillCount > 15)
            {
                StillFlag = true;
                StandStill.text = "Start exercise";
                StandStill.gameObject.SetActive(true);
                StillFrame = Frame;
            }
            else
            {
                StillFlag = false;
                StandStill.text = "please stand still";
                StandStill.gameObject.SetActive(true);
            }
            PrevShoulderLX = landmarkResult.viewportLandmarks[12][0];
            PrevHipLX = landmarkResult.viewportLandmarks[24][0];
            PrevElbowX = landmarkResult.viewportLandmarks[14][0];

        }
        else 
        {
            float d1 = landmarkResult.viewportLandmarks[11][0] - PrevShoulderLX;
            float d2 = landmarkResult.viewportLandmarks[23][0] - PrevHipLX;
            float d3 = landmarkResult.viewportLandmarks[13][0] - PrevElbowX;

            float delta = Math.Abs((d1 + d2) * 100);
            Debug.Log("delta" + delta);
            float TorsoSlope = (landmarkResult.viewportLandmarks[23][1] - landmarkResult.viewportLandmarks[11][1]) / (landmarkResult.viewportLandmarks[23][0] - landmarkResult.viewportLandmarks[11][0]);
            Debug.Log("TorsoSlope" + TorsoSlope);

            StillCount = (Math.Abs(delta) < 2 && Math.Abs(TorsoSlope * 10) < 2.1) ? (StillCount + 1) : (StillCount = 0);

            if (StillCount > 15)
            {
                StillFlag = true;
                StandStill.text = "Start exercise";
                StandStill.gameObject.SetActive(true);
                StillFrame = Frame;
            }
            else
            {
                StillFlag = false;
                StandStill.text = "please stand still";
                StandStill.gameObject.SetActive(true);
            }
            PrevShoulderLX = landmarkResult.viewportLandmarks[11][0];
            PrevHipLX = landmarkResult.viewportLandmarks[23][0];
            PrevElbowX = landmarkResult.viewportLandmarks[13][0];
        }
    }

    private float CheckBridge()
    {
        if (PronatedRight == true)
        {
            var A = landmarkResult.viewportLandmarks[12];
            var B = landmarkResult.viewportLandmarks[24];
            var C = landmarkResult.viewportLandmarks[26];

            float BridgeAngleRight = Vector2.Angle(A - B, C - B);
            Debug.Log("BridgeAngleRight" + BridgeAngleRight);
            BridgeCount = (BridgeAngleRight > 140) ? (BridgeCount + 1) : (BridgeCount = 0);
            RaiseUpCount = (147 > BridgeAngleRight && BridgeAngleRight > 126) ? (RaiseUpCount + 1) : (RaiseUpCount = 0);
        }
        else 
        {
            var D = landmarkResult.viewportLandmarks[11];
            var E = landmarkResult.viewportLandmarks[23];
            var F = landmarkResult.viewportLandmarks[25];

            float BridgeAngleLeft = Vector2.Angle(D - E, F - E);
            Debug.Log("BridgeAngleLeft" + BridgeAngleLeft);
            BridgeCount = (BridgeAngleLeft > 140) ? (BridgeCount + 1) : (BridgeCount = 0);
            RaiseUpCount = (147 > BridgeAngleLeft && BridgeAngleLeft > 126) ? (RaiseUpCount + 1) : (RaiseUpCount = 0);
        }
        Debug.Log("RaiseUpCount: "+RaiseUpCount);
        if (RaiseUpCount > 20)
        {
            Raise_Up.text = "Raise Up";
            Raise_Up.gameObject.SetActive(true);
        }
        else
        {
            Raise_Up.gameObject.SetActive(false);
        }

        if (BridgeCount > 10)
        {
            BridgeFlag = true;
            Exercise.text = "Bridge";
            Exercise.gameObject.SetActive(true);
            sliderCount = sliderCount+1f;
        }
        else
        {
            BridgeFlag = false;
            Exercise.gameObject.SetActive(false);
        }

        if (prevBridgeFlag != BridgeFlag)
        {
            BridgeCounter += 1;
            sliderCount = 0;
            Counter.text = (BridgeCounter / 2).ToString();

            if (BridgeCounter > 0 && (BridgeCounter % 2 == 0))
            {
                CounterAudio.Play();
            }

            if (BridgeCounter == 20)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        sliderValue = (float)(sliderCount/70f);
        slider.value = sliderValue; 

        prevBridgeFlag = BridgeFlag;

        return BridgeCounter;
    }

    private void CheckMovementLeft()
    {
        float d1y = landmarkResult.viewportLandmarks[23][1] - PrevHipLY;
        //float d2y = landmarkResult.viewportLandmarks[24][1] - PrevHipRY;

        float delta = Math.Abs((d1y) * 100);

        float TorsoSlope = (landmarkResult.viewportLandmarks[23][1] - landmarkResult.viewportLandmarks[11][1]) / (landmarkResult.viewportLandmarks[23][0] - landmarkResult.viewportLandmarks[11][0]);
        Debug.Log("TorsoSlope" + TorsoSlope);

        MoveCount = (Math.Abs(delta) > 2 && Math.Abs(TorsoSlope * 10) > 1.5) ? (MoveCount + 1) : (MoveCount = 0);

        if (MoveCount > 5)
        {
            StillFlag = false;
            //StandStill.gameObject.SetActive(true);
        }
        else
        {
            StillFlag = true;
            //StandStill.gameObject.SetActive(false);
        }

        PrevHipLY = landmarkResult.viewportLandmarks[23][1];
        PrevHipRY = landmarkResult.viewportLandmarks[24][1];
    }
}



