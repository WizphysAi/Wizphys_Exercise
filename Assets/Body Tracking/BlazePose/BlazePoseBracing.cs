using System.Threading;
using Cysharp.Threading.Tasks;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(WebCamInput))]
public sealed class BlazePoseBracing : MonoBehaviour
{
    [SerializeField]
    private BlazePose.Options options = default;
    [SerializeField]
    private RectTransform containerView = null;
    [SerializeField]
    private RawImage debugView = null;
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

    //Check Movement 
    int MoveCount = 0;
    float PrevHipLY = 0;
    float PrevHipRY = 0;
    float PrevKneeLY = 0;
    float PrevKneeRY = 0;

    //Check Bracing
    float StartingShoulderDist;
    int CheckBracingCount = 0;
    int BracingCounter = 0;
    bool CheckBracingFlag = false;
    bool PrevCheckBracingFlag = false;

    //Check Shrugging
    float StartinShoulderPosition;
    int CheckShruggingCount = 0;
    int ShruggingCounter = 0;
    bool ShruggingFlag = false;

    //Check NeckTilt
    float StartingNeckDist;
    int CheckNeckDistCount = 0;
    int NeckTiltCounter = 0;
    bool NeckTiltFlag = false;

    //Check NeckRoatation
    int CheckNeckRotationCount = 0;
    int NeckRotationCounter = 0; 
    bool NeckRotaionFlag = false;
    
    // Audio
    bool[] CheckPriority = new bool[4];
    int ErrorAudio = 0;
    int CoolDownCount = 0;
    float StartingNeckZ;
    [SerializeField] AudioSource backgroundMusic;
    [SerializeField] float volumeBG = 0.3f;

    // Still Signal 
    [SerializeField] Image greenSignal;
    [SerializeField] Image redSignal;

    // Slider
    public Slider slider;
    float sliderCount = 0;
    float sliderValue = 0;

    // Gyrovalues
    [SerializeField] float gyrovalues_new;
    [SerializeField] GameObject gyro;
    private Gyro_Manager  GyroScript;
    [SerializeField] GameObject gyroPanel;

    [SerializeField] private UniTask<bool> task;
    [SerializeField] private CancellationToken cancellationToken;

    private void Start()
    {
        slider.value = 0;
        loadingAnim.gameObject.SetActive(false);

        startExercise.gameObject.SetActive(false);
        Bracing.gameObject.SetActive(false);
        NeckTilt.gameObject.SetActive(false);
        NeckRotaion.gameObject.SetActive(false);
        Shrugging.gameObject.SetActive(false);
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

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);

        gyrovalues_new = GyroScript.gyrovalues*10;
        //gyrovalues_new = 2.0f;

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
                        CheckShrugging();
                        if (ShruggingFlag == false)
                        {
                            CheckNeck();
                        }
                        CheckNeckRotation();

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

