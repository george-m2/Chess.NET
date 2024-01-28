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

    private float tempEloSliderValue;
    private int tempEngineDropdownValue;
    private string tempMiniMaxDepth;

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
        
        eloSlider.onValueChanged.AddListener(delegate { tempEloSliderValue = eloSlider.value; });
        engineDropdown.onValueChanged.AddListener(delegate { tempEngineDropdownValue = engineDropdown.value; });
        miniMaxDepthInputField.onValueChanged.AddListener(delegate { tempMiniMaxDepth = miniMaxDepthInputField.text; });

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


    private void SaveEloSelection(float eloSliderValue)
    {
        //even though slider is set to whole integer values in Unity, it still returns a float
        //therefore rounding should result in the same value as it will always end in .0
        int roundedeloSliderValue = Mathf.RoundToInt(eloSliderValue);
        roundedeloSliderValue = 250; //0 * 250 = 0, so set to 250
        
        roundedeloSliderValue = (roundedeloSliderValue + 1) * 250; //Slider has 6 increments, 250-2000 elo
        JSONData data = ReadOrCreateJSONData();
        data.elo = roundedeloSliderValue;
        WriteJSONData(data);
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

    public void SetMiniMaxDepth(string depthText)
    {
        miniMaxDepth = int.Parse(depthText);
        SaveMiniMaxDepth();
    }

    private void SaveEngineSelection(int index)
    {
        string engineName = index == 1 ? "Stockfish" : "cobra";
        JSONData data = ReadOrCreateJSONData();
        data.selectedEngine = engineName;
        WriteJSONData(data);
    }

    private JSONData ReadOrCreateJSONData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "settings.json");
        JSONData data = new JSONData();

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            try
            {
                data = JsonUtility.FromJson<JSONData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error reading JSON: " + ex.Message);
                // Handle error or set default values
                data.depth = 3; // Default depth
                data.selectedEngine = "DefaultEngine"; // cobra as default
                data.elo = 850; // Default elo
            }
        }

        return data;
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

    private void SaveMiniMaxDepth()
    {
        JSONData data = ReadOrCreateJSONData();
        data.depth = miniMaxDepth;
        WriteJSONData(data);
    }
}

[Serializable]
public class JSONData
{
    public int depth;
    public string selectedEngine;
    public int elo;
}
