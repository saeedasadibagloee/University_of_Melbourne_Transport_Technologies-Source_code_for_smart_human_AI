using UnityEngine;
using System.Collections;
using OxOD;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Events;

public class FileSelector : MonoBehaviour
{
    [Header("OxOD Reference")]
    public FileDialog dialog;

    [Header("File Dialog Options")]
    public FileDialog.FileDialogMode mode;
    public string extensions;
    public int maxSize = -1;
    public bool saveLastPath = true;

    [Header("Events")]
    public UnityEvent OnFileSelected;

    [Header("Internal References")]
    public InputField selectedFile;

    [HideInInspector]
    public string result;

    public void SelectFile()
    {
        StartCoroutine(Select(result));
    }

    public IEnumerator Select(string path)
    {
        Debug.Log("[FileSelector] Starting file dialog");

        if (mode == FileDialog.FileDialogMode.Open)
        {
            yield return StartCoroutine(dialog.Open(path, extensions, "OPEN FILE", null, maxSize, saveLastPath));
        }
        else
        {
            yield return StartCoroutine(dialog.Save(path, extensions, "SAVE FILE", null, saveLastPath));
        }

        if (dialog.result != null)
        {
            Debug.Log("[FileSelector] Dialog ended, result: " + dialog.result);

            result = dialog.result;
            selectedFile.text = new FileInfo(dialog.result).Name;

            OnFileSelected.Invoke();
        }
        else
        {
            Debug.Log("[FileSelector] Dialog canceled");
        }
    }
}
