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
    [SerializeField] private Button backToMenuButton, settingsButton, saveButton;
    [SerializeField] private GameObject settingsPanel, mainMenuPanel;
    [SerializeField] private TMP_InputField miniMaxDepthInputField;
    [SerializeField] private TMP_Dropdown engineDropdown;
    [SerializeField] private Slider eloSlider;

    private List<Resolution> screenResolutions;
    private Resolution[] resolutions;
    private int currentResolutionIndex;

    private void Start()
    {
        InitializeSettings();
        AssignButtonListeners();
    }

    private void InitializeSettings()
    {
        resolutions = Screen.resolutions;
        screenResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();

        int currentRefreshRate = Screen.currentResolution.refreshRate;
        foreach (var res in resolutions)
        {
            if (res.refreshRate == currentRefreshRate)
            {
                screenResolutions.Add(res);
                if (res.width == Screen.width && res.height == Screen.height)
                {
                    currentResolutionIndex = screenResolutions.Count - 1;
                }
            }
        }

        List<string> options = new List<string>();
        foreach (var res in screenResolutions)
        {
            options.Add($"{res.width} x {res.height} {res.refreshRate}Hz");
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void AssignButtonListeners()
    {
        settingsButton.onClick.AddListener(ToggleSettingsPanel);
        backToMenuButton.onClick.AddListener(ToggleSettingsPanel);
        saveButton.onClick.AddListener(SaveAllSettings);
    }

    private void ToggleSettingsPanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
        mainMenuPanel.SetActive(!mainMenuPanel.activeSelf);
    }

    private void SaveAllSettings()
    {
        int depth = string.IsNullOrEmpty(miniMaxDepthInputField.text) ? 3 : int.Parse(miniMaxDepthInputField.text);
        string engine = engineDropdown.options[engineDropdown.value].text;
        int elo = (Mathf.RoundToInt(eloSlider.value) + 1) * 250;

        JSONData data = new JSONData { depth = depth, selectedEngine = engine, elo = elo };
        WriteJSONData(data);
        Debug.Log("All settings saved");
    }

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
        string filePath = Path.Combine(Application.persistentDataPath, "settings.json");
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
