namespace Core.PositionUpdater.HandlerFabric
{
    internal interface ISingleLevelHandlerFabric
    {
        ISingleLevelHandler SingleLevelHandler();
    }

    internal interface IMultiLevelHandlerFabric
    {
        IMultiLevelHandler MultilLevelHandler();
    }

    internal class SimpleSingleLevelHanderFabric : ISingleLevelHandlerFabric
    {
        private ISingleLevelHandler handler = null;

        public SimpleSingleLevelHanderFabric()
        {
            handler = new ExtraSingleLevelHandlers();
        }

        public ISingleLevelHandler SingleLevelHandler()
        {
            return handler;
        }
    }

    internal class SimpleMultiLevelHandlerFabric : IMultiLevelHandlerFabric
    {
        private IMultiLevelHandler handler = null;

        public SimpleMultiLevelHandlerFabric()
        {
            handler = new ExtraMultiLevelHanders();
        }

        public IMultiLevelHandler MultilLevelHandler()
        {
            return handler;
        }
    }
}