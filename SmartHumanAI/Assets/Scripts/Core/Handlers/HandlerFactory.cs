namespace Core.Handlers
{
    internal abstract class HandlerFactory<T> 
    {
        public abstract T GenerateHandler();
    }

    internal class BoostCppHandlerFactory : HandlerFactory<ICppDllHandler>
    {
        public override ICppDllHandler GenerateHandler()
        {
            return new CPPEnvHandler();
        }
    }

    internal abstract class DllHander
    {
        protected ICppDllHandler Handler = null;
        public abstract ICppDllHandler GetHandler();
    }

    internal class BoostCppEnvHander : DllHander
    {
        private readonly HandlerFactory<ICppDllHandler> _boostFactory = 
            new BoostCppHandlerFactory();       
        
        public BoostCppEnvHander()
        {
            Handler = _boostFactory.GenerateHandler();
        }

        public override ICppDllHandler GetHandler()
        {
            return Handler;
        }
    }
}
