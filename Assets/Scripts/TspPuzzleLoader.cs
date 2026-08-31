using System.Collections.Generic;
using UnityEngine;

public class TspPuzzleLoader : MonoBehaviour
{

    [SerializeField] private TspPuzzleRenderer puzzleRenderer;
    public TspPuzzleData CurrentPuzzle { get; private set; }

    public int CurrentNodeCount
    {
        get
        {
            if (CurrentPuzzle == null ||
                CurrentPuzzle.nodes == null)
            {
                return 0;
            }

            return CurrentPuzzle.nodes.Count;
        }
    }

    private TspPuzzleDatabase database;

    private readonly List<TspPuzzleData> matchingPuzzles =
        new();

    private int currentPuzzleIndex;

    private void Start()
    {
        LoadPuzzleDatabase();
    }

    private void LoadPuzzleDatabase()
    {
        TextAsset puzzleFile =
            Resources.Load<TextAsset>("puzzles");

        if (puzzleFile == null)
        {
            Debug.LogError(
                "Could not load Assets/Resources/puzzles.json"
            );
            return;
        }

        database =
            JsonUtility.FromJson<TspPuzzleDatabase>(
                puzzleFile.text
            );

        if (database == null ||
            database.puzzles == null ||
            database.puzzles.Count == 0)
        {
            Debug.LogError(
                "The puzzle database is empty or invalid."
            );
            return;
        }

        int initialNodeCount =
            database.puzzles[0].nodes.Count;

        SelectNodeCount(initialNodeCount);
    }

    public List<int> GetAvailableNodeCounts()
    {
        List<int> nodeCounts = new();

        if (database == null ||
            database.puzzles == null)
        {
            return nodeCounts;
        }

        foreach (TspPuzzleData puzzle in database.puzzles)
        {
            if (puzzle == null || puzzle.nodes == null)
                continue;

            int nodeCount = puzzle.nodes.Count;

            if (!nodeCounts.Contains(nodeCount))
                nodeCounts.Add(nodeCount);
        }

        nodeCounts.Sort();

        return nodeCounts;
    }

    public bool SelectNodeCount(int nodeCount)
    {
        matchingPuzzles.Clear();

        if (database == null ||
            database.puzzles == null)
        {
            return false;
        }

        foreach (TspPuzzleData puzzle in database.puzzles)
        {
            if (puzzle != null &&
                puzzle.nodes != null &&
                puzzle.nodes.Count == nodeCount)
            {
                matchingPuzzles.Add(puzzle);
            }
        }

        if (matchingPuzzles.Count == 0)
        {
            Debug.LogWarning(
                $"No puzzles contain {nodeCount} nodes."
            );

            return false;
        }

        currentPuzzleIndex = 0;
        SetCurrentPuzzle();

        return true;
    }

    public void LoadNextPuzzle()
    {
        if (matchingPuzzles.Count == 0)
        {
            Debug.LogError(
                "No puzzles match the selected node count."
            );
            return;
        }

        currentPuzzleIndex++;

        if (currentPuzzleIndex >= matchingPuzzles.Count)
            currentPuzzleIndex = 0;

        SetCurrentPuzzle();
    }

    private void SetCurrentPuzzle()
    {
        CurrentPuzzle =
            matchingPuzzles[currentPuzzleIndex];

        Debug.Log(
            $"Loaded puzzle {CurrentPuzzle.id} with " +
            $"{CurrentPuzzle.nodes.Count} nodes. " +
            $"Puzzle {currentPuzzleIndex + 1} of " +
            $"{matchingPuzzles.Count} at this difficulty."
        );
    }

    /* public void OnNodeCountDropdownChanged(int optionIndex)
    {
        int selectedNodeCount = optionIndex + 9;

        SelectNodeCount(selectedNodeCount);
    }
    */

    public void OnNodeCountDropdownChanged(int optionIndex)
{
    int selectedNodeCount = optionIndex + 9;

    if (SelectNodeCount(selectedNodeCount))
    {
        puzzleRenderer.RefreshPuzzle();
    }
}
}