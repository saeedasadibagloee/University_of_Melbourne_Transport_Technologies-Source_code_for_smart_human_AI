using System.Collections.Generic;
using eDriven.Core.Signals;

namespace Core.Signals
{
    enum CoreEventType { GroupDataUpdate, GateSharesUpdate, GateDecisionUpdate, ReactionTimeData }
     
    internal class GeneralEventData
    {
        public CoreEventType EType { get; set; }

        public object ObjectData { get; set; }

        public GeneralEventData(CoreEventType type, object objectData)
        {
            EType = type;
            ObjectData = objectData;
        }
    }

    internal class UpdateSignals
    {
        protected Signal _signal = null;

        public UpdateSignals(Signal generalEventSignal)
        {
            _signal = generalEventSignal;
        }
       
        public void SendSignal(CoreEventType sType, object signalData)
        {
            _signal.Emit(new GeneralEventData(sType, signalData));
        }
    }
}
