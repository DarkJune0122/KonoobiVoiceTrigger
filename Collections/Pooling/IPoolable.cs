using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceTrigger.Collections.Pooling;

public interface IPoolable
{
    /// <summary>
    /// Called on objects when you pool them.
    /// </summary>
    public void Reset();
}
