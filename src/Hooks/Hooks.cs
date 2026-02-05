
using MonoMod.RuntimeDetour;

namespace BuzzCreature.Hooks
{
    public static class Hooks
    {
        public static void PatchHooks()
        {
            On.FollowPathVisualizer.Update += FollowPathVisualizer_Update;
        }


        private static void FollowPathVisualizer_Update(On.FollowPathVisualizer.orig_Update orig, FollowPathVisualizer self, bool eu)
        {
            orig(self, eu);
        }
    }
}
