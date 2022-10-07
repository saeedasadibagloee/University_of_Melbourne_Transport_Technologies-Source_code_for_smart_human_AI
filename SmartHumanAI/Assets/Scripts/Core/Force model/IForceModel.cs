using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.ForceModel
{
    /// <summary>
    /// Commmon Interface for The Force Model
    /// </summary>
    interface IForceModel
    {

        void Apply(Domain.Agent pAgent);
    }
}
