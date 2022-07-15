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
public sealed class BlazePoseSA : MonoBehaviour
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

    public Image loadingAnim;

    [Header("Text")]
    [SerializeField] 
    Text StandStill;
    [SerializeField] 
    Text startExercise;
    [SerializeField] 
    Text startExercise_ls;

    [SerializeField] 
    Text NoBracing;
    [SerializeField] 
    Text TorsoTilt;
    [SerializeField] 
    Text ElbowBend;
    [SerializeField] 
    Text SideBend;
    [SerializeField]
    Text WristWrong;

    [SerializeField]
    Text Counter;


    [Header("Audio Souce")]
    //[SerializeField] AudioClip StartExercise_audio;
    //[SerializeField] AudioClip StandStill_audio;
    //[SerializeField] AudioClip StandInFrame_audio;
    [SerializeField] AudioClip IS_audio;
    [SerializeField] AudioClip sidebend_audio;
    [SerializeField] AudioClip torsotilt_audio;
    [SerializeField] AudioClip elbowbend_audio;
    [SerializeField] AudioClip wristup_audio;
    [SerializeField] AudioClip wristdown_audio;
    [SerializeField] AudioClip Counter_audio;


    //public AudioSource StartExerciseAudio;
    //public AudioSource StandStillAudio;
    //public AudioSource StandInFrameAudio;
    public AudioSource ISAudio;
    public AudioSource SideBendAudio;
    public AudioSource TorsoTiltAudio;
    public AudioSource ElbowBendAudio;
    public AudioSource WristUpAudio;
    public AudioSource WristDownAudio;
    public AudioSource CounterAudio;

    [Header("Error Object")]
    [SerializeField] 
    GameObject necktiltObject;
    [SerializeField] 
    GameObject NeckRotaionObject;
    [SerializeField] 
    GameObject r_shoulderShrugging;
    [SerializeField] 
    GameObject l_shoulderShrugging;

    [SerializeField]
    AudioClip startExercise_ls_Object;
    [SerializeField]
    AudioClip TorsoTilt_Object;
    [SerializeField]
    AudioClip ElbowBend_Object;
    [SerializeField]
    AudioClip SideBend_Object;


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
    [SerializeField] Image greenSignal;
    [SerializeField] Image redSignal;

    //Check Movement Variables
    int MoveCount = 0;
    float PrevHipLY = 0;
    float PrevHipRY = 0;
    float PrevKneeLY = 0;
    float PrevKneeRY = 0;

    //IS varialbes
    Vector4 StartingWristRight;
    Vector4 StartingElbowRight;
    Vector4 StartingWristLeft;
    Vector4 StartingElbowLeft;
    Vector4 WristRight;
    Vector4 WristLeft;
    int CheckISLeftCount;
    int CheckISRightCount;
    float IsAngleRight;
    float IsAngleLeft;
    int CheckISelbowRight;
    int CheckISelbowLeft;
    bool ISElbowBendFlag = false;
    int CheckISElbowLeftCount;
    int CheckISElbowRightCount;
    bool ISLeftFlag = false;
    bool PrevISLeftFlag = false;
    bool ISRightFlag = false;
    bool PrevISRightFlag = false;
    int ISCounter = 0;
    int CheckBracingISCount = 0;
    bool BracingFlag = false;
    int CheckTorsoTiltCount = 0;
    bool TorsoTiltFlag = false;
    float StartingShoulderDist;
    float StartingSideAngleRight;
    float StartingSideAngleLeft;
    int SideAngleRightCount;
    int SideAngleLeftCount;
    float StartingWristLeftY;
    int WristLeftUpCount = 0;
    int WristLeftDownCount = 0;
    bool[] CheckPriority = new bool[6];
    int ErrorAudio = 0;
    int CoolDownCount = 0;
    bool SideBendFlag = false;
    bool WristUpFlag = false;
    bool WristDownFlag = false;


    float StartingWristDist = 0;
    bool WristWide = false;
    bool SAFlag = false;
    bool PrevSAFlag = false;
    int SACounter = 0;
    float StartingWristY = 0;

    int WristUpCount = 0;
    int WristDownCount = 0;

    public Slider slider;
    float sliderCount = 0;
    float sliderValue = 0;

    private UniTask<bool> task;
    private CancellationToken cancellationToken;

    [SerializeField] float gyrovalues_new;
    [SerializeField] GameObject gyro;
    private Gyro_Manager  GyroScript;
    [SerializeField] GameObject gyroPanel;


    private void Start()
    {
        slider.value = 0;
        loadingAnim.gameObject.SetActive(false);
        startExercise_ls.gameObject.SetActive(false);
        NoBracing.gameObject.SetActive(false);
        TorsoTilt.gameObject.SetActive(false);
        ElbowBend.gameObject.SetActive(false);
        SideBend.gameObject.SetActive(false);
        WristWrong.gameObject.SetActive(false);

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);

        gyroPanel.gameObject.SetActive(false);

        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }

        pose = new BlazePose(options);

        drawer = new BlazePoseDrawer(Camera.main, gameObject.layer, containerView);

        cancellationToken = this.GetCancellationTokenOnDestroy();

        GetComponent<WebCamInput>().OnTextureUpdate.AddListener(OnTextureUpdate);

        StillFlag = false;

        ISAudio.clip = IS_audio;
        SideBendAudio.clip = sidebend_audio;
        TorsoTiltAudio.clip = torsotilt_audio;
        ElbowBendAudio.clip = elbowbend_audio;
        WristUpAudio.clip = wristup_audio;
        WristDownAudio.clip = wristdown_audio;
        CounterAudio.clip = Counter_audio;

        GyroScript = gyro.GetComponent<Gyro_Manager>();

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
        //drawer.DrawPoseResult(poseResult);
        startExercise_ls.gameObject.SetActive(false);
        NoBracing.gameObject.SetActive(false);
        TorsoTilt.gameObject.SetActive(false);
        ElbowBend.gameObject.SetActive(false);
        SideBend.gameObject.SetActive(false);
        WristWrong.gameObject.SetActive(false);

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);

        gyrovalues_new = GyroScript.gyrovalues*10;
        // gyrovalues_new = 2.0f;

        if (3 > gyrovalues_new && gyrovalues_new > 1.8)
        {
            gyroPanel.gameObject.SetActive(false);


            if (landmarkResult != null && landmarkResult.score > 0.2f)
            {
                //drawer.DrawCropMatrix(pose.CropMatrix);
                //Debug.Log("canvas.planeDistance: " + canvas.planeDistance);  

                if (StillFlag == true)
                {
                    CheckMovement();

                    if (StillFlag == true) {

                        greenSignal.gameObject.SetActive(true);
                        redSignal.gameObject.SetActive(false);

                        CheckSA();

                        // if (SAFlag == true)
                        // {
                        //     checkWristSA();
                        // }

                    }

                }
                else
                {
                    startExercise_ls.gameObject.SetActive(false);
                    NoBracing.gameObject.SetActive(false);
                    TorsoTilt.gameObject.SetActive(false);
                    ElbowBend.gameObject.SetActive(false);
                    SideBend.gameObject.SetActive(false);
                    WristWrong.gameObject.SetActive(false);
                    CheckStandStill();

                    greenSignal.gameObject.SetActive(false);
                    redSignal.gameObject.SetActive(true);
                }

                // if (StillFlag == true && !ISAudio.isPlaying)
                // {
                //     ISAudio.Play();
                // }

                //Debug.Log("landmarkResult.viewportLandmarks[11] X: " + landmarkResult.viewportLandmarks[11][0]);

                drawer.DrawLandmarkResult(landmarkResult, visibilityThreshold, canvas.planeDistance);

                if (options.landmark.useWorldLandmarks)
                {
                    drawer.DrawWorldLandmarks(landmarkResult, visibilityThreshold);
                }

                //Frame += 1;
                Frame = Frame + 1;

            }
            else {
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


        if (StillFlag == true)
        {

            if ((TorsoTiltFlag || SideBendFlag || ISElbowBendFlag || WristUpFlag || WristDownFlag))
            {
                if (CoolDownCount == 0)
                {

                    ISAudio.mute = true;
                    CheckPriority[0] = TorsoTiltFlag;
                    CheckPriority[1] = SideBendFlag;
                    CheckPriority[2] = ISElbowBendFlag;
                    CheckPriority[3] = WristUpFlag;
                    CheckPriority[4] = WristDownFlag;
                    CheckPriority[5] = true;

                    for (int i = 0; i < CheckPriority.Length; i++)
                    {
                        if (CheckPriority[i] == true)
                        {
                            ErrorAudio = i;
                            break;
                        }
                    }


                    if (!TorsoTiltAudio.isPlaying && !SideBendAudio.isPlaying && !ElbowBendAudio.isPlaying && !WristUpAudio.isPlaying && !WristDownAudio.isPlaying)
                    {
                        switch (ErrorAudio + 1)
                        {
                            case 1:
                                Debug.Log("Torso Tilt called");
                                TorsoTiltAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 2:
                                Debug.Log("Side Bend called");
                                SideBendAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 3:
                                Debug.Log("Elbow Bend called");
                                ElbowBendAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 4:
                                Debug.Log("Wrist Up called");
                                WristUpAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 5:
                                Debug.Log("Wrist Down called");
                                WristDownAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 6:
                                Debug.Log("NO error called");
                                break;
                        }
                    }
                }
            }
            else
            {

                if (!TorsoTiltAudio.isPlaying && !SideBendAudio.isPlaying && !ElbowBendAudio.isPlaying && !WristUpAudio.isPlaying && !WristDownAudio.isPlaying)
                {
                    // Debug.Log("ISAudio.mute = false");
                    ISAudio.mute = false;
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

    private void CheckMovement()
    {
        float d1y = landmarkResult.viewportLandmarks[23][1] - PrevHipLY;
        float d2y = landmarkResult.viewportLandmarks[24][1] - PrevHipRY;
        float d3y = landmarkResult.viewportLandmarks[25][1] - PrevKneeLY;
        float d4y = landmarkResult.viewportLandmarks[26][1] - PrevKneeRY;

        float delta = Math.Abs((d1y + d2y + d3y + d4y) * 100);

        //Debug.Log("move delta: " + delta);
        //Debug.Log("MoveCount: " + MoveCount);

        MoveCount = (Math.Abs(delta) > 2) ? (MoveCount + 1) : (MoveCount = 0);

        if (MoveCount > 2)
        {
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

    private float CheckSA()
    {
        float WristDist = landmarkResult.viewportLandmarks[16][0] - landmarkResult.viewportLandmarks[15][0];
        float HipY = landmarkResult.viewportLandmarks[24][0] - landmarkResult.viewportLandmarks[23][0];
        float WristY = (landmarkResult.viewportLandmarks[16][1] + landmarkResult.viewportLandmarks[15][1])/2;

        if (Frame < (StillFrame + 5))
        {
            StartingWristDist = WristDist;
            StartingWristY = WristY;
        }

        float deltaWristDist = Math.Abs(((WristDist - StartingWristDist) / NormalizingFactor) * 100);
        float delataWristY = ((WristY - HipY) / NormalizingFactor) *10;

        //Debug.Log("deltaWrist" + deltaWristDist);
        Debug.Log("delataWristY: " + delataWristY);

        if (deltaWristDist > 120) 
        {
            WristWide = true;
        }

        if (WristWide == true)
        {
            if(SAFlag == true && deltaWristDist < 65){
                SAFlag = false;
                WristWide = false;
                
            }
            else if(SAFlag == false && deltaWristDist < 45){
                SAFlag = true;
                WristWide = false;
            }
        }

        if (SAFlag == true)
        {
            startExercise_ls.text = "Serratus Anterior";
            startExercise_ls.gameObject.SetActive(true);
            sliderCount = sliderCount+1f;
        }
        else 
        {
            startExercise_ls.gameObject.SetActive(false);
        }


        if (PrevSAFlag != SAFlag)
        {
            SACounter += 1;
            sliderCount = 0;
            //WristWide = false;
            Counter.text = (SACounter / 2).ToString();

            if (SACounter > 0 && SACounter % 2 == 0)
            {
                CounterAudio.Play();
            }

            if (SACounter == 20)
            {
                loadingAnim.gameObject.SetActive(true);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        if(SAFlag == true && WristWide == false){
            if (delataWristY > 39 ){
                WristUpCount = WristUpCount + 1;
            }
            else if (delataWristY < 34.5 ){
                WristDownCount = WristDownCount + 1;
            }
            else{
                WristUpCount = 0;
                WristDownCount = 0;
                ElbowBend.gameObject.SetActive(false);
            }
        }
        else{
            WristUpCount = 0;
            WristDownCount = 0;
        }

        // Debug.Log("WristUpCount: "+WristUpCount);
        // Debug.Log("WristDownCount: "+WristDownCount);

        if(WristUpCount > 25){
            ElbowBend.text = "Wrist Up";
            ElbowBend.gameObject.SetActive(true);
        }
        if(WristDownCount > 25){
            ElbowBend.text = "Wrist Down";
            ElbowBend.gameObject.SetActive(true);
        }

        // Debug.Log("WristWide: " + WristWide);
        // Debug.Log("SAFlag: " + SAFlag);
        //Debug.Log("PrevSAFlag" + PrevSAFlag);

        sliderValue = (float)(sliderCount/70f);
        slider.value = sliderValue; 

        PrevSAFlag = SAFlag;

        return deltaWristDist;
    }

    // private float checkWristSA(){


    // }

}



