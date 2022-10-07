using Core.Handlers.ReactionTimeHandler;
using Core.PositionUpdater.ReactionTimeHandler;
using DataFormats;

namespace Core.PositionUpdater.HandlerFabric
{
    internal interface ISingleLevelHandler
    {
        IReactionTimeHandler ReactionTimeHandler();
    }

    internal class ExtraSingleLevelHandlers : ISingleLevelHandler
    {
        private IReactionTimeHandler handler;

        public ExtraSingleLevelHandlers()
        {
            switch (Params.Current.ReactionMethod)
            {
                case Def.ReactionMethod.ExpHazard:
                    handler = new ExponentialHazard();
                    break;
                case Def.ReactionMethod.WeibullHazard:
                    handler = new WeibullHazard();
                    break;
                case Def.ReactionMethod.ExpDist:
                    handler = new GeneralExpDistribution();
                    break;
                case Def.ReactionMethod.None:
                    handler = new ReactionTimeNone();
                    break;
            }
        }

        public IReactionTimeHandler ReactionTimeHandler()
        {
            return handler;
        }
    }
}