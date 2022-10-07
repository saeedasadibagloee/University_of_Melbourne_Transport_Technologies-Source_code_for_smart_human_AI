using System;
using System.Runtime.InteropServices;

namespace Core.Handlers
{
    internal class CPPEnvHandler : ICppDllHandler
    { 
        [DllImport("builder.dll")]
        static extern IntPtr CreateSharedObject();

        [DllImport("builder.dll")]
        static extern void DestroySharedObject(IntPtr ptr);

        [DllImport("builder.dll")]
        static extern void AddVertexData(IntPtr ptr, int v_id, float x, float y);

        [DllImport("builder.dll")]
        static extern bool BuildEnvironment(IntPtr ptr);

        [DllImport("builder.dll")]
        static extern int RoomNumber(IntPtr ptr);

        [DllImport("builder.dll")]
        static extern IntPtr ArrayPtr(IntPtr ptr, int offset);

        [DllImport("builder.dll")]
        static extern int ArraySize(IntPtr ptr, int offset);

        [DllImport("builder.dll")]
        static extern void CleanData(IntPtr ptr);

        [DllImport("builder.dll")]
        static extern void ProcessRequest(IntPtr ptr);        

        public CPPEnvHandler() { }

        public IntPtr CreateDllObject()
        {           
            return CreateSharedObject();
        }

        public void DestroyDllObject(IntPtr ptr)
        {
            DestroySharedObject(ptr);
        }

        public void AddVertexData_Impl(IntPtr ptr, int v_id, float x, float y)
        {
            AddVertexData(ptr, v_id, x, y);
        }

        public bool CreateEnvironment(IntPtr ptr)
        {
            return BuildEnvironment(ptr);
        }

        public void CleanInternalData(IntPtr ptr)
        {
            CleanData(ptr);
        }

        public int RoomNumber_Impl(IntPtr ptr)
        {
            return RoomNumber(ptr);
        }

        public IntPtr ArrayPtr_Impl(IntPtr ptr, int offset)
        {
            return ArrayPtr(ptr, offset);
        }

        public int ArraySize_Impl(IntPtr ptr, int offset)
        {
            return ArraySize(ptr, offset);
        }        
    }
}
