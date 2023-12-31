using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using ChessNET; //regex


public class UIManager : MonoBehaviour
{
    [SerializeField] public Button ResignButton;
    [SerializeField] public GameObject ResignPanel;
    [SerializeField] public Button yesResign;
    [SerializeField] public Button noResign;
    [SerializeField] public Button exportPGNButton;
    [SerializeField] public Sprite tick;
    [SerializeField] public Sprite exportIcon;
    [SerializeField] public TMP_Text PGNText;

    Chessboard chessboard;
    PGNExporter pgnExporter;

    public IEnumerator ShowAndHide(GameObject panel, float delay)
    {
        panel.SetActive(true);
        panel.transform.GetChild(0).gameObject.SetActive(true);
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
    }

    public IEnumerator TickExportButton(float delay)
    {
        exportPGNButton.interactable = false; // Disable the button
        exportPGNButton.GetComponent<Image>().sprite = tick;
        yield return new WaitForSeconds(delay);
        exportPGNButton.GetComponent<Image>().sprite = exportIcon;
        exportPGNButton.interactable = true; // Enable the button
    }

    private void Awake()
    {
        chessboard = FindObjectOfType<Chessboard>();
        pgnExporter = FindObjectOfType<PGNExporter>();
        ResignButton.onClick.AddListener(ShowResignPanel);
        exportPGNButton.onClick.AddListener(ExportHandler);
    }

    private void ShowResignPanel()
    {
        ResignPanel.SetActive(true);
        ResignPanel.transform.GetChild(0).gameObject.SetActive(true);
        yesResign.onClick.AddListener(() =>
        {
            chessboard.Restart();
            ResignPanel.SetActive(false);
        });
        noResign.onClick.AddListener(() => ResignPanel.SetActive(false));
    }

    private void ExportHandler()
    {
        var errcode = pgnExporter.ExportToPGN();
        if (errcode == 0)
        {
            StartCoroutine(TickExportButton(1.5f));
        }
    }
    public void UpdatePGNText()
    {
        var pgnString = pgnExporter.GeneratePGNString();
        //remove PGN header
        //TODO: Add header on export as opposed to removing it every time the text is updated
        const string headerPattern = @"\[Event "".*?""\]\n\[Site "".*?""\]\n\[Date "".*?""\]\n\[Round "".*?""\]\n\[White "".*?""\]\n\[Black "".*?""\]\n\[Result "".*?""\]\n";
        pgnString = Regex.Replace(pgnString, headerPattern, "");
        PGNText.text = pgnString;
        
    }
}