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

    private Resolution[] resolutions;
    private int currentResolutionIndex;

    private void Start()
    {
        HandleResolutionSettings();
        AssignButtonListeners();
        LoadSettingsIntoUI();
    }

    private void HandleResolutionSettings()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        // Add all native resolutions to the dropdown
        for (int i = 0; i < resolutions.Length; i++)
        {
            var res = resolutions[i];
            string option = $"{res.width} x {res.height}"; //e.g. 1920 x 1080
            options.Add(option);

            if (res.width == Screen.width && res.height == Screen.height) //if the current resolution matches the native resolution
            {
                currentResolutionIndex = i;
            }
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
        int depth = string.IsNullOrEmpty(miniMaxDepthInputField.text) ? 3 : int.Parse(miniMaxDepthInputField.text); //depth defaults to 3 if input is empty
        string engine = engineDropdown.options[engineDropdown.value].text;
        int elo = (Mathf.RoundToInt(eloSlider.value) + 1) * 250; //multiply by 250 as slider is in increments of 250, starting from 250-2000

        JSONData data = new JSONData { depth = depth, selectedEngine = engine, elo = elo };
        WriteJSONData(data);
        Debug.Log("All settings saved");
    }

    //assigned in Unity inspector
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void SetResolution(int resolutionIndex)
    {
        var resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, true);
        Debug.Log("Resolution set to: " + resolution.width + " x " + resolution.height);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log(Screen.fullScreen);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    private void LoadSettingsIntoUI()
    {
        JSONData data = ReadJSONData();

        miniMaxDepthInputField.text = data.depth.ToString(); //get depth from JSON
        eloSlider.value = (data.elo / 250) - 1; //elo is stored as a multiple of 250, so divide by 250 and subtract 1 to get the slider value

        int engineIndex = engineDropdown.options.FindIndex(option => option.text == data.selectedEngine); //find engine index from JSON
        if (engineIndex != -1)
        {
            engineDropdown.value = engineIndex;
        }
        //TODO: add all settings to JSON, as well as inbuilt Unity resolution/quality settings 
    }

    private JSONData ReadJSONData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "settings.json");
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<JSONData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error reading JSON: " + ex.Message);
            }
        }
        
        return new JSONData { depth = 3, selectedEngine = "DefaultEngine", elo = 850 };
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
