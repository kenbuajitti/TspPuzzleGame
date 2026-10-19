using UnityEngine;
using UnityEngine.SceneManagement;

public class TspMenuController : MonoBehaviour
{
    [SerializeField] private GameObject howToPlayPanel;

    private bool showAnalyticsOptions;

    private void Start()
    {
        RouteIQAnalytics.MenuShown();
        showAnalyticsOptions = RouteIQAnalytics.Choice == -1;
    }

    // Small menu-only privacy panel, independent of scene/Inspector wiring.
    private void OnGUI()
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        float scale = Mathf.Clamp(Screen.width / 960f, 0.5f, 2f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float height = Screen.height / scale;
        float panelHeight = showAnalyticsOptions ? 158 : 42;
        GUILayout.BeginArea(new Rect(12, Mathf.Max(12, height - panelHeight - 12), 360, panelHeight), GUI.skin.box);
        if (showAnalyticsOptions)
        {
            GUILayout.Label("Help improve RouteIQ?");
            var descriptionStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label("Allow Unity Analytics to collect play activity and a persistent player identifier? Optional; you can change this here anytime.", descriptionStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Allow analytics")) SetAnalyticsChoice(true);
            if (GUILayout.Button("No thanks")) SetAnalyticsChoice(false);
            GUILayout.EndHorizontal();
        }
        else if (GUILayout.Button("Analytics: " + (RouteIQAnalytics.Allowed ? "ON" : "OFF") + " - change"))
            showAnalyticsOptions = true;
        GUILayout.EndArea();
        GUI.matrix = oldMatrix;
    }

    private void SetAnalyticsChoice(bool allowed)
    {
        RouteIQAnalytics.SetPlayerChoice(allowed);
        showAnalyticsOptions = false;
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("TspGameScene");
    }

    public void ShowHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }
}