using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;


public class TspPuzzleRenderer : MonoBehaviour
{
    private bool selectionEnabled;
    [SerializeField] private TspRouteLine routeLine;
    [SerializeField] private TspPuzzleLoader puzzleLoader;
    [SerializeField] private RectTransform puzzleArea;
    [SerializeField] private Button nodeButtonPrefab;

    [SerializeField] private float horizontalPadding = 70f;
    [SerializeField] private float verticalPadding = 55f;

    public event Action<int> NodeSelected;
    private readonly List<RectTransform> nodeTransforms = new();

    private IEnumerator Start()
    {
        while (puzzleLoader != null &&
               puzzleLoader.CurrentPuzzle == null)
        {
            yield return null;
        }

        if (puzzleLoader == null)
        {
            Debug.LogError("Puzzle Loader has not been assigned.");
            yield break;
        }

        if (puzzleArea == null)
        {
            Debug.LogError("Puzzle Area has not been assigned.");
            yield break;
        }

        if (nodeButtonPrefab == null)
        {
            Debug.LogError("Node Button Prefab has not been assigned.");
            yield break;
        }

        DisplayPuzzle(puzzleLoader.CurrentPuzzle);
    }

    private void DisplayPuzzle(TspPuzzleData puzzle)
    {
        Canvas.ForceUpdateCanvases();

        float boardWidth = puzzleArea.rect.width;
        float boardHeight = puzzleArea.rect.height;

        for (int i = 0; i < puzzle.nodes.Count; i++)
        {
            TspNodeData node = puzzle.nodes[i];

            Button nodeButton =
                Instantiate(nodeButtonPrefab, puzzleArea);

            nodeButton.name = $"Node_{i}";
            // nodeButton.interactable = true;

            //====
            int nodeIndex = i;

            nodeButton.interactable = true;

            nodeButton.onClick.AddListener(() =>
            {
                if (selectionEnabled)
                    NodeSelected?.Invoke(nodeIndex);
            });
            //===

            float x = Mathf.Lerp(
                -boardWidth / 2f + horizontalPadding,
                boardWidth / 2f - horizontalPadding,
                node.x / 100f
            );

            float y = Mathf.Lerp(
                -boardHeight / 2f + verticalPadding,
                boardHeight / 2f - verticalPadding,
                node.y / 100f
            );

            nodeButton.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(x, y);

            nodeTransforms.Add(nodeButton.GetComponent<RectTransform>());

            /*TMP_Text label =
                nodeButton.GetComponentInChildren<TMP_Text>();

            label.text =
                $"{(char)('A' + i)}\n" +
                $"({node.x:F2}, {node.y:F2})";
            */
            TMP_Text label =
                nodeButton.transform.Find("NodeLabel")
                .GetComponent<TMP_Text>();

            label.text = ((char)('A' + i)).ToString();
        }

        Debug.Log($"Displayed {puzzle.nodes.Count} puzzle nodes.");
    
    }

    public void SetSelectionEnabled(bool enabled)
        { 
        selectionEnabled = enabled;
        } 

public Vector2 GetNodePosition(int nodeIndex)
    {
    if (nodeIndex < 0 || nodeIndex >= nodeTransforms.Count)
        return Vector2.zero;

    return nodeTransforms[nodeIndex].anchoredPosition;
    }

}