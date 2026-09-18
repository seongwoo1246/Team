using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = DebugLogger<SoundManager>;
//담당자 - 정성우
/*
 효과음, BGM등 소리를 담당 하는 매니저 
 */

[Serializable]
public struct SoundData
{
    public string soundName;
    public AudioClip clip;
}



public class SoundManager : Singleton<SoundManager> 
{

    [Header("Audio Sources")]
    [SerializeField] private AudioSource FadeOutSource;
    [SerializeField] private AudioSource FadeInSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("오디오 소스 리스트")]
    [SerializeField] private List<SoundData> bgmList;
    [SerializeField] private List<SoundData> sfxList;

    //빠른 검색을 위한 딕셔너리
    private Dictionary<string,AudioClip>bgmDict = new Dictionary<string,AudioClip>();
    private Dictionary<string,AudioClip>sfxDict = new Dictionary<string,AudioClip>();

   

    private void Start()
    {
        playBGM("고요한전장",true);

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
            FadeOutSource.clip = bgm;
            FadeOutSource.loop = loop;
            FadeOutSource.Play();
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
        FadeOutSource.Stop();
    }


    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if(FadeOutSource != null)
        {
            FadeOutSource.volume = volume;
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
        if(FadeOutSource.mute == false)
        {
            FadeOutSource.mute = true;
        }
        else if(FadeOutSource.mute == true)
        {
            FadeOutSource.mute = false;
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


    /// <summary>
    /// 노래를 교환 할 때 일어나는 함수 다음 노래로 페이드 인아웃을 통한 노래 교체
    /// </summary>
    /// <param name="newSound">바꿔줄 노래</param>
    /// <param name="fadeTime">페이드 하는 시간</param>
    /// <returns></returns>
    public async UniTask FadeSound(SoundData newSound , float fadeTime)
    {
        if(FadeOutSource.clip == newSound.clip)
        {
            return;
        }

        // 새로운 소스에 클립 할당 및 재생 시작 
        FadeInSource.clip = newSound.clip;
        FadeInSource.volume = 0f;
        FadeInSource.Play();

        float time = 0f;
        float startVloume = FadeOutSource.volume;

        while(time < fadeTime)
        {
            time += Time.deltaTime;
            float t = time / fadeTime;

            //기존 BGM 페이드 아웃 새 BGM 페이드 인 
            FadeOutSource.volume = Mathf.Lerp(startVloume, 0f, t);
            FadeInSource.volume = Mathf.Lerp(0f, 1f, t);

            await UniTask.Yield();

        }

        //기존 BGM 정지
        FadeOutSource.Stop();
        FadeOutSource.volume = 1f;

        //노래 스왑
        var temp = FadeOutSource;
        FadeOutSource = FadeInSource;
        FadeInSource = temp;

        
    }

   
}
