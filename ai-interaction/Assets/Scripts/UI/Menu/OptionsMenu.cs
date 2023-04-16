using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] AudioMixer audioMixer;
    public void SetVolume(float volume)
    {
        MainManager.Instance.volume = volume;
        audioMixer.SetFloat("volume", Mathf.Log10(volume) * 20);
    }
}
