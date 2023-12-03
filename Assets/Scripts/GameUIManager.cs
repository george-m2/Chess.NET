using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Chessboard;
using static PGNExporter;

public class UIManager : MonoBehaviour
{
    [SerializeField] public Button ResignButton;
    [SerializeField] public GameObject ResignPanel;
    [SerializeField] public Button yesResign;
    [SerializeField] public Button noResign;
    [SerializeField] public Button exportPGNButton;
    [SerializeField] public Sprite tick;
    [SerializeField] public Sprite exportIcon;

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
}