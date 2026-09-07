using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TspPuzzleLoader : MonoBehaviour
{

    [SerializeField] private TspPuzzleRenderer puzzleRenderer;
    [SerializeField] private TMP_Dropdown nodeCountDropdown;
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

    private readonly List<int> availableNodeCounts =
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

        ConfigureNodeCountDropdown();
    }

    private void ConfigureNodeCountDropdown()
    {
        availableNodeCounts.Clear();
        availableNodeCounts.AddRange(GetAvailableNodeCounts());

        if (availableNodeCounts.Count == 0)
        {
            Debug.LogError("No valid puzzles were found in puzzles.json.");
            return;
        }

        if (nodeCountDropdown == null)
        {
            Debug.LogError(
                "Node Count Dropdown is not assigned on TspPuzzleLoader."
            );

            SelectNodeCount(availableNodeCounts[0]);
            return;
        }

        List<string> options = new();

        foreach (int nodeCount in availableNodeCounts)
            options.Add(nodeCount.ToString());

        nodeCountDropdown.ClearOptions();
        nodeCountDropdown.AddOptions(options);
        nodeCountDropdown.SetValueWithoutNotify(0);
        nodeCountDropdown.RefreshShownValue();

        SelectNodeCount(availableNodeCounts[0]);
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

    public void OnNodeCountDropdownChanged(int optionIndex)
    {
        if (optionIndex < 0 ||
            optionIndex >= availableNodeCounts.Count)
        {
            Debug.LogWarning(
                $"Invalid node-count dropdown index: {optionIndex}."
            );
            return;
        }

        int selectedNodeCount = availableNodeCounts[optionIndex];

        if (SelectNodeCount(selectedNodeCount) &&
            puzzleRenderer != null)
        {
            puzzleRenderer.RefreshPuzzle();
        }
    }
}
