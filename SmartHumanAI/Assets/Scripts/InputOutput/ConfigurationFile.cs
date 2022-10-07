using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Core;
//using NUnit.Framework.Constraints;
using UnityEngine;
using Core = Core.Core;

namespace InputOutput
{
    class ConfigurationFile
    {
        public static string modelfile= "";
        public static string fileName = "inputfile.csv";
        public static string outputFile = "";
        public static int failedParameters = 0;
        public static List<string> analysisVariables = new List<string>();
        public static List<float> analysisValues = new List<float>();

        public static bool LoadAndVerifyFile()
        {
            failedParameters = 0;
            analysisVariables.Clear();
            analysisValues.Clear();

            if (!File.Exists(fileName))
            {
                Debug.Log("File " + fileName + " not detected.");
                return false;
            }

           
                }
            }
            
            return true;
        }

        private static bool CheckFieldMatches(string currentVariable, FieldInfo[] fields)
        {
            if (!fields.Any(field => currentVariable.Equals(field.Name))) return false;
            return true;
        }

        private static bool CheckFieldMatchesUtilities(string currentVariable, FieldInfo[] fields)
        {
            string withoutClassNumber = currentVariable.Substring(0, currentVariable.Length - 1);

            if (!fields.Any(field => withoutClassNumber.Equals(field.Name))) return false;
            return true;
        }

        public static void ApplyParameters()
        {
            for (int i = 0; i < analysisVariables.Count; i++)
     
                }
                
                string variableName = isUtility ? analysisVariables[i].Substring(0, analysisVariables[i].Length - 1) : analysisVariables[i];
                FieldInfo fieldInfo = classType.GetType().GetField(variableName);
                
                if (fieldInfo.FieldType == typeof(int))
                    fieldInfo.SetValue(classType, (int)analysisValues[i]);
                else if (fieldInfo.FieldType == typeof(uint))
                    fieldInfo.SetValue(classType, (uint)analysisValues[i]);
                else
                    fieldInfo.SetValue(classType, analysisValues[i]);

                if (isSplit)
                {
                    fieldInfo = classType2.GetType().GetField(variableName);

                    if (fieldInfo.FieldType == typeof(int))
                        fieldInfo.SetValue(classType2, 100 - (int)analysisValues[i]);
                    else
                        fieldInfo.SetValue(classType2, 100 - analysisValues[i]);
                }

                Debug.Log(fieldInfo.Name + " set to " + fieldInfo.GetValue(classType) + (isSplit ? " and " + fieldInfo.GetValue(classType2) : ""));
            }
        }
    }
}
