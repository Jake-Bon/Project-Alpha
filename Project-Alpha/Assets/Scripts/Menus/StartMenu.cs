using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StartMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    Resolution[] resolutions;
    public TMP_Dropdown resolutionDropdown;

    void Start() {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        //retrieves possible resolutions from machine and inputs 4:3 equivalent 
        //we can change this so the game is 16:9, i just think 4:3 is ~artsy~
        int maxHeight = resolutions[resolutions.Length-1].height;
        if(maxHeight >= 480)
            options.Add("640x480");
        if(maxHeight >= 720)
            options.Add("960x720");
        if(maxHeight >= 1080)
            options.Add("1440x1080");
        if(maxHeight >= 1440)
            options.Add("1920x1440");
        if(maxHeight >= 2160)
            options.Add("2880x2160");

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = 1;
        resolutionDropdown.RefreshShownValue();
    }

    public void StartGame() {
        Debug.Log("START");
        //SceneManager.LoadScene(1);
    }

    public void QuitGame() {
        Debug.Log("QUIT");
        Application.Quit();
    }

    public void SetResolution(int resolutionIndex) {
        switch (resolutionIndex) {
            case 0:
                Screen.SetResolution(640, 480, Screen.fullScreen);
                break;
            case 1:
                Screen.SetResolution(960, 720, Screen.fullScreen);
                break;
            case 2:
                Screen.SetResolution(1440, 1080, Screen.fullScreen);
                break;
            case 3:
                Screen.SetResolution(1920, 1440, Screen.fullScreen);
                break;
            case 4:
                Screen.SetResolution(2880, 2160, Screen.fullScreen);
                break;
        }
        Debug.Log("Resolution set to index " + resolutionIndex);
    }

    public void SetVolume(float volume) {
        audioMixer.SetFloat("MasterVolume", volume);
        Debug.Log("Volume set to " + volume);
    }

    public void SetFullscreen(bool isFullscreen) {
        Screen.fullScreen = isFullscreen;
        Debug.Log("Fullscreen set to " + isFullscreen);
    }

    public void ToggleFullscreen() {
        Screen.fullScreen = !Screen.fullScreen;
    }
}
