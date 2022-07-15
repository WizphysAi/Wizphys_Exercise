using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private AudioSource exerciseAudio;
    public AudioClip wrong1;
    public AudioClip wrong2;

    void Start()
    {
        exerciseAudio = GetComponent<AudioSource>();
    }

    void Update()
    {
        if(wrong1 == true)
        {
            exerciseAudio.PlayOneShot(wrong1, 1.0f);
        }

        if (wrong2 == true)
        {
            exerciseAudio.PlayOneShot(wrong2, 1.0f);
        }
    }
}
