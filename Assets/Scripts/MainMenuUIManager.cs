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
    [SerializeField] private TMP_Dropdown engineDropdown;
    [SerializeField] private Slider eloSlider;
    [SerializeField] private Button saveButton;

    private List<Resolution> screenResolutions;
    private Resolution[] resolutions;
    private int currentRefreshRate;
    private int currentResolutionIndex;
    private int miniMaxDepth = 3; //default minmax depth

    private void Start()
    {
        resolutions = Screen.resolutions;
        screenResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();
        currentRefreshRate = Screen.currentResolution.refreshRate;

        settingsButton.onClick.AddListener(() => 
        {
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        });

        backToMenuButton.onClick.AddListener(() => 
        {
            settingsPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        });
        
        saveButton.onClick.AddListener(SaveAllSettings);
        
        eloSlider.onValueChanged.AddListener(delegate { });
        engineDropdown.onValueChanged.AddListener(delegate { });
        miniMaxDepthInputField.onValueChanged.AddListener(delegate { });

        for (var i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].refreshRate == currentRefreshRate)
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

    private void SaveAllSettings()
    {
        JSONData data = new JSONData
        {
            depth = miniMaxDepthInputField.text == "" ? 3 : int.Parse(miniMaxDepthInputField.text),
            selectedEngine = engineDropdown.options[engineDropdown.value].text,
            elo = (Mathf.RoundToInt(eloSlider.value) + 1) * 250,
        };

        WriteJSONData(data);
        Debug.Log("All settings saved");
    }


    //IDEs may mark these methods as unused, but they are assigned to UnityEvents in the inspector
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
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

    private void WriteJSONData(JSONData data)
    {
        string folderPath = Application.persistentDataPath;
        string filePath = Path.Combine(folderPath, "settings.json");
        
        try
        {
            var json = JsonUtility.ToJson(data);
            File.WriteAllText(filePath, json);
            Debug.Log("Successfully wrote to JSON file at:" + filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error writing to JSON file: " + ex.Message);
        }
    }
}

[Serializable]
public class JSONData
{
    public int depth;
    public string selectedEngine;
    public int elo;
}
