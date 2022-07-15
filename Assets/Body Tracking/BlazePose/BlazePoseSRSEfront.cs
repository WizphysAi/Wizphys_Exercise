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
public sealed class BlazePoseSRSEfront : MonoBehaviour
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
    Text ExerciseSS;
    [SerializeField] 
    Text TorsoTilt;
    [SerializeField] 
    Text elbowBend;
    [SerializeField] 
    Text sideBend;

    [SerializeField] 
    Text NoBracing;
    [SerializeField] 
    Text Shrugging;
    [SerializeField]
    Text Counter;


    [Header("Audio Souce")]
    [SerializeField] AudioClip ss_audio;
    [SerializeField] AudioClip sidebend_audio;
    [SerializeField] AudioClip torsotilt_audio;
    [SerializeField] AudioClip shrugging_audio;
    [SerializeField] AudioClip elbowbend_audio;
    [SerializeField] AudioClip HandFront_audio;
    [SerializeField] AudioClip HandSide_audio;
    [SerializeField] AudioClip Counter_audio;

    public AudioSource SSAudio;
    public AudioSource SideBendAudio;
    public AudioSource TorsoTiltAudio;
    public AudioSource ShruggingAudio;
    public AudioSource ElbowBendAudio;
    public AudioSource HandFrontAudio;
    public AudioSource HandSideAudio;
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
    AudioClip torsoTiltObject;
    [SerializeField]
    AudioClip ElbowBendObject;
    [SerializeField]
    AudioClip sideBendObject;

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
    float PrevFootIndexLX = 0;
    float PrevFootIndexRX = 0;
    float PrevAnkleLX = 0;
    float PrevAnkleRX = 0;

    [Header("Exercise Start/Stop Signal")]
    [SerializeField] Image greenSignal;
    [SerializeField] Image redSignal;

    //Check Movement Variables
    int MoveCount = 0;
    float PrevHipLY = 0;
    float PrevHipRY = 0;
    float PrevKneeLY = 0;
    float PrevKneeRY = 0;

    //Bracing Variables
    float StartingShoulderDist;
    int CheckBracingCount = 0;
    float StartingNeckDist;
    int CheckNeckDistCount = 0;
    int CheckNeckRotationCount = 0;
    float StartinShoulderPosition;
    int CheckShruggingCount = 0;

    //SS variables
    bool CheckSSRightFlag = false;
    bool SRSEFlag = false;
    bool prevSRSEFlag = false;
    int CheckSSRightCount = 0;
    int CheckSSLeftCount = 0;
    int CheckSSRightSideCount = 0;
    int CheckSSLeftSideCount = 0;
    int CheckSSRightFrontCount = 0;
    int CheckSSLeftFrontCount = 0;
    int SRSECounter = 0;
    bool TorsoTiltFlag = false;
    int CheckTorsoTiltCount = 0;
    float StartingSideAngleRight = 0;
    float StartingSideAngleLeft = 0;
    int SideAngleRightCount = 0;
    int SideAngleLeftCount = 0;
    int CheckBracingSSCount = 0;
    bool BracingFlag = false;
    float StartingLeftShoulderY;
    float StartingRightShoulderY;
    float LeftShoulderCount = 0;
    float RightShoulderCount = 0;
    float ElbowBendCount = 0;
    int CoolDownCount = 0;
    bool ElbowBendFlag = false;
    bool SideBendFlag = false;
    bool ShruggingFlag = false;
    bool HandFrontFlag = false;
    bool HandSideFlag = false;
    int ErrorAudio = 0;
    bool[] CheckPriority = new bool[7];


    //Common in SS and SRSE
    float StartingRightWristY = 0;
    float StartingRightWristX = 0;
    float StartingLeftWristY = 0;
    float StartingLeftWristX = 0;

    //SRSE 
    float StartingWristDistX = 0;
    float StartingWristY = 0;
    float StartingTorsoDist = 0;
    int CheckSRSECount = 0;


    private UniTask<bool> task;
    private CancellationToken cancellationToken;

    [SerializeField] float gyrovalues_new;
    [SerializeField] GameObject gyro;
    private Gyro_Manager  GyroScript;
    [SerializeField] GameObject gyroPanel;

    private void Start()
    {
        loadingAnim.gameObject.SetActive(false);

        ExerciseSS.gameObject.SetActive(false);
        NoBracing.gameObject.SetActive(false);
        Shrugging.gameObject.SetActive(false);
        TorsoTilt.gameObject.SetActive(false);
        sideBend.gameObject.SetActive(false);
        elbowBend.gameObject.SetActive(false);

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

        SSAudio.clip = ss_audio;
        SideBendAudio.clip = sidebend_audio;
        TorsoTiltAudio.clip = torsotilt_audio;
        ShruggingAudio.clip = shrugging_audio;
        ElbowBendAudio.clip = elbowbend_audio;
        HandFrontAudio.clip = HandFront_audio;
        HandSideAudio.clip = HandSide_audio;
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
        ExerciseSS.gameObject.SetActive(false);
        NoBracing.gameObject.SetActive(false);
        Shrugging.gameObject.SetActive(false);
        TorsoTilt.gameObject.SetActive(false);
        sideBend.gameObject.SetActive(false);
        elbowBend.gameObject.SetActive(false);

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
                float t1 = (landmarkResult.viewportLandmarks[32][0] - landmarkResult.viewportLandmarks[28][0]);
                float t2 = (landmarkResult.viewportLandmarks[31][0] - landmarkResult.viewportLandmarks[27][0]);  

                if (StillFlag == true)
                {
                    if(SRSEFlag == false){
                        if(t1<0 && t2<0){
                            CheckMovementRight();
                        }
                        else if(t1>0 && t2>0){
                            CheckMovementLeft();
                        }
                    }

                    if (StillFlag == true)
                    {
                        greenSignal.gameObject.SetActive(true);
                        redSignal.gameObject.SetActive(false);

                        CheckBracing();

                        if(t1<0 && t2<0){
                            CheckSRSERight();
                            // CheckBracingSRSERight();
                            // CheckBackBendRight();

                        }
                        else if(t1>0 && t2>0){
                            CheckSRSELeft();
                            // CheckBracingSRSELeft();
                            // CheckBackBendLeft();
                        }

                        // CheckBracingSRSE();
                        // CheckSRSE();
                        // CheckNeck();
                        // CheckShrugging();
                    }
                    else
                    {
                        greenSignal.gameObject.SetActive(false);
                        redSignal.gameObject.SetActive(true);
                        //textElementStandStill.gameObject.SetActive(false);
                        ExerciseSS.gameObject.SetActive(false);
                        NoBracing.gameObject.SetActive(false);
                        TorsoTilt.gameObject.SetActive(false);
                        sideBend.gameObject.SetActive(false);
                        elbowBend.gameObject.SetActive(false);
                        if(t1<0 && t2<0){
                            CheckStandStillRight();
                        }
                        else if(t1>0 && t2>0){
                            CheckStandStillLeft();
                        }
                    }
                }
                else
                {
                    greenSignal.gameObject.SetActive(false);
                    redSignal.gameObject.SetActive(true);
                    //textElementStandStill.gameObject.SetActive(false);
                    ExerciseSS.gameObject.SetActive(false);
                    NoBracing.gameObject.SetActive(false);
                    TorsoTilt.gameObject.SetActive(false);
                    sideBend.gameObject.SetActive(false);
                    elbowBend.gameObject.SetActive(false);
                    if(t1<0 && t2<0){
                        CheckStandStillRight();
                    }
                    else if(t1>0 && t2>0){
                        CheckStandStillLeft();
                    }
                }

                AudioSource audioSource = GetComponent<AudioSource>();

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

        // if (StillFlag == true && !SSAudio.isPlaying)
        // {
        //     SSAudio.Play();
        // }

        if (StillFlag == true)
        {
            if ((TorsoTiltFlag || SideBendFlag || ShruggingFlag || ElbowBendFlag || HandFrontFlag || HandSideFlag))
            {
                if (CoolDownCount == 0)
                {
                    SSAudio.mute = true;

                    CheckPriority[0] = TorsoTiltFlag;
                    CheckPriority[1] = SideBendFlag;
                    CheckPriority[2] = ShruggingFlag;
                    CheckPriority[3] = ElbowBendFlag;
                    CheckPriority[4] = HandFrontFlag;
                    CheckPriority[5] = HandSideFlag;
                    CheckPriority[6] = true;

                    for (int i = 0; i < CheckPriority.Length; i++)
                    {
                        if (CheckPriority[i] == true)
                        {
                            ErrorAudio = i;
                            break;
                        }
                    }


                    if (!TorsoTiltAudio.isPlaying && !SideBendAudio.isPlaying && !ShruggingAudio.isPlaying && !ElbowBendAudio.isPlaying && !HandFrontAudio.isPlaying && !HandSideAudio.isPlaying)
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
                                Debug.Log("Shrugging called");
                                ShruggingAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 4:
                                Debug.Log("Elbow Bend called");
                                ElbowBendAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 5:
                                Debug.Log("Hand Front called");
                                HandFrontAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 6:
                                Debug.Log("Hand Side called");
                                HandSideAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 7:
                                Debug.Log("NO error called");
                                break;
                        }
                    }
                }
            }
            else
            {
                if (!TorsoTiltAudio.isPlaying && !SideBendAudio.isPlaying && !ShruggingAudio.isPlaying && !ElbowBendAudio.isPlaying && !HandFrontAudio.isPlaying && !HandSideAudio.isPlaying)
                {
                    // Debug.Log("BracingAudio.mute = false");
                    SSAudio.mute = false;
                }

                if (CoolDownCount > 0)
                {
                    CoolDownCount = CoolDownCount - 1;
                }
            }

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


    private void CheckMovementRight()
    {
        
        float d2y = landmarkResult.viewportLandmarks[26][1] - PrevHipRY;
        float d4y = landmarkResult.viewportLandmarks[28][1] - PrevKneeRY;
        float delta = Math.Abs((d2y + d4y) * 100);
        Debug.Log("deltaRight: "+delta);

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
        PrevHipRY = landmarkResult.viewportLandmarks[26][1];
        PrevKneeRY = landmarkResult.viewportLandmarks[28][1];
    }

    private void CheckMovementLeft()
    {
        float d1y = landmarkResult.viewportLandmarks[25][1] - PrevHipLY;
        float d3y = landmarkResult.viewportLandmarks[27][1] - PrevKneeLY;
        float delta = Math.Abs((d1y + d3y) * 100);
        Debug.Log("deltaLeft: "+delta);

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
        PrevHipLY = landmarkResult.viewportLandmarks[25][1];
        PrevKneeLY = landmarkResult.viewportLandmarks[27][1];
    }

