using System.Threading;
using Cysharp.Threading.Tasks;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(WebCamInput))]
public sealed class BlazePoseLowRow : MonoBehaviour
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
    [SerializeField] Text BackBend;
    [SerializeField] Text Counter;

    [Header("Audio Souce")]
    [SerializeField] AudioClip Bracing_Audio;
    [SerializeField] AudioClip Shrugging_Audio;
    [SerializeField] AudioClip NeckTilt_Audio;
    [SerializeField] AudioClip NeckRotaion_Audio;
    [SerializeField] AudioClip Counter_Audio;

    //private AudioSource source;
    public AudioSource BracingAudio;
    public AudioSource ShruggingAudio;
    public AudioSource NeckTiltAudio;
    public AudioSource NeckRotationAudio;
    public AudioSource CounterAudio;


    //Check StandStill
    int Frame;
    int StillFrame;
    bool CalibrationFlag = false;
    int CalibrationFrame;
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

    // BracingCount
    int CheckBracingCount = 0;
    float StartingShoulderDist;

    //LowRow Variable
    int LowRowAngleCount = 0;
    bool LowRowFlag = false;
    bool PrevLowRowFlag = false;
    int LowRowCounter = 0;

    //Audio
    bool ShruggingFlag = false;
    bool NeckTiltFlag = false;
    bool NeckRotaionFlag = false;
    bool[] CheckPriority = new bool[4];
    int ErrorAudio = 0;
    int CoolDownCount = 0;

    //Wrist function
    int WrsitUpCount = 0;
    int WrsitDownCount = 0;

    //BendBand
    int BackBendCount = 0;
    float StartingtorsoslopeLeft = 0;
    float StartingtorsoslopeRight = 0;

    [SerializeField] Image greenSignal;
    [SerializeField] Image redSignal;

    //Check Movement Variables
    int MoveCount = 0;
    float PrevHipLY = 0;
    float PrevHipRY = 0;
    float PrevKneeLY = 0;
    float PrevKneeRY = 0;

    //Counter
    int ShruggingCounter = 0;
    int NeckTiltCounter = 0;
    int NeckRotationCounter = 0;

    //Slider
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
        BackBend.gameObject.SetActive(false);
        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);
        gyroPanel.gameObject.SetActive(false);

        //source.PlayOneShot(bracingstart_Audio, 1f);


        pose = new BlazePose(options);
        drawer = new BlazePoseDrawer(Camera.main, gameObject.layer, containerView);
        cancellationToken = this.GetCancellationTokenOnDestroy();
        GetComponent<WebCamInput>().OnTextureUpdate.AddListener(OnTextureUpdate);

        StillFlag = false;
        CalibrationFlag = false;

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
        BackBend.gameObject.SetActive(false);
        //necktiltObject.gameObject.SetActive(false);
        Shrugging.gameObject.SetActive(false);

        greenSignal.gameObject.SetActive(false);
        redSignal.gameObject.SetActive(true);

        //if (SystemInfo.supportsGyroscope)
        //{
        //    //Debug.Log("GyroToUnity(Input.gyro.attitude): " + GyroToUnity(Input.gyro.attitude));
        //    //Debug.Log("GyroToUnity(Input.gyro.attitude)[0]: " + GyroToUnity(Input.gyro.attitude)[0]);

        //}

        gyrovalues_new = GyroScript.gyrovalues*10;
        //gyrovalues_new = 2f;

        if (3 > gyrovalues_new && gyrovalues_new > 1.8)
        {
            gyroPanel.gameObject.SetActive(false);
            if (landmarkResult != null && landmarkResult.score > 0.2f)
            {

                float t1 = (landmarkResult.viewportLandmarks[32][0] - landmarkResult.viewportLandmarks[28][0]);
                float t2 = (landmarkResult.viewportLandmarks[31][0] - landmarkResult.viewportLandmarks[27][0]);

                Debug.Log("t1:"+t1);
                Debug.Log("t2:"+t2);

                if (StillFlag == true)
                {
                    if(t1<0 && t2<0){
                        CheckMovementRight();
                    }
                    else if(t1>0 && t2>0){
                        CheckMovementLeft();
                    }
                    //Debug.Log("StillFlag " + StillFlag);
                    if (StillFlag == true)
                    {

                        greenSignal.gameObject.SetActive(true);
                        redSignal.gameObject.SetActive(false);

                        if(CalibrationFlag == false){
                            if(t1<0 && t2<0){
                                CheckAngleRight();
                            }
                            else if(t1>0 && t2>0){
                                CheckAngleLeft();
                            }
                        }else{
                            CheckBracing();
                            
                            if(t1<0 && t2<0){
                                CheckLowRowRight();
                                // CheckBackBendRight();
                                if(LowRowFlag == false){
                                    CheckWristRight();
                                }
                            }
                            else if(t1>0 && t2>0){
                                CheckLowRowLeft();
                                // CheckBackBendLeft();
                                if(LowRowFlag == false){
                                    CheckWristLeft();
                                }
                            }
                        }
                    }
                    else
                    {
                        greenSignal.gameObject.SetActive(false);
                        redSignal.gameObject.SetActive(true);

                        startExercise.gameObject.SetActive(false);
                        Bracing.gameObject.SetActive(false);
                        NeckTilt.gameObject.SetActive(false);           
                        BackBend.gameObject.SetActive(false);
                        //necktiltObject.gameObject.SetActive(false);
                        Shrugging.gameObject.SetActive(false);
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

                    startExercise.gameObject.SetActive(false);
                    Bracing.gameObject.SetActive(false);
                    NeckTilt.gameObject.SetActive(false);
                    //necktiltObject.gameObject.SetActive(false);
                    Shrugging.gameObject.SetActive(false);
                    if(t1<0 && t2<0){
                        CheckStandStillRight();
                    }
                    else if(t1>0 && t2>0){
                        CheckStandStillLeft();
                    }
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
                CalibrationFlag = false;
                greenSignal.gameObject.SetActive(false);
                redSignal.gameObject.SetActive(true);

            }
        }
        else
        {
            gyroPanel.gameObject.SetActive(true);
            StillFlag = false;
            CalibrationFlag = false;
            greenSignal.gameObject.SetActive(false);
            redSignal.gameObject.SetActive(true);
        }
            

        // if (StillFlag == true && !BracingAudio.isPlaying)
        // {
            
        //     BracingAudio.Play();
        // }


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
                                    //necktiltObject.gameObject.SetActive(true);
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
                                    // NeckRotaionObject.gameObject.SetActive(true);
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

    private void CheckMovementRight()
    {
        float d2y = landmarkResult.viewportLandmarks[24][1] - PrevHipRY;
        float d4y = landmarkResult.viewportLandmarks[26][1] - PrevKneeRY;
        float delta = Math.Abs((d2y + d4y) * 100);

        MoveCount = (Math.Abs(delta) > 2) ? (MoveCount + 1) : (MoveCount = 0);

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
        PrevHipRY = landmarkResult.viewportLandmarks[24][1];
        PrevKneeRY = landmarkResult.viewportLandmarks[26][1];
    }

    private void CheckMovementLeft()
    {
        float d1y = landmarkResult.viewportLandmarks[23][1] - PrevHipLY;
        float d3y = landmarkResult.viewportLandmarks[25][1] - PrevKneeLY;
        float delta = Math.Abs((d1y + d3y) * 100);


        MoveCount = (Math.Abs(delta) > 2) ? (MoveCount + 1) : (MoveCount = 0);

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
        PrevKneeLY = landmarkResult.viewportLandmarks[25][1];
    }

    private void CheckAngleLeft(){
        var A = landmarkResult.viewportLandmarks[15];
        var B = landmarkResult.viewportLandmarks[13];
        var C = landmarkResult.viewportLandmarks[11];

        float StartingLeftAngle = Vector2.Angle(A - B, C - B);
        Debug.Log("StartingLeftAngle: "+StartingLeftAngle);

        if(StartingLeftAngle<110){
            CalibrationFlag = true;
            CalibrationFrame = Frame;
        }else{
            CalibrationFlag = false;
        }
    }

    private void CheckAngleRight(){
        var A = landmarkResult.viewportLandmarks[16];
        var B = landmarkResult.viewportLandmarks[14];
        var C = landmarkResult.viewportLandmarks[12];

        float StartingRightAngle = Vector2.Angle(A - B, C - B);
        Debug.Log("StartingRightAngle: "+StartingRightAngle);

        if(StartingRightAngle<110){
            CalibrationFlag = true;
            CalibrationFrame = Frame;
        }else{
            CalibrationFlag = false;
        }
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
            standStill.text = "Start exercise";
            standStill.gameObject.SetActive(true);
            StillFrame = Frame;
            //NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
            NormalizingFactor = (y1) - (y2);
        }
        else
        {
            StillFlag = false;
            standStill.text = "Please stand still";
            standStill.gameObject.SetActive(true);
        }

        PrevShoulderLX = landmarkResult.viewportLandmarks[11][0];
        PrevHipLX = landmarkResult.viewportLandmarks[23][0];
        PrevKneeLX = landmarkResult.viewportLandmarks[25][0];
        PrevAnkleLX = landmarkResult.viewportLandmarks[27][0];
        PrevFootIndexLX = landmarkResult.viewportLandmarks[31][0];
        return 1;
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

        if ((Math.Abs(delta) < 1.5) && (landmarkResult.viewportLandmarks[32][0] < landmarkResult.viewportLandmarks[28][0]))
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
            //NormalizingFactor = ((d1y + d2y) / 2) - ((d3y + d4y) / 2);
            NormalizingFactor = (y1) - (y2);
        }
        else
        {
            StillFlag = false;
            standStill.text = "Please stand still";
            standStill.gameObject.SetActive(true);
        }

        PrevShoulderRX = landmarkResult.viewportLandmarks[12][0];
        PrevHipRX = landmarkResult.viewportLandmarks[24][0];
        PrevKneeRX = landmarkResult.viewportLandmarks[26][0];
        PrevAnkleRX = landmarkResult.viewportLandmarks[28][0];
        PrevFootIndexRX = landmarkResult.viewportLandmarks[32][0];
        return 1;
    }

    private float CheckLowRowLeft()
    {
        var A = landmarkResult.viewportLandmarks[11];
        var B = landmarkResult.viewportLandmarks[13];
        var C = landmarkResult.viewportLandmarks[15];

        float LowRowAngle = Vector2.Angle(A - B, C - B);
        Debug.Log("LowRowAngle: " + LowRowAngle);
        float deltaElbowHip = (Math.Abs(landmarkResult.viewportLandmarks[25][0] - landmarkResult.viewportLandmarks[13][0])/NormalizingFactor)*100;
        Debug.Log("deltaElbowHip: " + deltaElbowHip);
        // Debug.Log("deltaLowRowAngle: " + deltaLowRowAngle);

        LowRowAngleCount = (LowRowAngle < 70.0f && deltaElbowHip > 30.0f) ? (LowRowAngleCount + 1) : LowRowAngleCount = 0;

        if (LowRowAngleCount > 20)
        {
            LowRowFlag = true;
            Bracing.text = "Low Row";
            Bracing.gameObject.SetActive(true);
            sliderCount = sliderCount+1f;
        }
        else
        {
            LowRowFlag = false;
            Bracing.gameObject.SetActive(false);
        }

        if (PrevLowRowFlag != LowRowFlag)
        {
            LowRowCounter += 1;
            sliderCount = 0;
            Counter.text = (LowRowCounter / 2).ToString();

            if (LowRowCounter > 0 && (LowRowCounter % 2 == 0)) {
                CounterAudio.Play();
            }

            if (LowRowCounter == 20)
            {
                loadingAnim.gameObject.SetActive(true);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        sliderValue = (float)(sliderCount/70f);
        slider.value = sliderValue; 

        PrevLowRowFlag = LowRowFlag;
        return LowRowAngle;
    }

    private float CheckLowRowRight()
    {
        var A = landmarkResult.viewportLandmarks[12];
        var B = landmarkResult.viewportLandmarks[14];
        var C = landmarkResult.viewportLandmarks[16];

        float LowRowAngle = Vector2.Angle(A - B, C - B);
        Debug.Log("LowRowAngle: " + LowRowAngle);
        float deltaElbowHip = (Math.Abs(landmarkResult.viewportLandmarks[26][0] - landmarkResult.viewportLandmarks[14][0])/NormalizingFactor)*100;
        Debug.Log("deltaElbowHip: " + deltaElbowHip);
        // Debug.Log("deltaLowRowAngle: " + deltaLowRowAngle);

        LowRowAngleCount = (LowRowAngle < 70.0f && deltaElbowHip > 30.0f) ? (LowRowAngleCount + 1) : LowRowAngleCount = 0;

        if (LowRowAngleCount > 20)
        {
            LowRowFlag = true;
            Bracing.text = "Low Row";
            Bracing.gameObject.SetActive(true);
            sliderCount = sliderCount+1f;
        }
        else
        {
            LowRowFlag = false;
            Bracing.gameObject.SetActive(false);
        }

        if (PrevLowRowFlag != LowRowFlag)
        {
            LowRowCounter += 1;
            sliderCount = 0;
            Counter.text = (LowRowCounter / 2).ToString();

            if (LowRowCounter > 0 && (LowRowCounter % 2 == 0)) {
                CounterAudio.Play();
            }

            if (LowRowCounter == 20)
            {
                loadingAnim.gameObject.SetActive(true);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }

        sliderValue = (float)(sliderCount/70f);
        slider.value = sliderValue; 

        PrevLowRowFlag = LowRowFlag;
        return LowRowAngle;
    }

    private float CheckBracing()
    {
        float ShoulderDist = (float)(Math.Sqrt(((landmarkResult.viewportLandmarks[11][1] - landmarkResult.viewportLandmarks[12][1])*(landmarkResult.viewportLandmarks[11][1] - landmarkResult.viewportLandmarks[12][1])) + ((landmarkResult.viewportLandmarks[11][0] - landmarkResult.viewportLandmarks[12][0])*(landmarkResult.viewportLandmarks[11][0] - landmarkResult.viewportLandmarks[12][0]))));

        if(Frame < (CalibrationFrame + 3)){
            StartingShoulderDist = ShoulderDist;
        }

        float DeltaShoulderDist = ((ShoulderDist - StartingShoulderDist)/NormalizingFactor)*100;
        // Debug.Log("ShoulderDist: "+ShoulderDist);
        // Debug.Log("StartingShoulderDist: "+StartingShoulderDist);
        Debug.Log("DeltaShoulderDist: "+ DeltaShoulderDist);

        CheckBracingCount = (DeltaShoulderDist > 6.250f) ? (CheckBracingCount + 1) : (CheckBracingCount = 0);

        if (CheckBracingCount > 10)
        {
            NeckRotaion.text = "Bracing";
            NeckRotaion.gameObject.SetActive(true);
        }
        else
        {
            NeckRotaion.gameObject.SetActive(false);
        }

        return DeltaShoulderDist;
    }

    private float CheckBackBendRight()
    {
        float torsoslopeRight = (landmarkResult.viewportLandmarks[26][1] - landmarkResult.viewportLandmarks[24][1]) / (landmarkResult.viewportLandmarks[26][0] - landmarkResult.viewportLandmarks[24][0]);
        //float torsoslopeRight1 = (landmarkResult.viewportLandmarks[26][1] - landmarkResult.viewportLandmarks[12][1]) / (landmarkResult.viewportLandmarks[26][0] - landmarkResult.viewportLandmarks[12][0]);

        if(Frame < (CalibrationFrame + 3)){
            StartingtorsoslopeRight = torsoslopeRight;
        }

        float DeltaTorsoSlopeRight = (StartingtorsoslopeRight - torsoslopeRight);

        Debug.Log("DeltaTorsoSlopeRight: "+ DeltaTorsoSlopeRight);
        // Debug.Log("torsoslopeRight1: "+ torsoslopeRight1);

        BackBendCount = (torsoslopeRight > 3.5f) ? (BackBendCount + 1) : (BackBendCount = 0);

        if (BackBendCount > 15)
        {
            BackBend.text = "Back Bend";
            BackBend.gameObject.SetActive(true);
        }
        else
        {
            BackBend.gameObject.SetActive(false);
        }
        return torsoslopeRight;
    }

    private float CheckBackBendLeft()
    {
        float torsoslopeLeft = (landmarkResult.viewportLandmarks[25][1] - landmarkResult.viewportLandmarks[23][1]) / (landmarkResult.viewportLandmarks[25][0] - landmarkResult.viewportLandmarks[23][0]);

        if(Frame < (CalibrationFrame + 3)){
            StartingtorsoslopeLeft = torsoslopeLeft;
        }

        float DeltaTorsoSlopeLeft = (StartingtorsoslopeLeft - torsoslopeLeft);

        Debug.Log("DeltaTorsoSlopeLeft: "+ DeltaTorsoSlopeLeft);

        BackBendCount = (torsoslopeLeft < -4.5f) ? (BackBendCount + 1) : (BackBendCount = 0);

        if (BackBendCount > 15)
        {
            BackBend.text = "Back Bend";
            BackBend.gameObject.SetActive(true);
        }
        else
        {
            BackBend.gameObject.SetActive(false);
        }

        return torsoslopeLeft;
    }

    private float CheckWristRight()
    {
        float WrsitElbowDistY = ((landmarkResult.viewportLandmarks[16][1] - landmarkResult.viewportLandmarks[14][1])/NormalizingFactor)*100;

        WrsitUpCount = (WrsitElbowDistY > 0.0f) ? (WrsitUpCount + 1) : (WrsitUpCount = 0);
        WrsitDownCount = (WrsitElbowDistY < -17.5f) ? (WrsitDownCount + 1) : (WrsitDownCount = 0);

        if (WrsitUpCount > 30)
        {
            Shrugging.text = "Wrsit Up";
            Shrugging.gameObject.SetActive(true);
        }
        else if (WrsitDownCount > 35)
        {
            Shrugging.text = "Wrsit Down";
            Shrugging.gameObject.SetActive(true);
        }
        else
        {
            Shrugging.gameObject.SetActive(false);
        }

        return WrsitElbowDistY;
    }

    private float CheckWristLeft()
    {
        float WrsitElbowDistY = ((landmarkResult.viewportLandmarks[15][1] - landmarkResult.viewportLandmarks[13][1])/NormalizingFactor)*100;

        WrsitUpCount = (WrsitElbowDistY > 0.0f) ? (WrsitUpCount + 1) : (WrsitUpCount = 0);
        WrsitDownCount = (WrsitElbowDistY < -17.5f) ? (WrsitDownCount + 1) : (WrsitDownCount = 0);

        if (WrsitUpCount > 30)
        {
            Shrugging.text = "Wrsit Up";
            Shrugging.gameObject.SetActive(true);
        }
        else if (WrsitDownCount > 35)
        {
            Shrugging.text = "Wrsit Down";
            Shrugging.gameObject.SetActive(true);
        }
        else
        {
            Shrugging.gameObject.SetActive(false);
        }

        return WrsitElbowDistY;
    }

}