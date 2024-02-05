using System.Collections;
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
        

        private Chessboard _chessboard;
        private PGNExporter _pgnExporter;
        private Client _client; //Communication

        public IEnumerator ShowAndHide(GameObject panel, float delay)
        {
            panel.SetActive(true);
            panel.transform.GetChild(0).gameObject.SetActive(true);
            yield return new WaitForSeconds(delay);
            panel.SetActive(false);
        }

        private IEnumerator TickExportButton(float delay)
        {
            exportPGNButton.interactable = false; // Disable the button
            exportPGNButton.GetComponent<Image>().sprite = tick;
            yield return new WaitForSeconds(delay);
            exportPGNButton.GetComponent<Image>().sprite = exportIcon;
            exportPGNButton.interactable = true; // Enable the button
        }

        private void Awake()
        {
            _chessboard = FindObjectOfType<Chessboard>();
            _pgnExporter = FindObjectOfType<PGNExporter>();
            ResignButton.onClick.AddListener(ShowResignPanel);
            exportPGNButton.onClick.AddListener(ExportHandler);
            backButton.onClick.AddListener(() => _chessboard.MoveBack());
            forwardButton.onClick.AddListener(() => _chessboard.MoveForward());
        }

        private void ShowResignPanel()
        {
            ResignPanel.SetActive(true);
            ResignPanel.transform.GetChild(0).gameObject.SetActive(true);
            yesResign.onClick.AddListener(() =>
            {
                _chessboard.Restart();
                ResignPanel.SetActive(false);
            });
            noResign.onClick.AddListener(() => ResignPanel.SetActive(false));
            exitOnResign.onClick.AddListener(() => Application.Quit());
        }

        private void ExportHandler()
        {
            var errcode = _pgnExporter.ExportToPGN();
            if (errcode == 0)
            {
                StartCoroutine(TickExportButton(1.5f));
            }
        }

        public void UpdatePGNText()
        {
            var pgnString = _pgnExporter.GeneratePGNString(false);
            PGNText.text = pgnString;
        }

        public void HandleBestMoveNumber(string bestMoveNum)
        {
            BestMoveCountText.text = bestMoveNum;
        }
    }
}