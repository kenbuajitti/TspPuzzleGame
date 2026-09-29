using System.Collections;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared drop-in for the IQ series. No scene edits or Inspector setup needed.
// Only buttons whose displayed label is OTHER IQ GAMES are redirected.
public sealed class IQOtherGamesNavigation : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void IQGames_OpenCatalogue();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var host = new GameObject("IQ Games navigation");
        DontDestroyOnLoad(host);
        host.AddComponent<IQOtherGamesNavigation>();
    }

    private IEnumerator Start()
    {
        // Menus in the series may create buttons at runtime or change scenes.
        var delay = new WaitForSecondsRealtime(0.25f);
        while (true)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.GetComponent<IQOtherGamesBoundButton>() != null) continue;
                bool matches = false;
                foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
                    matches |= IsOtherGames(label.text);
                foreach (var label in button.GetComponentsInChildren<Text>(true))
                    matches |= IsOtherGames(label.text);
                if (!matches) continue;
                // Replaces serialized as well as runtime handlers so the old
                // destination cannot also open when the player clicks.
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(OpenCatalogue);
                button.gameObject.AddComponent<IQOtherGamesBoundButton>();
            }
            yield return delay;
        }
    }

    private static bool IsOtherGames(string text)
    {
        string plain = Regex.Replace(text ?? "", "<[^>]*>", "");
        return Regex.Replace(plain, "[^a-zA-Z]", "").ToUpperInvariant() == "OTHERIQGAMES";
    }

    public static void OpenCatalogue()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        IQGames_OpenCatalogue();
#else
        // Native builds remain open behind the system browser.
        Application.OpenURL("https://iqgamesonline.com/");
#endif
    }
}
