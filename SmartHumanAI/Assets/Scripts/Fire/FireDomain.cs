using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using DataFormats;
using InputOutput;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Fire
{
    public class FireDomain : MonoBehaviour
    {
        private static FireDomain _fireDomain;

        public static FireDomain Instance
        {
            get { return _fireDomain ?? (_fireDomain = FindObjectOfType<FireDomain>()); }
        }

        public bool isEnabled = false;
        public Text FireInformationText;

        public Text PlaybackButtonText;

        public GameObject FireCellPrefab;
        public GameObject FireCellsHolder;

        public GameObject FireSlicePrefab;
        private List<GameObject> xSlices = new List<GameObject>();
        private List<GameObject> ySlices = new List<GameObject>();
        private List<GameObject> zSlices = new List<GameObject>();
        private List<List<Texture2D>> xSliceTextures = new List<List<Texture2D>>();
        private List<List<Texture2D>> ySliceTextures = new List<List<Texture2D>>();
        private List<List<Texture2D>> zSliceTextures = new List<List<Texture2D>>();

        public FireVolume Volume = new FireVolume(30, 50, -15, 15, 0, 40);

        public InputField FireTimeField;

        
                    texture.Apply();
                    timeSlice.Add(texture);
                }
                zSliceTextures.Add(timeSlice);
                currentTime++;
            }

            sliceStorage.Clear();
            currentTime = 0;

            LoadCurrentSliceFrame();
        }

        private void ReadSliceFile(string path)
        {
            var fileStream = File.OpenRead(path);

            #region Read QUANTITY, SHORT_NAME & UNITS

            for (int i = 0; i < 3; i++)
            {
                ReadHeader(fileStream);

                // Read FORTRAN record.
                byte[] bytes = new byte[30];
                fileStream.Read(bytes, 0, 30);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (var byt in bytes)
                    stringBuilder.Append(Convert.ToChar(byt));
                stringvalues.Add(stringBuilder.ToString());

                ReadFooter(fileStream);
            }

            #endregion

            #region Read SEXTUPLET

            ReadHeader(fileStream);
            List<int> sextupletList = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                bytes4 = new byte[4];
                fileStream.Read(bytes4, 0, 4);
                sextupletList.Add(BitConverter.ToInt32(bytes4, 0));
            }
            ReadFooter(fileStream);

            #endregion

            int i1 = sextupletList[1] + 1;
            int j1 = sextupletList[3] + 1;
            int k1 = sextupletList[5] + 1;

            // Generate Fire Cells
            for (int i = 0; i < i1; i++)
            {
                for (int j = 0; j < j1; j++)
                {
                    for (int k = 0; k < k1; k++)
                    {
                        FireCell fireCell = new FireCell();
                        fireCell.X = i + Volume.XMin;
                        fireCell.Y = j + Volume.YMin;
                        fireCell.Z = k + Volume.ZMin;
                        fireCells.Add(fireCell);
                    }
                }
            }

            while (true)
            {
                //Read the time.
                if (!ReadHeader(fileStream))
                    break;
                bytes4 = new byte[4];
                fileStream.Read(bytes4, 0, 4);
                timeStorage.Add(BitConverter.ToSingle(bytes4, 0));
                ReadFooter(fileStream);

                //Read 3D slice for this time.
                ReadHeader(fileStream);

                float[,,] timeSlice = new float[i1, j1, k1];

                for (int k = 0; k < k1; k++)
                {
                    for (int j = 0; j < j1; j++)
                    {
                        for (int i = 0; i < i1; i++)
                        {
                            bytes4 = new byte[4];
                            fileStream.Read(bytes4, 0, 4);
                            timeSlice[i, j, k] = BitConverter.ToSingle(bytes4, 0);
                            if (timeSlice[i, j, k] > highestSootValue) highestSootValue = timeSlice[i, j, k];
                        }
                    }
                }

                sliceStorage.Add(timeSlice);

                ReadFooter(fileStream);
            }

            Debug.Log("Successfully read soot data.");
        }

        /// <summary>
        /// Read FORTRAN record footer.
        /// </summary>
        private void ReadFooter(FileStream fileStream)
        {
            bytes4 = new byte[4];
            fileStream.Read(bytes4, 0, 4);
            if (recordLength != BitConverter.ToInt32(bytes4, 0))
                throw new ApplicationException("Record header and footer do not match.");
        }

        /// <summary>
        /// Read FORTRAN record header.
        /// </summary>
        private bool ReadHeader(FileStream fileStream)
        {
            bytes4 = new byte[4];
            if (fileStream.Read(bytes4, 0, 4) == 0)
                return false;
            recordLength = BitConverter.ToInt32(bytes4, 0);
            return true;
        }

        public void RunFireSimulation()
        {
            FitBoxToEnvironment();

            GenerateFDSFile(UIController.Instance.ModelName + ".fds");

            Process fireProcess = new Process
            {
                StartInfo =
                {
                    FileName = "fds.exe",
                    Arguments = UIController.Instance.ModelName + ".fds",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                }
            };

            //* Start process and handlers
            try
            {
       
                               "\r\n");
            float floorHeight = Volume.ZMin / 10f;
            float ceilHeight = Volume.ZMax / 10f;

            foreach (Wall wall in FileOperations.Instance.CurrentModel.levels[0].wall_pkg.walls)
            {
                float yMin = 0;
                float yMax = 0;
                float xMin = 0;
                float xMax = 0;

                if (wall.angle == 0 || wall.angle == 180)
                {
                    xMin = xMax = wall.vertices[0].X;
                    yMin = Mathf.Min(wall.vertices[0].Y, wall.vertices[1].Y);
                    yMax = Mathf.Max(wall.vertices[0].Y, wall.vertices[1].Y);
                }
                else if (wall.angle == 90 || wall.angle == 270)
                {
                    yMin = yMax = wall.vertices[0].Y;
                    xMin = Mathf.Min(wall.vertices[0].X, wall.vertices[1].X);
                    xMax = Mathf.Max(wall.vertices[0].X, wall.vertices[1].X);
                }
                else
                {
                    Debug.Log("The wall is at an angle, disregarding in fire simulation.");
                    continue;
                }

                streamOut.WriteLine(
                    "&OBST ID=\'Wall " + wall.id + "\', XB=" +
                    xMin + "," + xMax + "," +
                    yMin + "," + yMax + "," +
                    floorHeight + "," + ceilHeight + "," +
                    " SURF_ID=\'GYPSUM BOARD\'/");
            }

            foreach (Gate gate in FileOperations.Instance.CurrentModel.levels[0].gate_pkg.gates)
            {
                float yMin = 0;
                float yMax = 0;
                float xMin = 0;
                float xMax = 0;

                if (gate.angle == 0 || gate.angle == 180)
                {
                    xMin = xMax = gate.vertices[0].X;
                    yMin = Mathf.Min(gate.vertices[0].Y, gate.vertices[1].Y);
                    yMax = Mathf.Max(gate.vertices[0].Y, gate.vertices[1].Y);
                }
                else if (gate.angle == 90 || gate.angle == 270)
                {
                    yMin = yMax = gate.vertices[0].Y;
                    xMin = Mathf.Min(gate.vertices[0].X, gate.vertices[1].X);
                    xMax = Mathf.Max(gate.vertices[0].X, gate.vertices[1].X);
                }
                else
                {
                    Debug.Log("The wall is at an angle, disregarding in fire simulation.");
                    continue;
                }

                streamOut.WriteLine(
                    "&OBST ID=\'Gate " + gate.id + "\', XB=" +
                    xMin + "," + xMax + "," +
                    yMin + "," + yMax + "," +
                    (floorHeight + (ceilHeight - floorHeight) * gateWallRatio) + "," + ceilHeight + "," +
                    " SURF_ID=\'GYPSUM BOARD\'/");
            }

            streamOut.WriteLine("&OBST ID=\'Obstruction #1\', XB=3.5,4.5,-1.0,1.0,0.0,0.0, SURF_ID=\'STEEL SHEET\'/ \r\n" +
                               "&OBST ID=\'Obstruction #2\', XB=3.5,4.5,-1.0,-1.0,0.0,0.1, SURF_ID=\'STEEL SHEET\'/ \r\n" +
                               "&OBST ID=\'Obstruction #3\', XB=3.5,4.5,1.0,1.0,0.0,0.1, SURF_ID=\'STEEL SHEET\'/ \r\n" +
                               "&OBST ID=\'Obstruction #4\', XB=3.5,3.5,-1.0,1.0,0.0,0.1, SURF_ID=\'STEEL SHEET\'/ \r\n" +
                               "&OBST ID=\'Obstruction #5\', XB=4.5,4.5,-1.0,1.0,0.0,0.1, SURF_ID=\'STEEL SHEET\'/ \r\n" +
                               "\r\n" +
                               "&VENT ID=\'Mesh Vent: Mesh [XMIN]\', SURF_ID=\'OPEN\', XB=3.0,3.0,-1.5,1.5,0.0,4.0/ \r\n" +
                               "&VENT ID=\'Mesh Vent: Mesh [XMAX]\', SURF_ID=\'OPEN\', XB=5.0,5.0,-1.5,1.5,0.0,4.0/ \r\n" +
                               "&VENT ID=\'Mesh Vent: Mesh [YMIN]\', SURF_ID=\'OPEN\', XB=3.0,5.0,-1.5,-1.5,0.0,4.0/ \r\n" +
                               "&VENT ID=\'Mesh Vent: Mesh [YMAX]\', SURF_ID=\'OPEN\', XB=3.0,5.0,1.5,1.5,0.0,4.0/ \r\n" +
                               "&VENT ID=\'Mesh Vent: Mesh [ZMAX]\', SURF_ID=\'OPEN\', XB=3.0,5.0,-1.5,1.5,4.0,4.0/ \r\n" +
                               "\r\n" +
                               "&BNDF QUANTITY=\'WALL TEMPERATURE\'/\r\n" +
                               "&BNDF QUANTITY=\'CPUA\', PART_ID=\'heptane droplets\'/\r\n" +
                               "&BNDF QUANTITY=\'MPUA\', PART_ID=\'heptane droplets\'/\r\n" +
                               "&BNDF QUANTITY=\'AMPUA\', PART_ID=\'heptane droplets\'/\r\n");
            streamOut.WriteLine();

            streamOut.WriteLine("\r\n" +
                                "&VENT MB=\'XMIN\', SURF_ID=\'OPEN\' /\r\n" +
                                "&VENT MB=\'XMAX\', SURF_ID=\'OPEN\' /\r\n" +
                                "&VENT MB=\'YMIN\', SURF_ID=\'OPEN\' /\r\n" +
                                "&VENT MB=\'YMAX\', SURF_ID=\'OPEN\' /\r\n" +
                                "&VENT MB=\'ZMIN\', SURF_ID=\'INERT\' /\r\n" +
                                "&VENT MB=\'ZMAX\', SURF_ID=\'GYPSUM BOARD\' /" +
                                "\r\n");
            streamOut.WriteLine();

            //GenerateDEVCs(streamOut);

            streamOut.WriteLine("&SLCF XB=" + Volume.GetXB() + ", QUANTITY=\'MASS FRACTION\', SPEC_ID=\'SOOT\'/");
            streamOut.WriteLine("&DUMP DT_SL3D=0.1/");
            streamOut.WriteLine("&TAIL /");

            streamOut.Flush();
            streamOut.Close();
        }

        public void OpenSmokeView()
        {
            Process.Start(UIController.Instance.ModelName + ".smv");
        }

        public void OutputFireCells()
        {
            string outputFile = "fireCells.csv";

            if (File.Exists(outputFile))
                File.Delete(outputFile);

            StreamWriter streamWriter = new StreamWriter(outputFile);

            foreach (var firecell in fireCells)
            {
                streamWriter.WriteLine(firecell.X + "," + firecell.Y + "," + firecell.Z);
            }

            streamWriter.Flush();
            streamWriter.Close();
        }

        public void GenerateFireCells()
        {
            foreach (var fireCellObj in fireCellObjects)
            {
                Destroy(fireCellObj);
            }

            fireCellObjects.Clear();

            for (var i = 0; i < fireCells.Count; i++)
            {
                var fireCell = fireCells[i];

                GameObject item = Instantiate(
                    FireCellPrefab,
                    new Vector3(fireCell.X / 10f, fireCell.Z / 10f, fireCell.Y / 10f),
                    Quaternion.identity,
                    FireCellsHolder.transform);

                fireCellObjects.Add(item);
                fireCell.CellMeshRenderer = item.GetComponent<MeshRenderer>();
            }
        }

        public void ChangeFireTime(string input)
        {
            float number;
            if (!float.TryParse(input, out number) && number > 1)
            {
                Debug.Log("Could not parse " + number);
                FireTimeField.text = endTime.ToString();
                return;
            }

            if (number < 1)
                number = 1;

            endTime = Mathf.RoundToInt(number);
            FireTimeField.text = endTime.ToString();
        }
    }

    public class FireCell
    {
        public int X;
        public int Y;
        public int Z;

        public MeshRenderer CellMeshRenderer;
    }
}