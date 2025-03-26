// These are your imports, mostly you'll be needing these 5 for every plugin. Some will need more.

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
// using static Obeliskial_Essentials.Essentials;
using System;


// The Plugin csharp file is used to 


// Make sure all your files have the same namespace and this namespace matches the RootNamespace in the .csproj file
namespace DivinationLogger{
    // These are used to create the actual plugin. If you don't need Obeliskial Essentials for your mod, 
    // delete the BepInDependency and the associated code "RegisterMod()" below.

    // If you have other dependencies, such as obeliskial content, make sure to include them here.
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    // [BepInDependency("com.stiffmeds.obeliskialessentials")] // this is the name of the .dll in the !libs folder.
    [BepInProcess("AcrossTheObelisk.exe")] //Don't change this

    // If PluginInfo isn't working, you are either:
    // 1. Using BepInEx v6
    // 2. Have an issue with your csproj file (not loading the analyzer or BepInEx appropriately)
    // 3. You have an issue with your solution file (not referencing the correct csproj file)


    public class Plugin : BaseUnityPlugin
    {
        
        // If desired, you can create configs for users by creating a ConfigEntry object here, 
        // and then use config = Config.Bind() to set the title, default value, and description of the config.
        // It automatically creates the appropriate configs.
        
        public static ConfigEntry<bool> EnableMod { get; set; }
        public static ConfigEntry<bool> EnableDebug { get; set; }
        public static ConfigEntry<string> SaveFolder { get; set; }
        public static ConfigEntry<bool> SaveToCSV { get; set; }
        public static ConfigEntry<bool> SaveToExcel { get; set; }
        public static ConfigEntry<bool> LogToLogOutput { get; set; }
        public static ConfigEntry<string> AbsoluteFolderPath { get; set; }
        public static ConfigEntry<int> DivinationsToLog { get; set; }
        internal int ModDate = int.Parse(DateTime.Today.ToString("yyyyMMdd"));
        private readonly Harmony harmony = new(PluginInfo.PLUGIN_GUID);
        internal static ManualLogSource Log;

        public static string debugBase = $"{PluginInfo.PLUGIN_GUID} ";

        private void Awake()
        {

            // The Logger will allow you to print things to the LogOutput (found in the BepInEx directory)
            Log = Logger;
            Log.LogInfo($"{PluginInfo.PLUGIN_GUID} {PluginInfo.PLUGIN_VERSION} has loaded!");
            
            // Sets the title, default values, and descriptions
            EnableMod = Config.Bind(new ConfigDefinition(PluginInfo.PLUGIN_NAME, "Enable Mod"), true, new ConfigDescription("If false, disables the mod. Restart the game upon changing this setting."));
            EnableDebug = Config.Bind(new ConfigDefinition(PluginInfo.PLUGIN_NAME, "Enable Debug"), true, new ConfigDescription("If true, Enables Debug Logging."));
            LogToLogOutput = Config.Bind(new ConfigDefinition(PluginInfo.PLUGIN_NAME, "Log to LogOutput"), false, new ConfigDescription("If true, logs the divinations to the LogOutput."));
            SaveToCSV = Config.Bind(new ConfigDefinition(PluginInfo.PLUGIN_NAME, "Save to CSV"), true, new ConfigDescription("If true, saves the divinations as a csv file."));
            SaveToExcel = Config.Bind(new ConfigDefinition(PluginInfo.PLUGIN_NAME, "Save to Excel"), false, new ConfigDescription("If true, saves the divinations as an Excel file."));            
            SaveFolder = Config.Bind(new ConfigDefinition(PluginInfo.PLUGIN_NAME, "Save Folder"), "", new ConfigDescription("Folder to Save to, if left blank, will write to the default save folder/your current seed"));
            AbsoluteFolderPath = Config.Bind(new ConfigDefinition(PluginInfo.PLUGIN_NAME, "Absolute Folder Path"), "", new ConfigDescription("Absolute FilePath to save to. If left blank, will default to Save Folder. Overrides Save Folder"));
            DivinationsToLog = Config.Bind(new ConfigDefinition(PluginInfo.PLUGIN_NAME, "Number of Divinations"), 10, new ConfigDescription("The number of divinations that will be automatically logged whenever you enter town."));

            // apply patches
            if(EnableMod.Value)
            {
                LogDebug("Excuting Patches");
                harmony.PatchAll();
            }
        }

        internal static void LogDebug(string msg)
        {
            if(EnableDebug.Value)
            {
                Log.LogDebug(debugBase + msg);
            }
            
        }
        internal static void LogInfo(string msg)
        {
            Log.LogInfo(debugBase + msg);
        }
        internal static void LogError(string msg)
        {
            Log.LogError(debugBase + msg);
        }
    }
}