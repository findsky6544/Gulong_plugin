using BepInEx;
using Gulong_plugin;
using System;
using System.Collections.Generic;

namespace Gulong_plugin
{
    interface IHook
    {
        void OnRegister(GulongPlugin plugin);

        //void OnUnregister(GulongPlugin plugin);
    }
}
