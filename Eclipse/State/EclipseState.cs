using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.State
{
    public interface EclipseState
    {
        void EnterState(EclipseStateContext eclipseStateContext);
        bool OnUp(EclipseStateContext eclipseStateContext, bool held);
        bool OnDown(EclipseStateContext eclipseStateContext, bool held);
        bool OnLeft(EclipseStateContext eclipseStateContext, bool held);
        bool OnRight(EclipseStateContext eclipseStateContext, bool held);
        bool OnPageUp(EclipseStateContext eclipseStateContext);
        bool OnPageDown(EclipseStateContext eclipseStateContext);
        bool OnEnter(EclipseStateContext eclipseStateContext);
        bool OnEscape(EclipseStateContext eclipseStateContext);
    }
}
