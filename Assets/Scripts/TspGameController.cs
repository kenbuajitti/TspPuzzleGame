using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class TspGameController : MonoBehaviour
{
    [SerializeField] private TspPuzzleLoader puzzleLoader;
    [SerializeField] private TspPuzzleRenderer puzzleRenderer;

    [SerializeField] private TspRouteLine routeLine;
    [SerializeField] private TspRouteLine optimalRouteLine;

    [SerializeField] private Button startButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button submitButton;

    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultsPanelText;
    [SerializeField] private TMP_Text resultsStatsText;
    [SerializeField] private TMP_Text resultsMessageText;
    [SerializeField] private Button nextPuzzleButton;
    [SerializeField] private Button retryPuzzleButton;
    [SerializeField] private Button playerRouteButton;
    [SerializeField] private Button optimalRouteButton;
    [SerializeField] private Button compareRoutesButton;
    [SerializeField] private Button backToResultsButton;
    [SerializeField] private GameObject routeNavigationPanel;
    [SerializeField] private TMP_Dropdown nodeCountDropdown;

    [SerializeField] private Button mainMenuButton;
    private readonly List<int> selectedPath = new();

    private bool gameRunning;
    private bool routeSubmitted;
    private enum BoardRouteView { Player, Optimal, Both }
    private BoardRouteView boardRouteView = BoardRouteView.Player;
        private float elapsedTime;

    private Toggle puzzleFilterToggle;
    private Button puzzleDoneButton;
    private Button browseNextButton;
    private Button browsePreviousButton;
    private TMP_Text puzzleCounterText;

    private void Awake()
    {
        // Reuse the existing scene's button appearance; no Inspector wiring required.
        puzzleFilterToggle = CreatePuzzleFilterToggle();
        puzzleDoneButton = CreateBrowserButton("PuzzleDoneButton");
        browsePreviousButton = CreateBrowserButton("BrowsePreviousButton");
        browsePreviousButton.GetComponentInChildren<TMP_Text>(true).text = "PREV";
        browseNextButton = CreateBrowserButton("BrowseNextButton");
        browseNextButton.GetComponentInChildren<TMP_Text>(true).text = "NEXT";
        puzzleCounterText = Instantiate(timerText, startButton.transform.parent);
        puzzleCounterText.name = "PuzzleCounterText";
        puzzleCounterText.raycastTarget = false;
        puzzleCounterText.alignment = TextAlignmentOptions.Center;
        puzzleCounterText.gameObject.SetActive(true);

        puzzleFilterToggle.onValueChanged.AddListener(SetPuzzleFilter);
        puzzleDoneButton.onClick.AddListener(TogglePuzzleDone);
        browseNextButton.onClick.AddListener(NextPuzzle);
        browsePreviousButton.onClick.AddListener(PreviousPuzzle);
        puzzleLoader.PuzzleChanged += ResetForSelectedPuzzle;
        puzzleLoader.SelectionChanged += UpdateBrowserUI;

        var layout = startButton.GetComponentInParent<TspResponsiveLayout>();
        if (layout != null)
            layout.RegisterPuzzleBrowser(puzzleFilterToggle, puzzleDoneButton, browsePreviousButton, browseNextButton, puzzleCounterText);
    }

    private Button CreateBrowserButton(string objectName)
    {
        Button button = Instantiate(startButton, startButton.transform.parent);
        button.name = objectName;
        button.onClick = new Button.ButtonClickedEvent();
        button.gameObject.SetActive(true);
        return button;
    }

    private Toggle CreatePuzzleFilterToggle()
    {
        var root = new GameObject("PuzzleFilterToggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        root.transform.SetParent(startButton.transform.parent, false);
        var background = root.GetComponent<Image>();
        background.color = new Color(.95f, .96f, .98f);
        var toggle = root.GetComponent<Toggle>();
        toggle.targetGraphic = background;

        var boxObject = new GameObject("Checkbox", typeof(RectTransform), typeof(Image));
        var box = boxObject.GetComponent<Image>();
        box.rectTransform.SetParent(root.transform, false);
        box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0f, .5f);
        box.rectTransform.anchoredPosition = new Vector2(20f, 0f);
        box.rectTransform.sizeDelta = new Vector2(26f, 26f);
        box.color = new Color(.22f, .25f, .3f);
        box.raycastTarget = false;

        var insetObject = new GameObject("CheckboxInside", typeof(RectTransform), typeof(Image));
        var inset = insetObject.GetComponent<Image>();
        inset.rectTransform.SetParent(box.transform, false);
        inset.rectTransform.sizeDelta = new Vector2(22f, 22f);
        inset.color = Color.white;
        inset.raycastTarget = false;

        var markObject = new GameObject("Checkmark", typeof(RectTransform), typeof(TextMeshProUGUI));
        var mark = markObject.GetComponent<TextMeshProUGUI>();
        mark.rectTransform.SetParent(box.transform, false);
        mark.rectTransform.sizeDelta = new Vector2(24f, 26f);
        mark.font = startButton.GetComponentInChildren<TMP_Text>(true).font;
        mark.text = "X";
        mark.fontSize = 22f;
        mark.fontStyle = FontStyles.Bold;
        mark.alignment = TextAlignmentOptions.Center;
        mark.color = new Color(.12f, .35f, .7f);
        mark.raycastTarget = false;
        toggle.graphic = mark;
        toggle.toggleTransition = Toggle.ToggleTransition.None;
        toggle.SetIsOnWithoutNotify(puzzleLoader.NotDoneOnly);
        mark.canvasRenderer.SetAlpha(toggle.isOn ? 1f : 0f);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(root.transform, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(40f, 2f);
        label.rectTransform.offsetMax = new Vector2(-5f, -2f);
        label.font = mark.font;
        label.text = "Not Done only";
        label.color = new Color(.12f, .14f, .18f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 20f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return toggle;
    }

    private void SetPuzzleFilter(bool notDoneOnly)
    {
        puzzleLoader.SetNotDoneOnly(notDoneOnly);
        // Keep the checkbox in sync even if the loader rejected a locked change.
        puzzleFilterToggle.SetIsOnWithoutNotify(puzzleLoader.NotDoneOnly);
    }

    private void TogglePuzzleDone()
    {
        puzzleLoader.SetCurrentPuzzleDone(!puzzleLoader.CurrentPuzzleDone);
    }

    private void UpdateBrowserUI()
    {
        bool hasPuzzle = puzzleLoader.CurrentPuzzle != null;
        bool canBrowse = !puzzleLoader.SelectionLocked;
        puzzleFilterToggle.SetIsOnWithoutNotify(puzzleLoader.NotDoneOnly);
        puzzleDoneButton.GetComponentInChildren<TMP_Text>(true).text =
            puzzleLoader.CurrentPuzzleDone ? "[X] DONE" : "[ ] DONE";
        puzzleCounterText.text = $"{puzzleLoader.CandidatePosition} of {puzzleLoader.CandidateCount}";
        puzzleFilterToggle.interactable = canBrowse;
        puzzleDoneButton.interactable = canBrowse && hasPuzzle;
        browseNextButton.interactable = canBrowse && hasPuzzle;
        browsePreviousButton.interactable = canBrowse && hasPuzzle;
        nextPuzzleButton.interactable = canBrowse && hasPuzzle;
        retryPuzzleButton.interactable = canBrowse && hasPuzzle;
        nodeCountDropdown.interactable = canBrowse;
        startButton.interactable = canBrowse && hasPuzzle && !routeSubmitted;
        if (!hasPuzzle)
            statusText.text = puzzleLoader.NotDoneOnly
                ? "All puzzles at this level are done! Turn off Not Done only or choose another level."
                : "No puzzles available at this level.";
    }

    private void Start()
    {
        puzzleRenderer.NodeSelected += SelectNode;
        puzzleRenderer.LayoutChanged += RedrawBoardRoutes;

        routeLine.color = Color.red;
        optimalRouteLine.color = Color.green;

        startButton.onClick.AddListener(StartGame);
        undoButton.onClick.AddListener(UndoMove);

        submitButton.onClick.AddListener(SubmitRoute);
        submitButton.gameObject.SetActive(false);

        nextPuzzleButton.onClick.AddListener(NextPuzzle);
        retryPuzzleButton.onClick.AddListener(RetryPuzzle);
        playerRouteButton.onClick.AddListener(ShowPlayerRouteOnly);
        optimalRouteButton.onClick.AddListener(ShowOptimalRouteOnly);
        compareRoutesButton.onClick.AddListener(ShowComparedRoutes);
        backToResultsButton.onClick.AddListener(ReturnToResults);
        routeNavigationPanel.SetActive(false);
        resultPanel.SetActive(false);

        undoButton.interactable = false;
        puzzleRenderer.SetSelectionEnabled(false);

        ShowOpeningInstructions();
        timerText.text = "0.0";

        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        UpdateBrowserUI();

    }

    private void ShowOpeningInstructions()
    {
        statusText.richText = true;
        statusText.textWrappingMode = TextWrappingModes.Normal;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMin = 12f;
        statusText.fontSizeMax = 25f;
        // Smaller opening copy fits the existing responsive status area.
        // Closing the size tag keeps later route/status messages at normal size.
        statusText.text = "<size=75%>All routes begin at \"<color=#FF0000>A</color>\". " +
            "Hit START to begin and navigate to successive nodes by clicking on them. " +
            "When you are finished, click SUBMIT.</size>";
    }

    private void Update()
    {
        if (!gameRunning)
            return;

        elapsedTime += Time.deltaTime;
        timerText.text = elapsedTime.ToString("F1");
    }

    private void StartGame()
    {
        if (puzzleLoader.CurrentPuzzle == null || puzzleLoader.SelectionLocked) return;
        puzzleLoader.SelectionLocked = true;
        UpdateBrowserUI();
        resultPanel.SetActive(false);
        routeSubmitted = false;
        boardRouteView = BoardRouteView.Player;
        puzzleRenderer.SetCompletionError(null);
        selectedPath.Clear();
        routeLine.ClearLine();
        optimalRouteLine.ClearLine();
        nodeCountDropdown.interactable = false;

        selectedPath.Add(0);
        puzzleRenderer.SetSelectedPath(selectedPath);
        UpdateRouteLine();

        elapsedTime = 0f;
        gameRunning = true;

        startButton.interactable = false;
        undoButton.interactable = false;

        puzzleRenderer.SetSelectionEnabled(true);

       // statusText.text = "Select your starting node";
        UpdatePathText();
        timerText.text = "0.0";
        resultPanel.SetActive(false);
        submitButton.gameObject.SetActive(false);
        routeNavigationPanel.SetActive(false);
    }

    private void SelectNode(int nodeIndex)
    {

        if (!gameRunning)
            return;

        if (selectedPath.Count == puzzleLoader.CurrentPuzzle.nodes.Count &&
        nodeIndex == selectedPath[0])
        {
            selectedPath.Add(nodeIndex);
            puzzleRenderer.SetSelectedPath(selectedPath);
            UpdateRouteLine();
            UpdatePathText();

            gameRunning = false;
            puzzleRenderer.SetCompletionError(CalculateErrorPercentage());
            puzzleRenderer.SetSelectionEnabled(false);
            // Keep Undo available until the player submits the route.
            undoButton.interactable = true;

           /* statusText.text += "\nRoute complete!";
            ShowOptimalRoute();
            ShowResults();  
            return;
            */
            statusText.text +=
                "\nRoute complete! Review your route, then select SUBMIT.";
                submitButton.gameObject.SetActive(true);
                return;

        }

        if (selectedPath.Contains(nodeIndex))
        {
            statusText.text =
                $"{(char)('A' + nodeIndex)} has already been selected";
            return;
        }

        selectedPath.Add(nodeIndex);
        puzzleRenderer.SetSelectedPath(selectedPath);
        UpdateRouteLine();
        undoButton.interactable = true;

        UpdatePathText();

        if (selectedPath.Count ==
            puzzleLoader.CurrentPuzzle.nodes.Count)
        {
            statusText.text += "\nSelect the starting node to finish";
        }
    }

    private void UndoMove()
    {
        if (routeSubmitted || puzzleLoader.CurrentPuzzle == null) return;
        puzzleRenderer.SetCompletionError(null);
        bool routeIsComplete =
            selectedPath.Count == puzzleLoader.CurrentPuzzle.nodes.Count + 1 &&
            selectedPath[selectedPath.Count - 1] == selectedPath[0];

        if ((!gameRunning && !routeIsComplete) || selectedPath.Count <= 1)
            return;

        // If the completed route is being reopened, hide SUBMIT and
        // allow node selection and the timer to continue.
        if (routeIsComplete)
        {
            submitButton.gameObject.SetActive(false);
            gameRunning = true;
            puzzleRenderer.SetSelectionEnabled(true);
        }

        selectedPath.RemoveAt(selectedPath.Count - 1);
        puzzleRenderer.SetSelectedPath(selectedPath);
        undoButton.interactable = selectedPath.Count > 1;
        UpdateRouteLine();

        UpdatePathText();

        if (selectedPath.Count == puzzleLoader.CurrentPuzzle.nodes.Count)
            statusText.text += "\nSelect the starting node to finish";
    }

private void UpdateRouteLine()
{
    List<Vector2> positions = new();

    foreach (int nodeIndex in selectedPath)
        positions.Add(puzzleRenderer.GetNodePosition(nodeIndex));

    routeLine.SetPoints(positions);
}
    private void UpdatePathText()
    {
        List<string> labels = new();

        foreach (int index in selectedPath)
            labels.Add(((char)('A' + index)).ToString());

        statusText.text = "Route: " + string.Join(" → ", labels);
    }
    /*private void ShowOptimalRoute()
{
    List<Vector2> positions = new();
    List<int> optimalPath = puzzleLoader.CurrentPuzzle.optimalPath;

    if (optimalPath == null || optimalPath.Count == 0)
    {
        Debug.LogError("The puzzle does not contain an optimal path.");
        return;
    }

    foreach (int nodeIndex in optimalPath)
        positions.Add(puzzleRenderer.GetNodePosition(nodeIndex));

    // Close the route if the JSON path does not repeat its first node.
    if (optimalPath[optimalPath.Count - 1] != optimalPath[0])
    {
        positions.Add(
            puzzleRenderer.GetNodePosition(optimalPath[0])
        );
    }

    optimalRouteLine.SetPoints(positions);
   
}
*/

private void ShowOptimalRoute()
{
    List<int> optimalPath =
        puzzleLoader.CurrentPuzzle.optimalPath;

    if (optimalPath == null || optimalPath.Count == 0)
    {
        Debug.LogError(
            "The puzzle does not contain an optimal path."
        );
        return;
    }

    List<int> closedPlayerPath =
        CreateClosedPath(selectedPath);

    List<int> closedOptimalPath =
        CreateClosedPath(optimalPath);

    HashSet<string> playerEdges =
        CreateEdgeSet(closedPlayerPath);

    HashSet<string> optimalEdges =
        CreateEdgeSet(closedOptimalPath);

    Color sharedColor =
        new Color(1f, 0.65f, 0f);

    List<Vector2> playerPositions = new();
    List<Color> playerColors = new();

    for (int i = 0; i < closedPlayerPath.Count; i++)
    {
        playerPositions.Add(
            puzzleRenderer.GetNodePosition(
                closedPlayerPath[i]
            )
        );

        if (i < closedPlayerPath.Count - 1)
        {
            string edge = GetEdgeKey(
                closedPlayerPath[i],
                closedPlayerPath[i + 1]
            );

            playerColors.Add(
                optimalEdges.Contains(edge)
                    ? sharedColor
                    : Color.red
            );
        }
    }

    List<Vector2> optimalPositions = new();
    List<Color> optimalColors = new();

    for (int i = 0; i < closedOptimalPath.Count; i++)
    {
        optimalPositions.Add(
            puzzleRenderer.GetNodePosition(
                closedOptimalPath[i]
            )
        );

        if (i < closedOptimalPath.Count - 1)
        {
            string edge = GetEdgeKey(
                closedOptimalPath[i],
                closedOptimalPath[i + 1]
            );

            optimalColors.Add(
                playerEdges.Contains(edge)
                    ? sharedColor
                    : Color.green
            );
        }
    }

    routeLine.SetColoredSegments(
        playerPositions,
        playerColors
    );

    optimalRouteLine.SetColoredSegments(
        optimalPositions,
        optimalColors
    );
}

private void ShowPlayerRouteOnly()
{
    boardRouteView = BoardRouteView.Player;
    resultPanel.SetActive(false);
    SetRouteNavigation(
        showPlayer: false,
        showOptimal: true,
        showCompare: true,
        showBack: true
    );

    optimalRouteLine.ClearLine();
    routeLine.color = Color.red;

    List<int> closedPlayerPath = CreateClosedPath(selectedPath);
    List<Vector2> positions = new();

    foreach (int nodeIndex in closedPlayerPath)
        positions.Add(puzzleRenderer.GetNodePosition(nodeIndex));

    routeLine.SetPoints(positions);
}

private void ShowOptimalRouteOnly()
{
    boardRouteView = BoardRouteView.Optimal;
    resultPanel.SetActive(false);
    SetRouteNavigation(
        showPlayer: true,
        showOptimal: false,
        showCompare: true,
        showBack: true
    );

    routeLine.ClearLine();
    optimalRouteLine.color = Color.green;

    List<int> closedOptimalPath =
        CreateClosedPath(puzzleLoader.CurrentPuzzle.optimalPath);

    List<Vector2> positions = new();

    foreach (int nodeIndex in closedOptimalPath)
        positions.Add(puzzleRenderer.GetNodePosition(nodeIndex));

    optimalRouteLine.SetPoints(positions);
}

private void ShowComparedRoutes()
{
    boardRouteView = BoardRouteView.Both;
    resultPanel.SetActive(false);
    SetRouteNavigation(
        showPlayer: true,
        showOptimal: true,
        showCompare: false,
        showBack: true
    );

    routeLine.ClearLine();
    optimalRouteLine.ClearLine();
    ShowOptimalRoute();
}

private void ReturnToResults()
{
    resultPanel.SetActive(true);
    SetRouteNavigation(
        showPlayer: true,
        showOptimal: true,
        showCompare: true,
        showBack: false
    );
}

private void SetRouteNavigation(
    bool showPlayer,
    bool showOptimal,
    bool showCompare,
    bool showBack)
{
    routeNavigationPanel.SetActive(true);
    playerRouteButton.gameObject.SetActive(showPlayer);
    optimalRouteButton.gameObject.SetActive(showOptimal);
    compareRoutesButton.gameObject.SetActive(showCompare);
    backToResultsButton.gameObject.SetActive(showBack);
}

private List<int> CreateClosedPath(List<int> path)
{
    List<int> closedPath = new(path);

    if (closedPath.Count > 0 &&
        closedPath[closedPath.Count - 1] != closedPath[0])
    {
        closedPath.Add(closedPath[0]);
    }

    return closedPath;
}

private HashSet<string> CreateEdgeSet(List<int> path)
{
    HashSet<string> edges = new();

    for (int i = 0; i < path.Count - 1; i++)
    {
        edges.Add(
            GetEdgeKey(path[i], path[i + 1])
        );
    }

    return edges;
}

private string GetEdgeKey(int firstNode, int secondNode)
{
    int lowerNode = Mathf.Min(firstNode, secondNode);
    int higherNode = Mathf.Max(firstNode, secondNode);

    return $"{lowerNode}-{higherNode}";
}
private void ShowResults()
{
    float playerLength = CalculateRouteLength(selectedPath);
    float optimalLength =
        CalculateRouteLength(puzzleLoader.CurrentPuzzle.optimalPath);

    float errorPercentage = CalculateErrorPercentage();

    resultsPanelText.text = "RESULTS";

    resultsStatsText.text =
        $"Optimal Path: {optimalLength:F2}\n" +
        $"Your Path: {playerLength:F2}\n" +
        $"Error: {errorPercentage:F2}%\n" +
        $"Time: {elapsedTime:F1} seconds";

    // Matching edges also recognize the optimal route travelled in reverse.
    // Approximately allows only floating-point noise, not rounded display values.
    bool isOptimal = CreateEdgeSet(CreateClosedPath(selectedPath)).SetEquals(
        CreateEdgeSet(CreateClosedPath(puzzleLoader.CurrentPuzzle.optimalPath))) ||
        Mathf.Approximately(playerLength, optimalLength);

    resultsMessageText.richText = true;
    resultsMessageText.text = GetAttainmentComment(errorPercentage, isOptimal);
    nodeCountDropdown.interactable = true;

    resultPanel.SetActive(true);
    SetRouteNavigation(
        showPlayer: true,
        showOptimal: true,
        showCompare: true,
        showBack: false
    );
}

private static string GetAttainmentComment(float errorPercentage, bool isOptimal)
{
    // Evaluate the unrounded percentage; each upper boundary is inclusive.
    if (isOptimal)
        return "<b>OPTIMAL — You're a genius!</b>";
    if (errorPercentage <= 1f)
        return "<b>MASTER — So close to perfection!</b>";
    if (errorPercentage <= 1.5f)
        return "<b>EXPERT — Excellent route finding!</b>";
    if (errorPercentage <= 2f)
        return "<b>PROFICIENT — A very strong solution!</b>";
    if (errorPercentage <= 3f)
        return "<b>GREAT EFFORT — Just a few improvements away!</b>";
    if (errorPercentage <= 5f)
        return "<b>DEVELOPING — Good start—try a different route!</b>";

    return "<b>EXPLORER — Keep exploring—every route teaches you something!</b>";
}

private float CalculateErrorPercentage()
{
    float optimalLength = CalculateRouteLength(puzzleLoader.CurrentPuzzle.optimalPath);
    if (optimalLength <= 0f) return 0f;
    float playerLength = CalculateRouteLength(selectedPath);
    return Mathf.Max(0f, (playerLength - optimalLength) / optimalLength * 100f);
}

// Rebuild the currently visible routes after rotation/resizing moves the nodes.
private void RedrawBoardRoutes()
{
    if (selectedPath.Count == 0) return;
    if (boardRouteView == BoardRouteView.Both)
    {
        ShowOptimalRoute();
        return;
    }
    if (boardRouteView == BoardRouteView.Optimal)
    {
        List<Vector2> points = new();
        foreach (int index in CreateClosedPath(puzzleLoader.CurrentPuzzle.optimalPath))
            points.Add(puzzleRenderer.GetNodePosition(index));
        optimalRouteLine.SetPoints(points);
        return;
    }
    UpdateRouteLine();
}

private float CalculateRouteLength(List<int> path)
{
    if (path == null || path.Count < 2)
        return 0f;

    float totalLength = 0f;
    List<TspNodeData> nodes = puzzleLoader.CurrentPuzzle.nodes;

    for (int i = 0; i < path.Count - 1; i++)
    {
        TspNodeData first = nodes[path[i]];
        TspNodeData second = nodes[path[i + 1]];

        totalLength += Vector2.Distance(
            new Vector2(first.x, first.y),
            new Vector2(second.x, second.y)
        );
    }

    // Close the optimal route if its starting node isn't repeated.
    if (path[path.Count - 1] != path[0])
    {
        TspNodeData last = nodes[path[path.Count - 1]];
        TspNodeData first = nodes[path[0]];

        totalLength += Vector2.Distance(
            new Vector2(last.x, last.y),
            new Vector2(first.x, first.y)
        );
    }

    return totalLength;
}
    private void SubmitRoute()
{
    if (routeSubmitted || puzzleLoader.CurrentPuzzle == null ||
        selectedPath.Count != puzzleLoader.CurrentPuzzle.nodes.Count + 1) return;
    routeSubmitted = true;
    puzzleLoader.SelectionLocked = false;
    UpdateBrowserUI();
    boardRouteView = BoardRouteView.Both;
    submitButton.gameObject.SetActive(false);
    undoButton.interactable = false;

    ShowOptimalRoute();
    ShowResults();
}
    
    private void PreviousPuzzle()
    {
        puzzleLoader.LoadPreviousPuzzle();
    }

    private void NextPuzzle()
    {
        puzzleLoader.LoadNextPuzzle();
    }

    private void ResetForSelectedPuzzle()
    {
        puzzleLoader.SelectionLocked = false;
        routeSubmitted = false;
        boardRouteView = BoardRouteView.Player;
        gameRunning = false;
        elapsedTime = 0f;
        selectedPath.Clear();
        puzzleRenderer.SetSelectedPath(selectedPath);
        puzzleRenderer.SetCompletionError(null);
        puzzleRenderer.SetSelectionEnabled(false);
        routeLine.ClearLine();
        optimalRouteLine.ClearLine();
        resultPanel.SetActive(false);
        submitButton.gameObject.SetActive(false);
        routeNavigationPanel.SetActive(false);
        puzzleRenderer.RefreshPuzzle();
        timerText.text = "0.0";
        ShowOpeningInstructions();
        undoButton.interactable = false;
        UpdateBrowserUI();
    }

    private void RetryPuzzle()
    {
        if (puzzleLoader.SelectionLocked || puzzleLoader.CurrentPuzzle == null) return;
        ResetForSelectedPuzzle();
    }


private void ReturnToMainMenu()
{
    gameRunning = false;
    SceneManager.LoadScene("TspMenuScene");
}
    private void OnDestroy()
    {
        if (puzzleFilterToggle != null)
            puzzleFilterToggle.onValueChanged.RemoveListener(SetPuzzleFilter);
        if (puzzleLoader != null)
        {
            puzzleLoader.PuzzleChanged -= ResetForSelectedPuzzle;
            puzzleLoader.SelectionChanged -= UpdateBrowserUI;
        }
        if (startButton != null) startButton.onClick.RemoveListener(StartGame);
        if (undoButton != null) undoButton.onClick.RemoveListener(UndoMove);
        if (nextPuzzleButton != null) nextPuzzleButton.onClick.RemoveListener(NextPuzzle);
        if (puzzleRenderer != null)
        {
            puzzleRenderer.NodeSelected -= SelectNode;
            puzzleRenderer.LayoutChanged -= RedrawBoardRoutes;
        }

        if (submitButton != null)
            submitButton.onClick.RemoveListener(SubmitRoute);

        if (playerRouteButton != null)
            playerRouteButton.onClick.RemoveListener(ShowPlayerRouteOnly);

        if (optimalRouteButton != null)
            optimalRouteButton.onClick.RemoveListener(ShowOptimalRouteOnly);

        if (compareRoutesButton != null)
            compareRoutesButton.onClick.RemoveListener(ShowComparedRoutes);

        if (backToResultsButton != null)
            backToResultsButton.onClick.RemoveListener(ReturnToResults);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        if (retryPuzzleButton != null)
            retryPuzzleButton.onClick.RemoveListener(RetryPuzzle);
    }

}
