﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.spacepuppy.Scenario
{

    [System.Flags()]
    public enum ActivateEvent
    {
        None = 0,
        OnStart = 1,
        OnEnable = 2,
        OnStartOrEnable = 3,
        Awake = 4,
        OnDisable = 8
    }
}
