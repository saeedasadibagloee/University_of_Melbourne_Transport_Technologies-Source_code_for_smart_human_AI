using Core.Handlers.ReactionTimeHandler;
using Core.PositionUpdater.EvacuationPathHandler;
using Core.PositionUpdater.ReactionTimeHandler;
using DataFormats;

namespace Core.PositionUpdater.HandlerFabric
{
    internal interface IMultiLevelHandler
    {
        IReactionTimeHandler ReactionTimeHandler();
        IDestinationLevelIdentifier DestinationLevelHandler();
    }

    internal class ExtraMultiLevelHanders : IMultiLevelHandler
    {
        private IReactionTimeHandler handler;
        private IDestinationLevelIdentifier destinationLevelHandler;

        public ExtraMultiLevelHanders()
        {
            switch (Params.Current.ReactionMethod)
            {
                case Def.ReactionMethod.WeibullHazard:
                    handler = new WeibullHazard();
                    break;
                case Def.ReactionMethod.ExpHazard:
                    handler = new ExponentialHazard();
                    break;
                case Def.ReactionMethod.ExpDist:
                    handler = new GeneralExpDistribution();
                    break;
                case Def.ReactionMethod.None:
                    handler = new ReactionTimeNone();
                    break;
            }
            
            destinationLevelHandler = new MultilLevelDestinationLevelID();
        }

        public IReactionTimeHandler ReactionTimeHandler()
        {
            return handler;
        }

        public IDestinationLevelIdentifier DestinationLevelHandler()
        {
            return destinationLevelHandler;
        }
    }
}