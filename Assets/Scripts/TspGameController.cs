using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private readonly List<int> selectedPath = new();

    private bool gameRunning;
        private float elapsedTime;

    private void Start()
    {
        puzzleRenderer.NodeSelected += SelectNode;

        routeLine.color = Color.red;
        optimalRouteLine.color = Color.green;

        startButton.onClick.AddListener(StartGame);
        undoButton.onClick.AddListener(UndoMove);

        submitButton.onClick.AddListener(SubmitRoute);
        submitButton.gameObject.SetActive(false);

        nextPuzzleButton.onClick.AddListener(NextPuzzle);
        resultPanel.SetActive(false);

        undoButton.interactable = false;
        puzzleRenderer.SetSelectionEnabled(false);

        statusText.text = "Select START to Begin";
        timerText.text = "0.0";

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
        resultPanel.SetActive(false);
        selectedPath.Clear();
        routeLine.ClearLine();
        optimalRouteLine.ClearLine();

        selectedPath.Add(0);
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
    }

    private void SelectNode(int nodeIndex)
    {

        if (!gameRunning)
            return;

        if (selectedPath.Count == puzzleLoader.CurrentPuzzle.nodes.Count &&
        nodeIndex == selectedPath[0])
        {
            selectedPath.Add(nodeIndex);
            UpdateRouteLine();
            UpdatePathText();

            gameRunning = false;
            puzzleRenderer.SetSelectionEnabled(false);
            undoButton.interactable = false;

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
        //if (!gameRunning || selectedPath.Count == 0)
        //    return;
        if (!gameRunning || selectedPath.Count <= 1)
                return;

        selectedPath.RemoveAt(selectedPath.Count - 1);
    //    undoButton.interactable = selectedPath.Count > 0;
        undoButton.interactable = selectedPath.Count > 1;
        UpdateRouteLine();

        if (selectedPath.Count == 0)
            statusText.text = "Select your starting node";
        else
            UpdatePathText();
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

    float errorPercentage = 0f;

    if (optimalLength > 0f)
    {
        errorPercentage =
            ((playerLength - optimalLength) / optimalLength) * 100f;
    }

    // Prevent tiny rounding errors from displaying a negative percentage.
    errorPercentage = Mathf.Max(0f, errorPercentage);

    resultsPanelText.text = "RESULTS";

    resultsStatsText.text =
        $"Optimal Path: {optimalLength:F2}\n" +
        $"Your Path: {playerLength:F2}\n" +
        $"Error: {errorPercentage:F2}%\n" +
        $"Time: {elapsedTime:F1} seconds";

    if (errorPercentage < 0.01f)
    {
        resultsMessageText.text =
            "Good going, you selected the optimal path!";
    }
    else
    {
        resultsMessageText.text =
            "Good try! See if you can get closer next time.";
    }

    resultPanel.SetActive(true);
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
    submitButton.gameObject.SetActive(false);

    ShowOptimalRoute();
    ShowResults();
}
    
    private void NextPuzzle()
{
    gameRunning = false;
    elapsedTime = 0f;

    selectedPath.Clear();

    routeLine.ClearLine();
    optimalRouteLine.ClearLine();

    resultPanel.SetActive(false);

    submitButton.gameObject.SetActive(false);

    puzzleRenderer.SetSelectionEnabled(false);

    puzzleLoader.LoadNextPuzzle();
    puzzleRenderer.RefreshPuzzle();

    timerText.text = "0.0";
    statusText.text = "Select START to Begin";

    startButton.interactable = true;
    undoButton.interactable = false;
}
    private void OnDestroy()
    {
        if (puzzleRenderer != null)
            puzzleRenderer.NodeSelected -= SelectNode;

        if (submitButton != null)
            submitButton.onClick.RemoveListener(SubmitRoute);
    }
}