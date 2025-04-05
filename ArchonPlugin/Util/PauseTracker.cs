using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subnautica_Archon.Util
{
    internal class PauseTracker
    {
        public PauseTracker(Action onPause, Action onUnpause)
        {
            OnPause = onPause;
            OnUnpause = onUnpause;
        }

        public Action OnPause { get; }
        public Action OnUnpause { get; }
        public bool IsPaused { get; private set; }
        public void Update()
        {
            bool isPaused = Time.deltaTime == 0;
            if (isPaused != IsPaused)
            {
                if (isPaused)
                    OnPause();
                else
                    OnUnpause();
                IsPaused = isPaused;
            }
        }
    }
}
