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
public sealed class BlazePoseRhomboids : MonoBehaviour
{
    [SerializeField]
    private BlazePose.Options options = default;
    [SerializeField]
    private RectTransform containerView = null;
    [SerializeField]
    private RawImage debugView = null;
    //[SerializeField]
    //private RawImage segmentationView = null;
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

    public Image loadingAnim;

    [Header("Text")]
    [SerializeField] Text standStill;
    [SerializeField] Text startExercise;
    [SerializeField] Text Bracing;
    [SerializeField] Text NeckTilt;
    [SerializeField] Text NeckRotaion;
    [SerializeField] Text Shrugging;
    [SerializeField] Text Counter;

    [SerializeField] Text WristOffset;
    [SerializeField] Text BackBend;

    [Header("Audio Souce")]
    [SerializeField] AudioClip Bracing_Audio;
    [SerializeField] AudioClip Shrugging_Audio;
    [SerializeField] AudioClip NeckTilt_Audio;
    [SerializeField] AudioClip NeckRotaion_Audio;
    [SerializeField] AudioClip Counter_Audio;

    public AudioSource BracingAudio;
    public AudioSource ShruggingAudio;
    public AudioSource NeckTiltAudio;
    public AudioSource NeckRotationAudio;
    public AudioSource CounterAudio;
    //private AudioSource source;

    [Header("Error Object")]
    [SerializeField] GameObject necktiltObject;
    [SerializeField] GameObject NeckRotaionObject;
    [SerializeField] GameObject r_shoulderShrugging;
    [SerializeField] GameObject l_shoulderShrugging;

    //Check StandStill
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
    float StartingShoulderDist;
    int CheckBracingCount = 0;
    bool CheckBracingFlag = false;
    bool PrevCheckBracingFlag = false;
    float StartingNeckDist;
    int CheckNeckDistCount = 0;
    int CheckNeckRotationCount = 0;
    float StartinShoulderPosition;
    int CheckShruggingCount = 0;
    int BracingCounter = 0;
    bool ShruggingFlag = false;
    bool NeckTiltFlag = false;
    bool NeckRotaionFlag = false;
    bool[] CheckPriority = new bool[4];
    int ErrorAudio = 0;
    int CoolDownCount = 0;

    [SerializeField] Image greenSignal;
    [SerializeField] Image redSignal;

    //Check Movement Variables
    int MoveCount = 0;
    float PrevHipLY = 0;
    float PrevHipRY = 0;
    float PrevKneeLY = 0;
    float PrevKneeRY = 0;

    //counter
    int ShruggingCounter = 0;
    int NeckTiltCounter = 0;
    int NeckRotationCounter = 0;

    //Rhomboids
    int wristUpCount = 0;
    int wristDownCount = 0;
    int RhomboidsCount = 0;
    float StartingTorsoDist = 0;

    public Slider slider;
    float sliderCount = 0;
    float sliderValue = 0;


    [SerializeField] private UniTask<bool> task;
    [SerializeField] private CancellationToken cancellationToken;

    [SerializeField] AudioSource backgroundMusic;
    [SerializeField] float volumeBG = 0.3f;

    [SerializeField] float gyrovalues_new;
    [SerializeField] GameObject gyro;
    private Gyro_Manager  GyroScript;
    [SerializeField] GameObject gyroPanel;

    private void Start()
    {
        slider.value = 0;
        loadingAnim.gameObject.SetActive(false);

        startExercise.gameObject.SetActive(false);
        Bracing.gameObject.SetActive(false);
        NeckTilt.gameObject.SetActive(false);
        NeckRotaion.gameObject.SetActive(false);
        Shrugging.gameObject.SetActive(false);
        WristOffset.gameObject.SetActive(false);
        BackBend.gameObject.SetActive(false);
        necktiltObject.gameObject.SetActive(false);
        NeckRotaionObject.gameObject.SetActive(false);
        r_shoulderShrugging.gameObject.SetActive(false);
        l_shoulderShrugging.gameObject.SetActive(false);

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);

        gyroPanel.gameObject.SetActive(false);

        //source.PlayOneShot(bracingstart_Audio, 1f);


        pose = new BlazePose(options);
        drawer = new BlazePoseDrawer(Camera.main, gameObject.layer, containerView);
        cancellationToken = this.GetCancellationTokenOnDestroy();
        GetComponent<WebCamInput>().OnTextureUpdate.AddListener(OnTextureUpdate);

        StillFlag = false;

        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        BracingAudio.clip = Bracing_Audio;
        ShruggingAudio.clip = Shrugging_Audio;
        NeckTiltAudio.clip = NeckTilt_Audio;
        NeckRotationAudio.clip = NeckRotaion_Audio;
        CounterAudio.clip = Counter_Audio;

