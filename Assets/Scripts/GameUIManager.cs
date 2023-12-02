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
    
    Chessboard chessboard;
    PGNExporter pgnExporter;

    public IEnumerator ShowAndHide(GameObject panel, float delay)
    {
        panel.SetActive(true);
        panel.transform.GetChild(0).gameObject.SetActive(true);
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
    }

    private void Awake()
    {
        chessboard = FindObjectOfType<Chessboard>();
        pgnExporter = FindObjectOfType<PGNExporter>();
        ResignButton.onClick.AddListener(ShowResignPanel);
        exportPGNButton.onClick.AddListener(pgnExporter.ExportToPGN);
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
    
}

