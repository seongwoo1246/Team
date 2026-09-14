using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = DebugLogger<SoundManager>;



[Serializable]
public struct SoundData
{
    public string soundName;
    public AudioClip clip;
}



public class SoundManager : Singleton<SoundManager>
{

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("오디오 소스 리스트")]
    [SerializeField] private List<SoundData> bgmList;
    [SerializeField] private List<SoundData> sfxList;

    //빠른 검색을 위한 딕셔너리
    private Dictionary<string,AudioClip>bgmDict = new Dictionary<string,AudioClip>();
    private Dictionary<string,AudioClip>sfxDict = new Dictionary<string,AudioClip>();

    private void Start()
    {
        InittializeDictionary();

        SetBGMVolume(PlayerPrefs.GetFloat("BGMSound", 0.5f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXSound", 0.5f));
    }


    /// <summary>
    /// 시작할 때 리스트를 딕셔너리로 바꿔주는 작업
    /// </summary>
    private void InittializeDictionary()
    {
        foreach(var data in bgmList)
        {
            if(!string.IsNullOrEmpty(data.soundName)&&data.clip!=null)
            {
                bgmDict[data.soundName] = data.clip;
            }
        }
        foreach(var data in sfxList)
        {
            if(!string.IsNullOrEmpty(data.soundName)&&data.clip!=null)
            {
                sfxDict[data.soundName] = data.clip;
            }
        }
    }


    /// <summary>
    /// 이름으로 BGM 재생하기
    /// </summary>
    /// <param name="soundName">BGM이름 적는 곳 "이름"</param>
    /// <param name="loop">반복할지 결정 기본 반복</param>
    public void playBGM(string soundName, bool loop = true)
    {
        if(bgmDict.TryGetValue(soundName, out AudioClip bgm))
        {
            bgmSource.clip = bgm;
            bgmSource.loop = loop;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"{soundName}BGM이 없습니다.");
        }
    }

    /// <summary>
    /// 효과음 출력하기
    /// </summary>
    /// <param name="soundName">효과음 이름 "이름"</param>
    public void playSFX(string soundName)
    {
        if(sfxDict.TryGetValue(soundName, out AudioClip sfx))
        {
            sfxSource.PlayOneShot(sfx);
            
        }
        else
        {
            Debug.LogWarning($"{soundName}SFX가 없습니다.");
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }


    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if(bgmSource != null)
        {
            bgmSource.volume = volume;
        }



        PlayerPrefs.SetFloat("BGMSound", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {

        volume = Mathf.Clamp01(volume);

        if (sfxSource != null)
        {
           sfxSource.volume = volume;
        }

        PlayerPrefs.SetFloat("SFXSound", volume);
        PlayerPrefs.Save();
    }

    public void MuteOnOffBGM()
    {
        if(bgmSource.mute == false)
        {
            bgmSource.mute = true;
        }
        else if(bgmSource.mute == true)
        {
            bgmSource.mute = false;
        }

    }
    public void MuteOnOffSFX()
    {
        if(sfxSource.mute == false)
        {
            sfxSource.mute = true;
        }
        else if(sfxSource.mute == true)
        {
            sfxSource.mute = false;
        }

    }
}
