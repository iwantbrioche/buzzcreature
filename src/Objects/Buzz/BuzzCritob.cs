
using DevInterface;
using Fisobs.Creatures;
using Fisobs.Properties;
using Fisobs.Sandbox;

namespace BuzzCreature.Objects.Buzz
{
    public class BuzzCritob: Critob
    {
        public BuzzCritob() : base(BuzzEnums.Buzz)
        {
            LoadedPerformanceCost = 20f;
            SandboxPerformanceCost = new(linear: 0.6f, exponential: 0.1f);
            ShelterDanger = ShelterDanger.Safe;
            CreatureName = "Buzz";
        }

        public override CreatureTemplate CreateTemplate()
        {
            List<TileTypeResistance> typeResistances = [];
            List<TileConnectionResistance> connectionResistances = [];

            typeResistances.Add(new TileTypeResistance(AItile.Accessibility.Air, 1f, PathCost.Legality.Allowed));
            //typeResistances.Add(new TileTypeResistance(AItile.Accessibility.Floor, 1f, PathCost.Legality.Allowed));
            //typeResistances.Add(new TileTypeResistance(AItile.Accessibility.Climb, 1f, PathCost.Legality.Allowed));
            connectionResistances.Add(new TileConnectionResistance(MovementConnection.MovementType.Standard, 1f, PathCost.Legality.Allowed));
            connectionResistances.Add(new TileConnectionResistance(MovementConnection.MovementType.OpenDiagonal, 1f, PathCost.Legality.Allowed));
            connectionResistances.Add(new TileConnectionResistance(MovementConnection.MovementType.ShortCut, 1f, PathCost.Legality.Allowed));
            connectionResistances.Add(new TileConnectionResistance(MovementConnection.MovementType.NPCTransportation, 10f, PathCost.Legality.Allowed));
            connectionResistances.Add(new TileConnectionResistance(MovementConnection.MovementType.OffScreenMovement, 1f, PathCost.Legality.Allowed));
            connectionResistances.Add(new TileConnectionResistance(MovementConnection.MovementType.BetweenRooms, 1f, PathCost.Legality.Allowed));

            CreatureTemplate buzzTemplate = new(BuzzEnums.Buzz, null, typeResistances, connectionResistances,
                new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 1f))
            {
                baseDamageResistance = 0.2f,
                baseStunResistance = 1f,
                instantDeathDamageLimit = 1.2f,
                abstractedLaziness = 100,
                AI = true,
                requireAImap = true,
                canFly = true,
                doPreBakedPathing = false,
                offScreenSpeed = 1.5f,
                bodySize = 0.25f,
                grasps = 1,
                preBakedPathingAncestor = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly),
                stowFoodInDen = false,
                visualRadius = 700f,
                movementBasedVision = 0.3f,
                communityInfluence = 0.5f,
                socialMemory = true,
                shortcutSegments = 1,
                shortcutColor = new(1f, 1f, 1f),
                dangerousToPlayer = 0.3f,
                waterRelationship = CreatureTemplate.WaterRelationship.AirOnly,
                meatPoints = 1,
                usesNPCTransportation = true
            };

            return buzzTemplate;
        }

        public override void EstablishRelationships()
        {
            Relationships relationships = new(BuzzEnums.Buzz);

            relationships.IsInPack(BuzzEnums.Buzz, 1f);

            relationships.Fears(CreatureTemplate.Type.Vulture, 1f);

            relationships.EatenBy(CreatureTemplate.Type.Vulture, 1f);

        }

        public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit)
        {
            return new BuzzAI(acrit, acrit.world);
        }

        public override Creature CreateRealizedCreature(AbstractCreature acrit)
        {
            return new Buzz(acrit, acrit.world);
        }
    }
}
