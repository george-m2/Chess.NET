using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public IEnumerator ShowAndHide(GameObject panel, float delay)
    {
        panel.SetActive(true);
        panel.transform.GetChild(0).gameObject.SetActive(true);
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
    }

}

