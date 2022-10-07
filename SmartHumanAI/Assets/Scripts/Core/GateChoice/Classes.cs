using System;
using System.Collections.Generic;
using Core.Logger;

namespace Core.GateChoice
{
    internal enum ClassType { None, Single, Double }

    /// <summary>
    /// Abstact class for Class Choice implementation
    /// </summary>
    internal abstract class ClassChoice
    {
        protected ClassChoice()
        {
            ClassUntilData = new Dictionary<int, Utilities>();
        }

        protected Dictionary<int, Utilities> ClassUntilData;


            }
            else
            {
                LogWriter.Instance.WriteToLog("Class Type Duplication");
                throw new Exception("Class Type Duplication");
            }
        }

        public virtual ClassType Type() { return ClassType.None; }
    }

    internal class SingleClass : ClassChoice
    {
        public override ClassType Type() { return ClassType.Single; }
    }

    internal class DoubleClass : ClassChoice
    {
        public override ClassType Type() { return ClassType.Double; }
    }
}