private float CheckStandStillRight()
    {
        float d1 = landmarkResult.viewportLandmarks[12][0] - PrevShoulderRX;
        float d2 = landmarkResult.viewportLandmarks[24][0] - PrevHipRX;
        float d3 = landmarkResult.viewportLandmarks[26][0] - PrevKneeRX;
        float d4 = landmarkResult.viewportLandmarks[28][0] - PrevAnkleRX;
        float d5 = landmarkResult.viewportLandmarks[32][0] - PrevFootIndexRX;

        float y1 = landmarkResult.viewportLandmarks[12][1];
        float y2 = landmarkResult.viewportLandmarks[24][1];

        float torsoslope = (landmarkResult.viewportLandmarks[24][1] - landmarkResult.viewportLandmarks[12][1])/(landmarkResult.viewportLandmarks[24][0] - landmarkResult.viewportLandmarks[12][0]);

        float delta = Math.Abs((d1 + d2 + d3 + d4 + d5) * 100);

        Debug.Log("delta right: "+delta);
        //Debug.Log("torsoslope: "+torsoslope);
        // Debug.Log("viewportLandmarks[32][0]: "+landmarkResult.viewportLandmarks[32][0]*100);
        // Debug.Log("viewportLandmarks[28][0]: "+landmarkResult.viewportLandmarks[28][0]*100);

        if ((Math.Abs(delta) < 1.5) && (landmarkResult.viewportLandmarks[32][0] < landmarkResult.viewportLandmarks[28][0]) && torsoslope<-1.5f)
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
            //NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
            NormalizingFactor = (y1) - (y2);
        }
        else
        {
            StillFlag = false;
            StandStill.text = "Please stand still";
            StandStill.gameObject.SetActive(true);
        }

        PrevShoulderRX = landmarkResult.viewportLandmarks[12][0];
        PrevHipRX = landmarkResult.viewportLandmarks[24][0];
        PrevKneeRX = landmarkResult.viewportLandmarks[26][0];
        PrevAnkleRX = landmarkResult.viewportLandmarks[28][0];
        PrevFootIndexRX = landmarkResult.viewportLandmarks[32][0];
        return 1;
    }

    private float CheckStandStillLeft()
    {
        float d1 = landmarkResult.viewportLandmarks[11][0] - PrevShoulderLX;
        float d2 = landmarkResult.viewportLandmarks[23][0] - PrevHipLX;
        float d3 = landmarkResult.viewportLandmarks[25][0] - PrevKneeLX;
        float d4 = landmarkResult.viewportLandmarks[27][0] - PrevAnkleLX;
        float d5 = landmarkResult.viewportLandmarks[31][0] - PrevFootIndexLX;

        float y1 = landmarkResult.viewportLandmarks[11][1];
        float y2 = landmarkResult.viewportLandmarks[23][1];

        float torsoslope = (landmarkResult.viewportLandmarks[23][1] - landmarkResult.viewportLandmarks[11][1])/(landmarkResult.viewportLandmarks[23][0] - landmarkResult.viewportLandmarks[11][0]);

        float delta = Math.Abs((d1 + d2 + d3 + d4 + d5) * 100);

        Debug.Log("delta left: "+delta);
        //Debug.Log("torsoslope: "+torsoslope);
        // Debug.Log("viewportLandmarks[32][0]: "+landmarkResult.viewportLandmarks[32][0]*100);
        // Debug.Log("viewportLandmarks[28][0]: "+landmarkResult.viewportLandmarks[28][0]*100);
        // && torsoslope>1.5f
        if ((Math.Abs(delta) < 2.0) && (landmarkResult.viewportLandmarks[31][0] > landmarkResult.viewportLandmarks[27][0] ))
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
            //NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
            NormalizingFactor = (y1) - (y2);
        }
        else
        {
            StillFlag = false;
            StandStill.text = "Please stand still";
            StandStill.gameObject.SetActive(true);
        }

        PrevShoulderLX = landmarkResult.viewportLandmarks[11][0];
        PrevHipLX = landmarkResult.viewportLandmarks[23][0];
        PrevKneeLX = landmarkResult.viewportLandmarks[25][0];
        PrevAnkleLX = landmarkResult.viewportLandmarks[27][0];
        PrevFootIndexLX = landmarkResult.viewportLandmarks[31][0];
        return 1;
    }


    //SRSE
    private float CheckSRSERight()
    {
        var A = landmarkResult.viewportLandmarks[24];
        var B = landmarkResult.viewportLandmarks[12];
        var C = landmarkResult.viewportLandmarks[14];

        float SRSEAngle = Vector2.Angle(A - B, C - B);
        Debug.Log("SRSEAngle: " + SRSEAngle);
        // Debug.Log("deltaLowRowAngle: " + deltaLowRowAngle);

        CheckSRSECount = (SRSEAngle > 32.0f) ? (CheckSRSECount + 1) : CheckSRSECount = 0;

        if (CheckSRSECount > 20)
        {
            SRSEFlag = true;
            ExerciseSS.text = "SRSE";
            ExerciseSS.gameObject.SetActive(true);
        }
        else
        {
            SRSEFlag = false;
            ExerciseSS.gameObject.SetActive(false);
        }

        // Debug.Log("CheckSRSECount: " + CheckSRSECount);

        if (prevSRSEFlag != SRSEFlag)
        {
            SRSECounter += 1;
            if (SRSECounter > 0 && SRSECounter % 2 == 0)
            {
                CounterAudio.Play();
            }
            Counter.text = (SRSECounter / 2).ToString();

            if (SRSECounter == 20)
            {
                loadingAnim.gameObject.SetActive(true);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
        prevSRSEFlag = SRSEFlag;

        return SRSEAngle;
    }


    private float CheckSRSELeft()
    {
        var A = landmarkResult.viewportLandmarks[13];
        var B = landmarkResult.viewportLandmarks[11];
        var C = landmarkResult.viewportLandmarks[23];

        float SRSEAngle = Vector2.Angle(A - B, C - B);
        Debug.Log("SRSEAngle: " + SRSEAngle);
        // Debug.Log("deltaLowRowAngle: " + deltaLowRowAngle);

        CheckSRSECount = (SRSEAngle > 32.0f) ? (CheckSRSECount + 1) : CheckSRSECount = 0;

        if (CheckSRSECount > 20)
        {
            SRSEFlag = true;
            ExerciseSS.text = "SRSE";
            ExerciseSS.gameObject.SetActive(true);
        }
        else
        {
            SRSEFlag = false;
            ExerciseSS.gameObject.SetActive(false);
        }

        if (prevSRSEFlag != SRSEFlag)
        {
            SRSECounter += 1;
            if (SRSECounter > 0 && SRSECounter % 2 == 0)
            {
                CounterAudio.Play();
            }
            Counter.text = (SRSECounter / 2).ToString();

            //Debug.Log("SSCounter" + SSCounter);
            if (SRSECounter == 20)
            {
                loadingAnim.gameObject.SetActive(true);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
        prevSRSEFlag = SRSEFlag;

        return SRSEAngle;
    }

    private float CheckBracing()
    {
        float DeltaShoulderDist = (float)(Math.Sqrt(((landmarkResult.viewportLandmarks[11][1] - landmarkResult.viewportLandmarks[12][1])*(landmarkResult.viewportLandmarks[11][1] - landmarkResult.viewportLandmarks[12][1])) + ((landmarkResult.viewportLandmarks[11][0] - landmarkResult.viewportLandmarks[12][0])*(landmarkResult.viewportLandmarks[11][0] - landmarkResult.viewportLandmarks[12][0])))*100);
        Debug.Log("DeltaShoulderDist: "+ DeltaShoulderDist);

        CheckBracingCount = (DeltaShoulderDist > 4.850f) ? (CheckBracingCount + 1) : (CheckBracingCount = 0);

        if (CheckBracingCount > 10)
        {
            NoBracing.text = "Bracing";
            NoBracing.gameObject.SetActive(true);
        }
        else
        {
            NoBracing.gameObject.SetActive(false);
        }
        return DeltaShoulderDist;
    }

    private float CheckShrugging()
    {
        float ShoulderPosition = ((landmarkResult.viewportLandmarks[12][1] + landmarkResult.viewportLandmarks[11][1]) / 2);
        if (Frame < (StillFrame + 5))
        {
            StartinShoulderPosition = ShoulderPosition;
        }
        float deltaShoulderPosition = ((ShoulderPosition - StartinShoulderPosition) / NormalizingFactor) * 100;
        //Debug.Log("deltaShoulderPosition: " + deltaShoulderPosition);
        CheckShruggingCount = (deltaShoulderPosition > 2.8f) ? (CheckShruggingCount + 1) : (CheckShruggingCount = 0);
        if (CheckShruggingCount > 20)
        {
            //Shrugging.text = "Shrugging";
            Shrugging.gameObject.SetActive(true);
        }
        else
        {
            Shrugging.gameObject.SetActive(false);
        }
        return deltaShoulderPosition;
    }

    private float CheckNeck()
    {
        if (Frame < (StillFrame + 5))
        {
            StartingNeckDist = (landmarkResult.viewportLandmarks[0][1] - (landmarkResult.viewportLandmarks[23][1] + landmarkResult.viewportLandmarks[24][1]) / 2);
        }
        float deltaNeckDist = ((StartingNeckDist - (landmarkResult.viewportLandmarks[0][1] - (landmarkResult.viewportLandmarks[23][1] + landmarkResult.viewportLandmarks[24][1]) / 2)) / NormalizingFactor) * 100;
        //Debug.Log("deltaNeckDist" + deltaNeckDist);
        CheckNeckDistCount = (deltaNeckDist > 7) ? (CheckNeckDistCount + 1) : (CheckNeckDistCount = 0);
        if (CheckNeckDistCount > 20)
        {
            elbowBend.text = "Neck Tilt";
            elbowBend.gameObject.SetActive(true);
        }
        else
        {
            elbowBend.gameObject.SetActive(false);
        }

        return deltaNeckDist;
    }

}