        StillCount = (Math.Abs(delta) < 1.5) ? (StillCount + 1) : (StillCount = 0);

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
        if (MoveCount > 2)
        {
            StillFlag = false;
        }
        else
        {
            StillFlag = true;
        }
        PrevHipLY = landmarkResult.viewportLandmarks[23][1];
        PrevHipRY = landmarkResult.viewportLandmarks[24][1];
        PrevKneeLY = landmarkResult.viewportLandmarks[25][1];
        PrevKneeRY = landmarkResult.viewportLandmarks[26][1];
    }

    private float CheckBracing()
    {
        float LeftShoulderX = (landmarkResult.viewportLandmarks[11][0] * 3 + landmarkResult.viewportLandmarks[13][0]) / 4;
        float RightShoulderX = (landmarkResult.viewportLandmarks[12][0] * 3 + landmarkResult.viewportLandmarks[14][0]) / 4;

        float ShoulderDist = RightShoulderX - LeftShoulderX;
        if (Frame < (StillFrame + 3))
        {
            StartingShoulderDist = ShoulderDist;
        }
        float DeltaShoulderDist = (((StartingShoulderDist - ShoulderDist) / StartingShoulderDist) * 100f);
        // Debug.Log("DeltaShoulderDist"+ DeltaShoulderDist);

        CheckBracingCount = (DeltaShoulderDist > 2.7f) ? (CheckBracingCount + 1) : (CheckBracingCount = 0);
        // Debug.Log("CheckBracingCount"+ CheckBracingCount);
        if (CheckBracingCount > 10)
        {
            //Bracing.text = "Bracing";
            CheckBracingFlag = true;
            Bracing.gameObject.SetActive(true);
            sliderCount = sliderCount+1f;
        }
        else
        {
            CheckBracingFlag = false;
            Bracing.gameObject.SetActive(false);
        }

        if (PrevCheckBracingFlag != CheckBracingFlag)
        {
            BracingCounter += 1;
            sliderCount = 0;
            Counter.text = (BracingCounter / 2).ToString();

            if (BracingCounter > 0 && (BracingCounter % 2 == 0)) {
                CounterAudio.Play();
            }

            if (BracingCounter == 20)
            {
                loadingAnim.gameObject.SetActive(true);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        sliderValue = (float)(sliderCount/70f);
        slider.value = sliderValue; 
        
        PrevCheckBracingFlag = CheckBracingFlag;
        return DeltaShoulderDist;
    }
    private float CheckNeck()
    {
        // Debug.Log("Z[0]: "+landmarkResult.viewportLandmarks[0][2]*100);
        if (Frame < (StillFrame + 5))
        {
            StartingNeckZ = landmarkResult.viewportLandmarks[0][2]*100;
            StartingNeckDist = (landmarkResult.viewportLandmarks[0][1] - (landmarkResult.viewportLandmarks[11][1] + landmarkResult.viewportLandmarks[12][1]) / 2);
        }
        float deltaNeckZ = (StartingNeckZ - landmarkResult.viewportLandmarks[0][2]*100);
        Debug.Log("deltaNeckZ: "+deltaNeckZ);

        float deltaNeckDist = ((StartingNeckDist - (landmarkResult.viewportLandmarks[0][1] - (landmarkResult.viewportLandmarks[11][1] + landmarkResult.viewportLandmarks[12][1]) / 2)) / NormalizingFactor) * 100;
        Debug.Log("deltaNeckDist" + deltaNeckDist);
        CheckNeckDistCount = (deltaNeckDist > 2.5) ? (CheckNeckDistCount + 1) : (CheckNeckDistCount = 0);
        // CheckNeckDistCount = (deltaNeckZ > 6.5) ? (CheckNeckDistCount + 1) : (CheckNeckDistCount = 0);
        Debug.Log("CheckNeckDistCount" + CheckNeckDistCount);
        if (CheckNeckDistCount > 15)
        {
            NeckTilt.text = "Neck Tilt";
            NeckTiltFlag = true;
            NeckTilt.gameObject.SetActive(true);
            //necktiltObject.gameObject.SetActive(true);
            
            if(NeckTiltFlag == true)
            {
                NeckTiltCounter += 1;
            }
        }
        else
        {
            NeckTiltFlag = false;
            NeckTilt.gameObject.SetActive(false);
            necktiltObject.gameObject.SetActive(false);

        }
        // return deltaNeckDist;
        return deltaNeckZ;
    }
    private float CheckShrugging()
    {
        float ShoulderPosition = ((landmarkResult.viewportLandmarks[12][1] + landmarkResult.viewportLandmarks[11][1]) / 2);
        float AnklePosition = ((landmarkResult.viewportLandmarks[25][1] + landmarkResult.viewportLandmarks[26][1]) / 2);
        if (Frame < (StillFrame + 5))
        {
            StartinShoulderPosition = ShoulderPosition;
        }
        float deltaShoulderPosition = ((ShoulderPosition - StartinShoulderPosition) / NormalizingFactor) * 100;
        // float deltaShoulderPosition = ((ShoulderPosition - AnklePosition) / NormalizingFactor) * 100;
        Debug.Log("deltaShoulderPosition: "+deltaShoulderPosition);
        CheckShruggingCount = (deltaShoulderPosition > 2.5f) ? (CheckShruggingCount + 1) : (CheckShruggingCount = 0);
        Debug.Log("CheckShruggingCount: "+CheckShruggingCount);
        if (CheckShruggingCount > 12)
        {
            Shrugging.text = "Shrugging";
            ShruggingFlag = true;
            Shrugging.gameObject.SetActive(true);
            //r_shoulderShrugging.gameObject.SetActive(true);
            //l_shoulderShrugging.gameObject.SetActive(true);

            if(ShruggingFlag == true)
            {
                ShruggingCounter += 1;
            }

        }
        else
        {
            ShruggingFlag = false;
            Shrugging.gameObject.SetActive(false);
            r_shoulderShrugging.gameObject.SetActive(false);
            l_shoulderShrugging.gameObject.SetActive(false);
        }
        return deltaShoulderPosition;
    }
    private float CheckNeckRotation()
    {
        float eyeSlope = ((landmarkResult.viewportLandmarks[5][1] - landmarkResult.viewportLandmarks[2][1]) / (landmarkResult.viewportLandmarks[5][0] - landmarkResult.viewportLandmarks[2][0])) * 10;
        if (eyeSlope > 1.5 || eyeSlope < -1.5)
        {
            CheckNeckRotationCount += 1;
        }
        else
        {
            CheckNeckRotationCount = 0;
        }
        if (CheckNeckRotationCount > 20)
        {
            NeckTilt.text = "NeckRotation";
            NeckRotaionFlag = true;
            NeckRotaion.gameObject.SetActive(true);
            //NeckRotaionObject.gameObject.SetActive(true);

            if(NeckRotaionFlag == true)
            {
                NeckRotationCounter += 1;
            }
        }
        else
        {
            NeckRotaionFlag = false;
            NeckRotaion.gameObject.SetActive(false);
            NeckRotaionObject.gameObject.SetActive(false);
        }
        return eyeSlope;
    }
}