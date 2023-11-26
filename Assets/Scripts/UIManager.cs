using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static Chessboard;

public class UIManager : MonoBehaviour
{
    [SerializeField] public Button ResignButton;
    [SerializeField] public GameObject ResignPanel;
    [SerializeField] public Button yesResign;
    [SerializeField] public Button noResign;
    Chessboard chessboard;

    private void Awake()
    {
        chessboard = FindObjectOfType<Chessboard>();
        ResignButton.onClick.AddListener(ShowResignPanel);
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

    public IEnumerator ShowAndHide(GameObject panel, float delay)
    {
        panel.SetActive(true);
        panel.transform.GetChild(0).gameObject.SetActive(true);
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
    }
}

