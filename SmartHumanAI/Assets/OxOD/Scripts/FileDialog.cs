using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataFormats;
using UnityEngine;
using UnityEngine.UI;

namespace OxOD
{
    public class FileDialog : MonoBehaviour
    {
        public enum FileDialogMode { Open, Save }

        [HideInInspector]
        public string result;
        [HideInInspector]
        FileDialogMode mode;
        [HideInInspector]
        public bool finished;

        [Header("References")]
        public Image windowIcon;
        public Text windowName;
        public InputField currentPath;
        public InputField fileName;
        public Button up;
        public Button commit;
        public Button cancel;
        public GameObject filesScrollRectContent;
        public GameObject drivesScrollRectContent;

        [Header("Lists Prefabs")]
        public GameObject filesScrollRectElement;
        public GameObject drivesScrollRectElement;

        [Header("Lists Icons")]
        public Sprite folderIcon;
        public Sprite fileIcon;

        private string workingPath;
        private string workingFile;
        private string[] allowedExtensions;
        private long maxSize = -1;
        private bool saveLastPath = true;
        private enum Sortby { Name, Date }
        private Sortby sortMethod = Sortby.Name;

        public IEnumerator Open(string path = null, string allowedExtensions = null, string windowName = "OPEN FILE", Sprite windowIcon = null, long maxSize = -1, bool saveLastPath = true)
        {
            mode = FileDialogMode.Open;
            commit.GetComponentInChildren<Text>().text = "OPEN";
            fileName.text = "";
            workingPath = "";
            workingFile = "";
            result = null;
            finished = false;
            this.maxSize = maxSize;
            this.saveLastPath = saveLastPath;

            if (!string.IsNullOrEmpty(allowedExtensions))
            {
                allowedExtensions = allowedExtensions.ToLower();
                this.allowedExtensions = allowedExtensions.Split('|');
            }

            if (string.IsNullOrEmpty(path))
                path = saveLastPath ? (string.IsNullOrEmpty(PlayerPrefs.GetString("OxOD.lastPath", null)) ? Application.dataPath + "/../" : PlayerPrefs.GetString("OxOD.lastPath", null)) : Application.dataPath + "/../";

            this.windowName.text = windowName;

            if (windowIcon)
                this.windowIcon.sprite = windowIcon;

            GoTo(path);
            gameObject.SetActive(true);

            SetDialogSize();

            while (!finished)
                yield return new WaitForSeconds(0.1f);
        }

        public IEnumerator Save(string path = null, string allowedExtensions = null, string windowName = "SAVE FILE", Sprite windowIcon = null, bool saveLastPath = true)
        {
            mode = FileDialogMode.Save;
            commit.GetComponentInChildren<Text>().text = "SAVE";
            fileName.text = "";
            workingPath = "";
            workingFile = "";
            result = null;
            finished = false;
            maxSize = -1;
            this.saveLastPath = saveLastPath;

            if (!string.IsNullOrEmpty(allowedExtensions))
            {
                allowedExtensions = allowedExtensions.ToLower();
                this.allowedExtensions = allowedExtensions.Split('|');
            }

            if (string.IsNullOrEmpty(path))
                path = saveLastPath ? (string.IsNullOrEmpty(PlayerPrefs.GetString("OxOD.lastPath", null)) ? Application.dataPath + "/../" : PlayerPrefs.GetString("OxOD.lastPath", null)) : Application.dataPath + "/../";

            this.windowName.text = windowName;

            if (windowIcon)
                this.windowIcon.sprite = windowIcon;

            GoTo(path);
            gameObject.SetActive(true);

            SetDialogSize();

            while (!finished)
                yield return new WaitForSeconds(0.1f);
        }

