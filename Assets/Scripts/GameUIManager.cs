using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChessNET;
using PGNDelegate;
using Communication;

namespace GameUIManager
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] public Button ResignButton;
        [SerializeField] public GameObject ResignPanel;
        [SerializeField] public Button yesResign;
        [SerializeField] public Button noResign;
        [SerializeField] public Button exitOnResign;
        [SerializeField] public Button exportPGNButton;
        [SerializeField] public Sprite tick;
        [SerializeField] public Sprite exportIcon;
        [SerializeField] public TMP_Text PGNText;
        [SerializeField] public TMP_Text BestMoveCountText;
        [SerializeField] public Button backButton;
        [SerializeField] public Button forwardButton;
        [SerializeField] public GameObject alertPanel;
        [SerializeField] public TMP_Text blunderText;
        [SerializeField] public TMP_Text acplText;
        [SerializeField] public Button acplGraphButton;
        public Image AcplContainerFill;
        private float maxBarSize;
        
        private Chessboard _chessboard;
        private PGNExporter _pgnExporter; //PGNDelegate.cs
        private Client _client; //Communication.cs

        public static IEnumerator ShowAndHideAlertPanel(GameObject panel, float delay)
        {
            panel.SetActive(true);
            panel.transform.GetChild(0).gameObject.SetActive(true); // show the alert text
            yield return new WaitForSeconds(delay); // wait for the delay
            panel.SetActive(false);
        }

        private IEnumerator TickExportButton(float delay)
        {
            exportPGNButton.interactable = false; // Disable the button
            exportPGNButton.GetComponent<Image>().sprite = tick;
            yield return new WaitForSeconds(delay); // wait for the delay
            exportPGNButton.GetComponent<Image>().sprite = exportIcon;
            exportPGNButton.interactable = true; // Enable the button
        }

        private void Awake()
        {
            //find instances in Unity, add listeners to buttons
            _chessboard = FindObjectOfType<Chessboard>();
            _pgnExporter = FindObjectOfType<PGNExporter>();
            ResignButton.onClick.AddListener(ShowResignPanel);
            exportPGNButton.onClick.AddListener(ExportHandler);
            backButton.onClick.AddListener(() => _chessboard.MoveBack());
            forwardButton.onClick.AddListener(() => _chessboard.MoveForward());
            acplGraphButton.onClick.AddListener(OpenACPLGraph);
        }

        private void ShowResignPanel()
        {
            ResignPanel.SetActive(true);
            ResignPanel.transform.GetChild(0).gameObject.SetActive(true);
            yesResign.onClick.AddListener(() =>
            {
                ResignPanel.SetActive(false);
                _chessboard.Restart();
            });
            noResign.onClick.AddListener(() => ResignPanel.SetActive(false));
            exitOnResign.onClick.AddListener(Application.Quit);
        }

        private void ExportHandler()
        {
            var errcode = _pgnExporter.ExportToPGN();
            if (errcode == 0)
            {
                StartCoroutine(TickExportButton(1.5f)); // show the tick for 1.5 seconds
            }
        }

        public void UpdatePGNText()
        {
            var pgnString = _pgnExporter.GeneratePGNString(false);
            pgnString.Substring(pgnString.Length - 4);
            PGNText.text = pgnString;
        }

        public void HandleBestMoveNumber(string bestMoveNum)
        {
            BestMoveCountText.text = bestMoveNum;
        }

        public void HandleBlunderNumber(string blunderNum)
        {
            blunderText.text = blunderNum;
        }

        public void HandleACPL(float acpl)
        {
            float normalizedACPL = Mathf.Clamp(Mathf.Abs(acpl) / 20f, 0f, 1f); // normalize ACPL to 0-1
            AcplContainerFill.fillAmount = normalizedACPL;

            float colorIntensity = Mathf.Clamp((acpl + 20) / 40f, 0f, 1f); 
            AcplContainerFill.color = new Color(colorIntensity, colorIntensity, colorIntensity); // change color based on ACPL

            acplText.text = acpl.ToString();
            acplText.color = colorIntensity > 0.5 ? Color.black : Color.white;



            var centipawn_accuracy = acpl / 100; // convert ACPL to centipawn accuracy

            // prepend "+" for positive ACPL values
            if (centipawn_accuracy is > 99 or < -99)
            {
                acplText.text = "MATE";
            }
            else
            {
                if (acpl > 0)
                {
                    acplText.text = "+" + centipawn_accuracy;
                }
                else
                {
                    acplText.text = centipawn_accuracy.ToString();
                }
            }
        }

        public void OpenACPLGraph()
        {
            Process.Start(Application.persistentDataPath + "/ACPLGraph.png"); // open the ACPL graph
        }

    }
}