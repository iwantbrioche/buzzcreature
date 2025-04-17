
using System;
using System.Drawing;

namespace BuzzCreature.Objects.Buzz
{
    public class BuzzAI : ArtificialIntelligence, IUseARelationshipTracker, IReactToSocialEvents, FriendTracker.IHaveFriendTracker, ILookingAtCreatures
    {
        public class Behavior(string value, bool register = false) : ExtEnum<Behavior>(value, register)
        {
            public static readonly Behavior Idle = new("Idle", register: true);
            public static readonly Behavior Flee = new("Flee", register: true);
        }

        public Buzz buzz;
        public Behavior behavior;
        public Tracker.CreatureRepresentation focusCreature;
        public CreatureLooker creatureLooker;

        public WorldCoordinate idlePos;
        public WorldCoordinate lastIdlePos;
        public int idleCounter;
        public int addOldIdlePosDelay;
        public List<WorldCoordinate> oldIdlePositions;

        public float currentUtility;

        private DebugDestinationVisualizer debugDestinationVisualizer;
        private DebugTrackerVisualizer debugTrackerVisualizer;
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
            AddModule(new StuckTracker(this, trackPastPositions: true, trackNotFollowingCurrentGoal: true));
            AddModule(new RelationshipTracker(this, tracker));
            AddModule(new UtilityComparer(this));
            stuckTracker.AddSubModule(new StuckTracker.GetUnstuckPosCalculator(stuckTracker));
            utilityComparer.AddComparedModule(threatTracker, null, 1f, 1.1f);
            utilityComparer.AddComparedModule(stuckTracker, null, 1f, 1.1f);
            creatureLooker = new(this, tracker, buzz, 0.4f, 20);
            behavior = Behavior.Idle;
            oldIdlePositions = [];

            debugDestinationVisualizer = new(world.game.abstractSpaceVisualizer, world, pathFinder, Color.red);
            debugTrackerVisualizer = new(tracker);
        }

        public override void Update()
        {
            //debugDestinationVisualizer?.Update();
            //debugTrackerVisualizer?.Update();

            if (buzz.room.game.devToolsActive && Input.GetMouseButton(2))
            {
                creature.abstractAI.SetDestination(buzz.room.GetWorldCoordinate((Vector2)Futile.mousePosition + buzz.room.game.cameras[0].pos));
            }

            if (addOldIdlePosDelay > 0) addOldIdlePosDelay--;

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
               //IdleBehavior();
            }
            else if (behavior == Behavior.Flee)
            {
                WorldCoordinate destination = threatTracker.FleeTo(creature.pos, 1, 30, currentUtility > 0.3f);
                if (threatTracker.mostThreateningCreature != null)
                {
                    focusCreature = threatTracker.mostThreateningCreature;
                }
                creature.abstractAI.SetDestination(destination);
            }

            base.Update();
        }

        public void IdleBehavior()
        {
            Vector2 pos = Random.value < 0.5f ? buzz.room.MiddleOfTile(idlePos) + Custom.RNV() * Random.value * 400f : buzz.mainBodyChunk.pos + Custom.RNV() * Random.value * 400f;
            if (IdleScore(buzz.room.GetWorldCoordinate(pos)) < IdleScore(idlePos))
            {
                idlePos = buzz.room.GetWorldCoordinate(pos);
            }
            if (IdleScore(idlePos) + idleCounter * 2f < IdleScore(pathFinder.GetDestination))
            {
                if (addOldIdlePosDelay < 1 && timeInRoom > 120)
                {
                    addOldIdlePosDelay = 90;
                    oldIdlePositions.Add(lastIdlePos);
                    if (oldIdlePositions.Count > 5)
                    {
                        oldIdlePositions.RemoveAt(0);
                    }
                    idleCounter = Random.Range(50, 200);
                }
                creature.abstractAI.SetDestination(idlePos);
                lastIdlePos = idlePos;
            }
            idleCounter--;

        }

        public float IdleScore(WorldCoordinate coord)
        {
            if (!pathFinder.CoordinatePossibleToGetBackFrom(coord))
            {
                return float.MaxValue;
            }
            float score = 0f;
            for (int i = 0; i < oldIdlePositions.Count; i++)
            {
                if (oldIdlePositions[i].room == coord.room)
                {
                    score -= Mathf.Pow(Mathf.Min(30f, oldIdlePositions[i].Tile.FloatDist(coord.Tile)), 2f) / (30f * (1f + i * 0.2f));
                }
            }
            for (int j = 0; j < tracker.CreaturesCount; j++)
            {
                if (Custom.DistLess(coord, tracker.GetRep(j).BestGuessForPosition(), 25f) && tracker.GetRep(j).dynamicRelationship.currentRelationship.type != CreatureTemplate.Relationship.Type.Ignores && tracker.GetRep(j).dynamicRelationship.state.alive)
                {
                    score += Custom.LerpMap(coord.Tile.FloatDist(tracker.GetRep(j).BestGuessForPosition().Tile), 1f, 25f, 30f * tracker.GetRep(j).representedCreature.creatureTemplate.bodySize, 0f, 0.5f);
                }
            }
            score -= Mathf.Pow(Mathf.Min(buzz.room.aimap.getTerrainProximity(coord.Tile), 5), 2f) * 2f;
            return score;
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
    }
}
