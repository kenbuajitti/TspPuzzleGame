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

            statusText.text += "\nRoute complete!";
            ShowOptimalRoute();
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
    private void ShowOptimalRoute()
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


    private void OnDestroy()
    {
        if (puzzleRenderer != null)
            puzzleRenderer.NodeSelected -= SelectNode;
    }
}