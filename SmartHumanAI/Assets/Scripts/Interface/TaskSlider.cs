using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class TaskSlider : MonoBehaviour
{

    public enum Direction { Left, Right }

    public RawImage background;
    public Direction SliderSide = Direction.Left;

    private float SlideSpeed = 0.1f;
    private float SlideAmount = 140f;
    private bool IsOut = false;

    private float startTime;
    private bool isSliding = false;
    private Vector2 _outPos;
    private Vector2 _inPos;
    private Color outColor = new Color(0.95f, 0.95f, 0.95f, 0f);
    private Color inColor = new Color(0.95f, 0.95f, 0.95f, 0.58f);
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (IsOut)
        {
            _outPos = rectTransform.anchoredPosition;
            _inPos = _outPos - SlideAmount * new Vector2(SliderSide == Direction.Right ? -1 : 1, 0);
        }
        else
        {
            _inPos = rectTransform.anchoredPosition;
            _outPos = _inPos - SlideAmount * new Vector2(SliderSide == Direction.Right ? 1 : -1, 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isSliding)
        {
            float fracComplete = (Time.time - startTime) / SlideSpeed;
            
            if (IsOut)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(_outPos, _inPos, fracComplete);
                background.color = Color.Lerp(outColor, inColor, fracComplete);
            }
            else
            {
                rectTransform.anchoredPosition = Vector2.Lerp(_inPos, _outPos, fracComplete);
                background.color = Color.Lerp(inColor, outColor, fracComplete);
            }

            if (!(fracComplete > 1.0f)) return;
            isSliding = false;
            IsOut = !IsOut;
            if (!IsOut)
                background.raycastTarget = true;
            else
                background.raycastTarget = false;
        }
    }

    public void SlideOut()
    {
        if (isSliding || IsOut) return;
        if (Input.GetMouseButton(1)) return;
        isSliding = true;
        startTime = Time.time;
        UIController.Instance.CloseAllMenus();
    }

    public void SlideIn()
    {
        if (isSliding || !IsOut) return;
        isSliding = true;
        startTime = Time.time;
    }
}
