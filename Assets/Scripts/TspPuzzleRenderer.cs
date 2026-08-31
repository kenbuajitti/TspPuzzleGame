using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TspPuzzleRenderer : MonoBehaviour
{
    private bool selectionEnabled;

    [SerializeField] private TspRouteLine routeLine;
    [SerializeField] private TspPuzzleLoader puzzleLoader;
    [SerializeField] private RectTransform puzzleArea;
    [SerializeField] private Button nodeButtonPrefab;

    [SerializeField] private float horizontalPadding = 70f;
    [SerializeField] private float verticalPadding = 55f;
    [SerializeField] private float nodeLabelFontSize = 24f;
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
            Debug.LogError(
                "Puzzle Loader has not been assigned."
            );
            yield break;
        }

        if (puzzleArea == null)
        {
            Debug.LogError(
                "Puzzle Area has not been assigned."
            );
            yield break;
        }

        if (nodeButtonPrefab == null)
        {
            Debug.LogError(
                "Node Button Prefab has not been assigned."
            );
            yield break;
        }

        DisplayPuzzle(puzzleLoader.CurrentPuzzle);
    }

    public void RefreshPuzzle()
    {
        selectionEnabled = false;

        foreach (RectTransform nodeTransform in nodeTransforms)
        {
            if (nodeTransform != null)
                Destroy(nodeTransform.gameObject);
        }

        nodeTransforms.Clear();

        if (puzzleLoader.CurrentPuzzle == null)
        {
            Debug.LogError(
                "There is no current puzzle to display."
            );
            return;
        }

        DisplayPuzzle(puzzleLoader.CurrentPuzzle);
    }

    private void DisplayPuzzle(TspPuzzleData puzzle)
    {
        if (puzzle == null ||
            puzzle.nodes == null ||
            puzzle.nodes.Count == 0)
        {
            Debug.LogError(
                "The selected puzzle contains no nodes."
            );
            return;
        }

        Canvas.ForceUpdateCanvases();

        float boardWidth = puzzleArea.rect.width;
        float boardHeight = puzzleArea.rect.height;

        for (int i = 0; i < puzzle.nodes.Count; i++)
        {
            TspNodeData node = puzzle.nodes[i];

            Button nodeButton =
                Instantiate(nodeButtonPrefab, puzzleArea);

            nodeButton.name = $"Node_{i}";

            int nodeIndex = i;

            nodeButton.interactable = true;

            /*nodeButton.onClick.AddListener(() =>
            {
                if (selectionEnabled)
                    NodeSelected?.Invoke(nodeIndex);
            });
            */

            EventTrigger trigger =
                nodeButton.gameObject.GetComponent<EventTrigger>();

            if (trigger == null)
              trigger = nodeButton.gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry pointerDownEntry =
                 new EventTrigger.Entry
                {
                      eventID = EventTriggerType.PointerDown
                };

            pointerDownEntry.callback.AddListener(_ =>
            {
                  if (selectionEnabled)
                       NodeSelected?.Invoke(nodeIndex);
            });

            trigger.triggers.Add(pointerDownEntry);

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

            RectTransform nodeTransform =
                nodeButton.GetComponent<RectTransform>();

            nodeTransform.anchoredPosition =
                new Vector2(x, y);

            nodeTransforms.Add(nodeTransform);

            TMP_Text label =
                nodeButton.transform
                    .Find("NodeLabel")
                    .GetComponent<TMP_Text>();

  /*          label.text =
                ((char)('A' + i)).ToString();
*/
              label.text =
                  ((char)('A' + i)).ToString();

            label.enableAutoSizing = false;
            label.fontSize = nodeLabelFontSize;

if (i == 0)
    label.color = Color.red;
        }

        Debug.Log(
            $"Displayed {puzzle.nodes.Count} puzzle nodes."
        );
    }

    public void SetSelectionEnabled(bool enabled)
    {
        selectionEnabled = enabled;
    }

    public Vector2 GetNodePosition(int nodeIndex)
    {
        if (nodeIndex < 0 ||
            nodeIndex >= nodeTransforms.Count)
        {
            return Vector2.zero;
        }

        return nodeTransforms[nodeIndex]
            .anchoredPosition;
    }
}