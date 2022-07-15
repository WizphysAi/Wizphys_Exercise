using System.Threading;
using Cysharp.Threading.Tasks;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// BlazePose form MediaPipe
/// https://github.com/google/mediapipe
/// https://viz.mediapipe.dev/demo/pose_tracking
/// </summary>
[RequireComponent(typeof(WebCamInput))]
public sealed class BlazePosePronatedSR : MonoBehaviour
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
    //public Text textElementError1;
    [SerializeField] Text pb_Left_Right;
    [SerializeField] Text neck_Up;
    //public Text textElementError4;
    [SerializeField] Text Counter;


    [Header("Audio Souce")]
    [SerializeField]
    AudioClip pronated_Audio;
    //[SerializeField]
    //AudioClip neckUp_Audio;
    //[SerializeField]
    //AudioClip pbLeft_Audio;
    //[SerializeField]
    //AudioClip pbRight_audio;

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

    //Check StandStill varibales
    int Frame;
    int StillFrame;
    int StillCount;
    bool StillFlag = false;
    float NormalizingFactor;
    float PrevShoulderLX = 0;
    float PrevShoulderRX = 0;
    float PrevHipLX = 0;
    float PrevHipRX = 0;
    float PrevKneeLX = 0;
    float PrevKneeRX = 0;


    //Check Movement Variables
    int MoveCount = 0;
    float PrevHipLY = 0;
    float PrevHipRY = 0;
    float PrevKneeLY = 0;
    float PrevKneeRY = 0;

    //SS variables
    float StartingLeftWristY;
    float StartingRightWristY;
    float StartingLeftWristX;
    float StartingRightWristX;
    public bool CheckSSRightFlag = false;
    public bool CheckSSLeftFlag = false;
    int CheckSSRightCount = 0;
    int CheckSSLeftCount = 0;
    int CheckSSRightSideCount = 0;
    int CheckSSLeftSideCount = 0;
    int CheckSSRightFrontCount = 0;
    int CheckSSLeftFrontCount = 0;
    int CheckElbowBendLeftCount = 0;
    int CheckElbowBendRightCount = 0;
    int CheckSideBendLeftCount = 0;
    int CheckTorsoTiltCount = 0;
    public bool TorsoTiltFlag = false;
    float StartingLeftShoulderY = 0;
    float StartingRightShoulderY = 0;
    int LeftShoulderCount = 0;
    int RightShoulderCount = 0;
    float StartingY;

    float StartingSideAngleRight = 0;
    float StartingSideAngleLeft = 0;
    int SideAngleRightCount = 0;
    int SideAngleLeftCount = 0;
    float PrevElbowX = 0;

    //PSR variables
    float StartingNeck = 0;
    int CheckPSRRightCount;
    int PSRLeftCount;
    int CheckPBRightCount;
    int CheckPBLeftCount;
    int CheckNeckPronatedCount;
    int PronatedCounter = 0;

    bool prevPronatedLeftFlag = false;
    bool PronatedLeftFlag = false;
    bool PBLeftFlag = false;

    bool PronatedRight = false;

    public Slider slider;
    float sliderCount = 0;
    float sliderValue = 0;


    private UniTask<bool> task;
    private CancellationToken cancellationToken;

    private void Start()
    {
        slider.value = 0;
        Exercise.gameObject.SetActive(false);
        //textElementError1.gameObject.SetActive(false);
        pb_Left_Right.gameObject.SetActive(false);
        neck_Up.gameObject.SetActive(false);
        //textElementError4.gameObject.SetActive(false);

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
        //Debug.Log("update called");
        //drawer.DrawPoseResult(poseResult);
        Exercise.gameObject.SetActive(false);
        //textElementError1.gameObject.SetActive(false);
        pb_Left_Right.gameObject.SetActive(false);
        neck_Up.gameObject.SetActive(false);
        //textElementError4.gameObject.SetActive(false);F

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);

        if (SystemInfo.supportsGyroscope)
        {
            //Debug.Log("GyroToUnity(Input.gyro.attitude): " + GyroToUnity(Input.gyro.attitude));
            //Debug.Log("GyroToUnity(Input.gyro.attitude)[0]: " + GyroToUnity(Input.gyro.attitude)[0]);
            //Debug.Log("Input.gyro.attitude.eulerAngles: "+ Input.gyro.attitude.eulerAngles);
        }

        if (landmarkResult != null && landmarkResult.score > 0.2f)
        {
            //drawer.DrawCropMatrix(pose.CropMatrix);
            //Debug.Log("canvas.planeDistance: " + canvas.planeDistance);  

            float TorsoSlope = (landmarkResult.viewportLandmarks[23][1] - landmarkResult.viewportLandmarks[11][1]) / (landmarkResult.viewportLandmarks[23][0] - landmarkResult.viewportLandmarks[11][0]);
            //Debug.Log("TorsoSlope" + TorsoSlope*10);

            if (StillFlag == true)
            {
                CheckMovementLeft();

                if (StillFlag == true)
                {
                    greenSignal.gameObject.SetActive(true);
                    redSignal.gameObject.SetActive(false);

                    // PSR Right
                    //CheckPSRRight();
                    //CheckPBRight();

                    // PSR Left
                    if (PronatedRight == true)
                    {
                        CheckPBRight();
                        // CheckPSRRight();   
                    }
                    else
                    {
                        CheckPBLeft();
                        // CheckPSRLeft();
                    }

                    // CheckPSRLeft();
                    // CheckPBLeft();
                    // CheckNeckPronated();
                }
            }
            else
            {
                //StandStill.gameObject.SetActive(false);
                Exercise.gameObject.SetActive(false);
                //textElementError1.gameObject.SetActive(false);
                pb_Left_Right.gameObject.SetActive(false);
                neck_Up.gameObject.SetActive(false);
                //textElementError4.gameObject.SetActive(false);

                greenSignal.gameObject.SetActive(false);
                redSignal.gameObject.SetActive(true);

                //CheckStandStill();
                //CheckStandStillPronatedRight();
                //CheckStandStillPronatedLeft();
                CheckStillPronated();
            }

            //Debug.Log("landmarkResult.viewportLandmarks[11] X: " + landmarkResult.viewportLandmarks[11][0]);

            drawer.DrawLandmarkResult(landmarkResult, visibilityThreshold, canvas.planeDistance);

            if (options.landmark.useWorldLandmarks)
            {
                drawer.DrawWorldLandmarks(landmarkResult, visibilityThreshold);
            }

            //Frame += 1;
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

        if (StillFlag == true && !audioSource.isPlaying)
        {
            //audioSource.loop = true;
            audioSource.clip = pronated_Audio;
            audioSource.Play();
        }

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


    private void CheckStandStill()
    {
        //Debug.Log("CheckStandStill called");
        float d1 = landmarkResult.viewportLandmarks[11][0] - PrevShoulderLX;
        float d2 = landmarkResult.viewportLandmarks[12][0] - PrevShoulderRX;
        float d3 = landmarkResult.viewportLandmarks[23][0] - PrevHipLX;
        float d4 = landmarkResult.viewportLandmarks[24][0] - PrevHipRX;
        float d5 = landmarkResult.viewportLandmarks[25][0] - PrevKneeLX;
        float d6 = landmarkResult.viewportLandmarks[26][0] - PrevKneeRX;

        float d1y = landmarkResult.viewportLandmarks[11][1];
        float d2y = landmarkResult.viewportLandmarks[12][1];
        float d3y = landmarkResult.viewportLandmarks[23][1];
        float d4y = landmarkResult.viewportLandmarks[24][1];

        float delta = Math.Abs((d1 + d2 + d3 + d4 + d5 + d6) * 100);

        //Debug.Log("delta: "+ delta);

        StillCount = (Math.Abs(delta) < 1.5) ? (StillCount + 1) : (StillCount = 0);

        if (StillCount > 15)
        {
            StillFlag = true;
            StandStill.text = "Start exercise";
            StandStill.gameObject.SetActive(true);
            StillFrame = Frame;

            NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
            //Debug.Log("NormalizingFactor" + NormalizingFactor);
            //StillFrame = frame
        }
        else
        {
            StillFlag = false;
            StandStill.text = "please stand still";
            StandStill.gameObject.SetActive(true);
        }

        PrevShoulderLX = landmarkResult.viewportLandmarks[11][0];
        PrevShoulderRX = landmarkResult.viewportLandmarks[12][0];
        PrevHipLX = landmarkResult.viewportLandmarks[23][0];
        PrevHipRX = landmarkResult.viewportLandmarks[24][0];
        PrevKneeLX = landmarkResult.viewportLandmarks[25][0];
        PrevKneeRX = landmarkResult.viewportLandmarks[26][0];
    }

    private void CheckStandStillPronatedRight()
    {
        float d1 = landmarkResult.viewportLandmarks[12][0] - PrevShoulderRX;
        float d2 = landmarkResult.viewportLandmarks[24][0] - PrevHipRX;
        //float d1y = landmarkResult.viewportLandmarks[12][1];
        //float d2y = landmarkResult.viewportLandmarks[24][1];

        float delta = Math.Abs((d1 + d2) * 100);
        //Debug.Log("delta: "+ delta);

        StillCount = (Math.Abs(delta) < 1.5) ? (StillCount + 1) : (StillCount = 0);

        if (StillCount > 20)
        {
            StillFlag = true;
            StandStill.text = "Start exercise";
            StandStill.gameObject.SetActive(true);
            StillFrame = Frame;
            //NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
            //Debug.Log("NormalizingFactor" + NormalizingFactor);
            //StillFrame = frame
        }
        else
        {
            StillFlag = false;
            StandStill.text = "please stand still";
            StandStill.gameObject.SetActive(true);
        }
        PrevShoulderRX = landmarkResult.viewportLandmarks[12][0];
        PrevHipRX = landmarkResult.viewportLandmarks[24][0];
    }

    private void CheckStandStillPronatedLeft()
    {
        float d1 = landmarkResult.viewportLandmarks[11][0] - PrevShoulderLX;
        float d2 = landmarkResult.viewportLandmarks[23][0] - PrevHipLX;
        float d3 = landmarkResult.viewportLandmarks[13][0] - PrevElbowX;
        //float d1y = landmarkResult.viewportLandmarks[12][1];
        //float d2y = landmarkResult.viewportLandmarks[24][1];

        float delta = Math.Abs((d1 + d2) * 100);

        float TorsoSlope = (landmarkResult.viewportLandmarks[23][1] - landmarkResult.viewportLandmarks[11][1]) / (landmarkResult.viewportLandmarks[23][0] - landmarkResult.viewportLandmarks[11][0]);
        Debug.Log("TorsoSlope" + TorsoSlope);

        StillCount = (Math.Abs(delta) < 2 && Math.Abs(TorsoSlope*10) < 1) ? (StillCount + 1) : (StillCount = 0);

        if (StillCount > 15)
        {
            StillFlag = true;
            StandStill.text = "Start exercise";
            StandStill.gameObject.SetActive(true);
            StillFrame = Frame;
            //NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
            //Debug.Log("NormalizingFactor" + NormalizingFactor);
            //StillFrame = frame
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

    private void CheckMovementLeft()
    {
        float d1y = landmarkResult.viewportLandmarks[23][1] - PrevHipLY;
        //float d2y = landmarkResult.viewportLandmarks[24][1] - PrevHipRY;

        float delta = Math.Abs((d1y) * 100);

        float TorsoSlope = (landmarkResult.viewportLandmarks[23][1] - landmarkResult.viewportLandmarks[11][1]) / (landmarkResult.viewportLandmarks[23][0] - landmarkResult.viewportLandmarks[11][0]);
        Debug.Log("TorsoSlope" + TorsoSlope);

        //Debug.Log("move delta: " + delta);
        //Debug.Log("MoveCount: " + MoveCount);

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


    private float CheckPSRRight()
    {
        Debug.Log("PSRRightcalled");
        var A = landmarkResult.viewportLandmarks[12];
        var B = landmarkResult.viewportLandmarks[14];
        var C = landmarkResult.viewportLandmarks[24];

        float PSRAngle = Vector2.Angle(A - B, C - B);
        //Debug.Log("PSRAngle: " + PSRAngle);

        CheckPSRRightCount = (PSRAngle < 130) ? (CheckPSRRightCount + 1) : (CheckPSRRightCount = 0);

        if (CheckPSRRightCount > 15)
        {
            Exercise.text = "PSR_Right";
            Exercise.gameObject.SetActive(true);
        }
        else
        {
            Exercise.gameObject.SetActive(false);
        }

        return PSRAngle;
    }

    private float CheckPSRLeft()
    {
        Debug.Log("PSRLeftcalled");
        var A = landmarkResult.viewportLandmarks[11];
        var B = landmarkResult.viewportLandmarks[13];
        var C = landmarkResult.viewportLandmarks[23];

        float PSRAngle = Vector2.Angle(A - B, C - B);
        Debug.Log("PSRAngle: " + PSRAngle);

        CheckPSRRightCount = (PSRAngle < 165) ? (CheckPSRRightCount + 1) : (CheckPSRRightCount = 0);

        if (CheckPSRRightCount > 15)
        {
            Exercise.text = "PSR_Left";
            Exercise.gameObject.SetActive(true);
        }
        else
        {
            Exercise.gameObject.SetActive(false);
        }

        return PSRAngle;
    }

    private float CheckPSRLeftold()
    {
        Debug.Log("PSRLeftcalled");
        var A = landmarkResult.viewportLandmarks[13];
        var B = landmarkResult.viewportLandmarks[11];
        var C = landmarkResult.viewportLandmarks[23];

        if (Frame < (StillFrame + 5))
        {
            StartingY = landmarkResult.viewportLandmarks[15][1];
        }

        float y = landmarkResult.viewportLandmarks[15][1];

        float deltaY = Math.Abs((StartingY - y)*100);
        //Debug.Log("deltaY: " + deltaY);
        float PSRAngle = Vector2.Angle(A - B, C - B);
        //Debug.Log("PSRAngle: " + PSRAngle);

        PSRLeftCount = ( deltaY > 12) ? (PSRLeftCount + 1) : (PSRLeftCount = 0);

        //Debug.Log("PSRLeftCount");
        if (PSRLeftCount > 10)
        {
            PronatedLeftFlag = true;
            Exercise.text = "PSR_Left";
            Exercise.gameObject.SetActive(true);
        }
        else
        {
            PronatedLeftFlag = false;
            Exercise.gameObject.SetActive(false);
        }

        if (prevPronatedLeftFlag != PronatedLeftFlag)
        {
            PronatedCounter += 1;
            Counter.text = (PronatedCounter / 2).ToString();
            //Debug.Log("SSCounter" + SSCounter);

            if (PronatedCounter > 0 && (PronatedCounter % 2 == 0))
            {
                CounterAudio.Play();
            }

            if (PronatedCounter == 20)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
        prevPronatedLeftFlag = PronatedLeftFlag;

        return PSRAngle;
    }

    private float CheckPBRight()
    {
        if (Frame < (StillFrame + 5))
        {
            StartingRightShoulderY = landmarkResult.viewportLandmarks[12][1];
        }

        float DeltaRightShoulder = ((StartingRightShoulderY - landmarkResult.viewportLandmarks[12][1]) / (landmarkResult.viewportLandmarks[12][0] - landmarkResult.viewportLandmarks[24][0])) * 100;
        Debug.Log("DeltaRightShoulder" + DeltaRightShoulder);

        CheckPBRightCount = (DeltaRightShoulder > 5) ? (CheckPBRightCount + 1) : (CheckPBRightCount = 0);

        if (CheckPBRightCount > 12)
        {
            PronatedLeftFlag = true;
            pb_Left_Right.text = "Pronated Bracing";
            pb_Left_Right.gameObject.SetActive(true);
            sliderCount = sliderCount+1f;
            
        }
        else
        {
            PronatedLeftFlag = false;
            pb_Left_Right.gameObject.SetActive(false);
        }

        if (prevPronatedLeftFlag != PronatedLeftFlag)
        {
            PronatedCounter += 1;
            sliderCount = 0;
            Counter.text = (PronatedCounter / 2).ToString();
            //Debug.Log("SSCounter" + SSCounter);

            if (PronatedCounter > 0 && (PronatedCounter % 2 == 0))
            {
                CounterAudio.Play();
            }

            if (PronatedCounter == 20)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        sliderValue = (float)(sliderCount/70f);
        slider.value = sliderValue; 

        prevPronatedLeftFlag = PronatedLeftFlag;

        return DeltaRightShoulder;
    }

    private float CheckPBLeft()
    {
        if (Frame < (StillFrame + 5))
        {
            StartingLeftShoulderY = landmarkResult.viewportLandmarks[11][1];
        }

        // Debug.Log("landmarkResult.viewportLandmarks[15][1]: " + landmarkResult.viewportLandmarks[15][1]);
        // Debug.Log("landmarkResult.viewportLandmarks[16][1]: " + landmarkResult.viewportLandmarks[16][1]);

        //float DeltaLeftShoulder = ((StartingLeftShoulderY - landmarkResult.viewportLandmarks[11][1]) / (landmarkResult.viewportLandmarks[11][0] - landmarkResult.viewportLandmarks[23][0])) * 100;
        float DeltaLeftShoulder = Math.Abs(((StartingLeftShoulderY - landmarkResult.viewportLandmarks[11][1]) / (landmarkResult.viewportLandmarks[11][0] - landmarkResult.viewportLandmarks[23][0])) * 100);
        Debug.Log("DeltaLeftShoulder: " + DeltaLeftShoulder);

        CheckPBLeftCount = (DeltaLeftShoulder > 5) ? (CheckPBLeftCount + 1) : (CheckPBLeftCount = 0);

        if (CheckPBLeftCount > 12)
        {
            //Debug.Log("Pronated Bracing Left");
            PronatedLeftFlag = true;
            pb_Left_Right.text = "Pronated Bracing";
            pb_Left_Right.gameObject.SetActive(true);
            sliderCount = sliderCount+1f;
        }
        else
        {
            PronatedLeftFlag = false;
            pb_Left_Right.gameObject.SetActive(false);
        }

        if (prevPronatedLeftFlag != PronatedLeftFlag)
        {
            PronatedCounter += 1;
            sliderCount = 0;
            Counter.text = (PronatedCounter / 2).ToString();
            //Debug.Log("SSCounter" + SSCounter);

            if (PronatedCounter > 0 && (PronatedCounter % 2 == 0))
            {
                CounterAudio.Play();
            }

            if (PronatedCounter == 20)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        sliderValue = (float)(sliderCount/70f);
        slider.value = sliderValue; 

        prevPronatedLeftFlag = PronatedLeftFlag;
        
        return DeltaLeftShoulder;
    }

    private float CheckNeckPronated()
    {
        if (Frame < (StillFrame + 5))
        {
            StartingNeck = landmarkResult.viewportLandmarks[0][1];
        }

        //float DeltaNeckDist = ((StartingNeck - landmarkResult.viewportLandmarks[0][1]) / (landmarkResult.viewportLandmarks[12][0] - landmarkResult.viewportLandmarks[24][0])) * 100;
        float DeltaNeckDist = Math.Abs(((StartingNeck - landmarkResult.viewportLandmarks[0][1]) / (landmarkResult.viewportLandmarks[12][0] - landmarkResult.viewportLandmarks[24][0])) * 100);
        Debug.Log("DeltaNeckDist" + DeltaNeckDist);
        //Debug.Log("DeltaNeckDist" + DeltaNeckDist);

        CheckNeckPronatedCount = (DeltaNeckDist > 8) ? (CheckNeckPronatedCount + 1) : (CheckNeckPronatedCount = 0);

        if (CheckNeckPronatedCount > 10)
        {
            neck_Up.text = "Neck up";
            neck_Up.gameObject.SetActive(true);
        }
        else
        {
            neck_Up.gameObject.SetActive(false);
        }
        return DeltaNeckDist;
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
            //float d1y = landmarkResult.viewportLandmarks[12][1];
            //float d2y = landmarkResult.viewportLandmarks[24][1];

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
                //NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
                //Debug.Log("NormalizingFactor" + NormalizingFactor);
                //StillFrame = frame
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
            //float d1y = landmarkResult.viewportLandmarks[12][1];
            //float d2y = landmarkResult.viewportLandmarks[24][1];

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
                //NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
                //Debug.Log("NormalizingFactor" + NormalizingFactor);
                //StillFrame = frame
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
}