        private void SetDialogSize()
        {
            if (Camera.main.scaledPixelHeight > Consts.UIScaleHeighChange2)
                transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(Screen.width * 0.25f, 620), Screen.height * 0.4f);
            else if (Camera.main.scaledPixelHeight > Consts.UIScaleHeighChange1)
                transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(Screen.width * 0.375f, 620), Screen.height * 0.6f);
            else
                transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(Screen.width * 0.5f, 620), Screen.height * 0.8f);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        public void GoUp()
        {
            OpenDir(workingPath + "/../");
        }

        public void GoTo(string newPath)
        {
            if (new DirectoryInfo(newPath).Exists)
                OpenDir(newPath + "/");
            else
            {
                if (mode == FileDialogMode.Open)
                {
                    if (new FileInfo(newPath).Exists)
                    {
                        OpenDir(new FileInfo(newPath).Directory.FullName + "/");
                        SelectFile(newPath);
                    }
                    else
                    {
                        OpenDir(Application.dataPath + "/../");
                    }
                }
                else
                {

                    if (new DirectoryInfo(new FileInfo(newPath).Directory.FullName + "/").Exists)
                    {
                        OpenDir(new FileInfo(newPath).Directory.FullName + "/");
                        SelectFile(newPath);
                    }
                    else
                    {
                        OpenDir(Application.dataPath + "/../");
                    }
                }
            }
        }

        public void SelectFile(string file)
        {
            if (mode == FileDialogMode.Open)
                workingFile = Path.GetFullPath(file);
            else
                workingFile = new FileInfo(Path.GetFullPath(file)).Name;
            UpdateFileInfo();
        }

        public void OnCommitClick()
        {
            if (mode == FileDialogMode.Open)
                result = Path.GetFullPath(workingFile);
            else
                result = Path.GetFullPath(workingPath + "/" + workingFile);
            finished = true;

            if (saveLastPath)
                PlayerPrefs.SetString("OxOD.lastPath", workingPath);
            Hide();
        }

        public void OnCancelClick()
        {
            result = null;
            finished = true;
            Hide();
        }

        public void ClearSelection()
        {
            if (mode == FileDialogMode.Open)
            {
                workingFile = "";
                UpdateFileInfo();
            }
        }

        public void OnTypedFilename(string newName)
        {
            if (mode == FileDialogMode.Open)
                workingFile = workingPath + "/" + newName;
            else
                workingFile = newName;
            UpdateFileInfo();
        }

        public void OnTypedEnd(string newName)
        {
            if (mode == FileDialogMode.Save)
            {
                if (allowedExtensions != null)
                {
                    if (allowedExtensions.Contains(new FileInfo(workingFile).Extension.ToLower()))
                        workingFile = newName;
                    else
                        workingFile = newName + allowedExtensions[0];
                }
                else
                    workingFile = newName;
            }
            UpdateFileInfo();
        }

        public void UpdateFileInfo()
        {
            if (mode == FileDialogMode.Open)
            {
                try
                {
                    fileName.text = new FileInfo(workingFile).Name;
                    commit.interactable = File.Exists(workingFile);
                }
                catch (Exception)
                {
                    fileName.text = "";
                    commit.interactable = false;
                }
            }
            else
            {
                if (workingFile.Length > 0)
                    fileName.text = new FileInfo(workingFile).Name;
                commit.interactable = workingFile.Length > 0 ? true : false;
            }
        }

        public void OpenDir(string path)
        {
            ClearSelection();
            workingPath = Path.GetFullPath(path);
            UpdateElements();
            UpdateDrivesList();
            UpdateFilesList();
        }

        public void OnNameClick()
        {
            sortMethod = Sortby.Name;
            UpdateFilesList();
        }

        public void OnDateClick()
        {
            sortMethod = Sortby.Date;
            UpdateFilesList();
        }

        private void UpdateElements()
        {
            currentPath.text = workingPath;
        }

        private void UpdateDrivesList()
        {
            GameObject target = drivesScrollRectContent;
            for (int i = 0; i < target.transform.childCount; i++)
            {
                Destroy(target.transform.GetChild(i).gameObject);
            }

            string[] info = Directory.GetLogicalDrives();

            for (int i = 0; i < info.Length; i++)
            {
                GameObject obj = Instantiate(drivesScrollRectElement, Vector3.zero, Quaternion.identity);
                obj.transform.SetParent(target.transform, true);
                obj.transform.localScale = new Vector3(1, 1, 1);

                FileListElement element = obj.GetComponent<FileListElement>();
                element.instance = this;
                element.data = info[i];
                element.elementName.text = info[i];
                element.isFile = false;
            }
        }

        private string GetFileSizeText(long size)
        {
            string format = "#.##";
            if (size / 1024.0f < 1)
                return "< 1 KB";
            if (size / 1024.0f < 1024)
                return "" + (size / 1024.0f).ToString(format) + " KB";
            if (size / 1024.0f / 1024.0f < 1024)
                return "" + (size / 1024.0f / 1024.0f).ToString(format) + " MB";
            return "" + (size / 1024.0f / 1024.0f / 1024.0f).ToString(format) + " GB";
        }

        private void UpdateFilesList()
        {
            GameObject target = filesScrollRectContent;
            for (int i = 0; i < target.transform.childCount; i++)
            {
                Destroy(target.transform.GetChild(i).gameObject);
            }

            DirectoryInfo dir = new DirectoryInfo(workingPath);
            try
            {

                DirectoryInfo[] info = dir.GetDirectories();

                for (int i = 0; i < info.Length; i++)
                {
                    GameObject obj = Instantiate(filesScrollRectElement, Vector3.zero, Quaternion.identity);
                    obj.transform.SetParent(target.transform, true);
                    obj.transform.localScale = new Vector3(1, 1, 1);

                    FileListElement element = obj.GetComponent<FileListElement>();
                    element.instance = this;
                    element.data = info[i].FullName + "/";
                    element.elementName.text = info[i].Name;
                    element.size.text = "";
                    element.date.text = GetDateText(info[i].LastWriteTime);
                    element.icon.sprite = folderIcon;
                    element.isFile = false;
                }

                if (allowedExtensions != null)
                {
                    FileInfo[] fileinfo = new FileInfo[0];

                    switch (sortMethod)
                    {
                        case Sortby.Date:
                            fileinfo = dir.GetFiles().Where(f => allowedExtensions.Contains(f.Extension.ToLower())).OrderByDescending(o => o.LastWriteTime).ToArray();
                            break;
                        case Sortby.Name:
                            fileinfo = dir.GetFiles().Where(f => allowedExtensions.Contains(f.Extension.ToLower())).ToArray();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("sortMethod", sortMethod, null);
                    }

                    for (int i = 0; i < fileinfo.Length; i++)
                    {
                        if (maxSize > 0)
                        {
                            if (fileinfo[i].Length < maxSize)
                            {
                                GameObject obj = Instantiate(filesScrollRectElement, Vector3.zero, Quaternion.identity);
                                obj.transform.SetParent(target.transform, true);
                                obj.transform.localScale = new Vector3(1, 1, 1);

                                FileListElement element = obj.GetComponent<FileListElement>();
                                element.instance = this;
                                element.data = fileinfo[i].FullName;
                                element.date.text = GetDateText(fileinfo[i].LastWriteTime);
                                element.size.text = GetFileSizeText(fileinfo[i].Length);
                                element.elementName.text = fileinfo[i].Name;
                                element.icon.sprite = fileIcon;
                                element.isFile = true;
                            }
                        }
                        else
                        {
                            GameObject obj = Instantiate(filesScrollRectElement, Vector3.zero, Quaternion.identity);
                            obj.transform.SetParent(target.transform, true);
                            obj.transform.localScale = new Vector3(1, 1, 1);

                            FileListElement element = obj.GetComponent<FileListElement>();
                            element.instance = this;
                            element.data = fileinfo[i].FullName;
                            element.size.text = GetFileSizeText(fileinfo[i].Length);
                            element.date.text = GetDateText(fileinfo[i].LastWriteTime);
                            element.elementName.text = fileinfo[i].Name;
                            element.icon.sprite = fileIcon;
                            element.isFile = true;
                        }
                    }
                }
                else
                {
                    FileInfo[] fileinfo = dir.GetFiles();

                    for (int i = 0; i < fileinfo.Length; i++)
                    {
                        if (maxSize > 0)
                        {
                            if (fileinfo[i].Length < maxSize)
                            {
                                GameObject obj = Instantiate(filesScrollRectElement, Vector3.zero, Quaternion.identity);
                                obj.transform.SetParent(target.transform, true);
                                obj.transform.localScale = new Vector3(1, 1, 1);

                                FileListElement element = obj.GetComponent<FileListElement>();
                                element.instance = this;
                                element.data = fileinfo[i].FullName;
                                element.size.text = GetFileSizeText(fileinfo[i].Length);
                                element.elementName.text = fileinfo[i].Name;
                                element.icon.sprite = fileIcon;
                                element.isFile = true;
                            }
                        }
                        else
                        {
                            GameObject obj = Instantiate(filesScrollRectElement, Vector3.zero, Quaternion.identity);
                            obj.transform.SetParent(target.transform, true);
                            obj.transform.localScale = new Vector3(1, 1, 1);

                            FileListElement element = obj.GetComponent<FileListElement>();
                            element.instance = this;
                            element.data = fileinfo[i].FullName;
                            element.size.text = GetFileSizeText(fileinfo[i].Length);
                            element.elementName.text = fileinfo[i].Name;
                            element.icon.sprite = fileIcon;
                            element.isFile = true;
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private string GetDateText(DateTime dt)
        {
            return dt.Day.ToString("00") + "/" + dt.Month.ToString("00") + "/" + dt.Year.ToString().Remove(0, 2);
        }
    }
}