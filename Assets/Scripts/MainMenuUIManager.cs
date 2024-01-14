using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;



public class Menu : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private TMP_InputField miniMaxDepthInputField;
    
    private List<Resolution> screenResolutions;
    private Resolution[] resolutions;
    private int currentRefreshRate;
    private int currentResolutionIndex;
    private int miniMaxDepth = 3; //default minmax depth 

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void Start()
    {
        //TODO: Rewrite to use refreshRateRatio
        resolutions = Screen.resolutions;
        screenResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();
        currentRefreshRate = Screen.currentResolution.refreshRate;

        settingsButton.onClick.AddListener(() => 
        {
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        });
        
        //Back button
        backToMenuButton.onClick.AddListener(() => 
        {
            settingsPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        });


        for(var i = 0; i < resolutions.Length; i++)
        {
            if(resolutions[i].refreshRate == currentRefreshRate)
            {
                screenResolutions.Add(resolutions[i]);
            }
        }
        List<string> options = new List<string>();
        for (int i = 0; i < screenResolutions.Count; i++)
        {
            string resolutionOption = screenResolutions[i].width + " x " + screenResolutions[i].height + " " + screenResolutions[i].refreshRate + "Hz";
            options.Add(resolutionOption);
            if (screenResolutions[i].width == Screen.width && screenResolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        
    }
    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = screenResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, true);
    }
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetMiniMaxDepth(int depth)
    {
        var miniMaxDepthText = miniMaxDepthInputField.text;
        miniMaxDepth = int.Parse(miniMaxDepthText);
        WriteMiniMaxDepthToJSON();
    }
    public void WriteMiniMaxDepthToJSON()
    {
        try
        {
            var depthData = ScriptableObject.CreateInstance<DepthData>();
            depthData.depth = miniMaxDepth;
            var json = JsonUtility.ToJson(depthData);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "settings.json"), json);
            Debug.Log("Successfully wrote to JSON file");
        }
        catch (Exception)
        {
            Debug.Log("Error writing to JSON file at " + Path.Combine(Application.persistentDataPath, "settings.json"));
        }
    }
}

//class used to allow Unity JSON methods
[Serializable]
public class DepthData: ScriptableObject
{
    [SerializeField] public int depth;
}




   


