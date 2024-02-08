using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Gulong_plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class GulongPlugin : BaseUnityPlugin
    {
        /// <summary>
         /// 加载
         /// </summary>
        void RegisterHook(Type t)
        {
            try
            {
                IHook hook = Activator.CreateInstance(t) as IHook;
                hook.OnRegister(this);
                Harmony.CreateAndPatchAll(t);
                Console.WriteLine($"Patch {t.Name} Success!");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Patch {t.Name} Failed! Exception={e}");
                moduleEntries[t].Value = false;
            }
        }

        /// <summary>
        /// 卸载，还没搞懂，先重启吧
        /// </summary>
        void UnregisterHook(Type t)
        {
            //hook.OnUnregister(this);
            //Harmony
            //Console.WriteLine("Unpatch " + hook.GetType().Name);
            //hooks.Remove(hook);
        }

        /*void SettingChanged(object sender, EventArgs e)
        {
            ConfigEntry<bool> ChangedSetting = (ConfigEntry<bool>)((SettingChangedEventArgs)e).ChangedSetting;
            if (ChangedSetting.Value)
            {
                Config.Bind(ChangedSetting.Definition, false);
            }
            else
            {
                if (moduleEntries.ContainsValue(ChangedSetting))
                {
                    Config.Remove(ChangedSetting.Definition);
                }
            }
        }*/

        private Dictionary<Type, ConfigEntry<bool>> moduleEntries = new Dictionary<Type, ConfigEntry<bool>>();
        private List<ConfigDefinition> ConfigDefinitionList = new List<ConfigDefinition>();
        public Action onUpdate;

        /// <summary>
        /// 注册各模块的钩子
        /// </summary>
        void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine($"当前程序：{assembly.FullName}");
            var hookTypes = from t in assembly.GetTypes() where typeof(IHook).IsAssignableFrom(t) && !t.IsAbstract select t;
            Console.WriteLine("美好的初始化开始，统计钩子模块");
            foreach (var hookType in hookTypes)
            {
                DisplayNameAttribute displayName = (DisplayNameAttribute)hookType.GetCustomAttribute(typeof(DisplayNameAttribute));
                DescriptionAttribute description = (DescriptionAttribute)hookType.GetCustomAttribute(typeof(DescriptionAttribute));
                var adv1 = new ConfigDescription(description.Description + "，重启游戏生效", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 4 });
                ConfigDefinition ConfigDefinition = new ConfigDefinition("模块选择", displayName.DisplayName);
                ConfigDefinitionList.Add(ConfigDefinition);
                moduleEntries[hookType] = Config.Bind(ConfigDefinition, true, adv1);
                //moduleEntries[hookType].SettingChanged += SettingChanged;
                Console.WriteLine($"计入模块 [{displayName.DisplayName}]");
            }

            foreach (var modulePair in moduleEntries)
            {
                if (modulePair.Value.Value)
                    RegisterHook(modulePair.Key);
            }
            Console.WriteLine($"可注册钩子模块共计{moduleEntries.Count}个");
        }

        void Start()
        {
            Console.WriteLine("美好的第一帧开始");
        }

        void Update()
        {
            onUpdate?.Invoke();
        }
    }
}
