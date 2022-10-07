using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DataFormats;

public class ColorPicker : MonoBehaviour {
	
	public bool isDraggable = false;
	public Color pickedColor = Color.white;
	public GameObject pickerObj;
    public RawImage colorLeft;
    public RawImage colorRight;
	[Range(0, 255)]
	public int alphaTolerancy = 0;
	
	void Start(){
		Color actColor = GetComponent<Image> ().sprite.texture.GetPixel (Mathf.RoundToInt ((pickerObj.transform.position.x - transform.position.x)*(1/GetComponent<RectTransform>().localScale.x)*(1/GetComponentInParent<Canvas>().scaleFactor)), Mathf.RoundToInt ((pickerObj.transform.position.y - transform.position.y)*(1/GetComponent<RectTransform>().localScale.y)*(1/GetComponentInParent<Canvas>().scaleFactor)) + GetComponent<Image> ().sprite.texture.height);
		if (actColor.a >= ((255 - alphaTolerancy) / 255f)) {
			pickedColor = actColor;
		}

        if (colorLeft != null)
            colorLeft.color = Consts.defaultButtonColor;

        if (colorRight != null)
            colorRight.color = Consts.defaultButtonColor;
    }
	
	public void OnEnter(){
		isDraggable = true;
	}
	
	public void OnExit(){
		isDraggable = false;
	}
	
	public void OnClick()
	{ 
		if (isDraggable) {
			Color actColor = GetComponent<Image> ().sprite.texture.GetPixel (Mathf.RoundToInt ((Input.mousePosition.x - transform.position.x) * (1 / GetComponent<RectTransform> ().localScale.x) * (1 / GetComponentInParent<Canvas> ().scaleFactor)), Mathf.RoundToInt ((Input.mousePosition.y - transform.position.y) * (1 / GetComponent<RectTransform> ().localScale.y) * (1 / GetComponentInParent<Canvas> ().scaleFactor)) + GetComponent<Image> ().sprite.texture.height);
			if (actColor.a >= ((255 - alphaTolerancy) / 255f)) {
				pickedColor = actColor;
				pickerObj.transform.position = Input.mousePosition;
			}
		}

        if (colorLeft != null)
            colorLeft.color = pickedColor;

        if (colorRight != null)
            colorRight.color = pickedColor;
    }
	
}
