using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using TMPro;
using System;
using I2.Loc;

public class TooltipController : MonoBehaviour
{
    public GameObject tooltip;
    public bool isRightSide = false;

    private const float yPos = -44.19995f;
    private const float buttonHeight = 40f;
    private const float transitionTime = 0.5f;
    
    private int buttonId = 0;
    private bool isTransitioning = false;
    private float startTransition = 0f;

    private Vector2 startPos;
    private Vector2 targetPos;
    public float smoothTime = 0.2f;
    private Vector2 velocity = Vector2.zero;

    private List<string> buttonTextsL = new List<string>() { "Create", "Level", "Stairs", "Agents", "Items", "Danger", "Heatmap", "General", "Agent", "Forces", "Choices", "Collisions", "Decisions", "Reactions", "Behaviour", "Fire" };
    private List<string> buttonTextsR = new List<string>() { "Run Sim", "Playback" };
    private bool startHidingTooltip = false;
    private bool startShowingTooltip = false;
    private bool isVisible = false;

    public void Start()
    {
        if (tooltip == null)
            return;

        startPos = tooltip.GetComponent<RectTransform>().anchoredPosition;
    }

    public void Update()
    {
        
    public void HideTooltop()
    {
        startHidingTooltip = true;
    }
}
