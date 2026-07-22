using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] Button[] menuButtons;
    [SerializeField] Toggle seedToggle;
    [SerializeField] TMP_InputField ClientCountInput;
    [SerializeField] TMP_InputField NameField;
    [SerializeField] TextMeshProUGUI coinsTxt;
    [SerializeField] GameObject panelObj;
    [SerializeField] float MenuTransitionTime = 1f;
    [SerializeField] GameObject loadingPanel;

    int clientCount;
    void SetButtonsInteractable(bool value)
    {
        foreach (var btn in menuButtons)
            btn.interactable = value;
    }
    void OnEnable()
    {
        if (MyPlayerDataStatic.playerName != "")
            NameField.text = MyPlayerDataStatic.playerName;
        coinsTxt.text = MyPlayerDataStatic.coins.ToString();
    }
    /* async void RunWithButtonLock(Func<Task> action)
     {
         // SetButtonsInteractable(true);

         try
         {
             await action();
         }
         catch (Exception e)
         {
             Debug.LogError(e);
             // SetButtonsInteractable(true);
         }
     }*/

    // ================= HOST =================

    public async void CreateLobby()
    {
        ToggleSeed();
        loadingPanel.SetActive(true);

        bool success = await HostSingleton.Instance.GameManager.StartHostAsync(false);

        if (!success)
        {
            // Only hide if something went wrong
            loadingPanel.SetActive(false);
        }
    }

    // ================= CLIENT =================

public async void JoinLobby()
{
    if (string.IsNullOrWhiteSpace(joinCodeField.text))
        return;

    ToggleSeed();

    loadingPanel.SetActive(true);   // SHOW LOADING

    bool success = false;

    try
    {
        success = await ClientSingleton.Instance.GameManager
            .StartClientAsync(true, joinCodeField.text);
    }
    catch (System.Exception e)
    {
        Debug.LogError(e);
    }

    // ONLY hide if failed
    if (!success)
    {
        loadingPanel.SetActive(false);
    }
}


    public void SwitchPanel(bool isPanelActive)
    {
        float baseScale = 0f;
        float finalScale = 1f;
        if (!isPanelActive)
        {
            baseScale = 1f;
            finalScale = 0f;
        }
        else
            panelObj.SetActive(isPanelActive);
        StartCoroutine(ScaleThenToggle(baseScale, finalScale, isPanelActive));
    }
    IEnumerator ScaleThenToggle(float baseScale, float finalScale, bool isPanelActive)
    {
        yield return StartCoroutine(ScaleToOne(baseScale, finalScale));

        panelObj.SetActive(isPanelActive);
    }

    IEnumerator ScaleToOne(float baseScale, float finalScale)
    {
        float time = 0f;

        while (time < MenuTransitionTime)
        {
            float scale = Mathf.Lerp(baseScale, finalScale, time / MenuTransitionTime);
            panelObj.transform.localScale = new Vector3(scale, scale, scale);

            time += Time.deltaTime;
            yield return null;
        }

        // Ensure exact final scale
        panelObj.transform.localScale = Vector3.one * finalScale;
    }

    void ToggleSeed()
    {
        if (NameField.text == "")
            NameField.text = "Newbie";
        MyPlayerDataStatic.playerName = NameField.text;

        if (seedToggle.isOn)
        {
            MyUtilities.UseRandomSeed = true;
        }
        else
        {
            MyUtilities.UseRandomSeed = false;
        }
    }
}
