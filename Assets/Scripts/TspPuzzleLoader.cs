using UnityEngine;
public class TspPuzzleLoader : MonoBehaviour
{
    public TspPuzzleData CurrentPuzzle { get; private set; }

    private void Start()
    {
        LoadPuzzleDatabase();
    }

    private void LoadPuzzleDatabase()
    {
        TextAsset puzzleFile = Resources.Load<TextAsset>("puzzles");

        if (puzzleFile == null)
        {
            Debug.LogError(
                "Could not load Assets/Resources/puzzles.json"
            );
            return;
        }

        TspPuzzleDatabase database =
            JsonUtility.FromJson<TspPuzzleDatabase>(puzzleFile.text);

        if (database == null ||
            database.puzzles == null ||
            database.puzzles.Count == 0)
        {
            Debug.LogError("The puzzle database is empty or invalid.");
            return;
        }

        CurrentPuzzle = database.puzzles[0];

        Debug.Log(
            $"Loaded puzzle {CurrentPuzzle.id} " +
            $"with {CurrentPuzzle.nodes.Count} nodes."
        );
    }
}