        GyroScript = gyro.GetComponent<Gyro_Manager>();

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
        startExercise.gameObject.SetActive(false);
        Bracing.gameObject.SetActive(false);
        NeckTilt.gameObject.SetActive(false);
        necktiltObject.gameObject.SetActive(false);
        Shrugging.gameObject.SetActive(false);
        WristOffset.gameObject.SetActive(false);
        BackBend.gameObject.SetActive(false);

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);

        gyrovalues_new = GyroScript.gyrovalues*10;
        // gyrovalues_new = 2.0f;
        if (3 > gyrovalues_new && gyrovalues_new > 1.8)
        {
            gyroPanel.gameObject.SetActive(false);

            if (landmarkResult != null && landmarkResult.score > 0.2f)
            {
                if (StillFlag == true)
                {
                    CheckMovement();
                    //Debug.Log("StillFlag " + StillFlag);
                    if (StillFlag == true)
                    {
                        greenSignal.gameObject.SetActive(true);
                        redSignal.gameObject.SetActive(false);

                        CheckBracing();
                        CheckWristRhomboids();
                        CheckRhomboids();
                        CheckWristsCenter();
                        CheckBackBend();

                    }
                    else
                    {
                        greenSignal.gameObject.SetActive(false);
                        redSignal.gameObject.SetActive(true);

                        startExercise.gameObject.SetActive(false);
                        Bracing.gameObject.SetActive(false);
                        NeckTilt.gameObject.SetActive(false);
                        necktiltObject.gameObject.SetActive(false);
                        Shrugging.gameObject.SetActive(false);
                        CheckStandStill();
                    }
                }
                else
                {
                    greenSignal.gameObject.SetActive(false);
                    redSignal.gameObject.SetActive(true);

                    startExercise.gameObject.SetActive(false);
                    Bracing.gameObject.SetActive(false);
                    NeckTilt.gameObject.SetActive(false);
                    necktiltObject.gameObject.SetActive(false);
                    Shrugging.gameObject.SetActive(false);
                    CheckStandStill();
                }

                drawer.DrawLandmarkResult(landmarkResult, visibilityThreshold, canvas.planeDistance);
                if (options.landmark.useWorldLandmarks)
                {
                    drawer.DrawWorldLandmarks(landmarkResult, visibilityThreshold);
                }

                Frame = Frame + 1;
            }
            else
            {
                StillFlag = false;
                greenSignal.gameObject.SetActive(false);
                redSignal.gameObject.SetActive(true);
            }
        }
        else
        {
            gyroPanel.gameObject.SetActive(true);
            StillFlag = false;
            greenSignal.gameObject.SetActive(false);
            redSignal.gameObject.SetActive(true);
        }
            

        if (StillFlag == true && !BracingAudio.isPlaying)
        {
            BracingAudio.Play();
        }


        //Audio Triggers 
        if (StillFlag == true)
        {
            if ((ShruggingFlag || NeckTiltFlag || NeckRotaionFlag))
            {
                if (CoolDownCount == 0)
                {

                    BracingAudio.mute = true;
                    CheckPriority[0] = ShruggingFlag;
                    CheckPriority[1] = NeckTiltFlag;
                    CheckPriority[2] = NeckRotaionFlag;
                    CheckPriority[3] = true;

                    for (int i = 0; i < CheckPriority.Length; i++)
                    {
                        if (CheckPriority[i] == true)
                        {
                            ErrorAudio = i;
                            break;
                        }

                    }

                    if (!ShruggingAudio.isPlaying && !NeckTiltAudio.isPlaying && !NeckRotationAudio.isPlaying)
                    {
                        switch (ErrorAudio + 1)
                        {
                            case 1:

                                if(ShruggingCounter < 3)  //counter+1
                                {
                                    ShruggingAudio.Play();
                                    Shrugging.gameObject.SetActive(true);
                                    r_shoulderShrugging.gameObject.SetActive(true);
                                    l_shoulderShrugging.gameObject.SetActive(true);
                                }

                                if (ShruggingAudio.isPlaying) //lower master audio sound
                                {
                                    backgroundMusic.volume = volumeBG;
                                }
                                else
                                    backgroundMusic.volume = 1f;
                                

                                CoolDownCount = CoolDownCount + 30; //30 = frames
                                break;
                            case 2:
                                
                                if (NeckTiltCounter < 3)  //counter+1
                                {
                                    NeckTiltAudio.Play();
                                    NeckTilt.gameObject.SetActive(true);
                                    necktiltObject.gameObject.SetActive(true);
                                }
                                if (NeckTiltAudio.isPlaying) //lower master audio sound
                                {
                                    backgroundMusic.volume = volumeBG;
                                }
                                else
                                    backgroundMusic.volume = 1f;

                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 3:
                                if (NeckRotationCounter < 3)  //counter+1
                                {
                                    NeckRotationAudio.Play();
                                    NeckRotaion.gameObject.SetActive(true);
                                    NeckRotaionObject.gameObject.SetActive(true);
                                }
                                if (NeckRotationAudio.isPlaying) //lower master audio sound
                                {
                                    backgroundMusic.volume = volumeBG;
                                }
                                else
                                    backgroundMusic.volume = 1f;

                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 4:
                                //Debug.Log("NO error called");
                                break;
                        }
                    }
                }
            }
            else
            {

                if (!ShruggingAudio.isPlaying && !NeckTiltAudio.isPlaying && !NeckRotationAudio.isPlaying)
                {
                    //Debug.Log("BracingAudio.mute = false");
                    BracingAudio.mute = false;
                }

                if (CoolDownCount > 0)
                {
                    CoolDownCount = CoolDownCount - 1;
                }

            }


            // AudioPlay Counter
            //ShruggingCounter
            //NeckTiltCounter
            //NeckRotationCounter
            //Debug.Log("CoolDownCount:" + CoolDownCount);

        }


    }


    private Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    private void Invoke(Texture texture)
    {
        landmarkResult = pose.Invoke(texture);
        poseResult = pose.PoseResult;
        if (pose.LandmarkInputTexture != null)
        {
            debugView.texture = pose.LandmarkInputTexture;
        }
        //if (landmarkResult != null && landmarkResult.SegmentationTexture != null)
        //{
        //    segmentationView.texture = landmarkResult.SegmentationTexture;
        //}
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
        if (Math.Abs(delta) < 1.5)
        {
            StillCount += 1;
        }
        else
        {
            StillCount = 0;
        }
        if (StillCount > 15)
        {
            StillFlag = true;
            standStill.text = "Start exercise";
            standStill.gameObject.SetActive(true);
            StillFrame = Frame;
            NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
        }
        else
        {
            StillFlag = false;
            standStill.text = "Please stand still";
            standStill.gameObject.SetActive(true);
        }
        PrevShoulderLX = landmarkResult.viewportLandmarks[11][0];
        PrevShoulderRX = landmarkResult.viewportLandmarks[12][0];
        PrevHipLX = landmarkResult.viewportLandmarks[23][0];
        PrevHipRX = landmarkResult.viewportLandmarks[24][0];
        PrevKneeLX = landmarkResult.viewportLandmarks[25][0];
        PrevKneeRX = landmarkResult.viewportLandmarks[26][0];
    }

    private void CheckMovement()
    {
        float d1y = landmarkResult.viewportLandmarks[23][1] - PrevHipLY;
        float d2y = landmarkResult.viewportLandmarks[24][1] - PrevHipRY;
        float d3y = landmarkResult.viewportLandmarks[25][1] - PrevKneeLY;
        float d4y = landmarkResult.viewportLandmarks[26][1] - PrevKneeRY;
        float delta = Math.Abs((d1y + d2y + d3y + d4y) * 100);


        MoveCount = (Math.Abs(delta) > 2) ? (MoveCount + 1) : (MoveCount = 0);
        //Debug.Log("move delta: " + delta + "---" + MoveCount);
        //Debug.Log("MoveCount: " );
        if (MoveCount > 2)
        {
            //Debug.Log("StillFLag false");
            StillFlag = false;
            //textElementStandStill.gameObject.SetActive(true);
        }
        else
        {
            StillFlag = true;
            //textElementStandStill.gameObject.SetActive(false);
        }
        PrevHipLY = landmarkResult.viewportLandmarks[23][1];
        PrevHipRY = landmarkResult.viewportLandmarks[24][1];
        PrevKneeLY = landmarkResult.viewportLandmarks[25][1];
        PrevKneeRY = landmarkResult.viewportLandmarks[26][1];
    }

    private float CheckBracing()
    {
        float LeftShoulderX = (landmarkResult.viewportLandmarks[11][0]*3 + landmarkResult.viewportLandmarks[13][0]) / 4;
        float RightShoulderX = (landmarkResult.viewportLandmarks[12][0]*3 + landmarkResult.viewportLandmarks[14][0]) / 4;

        //float LeftShoulderX = (landmarkResult.viewportLandmarks[11][0]);
        //float RightShoulderX = (landmarkResult.viewportLandmarks[12][0]);

        float ShoulderDist = RightShoulderX - LeftShoulderX;
        if (Frame < (StillFrame + 3))
        {
            StartingShoulderDist = ShoulderDist;
        }
        float DeltaShoulderDist = (((StartingShoulderDist - ShoulderDist) / StartingShoulderDist) * 100f);
        Debug.Log("DeltaShoulderDist"+ DeltaShoulderDist);

        CheckBracingCount = (DeltaShoulderDist > 2.7f) ? (CheckBracingCount + 1) : (CheckBracingCount = 0);

        if (CheckBracingCount > 10)
        {
            Shrugging.text = "Bracing";
            CheckBracingFlag = true;
            Shrugging.gameObject.SetActive(true);
        }
        else
        {
            CheckBracingFlag = false;
            Shrugging.gameObject.SetActive(false);
        }

        return DeltaShoulderDist;
    }


    private float CheckWristRhomboidsOld()
    {
        //float LeftShoulderY = landmarkResult.viewportLandmarks[11][1];
        //float RightShoulderY = landmarkResult.viewportLandmarks[12][1];

        //Debug.Log("slider.value"+slider.value);

        float WristMid = ((landmarkResult.viewportLandmarks[15][1] + landmarkResult.viewportLandmarks[16][1]) / 2);
        float HipMid = ((landmarkResult.viewportLandmarks[23][1] + landmarkResult.viewportLandmarks[24][1]) / 2);
        float WristDist = (Math.Abs(landmarkResult.viewportLandmarks[16][0] - landmarkResult.viewportLandmarks[15][0]))*100;
        float WristHipDist = (WristMid - HipMid) * 100;

        //wristUpCount = (WristHipDist > 11.0f && WristDist < 45) ? (wristUpCount + 1) : (wristUpCount = 0);
        //wristDownCount = (WristHipDist < 9.8f && WristDist < 45) ? (wristDownCount + 1) : (wristDownCount = 0);

        if (WristDist < 45) 
        {
            wristUpCount = (WristHipDist > 12.75f) ? (wristUpCount + 1) : (wristUpCount = 0);
            wristDownCount = (WristHipDist < 9.5f) ? (wristDownCount + 1) : (wristDownCount = 0);

            if (wristUpCount > 15)
            {
                NeckRotaion.text = "Wrist Up " + WristHipDist.ToString("n2");
                NeckRotaion.gameObject.SetActive(true);

            }
            else if (wristDownCount > 15)
            {
                NeckRotaion.text = "Wrist Down " + WristHipDist.ToString("n2");
                NeckRotaion.gameObject.SetActive(true);
            }
            else
            {
                NeckRotaion.text = "Normal " + WristHipDist.ToString("n2");
                NeckRotaion.gameObject.SetActive(false);
            }
        }
        //Debug.Log("wristUpCount: " + wristUpCount);
        //Debug.Log("wristDownCount: " + wristDownCount);
        //Debug.Log("WristDist: "+ WristDist);
        return WristHipDist;
    }

    private float CheckWristRhomboids()
    {
        //float LeftShoulderY = landmarkResult.viewportLandmarks[11][1];
        //float RightShoulderY = landmarkResult.viewportLandmarks[12][1];

        //Debug.Log("slider.value"+slider.value);

        float WristMid = ((landmarkResult.viewportLandmarks[15][1] + landmarkResult.viewportLandmarks[16][1]) / 2);
        float ElbowMid = ((landmarkResult.viewportLandmarks[13][1] + landmarkResult.viewportLandmarks[14][1]) / 2);
        float WristDist = (Math.Abs(landmarkResult.viewportLandmarks[16][0] - landmarkResult.viewportLandmarks[15][0]))*100;
        float WristElbowDist = (((WristMid - ElbowMid)/NormalizingFactor) * 100);

        //wristUpCount = (WristHipDist > 11.0f && WristDist < 45) ? (wristUpCount + 1) : (wristUpCount = 0);
        //wristDownCount = (WristHipDist < 9.8f && WristDist < 45) ? (wristDownCount + 1) : (wristDownCount = 0);
        float WristHipDist = WristElbowDist;
        Debug.Log("WristHipDist: "+WristHipDist);
        if (WristDist < 45) 
        {
            wristUpCount = (WristHipDist > 30.0f) ? (wristUpCount + 1) : (wristUpCount = 0);
            wristDownCount = (WristHipDist < -10.0f) ? (wristDownCount + 1) : (wristDownCount = 0);

            if (wristUpCount > 15)
            {
                NeckRotaion.text = "Wrist Up ";
                NeckRotaion.gameObject.SetActive(true);

            }
            else if (wristDownCount > 15)
            {
                NeckRotaion.text = "Wrist Down ";
                NeckRotaion.gameObject.SetActive(true);
            }
            else
            {
                NeckRotaion.text = "Normal " + WristHipDist.ToString("n2");
                NeckRotaion.gameObject.SetActive(false);
            }
        }
        //Debug.Log("wristUpCount: " + wristUpCount);
        //Debug.Log("wristDownCount: " + wristDownCount);
        //Debug.Log("WristDist: "+ WristDist);
        return WristHipDist;
    }

    private float CheckWristsCenter()
    {
        float WristDist = (landmarkResult.viewportLandmarks[16][0] - landmarkResult.viewportLandmarks[15][0]) * 100;
        float WristCenter = ((landmarkResult.viewportLandmarks[16][0] + landmarkResult.viewportLandmarks[15][0]))/2;
        float HipCenter = ((landmarkResult.viewportLandmarks[24][0] + landmarkResult.viewportLandmarks[23][0]))/2;

        float offset = (WristCenter - HipCenter) * 100;

        //Debug.Log("WristCenter: " + WristCenter*100);
        //Debug.Log("HipCenter: " + HipCenter*100);
        //Debug.Log("offset: " + offset);
        //wristUpCount = (WristHipDist > 11.0f) ? (wristUpCount + 1) : (wristUpCount = 0);
        //wristDownCount = (WristHipDist < 9.8f) ? (wristDownCount + 1) : (wristDownCount = 0);

        if (WristDist > 40) {
            if (offset > 4.5)
            {
                WristOffset.text = "Right Wrist offset";
                WristOffset.gameObject.SetActive(true);
            }
            else if (offset < -4.5)
            {
                WristOffset.text = "Left Wrist offset";
                WristOffset.gameObject.SetActive(true);
            }
            else
            {
                WristOffset.gameObject.SetActive(false);
            }
        }

        return offset;
    }

    private float CheckRhomboids()
    {
        //float LeftShoulderY = landmarkResult.viewportLandmarks[11][1];
        //float RightShoulderY = landmarkResult.viewportLandmarks[12][1];
        float WristDist = ((landmarkResult.viewportLandmarks[16][0] - landmarkResult.viewportLandmarks[15][0])/NormalizingFactor) * 100;
        Debug.Log("WristDist"+WristDist);
        RhomboidsCount = (WristDist > 155) ? (RhomboidsCount + 1) : (RhomboidsCount = 0);

        if (RhomboidsCount > 20)
        {
            Bracing.text = "Rhomboids";
            Bracing.gameObject.SetActive(true);
            CheckBracingFlag = true;
            sliderCount = sliderCount+1f;
        }
        else
        {
            Bracing.gameObject.SetActive(false);
            CheckBracingFlag = false;
        }

        if (PrevCheckBracingFlag != CheckBracingFlag)
        {
            BracingCounter += 1;
            sliderCount = 0;
            Debug.Log("BracingCounter"+ BracingCounter/2);
            Counter.text = (BracingCounter / 2).ToString();

            if (BracingCounter > 0 && (BracingCounter % 2 == 0))
            {
                CounterAudio.Play();
            }

            if (BracingCounter == 20)
            {
                loadingAnim.gameObject.SetActive(true);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        Debug.Log("sliderCount: "+sliderCount);
        Debug.Log("sliderCount/70: "+sliderCount/70f);
        sliderValue = (float)(sliderCount/70f);
        Debug.Log("sliderValue: "+sliderValue);
        slider.value = sliderValue; 
        PrevCheckBracingFlag = CheckBracingFlag;

        return WristDist;
    }

    private float CheckBackBend()
    {
        float ShoulderCenter = ((landmarkResult.viewportLandmarks[12][1] + landmarkResult.viewportLandmarks[11][1])) / 2;
        float HipCenter = ((landmarkResult.viewportLandmarks[24][1] + landmarkResult.viewportLandmarks[23][1])) / 2;

        float TorsoDist = (ShoulderCenter - HipCenter) * 100;

        if (Frame < (StillFrame + 5))
        {
            StartingTorsoDist = TorsoDist;
        }

        float deltaTorsoDist = (StartingTorsoDist - TorsoDist) / NormalizingFactor;
        //Debug.Log("deltaTorsoDist: " + deltaTorsoDist);
        if (deltaTorsoDist > 10)
        {
            BackBend.text = "Back Bend";
            BackBend.gameObject.SetActive(true);
        }
        else
        {
            BackBend.gameObject.SetActive(false);
        }

        return deltaTorsoDist;
    }

}