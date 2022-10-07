using System.Collections.Generic;
using UnityEngine;

public class GridDisplay : MonoBehaviour
{
    private BoxCollider _boxCollider;
    private LineRenderer _line;
    private const float Offset = 0.01f;
    private const float Width = 0.15f;
    private const float ExtraColliderScale = 20f;

    public void Initialise()
    {
        _line = gameObject.AddComponent<LineRenderer>();
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.material = (Material)Resources.Load("GridMaterial", typeof(Material));
        _line.startWidth = Width;
        _line.endWidth = Width;
    }

    public void ChangeLineWidth(float width)
    {
        _line.startWidth = width;
        _line.endWidth = width;
    }

    public void MakeGrid(int width, int height, float yPos)
    {
        Vector3[] positions = FixLine(GenerateGrid(width, height, yPos + Offset));
        _line.positionCount = positions.Length;
        _line.SetPositions(positions);
        _line.enabled = true;

        _boxCollider = gameObject.AddComponent<BoxCollider>();
        _boxCollider.size *= ExtraColliderScale;
        _boxCollider.size = new Vector3(_boxCollider.size.x, 0, _boxCollider.size.z);
    }

    public void ToggleGridDisplay(bool lineDisplay)
    {
        _line.enabled = lineDisplay;
    }

    private static Vector3[] GenerateGrid(int width, int height, float yPos)
    {
        List<Vector3> grid = new List<Vector3>();

        if (height * width == 0)
        {
            Debug.LogError("Cannot create a zero size grid.");
            return null;
        }

        for (int i = 0; i <= width; i++)
        {
            if (i % 2 == 0)
            {
                grid.Add(new Vector3(i, yPos, 0));
                grid.Add(new Vector3(i, yPos, height));
            }
            else
            {
                grid.Add(new Vector3(i, yPos, height));
                grid.Add(new Vector3(i, yPos, 0));
            }
        }

        if (width % 2 == 0)
            grid.Add(new Vector3(0, yPos, height));


        for (int i = 0; i <= height; i++)
        {
            if (i % 2 == 0)
            {
                grid.Add(new Vector3(0, yPos, i));
                grid.Add(new Vector3(width, yPos, i));
            }
            else
            {
                grid.Add(new Vector3(width, yPos, i));
                grid.Add(new Vector3(0, yPos, i));
            }
        }

        return grid.ToArray();
    }

    /// <summary>
    /// Basically it puts extra points before and after every positions, so the lines between them will be rendered correctly.
    /// See http://answers.unity3d.com/questions/909428/linerenderer-end-width-bug.html
    /// </summary>
    /// <param name="original"></param>
    /// <returns>New fixed Vector3 array.</returns>
    private static Vector3[] FixLine(Vector3[] original)
    {
        if(original.Length < 2)
        {
            Debug.LogError("Length of line to fix is " + original.Length + " long.");
        }
        Vector3[] res = new Vector3[original.Length * 3 - 2];
        for (int i = 0; i < res.Length; i++)
        {
            if (i % 3 == 0)
            {
                res[i] = original[i / 3];
            }
            else if (i % 3 == 1)
            {
                res[i] = Vector3.Lerp(original[(i - 1) / 3], original[(i + 2) / 3], 0.0001f);
            }
            else if (i % 3 == 2)
            {
                res[i] = Vector3.Lerp(original[(i + 1) / 3], original[(i - 2) / 3], 0.0001f);
            }
        }
        return res;
    }
}