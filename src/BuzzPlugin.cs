global using System;
global using System.Linq;
global using System.Text.RegularExpressions;
global using System.Collections.Generic;
global using System.Reflection;
global using BepInEx;
global using MoreSlugcats;
global using Mono.Cecil.Cil;
global using MonoMod.Cil;
global using UnityEngine;
global using RWCustom;
global using Random = UnityEngine.Random;
global using Vector2 = UnityEngine.Vector2;
global using Color = UnityEngine.Color;
global using Custom = RWCustom.Custom;
using System.Security;
using System.Security.Permissions;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using BuzzCreature.Objects.Buzz;
using Fisobs.Core;
using System.IO;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace BuzzCreature
{
    [BepInPlugin(MOD_ID, MOD_NAME, MOD_VER)]
    public class BuzzPlugin : BaseUnityPlugin
    {
        public const string MOD_ID = "iwantbread.buzzcreature";
        public const string MOD_NAME = "Buzz Creature";
        public const string MOD_VER = "0.0";
        //private const string ATLASES_DIR = "buzzAtlases";
        public static new ManualLogSource Logger { get; private set; }
        private void OnEnable()
        {
            BuzzEnums.Register();
            Content.Register(new BuzzCritob());

            Hooks.Hooks.PatchHooks();

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
            Logger = base.Logger;
        }

        private bool IsInit;
        private bool PostIsInit;

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;


                Futile.atlasManager.LoadAtlas("buzzAtlases/BuzzBodyAtlas");
                Futile.atlasManager.LoadAtlas("buzzAtlases/BuzzEyeAtlas");

                IsInit = true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"{MOD_NAME} failed to load!");
                Logger.LogError(ex);
                //throw;
            }
        }

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (PostIsInit) return;

                PostIsInit = true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"{MOD_NAME} PostModsInit failed to load!");
                Logger.LogError(ex);
                //throw;
            }
        }

        //private static void LoadAtlases()
        //{
        //    string[] atlasPaths = AssetManager.ListDirectory(ATLASES_DIR);
        //    foreach (string filePath in atlasPaths)
        //    {
        //        if (Path.GetExtension(filePath) == ".txt")
        //        {
        //            string atlasName = Path.GetFileNameWithoutExtension(filePath);
        //            try
        //            {
        //                Logger.LogDebug($"loading {ATLASES_DIR + Path.AltDirectorySeparatorChar + atlasName} atlas!");

        //                Futile.atlasManager.LoadAtlas(ATLASES_DIR + Path.AltDirectorySeparatorChar + atlasName);
        //            }
        //            catch (Exception ex)
        //            {
        //                Logger.LogError($"Error while loading {MOD_NAME} atlases!");
        //                Logger.LogError(ex);
        //                throw;
        //            }
        //        }
        //    }

        //    Logger.LogInfo($"Loaded {MOD_NAME} atlases successfully!");
        //}

    }
}
