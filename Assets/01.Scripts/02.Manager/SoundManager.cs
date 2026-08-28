using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{

    // BGM 오디오 소스

    // SFX 오디오 소스

    private void Start()
    {
        SetBGMVolume(PlayerPrefs.GetFloat("BGMSound", 0.5f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXSound", 0.5f));
    }

    public void SetBGMVolume(float volume)
    {

        PlayerPrefs.SetFloat("BGMSound", volume);
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("SFXSound", volume);
    }

}
