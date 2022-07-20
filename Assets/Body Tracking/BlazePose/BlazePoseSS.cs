using System.Threading;
using Cysharp.Threading.Tasks;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[RequireComponent(typeof(WebCamInput))]
public sealed class BlazePoseSS : MonoBehaviour
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

    [SerializeField, FilePopup("*.tflite")]
    private string palmModelFile = "coco_ssd_mobilenet_quant.tflite";
    [SerializeField, FilePopup("*.tflite")]
    private string landmarkModelFile = "coco_ssd_mobilenet_quant.tflite";

    [SerializeField]
    private RawImage cameraView = null;
    [SerializeField]
    private RawImage debugPalmView = null;
    [SerializeField]
    //private bool runBackground;

    private PalmDetect palmDetect;
    private HandLandmarkDetect landmarkDetect;
    private HandLandmarkDetect landmarkDetect_new;

    // just cache for GetWorldCorners
    private readonly Vector3[] rtCorners = new Vector3[4];
    private readonly Vector3[] worldJoints = new Vector3[HandLandmarkDetect.JOINT_COUNT];
    private PrimitiveDraw draw;
    private List<PalmDetect.Result> palmResults;
    private HandLandmarkDetect.Result landmarkResult_hand;
    private HandLandmarkDetect.Result landmarkResult_new;
    //private UniTask<bool> task;
    //private CancellationToken cancellationToken;


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
    Text wristOrientation;
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
    [SerializeField] AudioClip Wrist_audio;

    public AudioSource SSAudio;
    public AudioSource SideBendAudio;
    public AudioSource TorsoTiltAudio;
    public AudioSource ShruggingAudio;
    public AudioSource ElbowBendAudio;
    public AudioSource HandFrontAudio;
    public AudioSource HandSideAudio;
    public AudioSource CounterAudio;
    // public AudioSource WristAudio;

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
    // [SerializeField]
    // GameObject WristObject;

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
    float StartingLeftWristY;
    float StartingLeftWristX;
    bool CheckSSRightFlag = false;
    bool SSLeftFlag = false;
    bool prevSSLeftFlag = false;
    int CheckSSRightCount = 0;
    int CheckSSLeftCount = 0;
    int CheckSSRightSideCount = 0;
    int CheckSSLeftSideCount = 0;
    int CheckSSRightFrontCount = 0;
    int CheckSSLeftFrontCount = 0;
    int SSCounter = 0;
    bool TorsoTiltFlag = false;
    bool WristFlag = false;
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
    int FullCanCount = 0;
    int EmptyCanCount = 0;
    int ErrorAudio = 0;
    bool[] CheckPriority = new bool[7];

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

        ExerciseSS.gameObject.SetActive(false);
        NoBracing.gameObject.SetActive(false);
        Shrugging.gameObject.SetActive(false);
        TorsoTilt.gameObject.SetActive(false);
        sideBend.gameObject.SetActive(false);
        elbowBend.gameObject.SetActive(false);
        wristOrientation.gameObject.SetActive(false);

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);

        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }

        pose = new BlazePose(options);

        palmDetect = new PalmDetect(palmModelFile);
        landmarkDetect = new HandLandmarkDetect(landmarkModelFile);
        landmarkDetect_new = new HandLandmarkDetect(landmarkModelFile);

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
        // WristAudio.clip = Wrist_audio;

        gyroPanel.gameObject.SetActive(false);

        GyroScript = gyro.GetComponent<Gyro_Manager>();
        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // palmDetect = new PalmDetect(palmModelFile);
        // landmarkDetect = new HandLandmarkDetect(landmarkModelFile);
        // landmarkDetect_new = new HandLandmarkDetect(landmarkModelFile);
        // Debug.Log($"landmark dimension: {landmarkDetect.Dim}");

        //draw = new PrimitiveDraw();

        // var webCamInput = GetComponent<WebCamInput>();
        // webCamInput.OnTextureUpdate.AddListener(OnTextureUpdate);
    }

    private void OnDestroy()
    {
        GetComponent<WebCamInput>().OnTextureUpdate.RemoveListener(OnTextureUpdate);
        pose?.Dispose();
        drawer?.Dispose();

        //var webCamInput = GetComponent<WebCamInput>();
        //webCamInput.OnTextureUpdate.RemoveListener(OnTextureUpdate);

        palmDetect?.Dispose();
        landmarkDetect?.Dispose();
        landmarkDetect_new?.Dispose();
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
        // WristObject.gameObject.SetActive(false);


        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);


        gyrovalues_new = GyroScript.gyrovalues * 10;
        //gyrovalues_new = 2.0f;

        if (3 > gyrovalues_new && gyrovalues_new > 1.8)
        {
            gyroPanel.gameObject.SetActive(false);

            if (landmarkResult != null && landmarkResult.score > 0.2f)
            {
                //drawer.DrawCropMatrix(pose.CropMatrix);
                //Debug.Log("canvas.planeDistance: " + canvas.planeDistance);  
                //Debug.Log("StillFalg: "+StillFlag);
                if (StillFlag == true)
                {
                    CheckMovement();

                    if (StillFlag == true)
                    {
                        greenSignal.gameObject.SetActive(true);
                        redSignal.gameObject.SetActive(false);
                        // Bracing
                        //CheckBracing();
                        //CheckShrugging();
                        //CheckNeck();
                        //CheckNeckRotation();

                        // SS Left
                        CheckBracingSS();
                        CheckSSLeft();
                        
                        if (SSLeftFlag == true)
                        {
                            CheckWristOrientation();
                            CheckElbowBendLeft();
                        }
                        else{
                            wristOrientation.gameObject.SetActive(false);
                        }
                     
                        //Debug.Log("palmResults.Count: "+palmResults.Count);
                        // if (palmResults != null && palmResults.Count > 0)
                        // {
                        //     // DrawFrames(palmResults);
                        // }
                        // //Debug.Log("landmarkResult_hand: "+landmarkResult_hand.score);
                        // if (landmarkResult_hand != null && landmarkResult_hand.score > 0.2f)
                        // {
                        //     //DrawCropMatrix(landmarkDetect.CropMatrix);
                        //     //print("First hand keypoints");
                        //     //Debug.Log("worldJoints[4][0]: "+worldJoints[4][0]);
                        //     // List<float> coord = DrawJoints(landmarkResult_hand.joints);
                        //     // Debug.Log("coord[0]"+coord[0]);
                        // }

                        CheckTorsoTilt();
                        if (TorsoTiltFlag == false)
                        {
                            CheckSideBend();
                        }
                        if (TorsoTiltFlag == false && SideBendFlag == false)
                        {
                            CheckShruggingSS();
                        }

                        // SS Right
                        //CheckSSRight();
                        //if (CheckSSRightFlag == true)
                        //{
                        //    CheckElbowBendRight();
                        //}

                        //CheckSideBend();
                        //CheckTorsoTilt();
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
                    wristOrientation.gameObject.SetActive(false);
                    // WristObject.gameObject.SetActive(false);
                    CheckStandStill();
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
            else
            {
                StillFlag = false;
                greenSignal.gameObject.SetActive(false);
                redSignal.gameObject.SetActive(true);
            }
        }
        else
        {
            //gyroPanel.gameObject.SetActive(true);
            StillFlag = false;
            greenSignal.gameObject.SetActive(false);
            redSignal.gameObject.SetActive(true);
        }

        if (StillFlag == true && !SSAudio.isPlaying)
        {
            SSAudio.Play();
        }

        if (StillFlag == true)
        {

            if ((TorsoTiltFlag || SideBendFlag || ShruggingFlag || ElbowBendFlag || HandFrontFlag || HandSideFlag || WristFlag))
            {
                if (CoolDownCount == 0)
                {

                    SSAudio.mute = true;

                    CheckPriority[0] = WristFlag;
                    CheckPriority[1] = TorsoTiltFlag;
                    CheckPriority[2] = SideBendFlag;
                    CheckPriority[3] = ShruggingFlag;
                    CheckPriority[4] = ElbowBendFlag;
                    CheckPriority[5] = HandFrontFlag;
                    CheckPriority[6] = HandSideFlag;
                    CheckPriority[7] = true;

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
                                Debug.Log("Wrist Alighment wrong called");
                                // WristAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 2:
                                Debug.Log("Torso Tilt called");
                                TorsoTiltAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 3:
                                Debug.Log("Side Bend called");
                                SideBendAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 4:
                                Debug.Log("Shrugging called");
                                ShruggingAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 5:
                                Debug.Log("Elbow Bend called");
                                ElbowBendAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 6:
                                Debug.Log("Hand Front called");
                                HandFrontAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 7:
                                Debug.Log("Hand Side called");
                                HandSideAudio.Play();
                                CoolDownCount = CoolDownCount + 30;
                                break;
                            case 8:
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
                    Debug.Log("BracingAudio.mute = false");
                    SSAudio.mute = false;
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
        //poseResult = pose.PoseResult;

        // if (pose.LandmarkInputTexture != null)
        // {
        //     //debugView.texture = pose.LandmarkInputTexture;
        // }
        // if (landmarkResult != null && landmarkResult.SegmentationTexture != null)
        // {
        //     segmentationView.texture = landmarkResult.SegmentationTexture;
        // }

        if(SSLeftFlag == true && Frame%5 == 0){
            Debug.Log("Hand_Model_called: ");
            palmDetect.Invoke(texture);
            // cameraView.material = palmDetect.transformMat;
            // cameraView.rectTransform.GetWorldCorners(rtCorners);

            palmResults = palmDetect.GetResults(0.7f, 0.3f);


            if (palmResults.Count <= 0) return;

            // Detect only first palm
            landmarkDetect.Invoke(texture, palmResults[0]);
            //debugView.texture = landmarkDetect.inputTex;
            //debugPalmView.texture = landmarkDetect.inputTex;
            landmarkResult_hand = landmarkDetect.GetResult();

            if (palmResults.Count > 1)
            {
                landmarkDetect_new.Invoke(texture, palmResults[1]);
                //debugPalmView.texture = landmarkDetect_new.inputTex;
                landmarkResult_new = landmarkDetect_new.GetResult();
            }
        }
    }

    private async UniTask<bool> InvokeAsync(Texture texture)
    {
        landmarkResult = await pose.InvokeAsync(texture, cancellationToken);
        //poseResult = pose.PoseResult;
        if (pose.LandmarkInputTexture != null)
        {
            debugView.texture = pose.LandmarkInputTexture;
        }
        
        palmResults = await palmDetect.InvokeAsync(texture, cancellationToken);
        cameraView.material = palmDetect.transformMat;
        cameraView.rectTransform.GetWorldCorners(rtCorners);

        if (palmResults.Count <= 0) return false;

        landmarkResult_hand = await landmarkDetect.InvokeAsync(texture, palmResults[0], cancellationToken);
        debugPalmView.texture = landmarkDetect.inputTex;

        if (palmResults.Count > 1)
        {
            landmarkResult_new = await landmarkDetect_new.InvokeAsync(texture, palmResults[1], cancellationToken);
            debugPalmView.texture = landmarkDetect_new.inputTex;
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

        if (Math.Abs(delta) < 1.5)
        {
            StillCount += 1;
        }
        else
        {
            StillCount = 0;
        }

        //Debug.Log("StillCount: "+ StillCount);

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

        Debug.Log("StillFlag: "+StillFlag);

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

    private float CheckBracingSS()
    {
        float LeftShoulderX = landmarkResult.viewportLandmarks[11][0];
        float RightShoulderX = landmarkResult.viewportLandmarks[12][0];
        float ShoulderDist = RightShoulderX - LeftShoulderX;
        if (Frame < (StillFrame + 3))
        {
            StartingShoulderDist = ShoulderDist;
        }
        //Debug.Log("StartingShoulderDist"+ StartingShoulderDist);
        //Debug.Log("ShoulderDist"+ ShoulderDist);

        //float DeltaShoulderDist = (((StartingShoulderDist - ShoulderDist) / NormalizingFactor) * 100f) *10f;
        float DeltaShoulderDist = (((StartingShoulderDist - ShoulderDist) / StartingShoulderDist) * 100f);
        Debug.Log("Delta" + DeltaShoulderDist);

        CheckBracingSSCount = (DeltaShoulderDist > 2.5f) ? (CheckBracingSSCount + 1) : (CheckBracingSSCount = 0);

        if (CheckBracingSSCount > 10)
        {
            //Debug.Log("Bracing");
            BracingFlag = true;
        }
        else
        {
            BracingFlag = false;
        }

        if (SSLeftFlag == true && BracingFlag == false)
        {
            NoBracing.text = "No Bracing";
            NoBracing.gameObject.SetActive(true);
        }
        else if (SSLeftFlag == false)
        {
            NoBracing.gameObject.SetActive(false);
        }

        return DeltaShoulderDist;
    }

    private float CheckShruggingSS()
    {
        float LeftShoulderY = landmarkResult.viewportLandmarks[11][1];
        float RightShoulderY = landmarkResult.viewportLandmarks[12][1];
        //float ShoulderPosition = ((landmarkResult.viewportLandmarks[12][1] + landmarkResult.viewportLandmarks[11][1]) / 2);

        if (Frame < (StillFrame + 5))
        {
            StartingLeftShoulderY = LeftShoulderY;
            StartingRightShoulderY = RightShoulderY;
            //StartinShoulderPosition = ShoulderPosition;
        }

        float deltaLeftShoulder = ((LeftShoulderY - StartingLeftShoulderY) / NormalizingFactor) * 100;
        float deltaRightShoulder = ((RightShoulderY - StartingRightShoulderY) / NormalizingFactor) * 100;
        Debug.Log("deltaLeftShoulder: "+deltaLeftShoulder);
        LeftShoulderCount = (deltaLeftShoulder > 3.7f) ? (LeftShoulderCount + 1) : (LeftShoulderCount = 0);
        Debug.Log("LeftShoulderCount: "+LeftShoulderCount);
        RightShoulderCount = (deltaRightShoulder > 3.7f) ? (RightShoulderCount + 1) : (RightShoulderCount = 0);

        if (LeftShoulderCount > 30 || RightShoulderCount > 30)
        {
            //Debug.Log("Shrugging: " + deltaShoulderPosition);
            ShruggingFlag = true;
            Shrugging.text = "Shrugging";
            Shrugging.gameObject.SetActive(true);
        }
        else
        {
            ShruggingFlag = false;
            Shrugging.gameObject.SetActive(false);
        }

        return Math.Max(deltaLeftShoulder, deltaRightShoulder);

    }


    private float CheckSSLeft()
    {
        Debug.Log("[15]Z: "+landmarkResult.viewportLandmarks[15][2]*100);
        if (Frame < (StillFrame + 5))
        {
            StartingLeftWristY = landmarkResult.viewportLandmarks[15][1];
            StartingLeftWristX = landmarkResult.viewportLandmarks[15][0];
        }

        // Right Hand
        var A = landmarkResult.viewportLandmarks[23];
        var B = landmarkResult.viewportLandmarks[11];
        var C = landmarkResult.viewportLandmarks[15];

        float angle = Vector2.Angle(A - B, C - B);
        float deltaLeftWristY = Math.Abs(((landmarkResult.viewportLandmarks[15][1] - StartingLeftWristY) / NormalizingFactor) * 100) * 10;
        float deltaLeftWristX = Math.Abs(((StartingLeftWristX - landmarkResult.viewportLandmarks[15][0]) / NormalizingFactor) * 100) * 10;

        //Debug.Log("deltaLeftWristY: " + deltaLeftWristY);
        //Debug.Log("deltaLeftWristX: " + deltaLeftWristX);

        if ((angle > 25) && (deltaLeftWristX > 200) && (deltaLeftWristY > 130))
        {
            CheckSSLeftCount += 1;
        }
        else if ((deltaLeftWristX < 150) & (deltaLeftWristY > 170))
        {
            CheckSSLeftFrontCount += 1;
        } 
        else if ((deltaLeftWristX > 100) & (deltaLeftWristY < 130))
        {
            CheckSSLeftSideCount += 1;
        }
        else
        {
            CheckSSLeftCount = 0;
            CheckSSLeftFrontCount = 0;
            CheckSSLeftSideCount = 0;
        }

        //

        SSLeftFlag = false;
        HandFrontFlag = false;
        HandSideFlag = false;

        if (CheckSSLeftCount > 10)
        {
            SSLeftFlag = true;
            ExerciseSS.text = "Supra Spinatus left";
            ExerciseSS.gameObject.SetActive(true);
            sliderCount = sliderCount+1f;
            //Debug.Log("SS_right");
        }
        else if (CheckSSLeftFrontCount > 10)
        {
            HandFrontFlag = true;
            ExerciseSS.text = "hand front";
            ExerciseSS.gameObject.SetActive(true);
            //Debug.Log("hand_front = True");
        }
        else if (CheckSSLeftSideCount > 35)
        {
            HandSideFlag = true;
            ExerciseSS.text = "hand side";
            ExerciseSS.gameObject.SetActive(true);
            //Debug.Log("hand_side = True");
        }
        else
        {
            SSLeftFlag = false;
            ExerciseSS.gameObject.SetActive(false);
        }
        //Debug.Log("angle: "+ angle);
        if (prevSSLeftFlag != SSLeftFlag) {
            SSCounter += 1;
            sliderCount = 0;
            if (SSCounter > 0 && SSCounter % 2 == 0) {
                CounterAudio.Play();
            }
            Counter.text = (SSCounter/2).ToString();

            //Debug.Log("SSCounter" + SSCounter);
            if (SSCounter == 20)
            {
                loadingAnim.gameObject.SetActive(true);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
        sliderValue = (float)(sliderCount/70f);
        slider.value = sliderValue; 
        prevSSLeftFlag = SSLeftFlag;
        return angle;
    }


    private float CheckSSRight()
    {
        Debug.Log("CheckSSRight");
        if (Frame < (StillFrame + 5))
        {
            StartingLeftWristY = landmarkResult.viewportLandmarks[16][1];
            StartingLeftWristX = landmarkResult.viewportLandmarks[16][0];
        }

        // Right Hand
        var A = landmarkResult.viewportLandmarks[24];
        var B = landmarkResult.viewportLandmarks[12];
        var C = landmarkResult.viewportLandmarks[16];

        float angle = Vector2.Angle(A - B, C - B);
        float deltaRightWristY = Math.Abs(((landmarkResult.viewportLandmarks[16][1] - StartingLeftWristY) / NormalizingFactor) * 100) * 10;
        float deltaRightWristX = Math.Abs(((StartingLeftWristX - landmarkResult.viewportLandmarks[16][0]) / NormalizingFactor) * 100) * 10;

        //Debug.Log("deltaRightWristY: " + deltaRightWristY);
        //Debug.Log("deltaRightWristX: " + deltaRightWristX);

        if ((angle > 25) && (deltaRightWristX > 200) && (deltaRightWristY > 130))
        {
            CheckSSRightCount += 1;
        }
        else if ((deltaRightWristX < 150) & (deltaRightWristY > 170))
        {
            CheckSSRightFrontCount += 1;
        }
        else if ((deltaRightWristX > 65) & (deltaRightWristY < 130))
        {
            CheckSSRightSideCount += 1;
        }
        else
        {
            CheckSSRightCount = 0;
            CheckSSRightFrontCount = 0;
            CheckSSRightSideCount = 0;
        }

        CheckSSRightFlag = false;
        HandFrontFlag = false;
        HandSideFlag = false;

        Debug.Log("CheckSSRightFlag"+ CheckSSRightCount);
        if (CheckSSRightCount > 10)
        {
            Debug.Log("Supra Spinatus right");
            CheckSSRightFlag = true;
            ExerciseSS.text = "SS_right";
            ExerciseSS.gameObject.SetActive(true);
            //Debug.Log("SS_right");
        }
        else if (CheckSSRightFrontCount > 10)
        {
            
            ExerciseSS.text = "hand front";
            ExerciseSS.gameObject.SetActive(true);
            //Debug.Log("hand_front = True");
        }
        else if (CheckSSRightSideCount > 15)
        {
            
            ExerciseSS.text = "hand side";
            ExerciseSS.gameObject.SetActive(true);
            //Debug.Log("hand_side = True");
        }
        else
        {

            ExerciseSS.gameObject.SetActive(false);
        }
        //Debug.Log("angle: "+ angle);
        return angle;
    }

    private void CheckWristOrientation(){

        if(palmResults != null && palmResults.Count > 0)
        {
            if (palmResults.Count > 1)
            {
                Debug.Log("Two Hands detected");
                //Debug.Log("SecondHand [4][0]: "+landmarkResult_new.joints[4][0]);
                
                if(landmarkResult_hand.joints[4][0] < landmarkResult_new.joints[4][0])
                {
                    // Debug.Log("landmarkResult_hand.joints[5][1]: "+landmarkResult_hand.joints[5][1]);
                    // Debug.Log("landmarkResult_hand.joints[17][1]: "+landmarkResult_hand.joints[17][1]);
                    // Debug.Log("old Hand is left");
                    if(landmarkResult_hand.joints[5][1] > landmarkResult_hand.joints[17][1])
                    {
                        Debug.Log("Thumb Up");
                        FullCanCount = FullCanCount + 1;
                        wristOrientation.text = "Thumb Up";
                        wristOrientation.gameObject.SetActive(true);
                    }
                    else{
                        Debug.Log("Thumb Down");
                        EmptyCanCount = EmptyCanCount + 1;
                        wristOrientation.text = "Thumb Down";
                        wristOrientation.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // Debug.Log("landmarkResult_new.joints[5][1]: "+landmarkResult_new.joints[5][1]);
                    // Debug.Log("landmarkResult_new.joints[17][1]: "+landmarkResult_new.joints[17][1]); 
                    // Debug.Log("new Hand is left");
                    if(landmarkResult_new.joints[5][1] > landmarkResult_new.joints[17][1]){
                        Debug.Log("Thumb Up");
                        wristOrientation.text = "Thumb Up";
                        wristOrientation.gameObject.SetActive(true);
                    }
                    else{
                        Debug.Log("Thumb Down");
                        wristOrientation.text = "Thumb Down";
                        wristOrientation.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                if(landmarkResult_hand.joints[5][1] > landmarkResult_hand.joints[17][1])
                {
                    Debug.Log("Thumb Up");
                    wristOrientation.text = "Thumb Up";
                    wristOrientation.gameObject.SetActive(true);
                }
                else{
                    Debug.Log("Thumb Down");
                    wristOrientation.text = "Thumb Down";
                    wristOrientation.gameObject.SetActive(true);
                }
            }
        }
    }

    private float CheckElbowBendLeft()
    {
        var A = landmarkResult.viewportLandmarks[11];
        var B = landmarkResult.viewportLandmarks[13];
        var C = landmarkResult.viewportLandmarks[15];

        float ElbowAngle = Vector2.Angle(A - B, C - B);
        Debug.Log("ElbowAngle: " + ElbowAngle);

        ElbowBendCount = (ElbowAngle < 145) ? (ElbowBendCount + 1) : ElbowBendCount = 0;

        if (ElbowBendCount > 20)
        {
            elbowBend.text = "ElbowBend";
            elbowBend.gameObject.SetActive(true);
            ElbowBendFlag = true;
        }
        else
        {
            elbowBend.gameObject.SetActive(false);
            ElbowBendFlag = false;
        }

        return ElbowAngle;
    }

    private float CheckElbowBendRight()
    {
        var A = landmarkResult.viewportLandmarks[12];
        var B = landmarkResult.viewportLandmarks[14];
        var C = landmarkResult.viewportLandmarks[16];

        float ElbowAngle = Vector2.Angle(A - B, C - B);
        //Debug.Log("ElbowAngle: " + ElbowAngle);
        if (ElbowAngle < 145)
        {
            elbowBend.text = "ElbowBend";
            elbowBend.gameObject.SetActive(true);
        }
        else
        {
            elbowBend.gameObject.SetActive(false);
        }

        return ElbowAngle;
    }

    private float CheckTorsoTilt()
    {
        float tilt = Math.Abs((landmarkResult.viewportLandmarks[24][2] - landmarkResult.viewportLandmarks[23][2])) * 100;
        //Debug.Log("tilt" + tilt);

        CheckTorsoTiltCount = (tilt > 18) ? (CheckTorsoTiltCount + 1) : (CheckTorsoTiltCount = 0);
        //Debug.Log("CheckTorsoTiltCount" + CheckTorsoTiltCount);

        if (CheckTorsoTiltCount > 12)
        {
            TorsoTiltFlag = true;
            TorsoTilt.text = "Torso Tilt";
            TorsoTilt.gameObject.SetActive(true);
        }
        else
        {
            TorsoTiltFlag = false;
            TorsoTilt.gameObject.SetActive(false);
        }
        return tilt;
    }

    private float CheckSideBend()
    {
        var A = landmarkResult.viewportLandmarks[12];
        var B = landmarkResult.viewportLandmarks[24];
        var C = landmarkResult.viewportLandmarks[26];

        float SideAngleRight = Vector2.Angle(A - B, C - B);

        var AA = landmarkResult.viewportLandmarks[11];
        var BB = landmarkResult.viewportLandmarks[23];
        var CC = landmarkResult.viewportLandmarks[25];

        float SideAngleLeft = Vector2.Angle(AA - BB, CC - BB);

        if (Frame < (StillFrame + 5))
        {
            StartingSideAngleRight = SideAngleRight;
            StartingSideAngleLeft = SideAngleLeft;
        }

        float DeltaSideAngleRight = StartingSideAngleRight - SideAngleRight;
        float DeltaSideAngleLeft = StartingSideAngleLeft - SideAngleLeft;

        SideAngleRightCount = (DeltaSideAngleRight > 6.5) ? (SideAngleRightCount + 1) : (SideAngleRightCount = 0);
        SideAngleLeftCount = (DeltaSideAngleLeft > 6.5) ? (SideAngleLeftCount + 1) : (SideAngleLeftCount = 0);

        if (SideAngleRightCount > 12)
        {
            SideBendFlag = true;
            sideBend.text = "Side Bend";
            sideBend.gameObject.SetActive(true);
        }
        else if (SideAngleLeftCount > 12)
        {
            SideBendFlag = true;
            sideBend.text = "Side Bend";
            sideBend.gameObject.SetActive(true);
        }
        else {
            SideBendFlag = false; 
            sideBend.gameObject.SetActive(false);
        }

        return 1;
    }

    private void DrawFrames(List<PalmDetect.Result> palms)
    {
        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        draw.color = Color.green;
        foreach (var palm in palms)
        {
            draw.Rect(MathTF.Lerp(min, max, palm.rect, true), 0.02f, min.z);

            foreach (var kp in palm.keypoints)
            {
                draw.Point(MathTF.Lerp(min, max, (Vector3)kp, true), 0.05f);
            }
        }
        draw.Apply();
    }

    void DrawCropMatrix(in Matrix4x4 matrix)
    {
        draw.color = Color.red;

        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        var mtx = matrix.inverse;
        Vector3 a = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 0, 0)));
        Vector3 b = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 0, 0)));
        Vector3 c = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 1, 0)));
        Vector3 d = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 1, 0)));

        draw.Quad(a, b, c, d, 0.02f);
        draw.Apply();
    }

    private List<float> DrawJoints(Vector3[] joints)
    {
        //draw.color = Color.blue;

        // Get World Corners
        Debug.Log("min: "+rtCorners[0]);
        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        Matrix4x4 mtx = Matrix4x4.identity;

        // Get joint locations in the world space
        float zScale = max.x - min.x;
        Debug.Log("zScale: "+zScale);
        for (int i = 0; i < HandLandmarkDetect.JOINT_COUNT; i++)
        {
            Vector3 p0 = mtx.MultiplyPoint3x4(joints[i]);
            Vector3 p1 = MathTF.Lerp(min, max, p0);
            p1.z += (p0.z - 0.5f) * zScale;
            worldJoints[i] = p1;
        }

        // Cube
        //Debug.Log("worldJoints[5] " + worldJoints[5]);
        //Debug.Log("worldJoints[4] " + worldJoints[4]);
        //Debug.Log("worldJoints[17] " + worldJoints[17]);

        // for (int i = 0; i < HandLandmarkDetect.JOINT_COUNT; i++)
        // {
        //     if (i == 5 || i == 4 || i == 17)
        //     {
        //         draw.Cube(worldJoints[i], 0.1f);
        //     }
        // }

        // // Connection Lines
        // var connections = HandLandmarkDetect.CONNECTIONS;
        // for (int i = 0; i < connections.Length; i += 2)
        // {
        //     if (i == 5 || i == 4 || i == 17)
        //     {
        //         draw.Line3D(
        //             worldJoints[connections[i]],
        //             worldJoints[connections[i + 1]],
        //             0.05f);
        //     }
        // }
        // draw.Apply();

        List<float> coords = new List<float>();
        coords.Add(worldJoints[4][0]);
        coords.Add(worldJoints[4][1]);
        coords.Add(worldJoints[4][2]);
        coords.Add(worldJoints[5][0]);
        coords.Add(worldJoints[5][1]);
        coords.Add(worldJoints[5][2]);
        coords.Add(worldJoints[17][0]);
        coords.Add(worldJoints[17][1]);
        coords.Add(worldJoints[17][2]);
        return (coords);
    }

    private void orientation(List<float> coordinates)
    {
        if (coordinates[7] > coordinates[4])
        {
            if (coordinates[4] > coordinates[1])
            {
                Debug.Log("Orientation correct");
                WristFlag = false;
                //WristObject.gameObject.SetActive(false);

            }
            else
            {
                Debug.Log("Point thumb down");
                WristFlag = true;
                //wristAlignment.text = "Wrist alignment wrong";
                //WristObject.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.Log("Wrist orientation incorrect, point your thumb down");
            WristFlag = true;
            //wristAlignment.text = "Wrist alignment wrong";
            //WristObject.gameObject.SetActive(true);
        }
    }

    private void Right_hand_tracking(List<float> coord, List<float> coord_new)
    {
        print("Right hand detected");
        if (coord[0] > coord_new[0])
        {
            orientation(coord);
            print(coord);
        }
        else
        {
            orientation(coord_new);
            print(coord_new);
        }
    }

    private void Left_hand_tracking(List<float> coord, List<float> coord_new)
    {
        print("Left hand detected");
        if (coord[0] < coord_new[0])
        {
            orientation(coord);
            print(coord);
        }
        else
        {
            orientation(coord_new);
            print(coord_new);
        }
    }

    private void hand_tracking(List<float> coord)
    {
        print("Left hand detected");
        orientation(coord);
    }

}



