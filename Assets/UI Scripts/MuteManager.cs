using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuteManager : MonoBehaviour
{

    private bool muted;

    public GameObject muteButton;
    public GameObject unMuteButton;
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        muteButton.gameObject.SetActive(true);
        unMuteButton.gameObject.SetActive(false);
    }

    public void MuteButton()
    {
        ToggleAudio();
        Debug.Log("Audio Toggle");
    }

    public void DisableAudio()
    {
        SetAudioMute(false);
        muteButton.gameObject.SetActive(true);
        unMuteButton.gameObject.SetActive(false);
    }

    public void EnableAudio()
    {
        SetAudioMute(true);
        muteButton.gameObject.SetActive(false);
        unMuteButton.gameObject.SetActive(true);

    }

    public void ToggleAudio()
    {
        if (muted)
            DisableAudio();
        else
            EnableAudio();
    }

    private void SetAudioMute(bool mute)
    {
        AudioSource[] sources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
        for (int index = 0; index < sources.Length; ++index)
        {
            sources[index].mute = mute;
        }
        muted = mute;
    }
}
