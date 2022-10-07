using System;

namespace Core.Handlers
{    
    interface IDllLoader
    {
        IntPtr CreateDllObject();
        void DestroyDllObject(IntPtr ptr);
    }

    interface IEnvironmentBuilder
    {
        bool CreateEnvironment(IntPtr ptr);
        void CleanInternalData(IntPtr ptr);
    }

    interface ICppDllHandler : IDllLoader, IEnvironmentBuilder
    {
        void AddVertexData_Impl(IntPtr ptr, int vId, float x, float y);       

        int RoomNumber_Impl(IntPtr ptr);

        IntPtr ArrayPtr_Impl(IntPtr ptr, int offset);

        int ArraySize_Impl(IntPtr ptr, int offset);        
    }
}
