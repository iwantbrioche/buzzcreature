
namespace BuzzCreature.Objects.Buzz
{
    public static class BuzzEnums
    {
        public static CreatureTemplate.Type Buzz;

        public static Smoke.SmokeSystem.SmokeType BuzzSmoke;

        public static void Register()
        {
            Buzz = new("Buzz", register: true);
            BuzzSmoke = new("BuzzSmoke", register: true);
        }
    }
}
