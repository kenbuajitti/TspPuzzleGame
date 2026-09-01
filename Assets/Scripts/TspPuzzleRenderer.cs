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
    [SerializeField] private float touchRadius = 70f;

    public event Action<int> NodeSelected;

    private readonly List<RectTransform> nodeTransforms = new();
    private readonly HashSet<int> selectedNodes = new();

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

        ConfigurePuzzleAreaInput();
        DisplayPuzzle(puzzleLoader.CurrentPuzzle);
    }

    private void ConfigurePuzzleAreaInput()
    {
        Graphic inputGraphic = puzzleArea.GetComponent<Graphic>();

        if (inputGraphic == null)
        {
            Image transparentImage =
                puzzleArea.gameObject.AddComponent<Image>();

            transparentImage.color = Color.clear;
            inputGraphic = transparentImage;
        }

        inputGraphic.raycastTarget = true;

        EventTrigger trigger =
            puzzleArea.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = puzzleArea.gameObject.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new List<EventTrigger.Entry>();

        trigger.triggers.RemoveAll(
            entry => entry.eventID == EventTriggerType.PointerDown
        );

        EventTrigger.Entry pointerDownEntry =
            new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };

        pointerDownEntry.callback.AddListener(
            HandlePuzzleAreaPointerDown
        );

        trigger.triggers.Add(pointerDownEntry);
    }

    private void HandlePuzzleAreaPointerDown(
        BaseEventData eventData)
    {
        if (!selectionEnabled ||
            nodeTransforms.Count == 0)
        {
            return;
        }

        PointerEventData pointerData =
            eventData as PointerEventData;

        if (pointerData == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                puzzleArea,
                pointerData.position,
                pointerData.pressEventCamera,
                out Vector2 tapPosition))
        {
            return;
        }

        int closestNodeIndex = -1;
        float closestDistanceSquared = touchRadius * touchRadius;

        for (int i = 0; i < nodeTransforms.Count; i++)
        {
            if (!IsNodeAvailable(i))
                continue;

            float distanceSquared =
                (nodeTransforms[i].anchoredPosition - tapPosition)
                .sqrMagnitude;

            if (distanceSquared <= closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestNodeIndex = i;
            }
        }

        if (closestNodeIndex >= 0)
            NodeSelected?.Invoke(closestNodeIndex);
    }

    private bool IsNodeAvailable(int nodeIndex)
    {
        if (!selectedNodes.Contains(nodeIndex))
            return true;

        // A becomes selectable again only after every node is used.
        return nodeIndex == 0 &&
               selectedNodes.Count == nodeTransforms.Count;
    }

    public void RefreshPuzzle()
    {
        selectionEnabled = false;
        selectedNodes.Clear();

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

            nodeButton.interactable = true;

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

            // The puzzle area handles taps so overlapping node hit areas
            // can be resolved by distance and availability.
            foreach (Graphic graphic in
                     nodeButton.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

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

    public void SetSelectedPath(IEnumerable<int> selectedPath)
    {
        selectedNodes.Clear();

        if (selectedPath == null)
            return;

        foreach (int nodeIndex in selectedPath)
            selectedNodes.Add(nodeIndex);
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
