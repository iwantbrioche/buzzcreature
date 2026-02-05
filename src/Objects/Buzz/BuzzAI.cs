
using System;
using System.Drawing;

namespace BuzzCreature.Objects.Buzz
{
    public class BuzzAI : ArtificialIntelligence, IUseARelationshipTracker, IUseItemTracker, IReactToSocialEvents, ILookingAtCreatures
    {
        public class Behavior(string value, bool register = false) : ExtEnum<Behavior>(value, register)
        {
            public static readonly Behavior Idle = new("Idle", register: true);
            public static readonly Behavior Flee = new("Flee", register: true);
            public static readonly Behavior SearchForItems = new("SearchForItems", register: true);
        }

        public Buzz buzz;
        public Behavior behavior;
        public Tracker.CreatureRepresentation focusCreature;
        public CreatureLooker creatureLooker; // 👀

        public float currentUtility;

        private DebugDestinationVisualizer debugDestinationVisualizer;
        private DebugTrackerVisualizer debugTrackerVisualizer;
        public FollowPathVisualizer followPathVisualizer;
        public BuzzAI(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
        {
            buzz = abstractCreature.realizedCreature as Buzz;
            buzz.AI = this;
            AddModule(new StandardPather(this, world, abstractCreature));
            pathFinder.stepsPerFrame = 30;
            (pathFinder as StandardPather).heuristicCostFac = 1f;
            (pathFinder as StandardPather).heuristicDestFac = 2f;
            pathFinder.accessibilityStepsPerFrame = 60;
            AddModule(new Tracker(this, 5, 10, 450, 0.5f, 5, 5, 10));
            AddModule(new ThreatTracker(this, 3));
            AddModule(new ItemTracker(this, 10, 10, -1, -1, stopTrackingCarried: true));
            AddModule(new StuckTracker(this, trackPastPositions: true, trackNotFollowingCurrentGoal: true));
            AddModule(new RelationshipTracker(this, tracker));
            AddModule(new UtilityComparer(this));
            stuckTracker.AddSubModule(new StuckTracker.GetUnstuckPosCalculator(stuckTracker));
            utilityComparer.AddComparedModule(threatTracker, null, 1.1f, 1.1f);
            utilityComparer.AddComparedModule(stuckTracker, null, 1f, 1.1f);
            creatureLooker = new(this, tracker, buzz, 0.8f, 20);
            behavior = Behavior.Idle;


            //debugDestinationVisualizer = new(world.game.abstractSpaceVisualizer, world, pathFinder, Color.red);
            //debugTrackerVisualizer = new(tracker);
            //itemTracker.visualize = true;
            //pathFinder.visualize = true;
        }

        public override void Update()
        {
            debugDestinationVisualizer?.Update();
            debugTrackerVisualizer?.Update();

            

            if (buzz.room.game.devToolsActive && Input.GetMouseButton(2))
            {
                IntVector2 tilePosition = buzz.room.game.world.activeRooms[0].GetTilePosition((Vector2)Futile.mousePosition + buzz.room.game.cameras[0].pos);
                creature.abstractAI.SetDestination(new WorldCoordinate(buzz.room.game.cameras[0].room.abstractRoom.index, tilePosition.x, tilePosition.y, -1));
            }

            creatureLooker.Update();

            AIModule aiModule = utilityComparer.HighestUtilityModule();
            currentUtility = utilityComparer.HighestUtility();


            if (aiModule != null)
            {
                if (aiModule is ThreatTracker)
                {
                    behavior = Behavior.Flee;
                }
            }
            if (currentUtility < 0.1f)
            {
                behavior = Behavior.Idle;
            }

            if (behavior == Behavior.Idle)
            {

            }
            else if (behavior == Behavior.Flee)
            {
                WorldCoordinate destination = threatTracker.FleeTo(creature.pos, 1, 30, false);
                if (threatTracker.mostThreateningCreature != null)
                {
                    focusCreature = threatTracker.mostThreateningCreature;
                }
                creature.abstractAI.SetDestination(destination);
            }
            else if (behavior == Behavior.SearchForItems)
            {
            }

            //DebugDrawing.DrawText(buzz.room, behavior.value, buzz.firstChunk.pos + new Vector2(15f, 30f), Color.white);

            base.Update();
        }

        public override PathCost TravelPreference(MovementConnection coord, PathCost cost)
        {
            if (buzz.flying)
            {

                cost = new PathCost(cost.resistance + (buzz.room.aimap.getTerrainProximity(coord.destinationCoord) > 3 ? 0f : Custom.LerpMap(buzz.room.aimap.getTerrainProximity(coord.destinationCoord), 0f, 3f, 600f, 0f)), cost.legality);
                if (coord.type == MovementConnection.MovementType.ShortCut || buzz.room.aimap.getAItile(coord.destinationCoord).narrowSpace)
                {
                    cost = new PathCost(cost.resistance, cost.legality);
                }
            }
            else
            {
                cost = new PathCost(cost.resistance, cost.legality);
                if (buzz.room.aimap.getAItile(coord.destinationCoord).acc != AItile.Accessibility.Corridor && buzz.room.aimap.getAItile(coord.destinationCoord).acc != AItile.Accessibility.Climb)
                {
                    if (buzz.room.aimap.getAItile(coord.destinationCoord).smoothedFloorAltitude > 1)
                    {
                        cost.legality = PathCost.Legality.Unallowed;
                    }
                }
            }
            //DebugDrawing.DrawCross(buzz.room, buzz.room.MiddleOfTile(coord.destinationCoord), Color.Lerp(Color.white, Color.red, Mathf.InverseLerp(cost.resistance, 0f, 200f)), 15f, 1.25f);
            return cost;
        }

        public AIModule ModuleToTrackRelationship(CreatureTemplate.Relationship relationship)
        {
            if (relationship.type == CreatureTemplate.Relationship.Type.Afraid)
            {
                return threatTracker;
            }
            return null;
        }

        public CreatureTemplate.Relationship UpdateDynamicRelationship(RelationshipTracker.DynamicRelationship dRelation)
        {
            CreatureTemplate.Relationship relationship = StaticRelationship(dRelation.trackerRep.representedCreature);
            return relationship;
        }

        public RelationshipTracker.TrackedCreatureState CreateTrackedCreatureState(RelationshipTracker.DynamicRelationship rel)
        {
            return new RelationshipTracker.TrackedCreatureState();
        }

        public void SocialEvent(SocialEventRecognizer.EventID ID, Creature subjectCrit, Creature objectCrit, PhysicalObject involvedItem)
        {
        }

        public void GiftRecieved(SocialEventRecognizer.OwnedItemOnGround gift)
        {
        }

        public void LookAtNothing()
        {
        }

        public float CreatureInterestBonus(Tracker.CreatureRepresentation crit, float score)
        {
            return score;
        }

        public Tracker.CreatureRepresentation ForcedLookCreature()
        {
            return buzz.AI.focusCreature;
        }

        public bool TrackItem(AbstractPhysicalObject obj)
        {
            return true;
        }

        public void SeeThrownWeapon(PhysicalObject obj, Creature thrower)
        {
        }
    }
}
