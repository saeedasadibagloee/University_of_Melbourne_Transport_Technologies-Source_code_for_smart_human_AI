using DataFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogPrefabrications : MonoBehaviour
{
    public GameObject fileDialog = null;
    public Text sizeText = null;

    public List<Button> buttons = null;
    public List<Sprite> R1s = null;
    public List<Sprite> R2s = null;
    public List<Sprite> B1s = null;
    public List<Sprite> B2s = null;

    public int size = 1;
    public int type = 0;
    public int item = 0;

    private static DialogPrefabrications _dialogCarriageEditor;

    public static DialogPrefabrications Instance
    {
        get { return _dialogCarriageEditor ?? (_dialogCarriageEditor = FindObjectOfType<DialogPrefabrications>()); }
    }

    public bool IsActive()
    {
        return fileDialog.activeInHierarchy;
    }

    public void RefreshButtons()
    {
        if (type == 0) // Room prefabs
        {
            buttons[0].interactable = true;
            buttons[1].interactable = true;

            if (R1s.Count - 1 < size)
                Debug.LogError("Not enough sprites");
            else
                buttons[0].image.sprite = R1s[size];

            if (R2s.Count - 1 < size)
                Debug.LogError("Not enough sprites");
            else
                buttons[1].image.sprite = R2s[size];

            buttons[2].interactable = false;
            buttons[3].interactable = false;
            buttons[4].interactable = false;
            buttons[5].interactable = false;
            buttons[6].interactable = false;
            buttons[7].interactable = false;
        }
        else // Barricade prefabs
        {
            if (size == 0)
                return;

            buttons[0].interactable = true;
            buttons[1].interactable = true;

            buttons[0].image.sprite = B1s[size - 1];
            buttons[1].image.sprite = B2s[size - 1];

            buttons[2].interactable = false;
            buttons[3].interactable = false;
            buttons[4].interactable = false;
            buttons[5].interactable = false;
            buttons[6].interactable = false;
            buttons[7].interactable = false;
        }
    }

    public void Active(bool active, int type)
    {
        this.type = type;
        Active(active);
    }

    public void Active(bool active)
    {
        fileDialog.SetActive(active);
        if (!active) return;
        RefreshButtons();
    }

    public void UpdateSize(float size)
    {
        this.size = (int)size;
        if (this.size == 0 && type == 0)
            this.size = 1; //Barricades can't be 0 sized
        RefreshButtons();
        sizeText.text = this.size.ToString();
    }

    public void CloseDialog()
    {
        Active(false);
    }

    public void OkayButton()
    {
        CloseDialog();
    }

    public void ChooseItem(int item = 0)
    {
        this.item = item;
        CloseDialog();
    }

    public List<GameObject> GenerateSelectedPrefab(Vector3 loc)
    {
        List<GameObject> prefab = new List<GameObject>();
        //Debug.Log("Type: " + type + " Size: " + size + " Item: " + item);

        if (type == 0)
        {
            switch (item)
            {
                case 1:
                    prefab = GenerateRoom1(loc);
                    break;
                case 2:
                    prefab = GenerateRoom2(loc);
                    break;
            }
        }
        else
        {
            switch (item)
            {
                case 1:
                    prefab = GenerateBarricade1(loc);
                    break;
                case 2:
                    prefab = GenerateBarricade2(loc);
                    break;
            }
        }

        return prefab;
    }

    private List<GameObject> GenerateBarricade1(Vector3 loc)
    {
        List<GameObject> prefab = new List<GameObject>();

        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x, loc.z), new Vertex(loc.x, loc.z + size)), true));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x, loc.z), new Vertex(loc.x + size, loc.z)), true));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + size, loc.z + size), new Vertex(loc.x, loc.z + size)), true));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + size, loc.z + size), new Vertex(loc.x + size, loc.z)), true));

        return prefab;
    }

    private List<GameObject> GenerateBarricade2(Vector3 loc)
    {
        List<GameObject> prefab = new List<GameObject>();

        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x, loc.z), new Vertex(loc.x + size, loc.z + size)), true));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + size, loc.z + size), new Vertex(loc.x + (2*size), loc.z)), true));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + (2 * size), loc.z), new Vertex(loc.x + size, loc.z - size)), true));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + size, loc.z - size), new Vertex(loc.x, loc.z)), true));

        return prefab;
    }

    private List<GameObject> GenerateRoom1(Vector3 loc)
    {
        List<GameObject> prefab = new List<GameObject>();

        int r = 4 + (2 * size);

        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x, loc.z), new Vertex(loc.x + r, loc.z))));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x, loc.z + (2 * size) + 2), new Vertex(loc.x + r, loc.z + (2 * size) + 2))));

        prefab.Add(Create.Instance.GateToObject(new Gate(new Vertex(loc.x, loc.z + size), new Vertex(loc.x, loc.z + 2 + size))));
        prefab.Add(Create.Instance.GateToObject(new Gate(new Vertex(loc.x + r, loc.z + size), new Vertex(loc.x + r, loc.z + 2 + size))));

        if (size > 0)
        {
            prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x, loc.z), new Vertex(loc.x, loc.z + size))));
            prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x, loc.z + size + 2), new Vertex(loc.x, loc.z + (2 * size) + 2))));
            prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + r, loc.z), new Vertex(loc.x + r, loc.z + size))));
            prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + r, loc.z + size + 2), new Vertex(loc.x + r, loc.z + (2 * size) + 2))));
        }

        return prefab;
    }

    private List<GameObject> GenerateRoom2(Vector3 loc)
    {
        List<GameObject> prefab = new List<GameObject>();

        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x, loc.z),
                                                         new Vertex(loc.x, loc.z + 5 + (3 * size)))));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + (2 * size) + 2, loc.z),
                                                         new Vertex(loc.x + (2 * size) + 2, loc.z + 3 + size))));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + 5 + (3 * size), loc.z + 5 + (3 * size)),
                                                         new Vertex(loc.x, loc.z + 5 + (3 * size)))));
        prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + 5 + (3 * size), loc.z + 3 + size),
                                                         new Vertex(loc.x + (2 * size) + 2, loc.z + 3 + size))));
        prefab.Add(Create.Instance.GateToObject(new Gate(new Vertex(loc.x + size, loc.z), new Vertex(loc.x + size + 2, loc.z))));
        prefab.Add(Create.Instance.GateToObject(new Gate(new Vertex(loc.x + 5 + (3 * size), loc.z + 5 + (2 * size)), new Vertex(loc.x + 5 + (3 * size), loc.z + 3 + (2 * size)))));

        if (size > 0)
        {
            prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x, loc.z),
                                                             new Vertex(loc.x + size, loc.z))));
            prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + size + 2, loc.z),
                                                             new Vertex(loc.x + (2 * size) + 2, loc.z))));
            prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + 5 + (3 * size), loc.z + 3 + size),
                                                             new Vertex(loc.x + 5 + (3 * size), loc.z + 3 + (2 * size)))));
            prefab.Add(Create.Instance.WallToObject(new Wall(new Vertex(loc.x + 5 + (3 * size), loc.z + 5 + (2 * size)),
                                                             new Vertex(loc.x + 5 + (3 * size), loc.z + 5 + (3 * size)))));
        }

        return prefab;
    }
}
