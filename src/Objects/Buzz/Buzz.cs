
namespace BuzzCreature.Objects.Buzz
{
    public class Buzz : Creature
    {
        public class MovementMode(string value, bool register = false) : ExtEnum<MovementMode>(value, register)
        {
            public static readonly MovementMode Flying = new("Flying", register: true);
            public static readonly MovementMode Crawl = new("Crawl", register: true);
        }

        public BuzzAI AI;
        public MovementMode movementMode;
        public Vector2 moveDir;
        public Vector2 thrustVel;
        public Vector2 lookDirection;
        public Vector2 bodyRotation;
        public Vector2 lastBodyRotation;
        public float sinCounter;
        public float jetPower = 1f;

        private bool debugVis = false;
        private DebugSprite debugDirSprite;
        public Buzz(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
        {
            bodyChunks = new BodyChunk[2];
            bodyChunkConnections = new BodyChunkConnection[1];

            bodyChunks[0] = new(this, 0, default, 10f, 1f);
            bodyChunks[1] = new(this, 1, default, 8f, 1f);
            bodyChunkConnections[0] = new(bodyChunks[0], bodyChunks[1], 20f, BodyChunkConnection.Type.Pull, 1.5f, -1f);
            mainBodyChunkIndex = 0;
            airFriction = 0.999f;
            gravity = 0.9f;
            bounce = 0.3f;
            surfaceFriction = 0.4f;
            collisionLayer = 1;
            waterFriction = 0.96f;
            buoyancy = 1.05f;
            sinCounter = Random.value;
            movementMode = MovementMode.Flying;

            if (debugVis)
            {
                debugDirSprite = new(default, new FSprite("pixel"), abstractCreature.realizedCreature.room);
                debugDirSprite.sprite.anchorY = 0f;
                debugDirSprite.sprite.scaleX = 2f;
                debugDirSprite.sprite.color = Color.green;
            }
        }

        public override void InitiateGraphicsModule()
        {
            graphicsModule ??= new BuzzGraphics(this);
        }

        public override void NewRoom(Room newRoom)
        {
            base.NewRoom(newRoom);
            if (debugVis)
            {
                if (debugDirSprite.room != newRoom)
                {
                    debugDirSprite.RemoveFromRoom();
                    newRoom.AddObject(debugDirSprite);
                }
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (room == null)
            {
                return;
            }
            if (room.game.devToolsActive && Input.GetKey("b") && room.game.cameras[0].room == room)
            {
                bodyChunks[0].vel += Custom.DirVec(bodyChunks[0].pos, (Vector2)Futile.mousePosition + room.game.cameras[0].pos) * 14f;
                Stun(12);
            }

            lastBodyRotation = bodyRotation;
            bodyRotation = new (Custom.DirVec(bodyChunks[0].pos, bodyChunks[1].pos).x, Custom.DirVec(bodyChunks[1].pos, bodyChunks[0].pos).y);
            if (Consious && AI.creatureLooker.lookCreature != null)
            {
                if (AI.creatureLooker.lookCreature.VisualContact)
                {
                    lookDirection = Custom.DirVec(mainBodyChunk.pos, AI.creatureLooker.lookCreature.representedCreature.realizedCreature.firstChunk.pos);
                }
                else
                {
                    lookDirection = Custom.DirVec(mainBodyChunk.pos, room.MiddleOfTile(AI.creatureLooker.lookCreature.BestGuessForPosition()));
                }
                Vector2 lookRot = Custom.RotateAroundOrigo(lookDirection, Custom.AimFromOneVectorToAnother(Vector2.zero, new Vector2(0f - bodyRotation.x, bodyRotation.y)));
            }
            else
            {
                lookDirection = Vector2.Lerp(lookDirection, Vector2.zero, 0.1f);
            }

            Act();
        }

        public void Act()
        {
            AI.Update();

            StandardPather pather = AI.pathFinder as StandardPather;

            if ((State as HealthState).health < 0.5f && Random.value > (State as HealthState).health && Random.value < 1f / 3f)
            {
                Stun(4);
                if ((State as HealthState).health <= 0f && Random.value < 0.25f)
                {
                    Die();
                }
            }
            if (Consious)
            {
                MovementConnection movementConnection = pather.FollowPath(room.GetWorldCoordinate(mainBodyChunk.pos), true);
                if (movementConnection != default)
                {
                    // Shortcuts and NPCTransport
                    GoThroughFloors = movementConnection.destinationCoord.y < movementConnection.startCoord.y;
                    if (movementConnection.type is MovementConnection.MovementType.ShortCut or MovementConnection.MovementType.NPCTransportation)
                    {
                        enteringShortCut = movementConnection.StartTile;
                        if (movementConnection.type is MovementConnection.MovementType.NPCTransportation)
                        {
                            NPCTransportationDestination = movementConnection.destinationCoord;
                        }
                    }
                    else
                    {
                        moveDir = room.MiddleOfTile(movementConnection.destinationCoord);
                        MovementConnection pathConnection = movementConnection;
                        int pathNum = 1;

                        for (int i = 0; i < 4; i++)
                        {
                            pathConnection = pather.FollowPath(movementConnection.destinationCoord, false);

                            if (pathConnection == default) break;
                            moveDir += room.MiddleOfTile(pathConnection.destinationCoord);
                            pathNum++;
                        }
                        moveDir /= pathNum;

                        if (enteringShortCut.HasValue)
                        {
                            moveDir = room.MiddleOfTile(enteringShortCut.Value);
                        }

                        if (movementMode == MovementMode.Flying)
                        {
                            // Bob up and down
                            sinCounter += 1f / Mathf.Lerp(25f, 85f, Random.value);
                            if (sinCounter > 1f) sinCounter -= 1f;

                            bodyChunks[0].vel.y += Mathf.Sin(sinCounter * Mathf.PI * Mathf.Sin(sinCounter * Mathf.PI) * 2f) * 0.2f;
                            bodyChunks[1].vel.y += Mathf.Sin(sinCounter * Mathf.PI * Mathf.Sin(sinCounter * Mathf.PI) * 2f) * 0.2f;

                            bodyChunks[0].vel.y += 0.8f;

                            // Keep away from terrain while flying
                            if (room.aimap.getTerrainProximity(moveDir) == 1 && !room.aimap.getAItile(moveDir).narrowSpace && !enteringShortCut.HasValue)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    if (room.GetTile(room.GetTilePosition(moveDir) + Custom.fourDirections[i]).Solid)
                                    {
                                        moveDir -= Custom.fourDirections[i].ToVector2() * 15f;
                                    }
                                }
                            }

                            float accel = 1f;
                            if (AI.pathFinder.GetDestination.room == room.abstractRoom.index && Custom.DistLess(room.MiddleOfTile(AI.pathFinder.destination.Tile), mainBodyChunk.pos, 30f) && AI.VisualContact(room.MiddleOfTile(AI.pathFinder.destination.Tile), 0f))
                            {
                                // Slow down when near destination
                                accel *= Mathf.Lerp(0.2f, 1f, Mathf.InverseLerp(0f, 40f, Vector2.Distance(room.MiddleOfTile(AI.pathFinder.destination.Tile), mainBodyChunk.pos)));
                            }

                            thrustVel = Vector2.ClampMagnitude(moveDir - bodyChunks[0].pos, 20f) / 15f * 1.75f * accel;


                            // Slow down passively and float
                            bodyChunks[0].vel *= 0.95f;
                            bodyChunks[1].vel *= 0.85f;
                            bodyChunks[0].vel.y += gravity * Mathf.Lerp(0.9f, -1.3f, Mathf.InverseLerp(-0.4f, -2f, thrustVel.y) / 1.5f + Mathf.InverseLerp(0.3f, 1f, Mathf.Abs(thrustVel.x)) / 2f);
                            bodyChunks[1].vel.y += gravity * Mathf.Lerp(0.6f, -0.9f, Mathf.InverseLerp(-0.4f, -2f, thrustVel.y) / 1.5f + Mathf.InverseLerp(0.3f, 1f, Mathf.Abs(thrustVel.x)) / 2f);

                            // Go towards destPos
                            bodyChunks[0].vel += thrustVel;

                            // Rotate butt away from lookDir
                            bodyChunks[1].vel -= lookDirection * 0.1f;

                            if (Custom.DistLess(bodyChunks[1].pos, bodyChunks[0].pos, 10f))
                            {
                                bodyChunks[1].vel -= Custom.DirVec(bodyChunks[1].pos, bodyChunks[0].pos) + new Vector2(Random.value, 0f);
                                bodyChunks[0].vel += Custom.DirVec(bodyChunks[1].pos, bodyChunks[0].pos) + new Vector2(Random.value, 0f);
                            }

                            if (debugVis)
                            {
                                debugDirSprite.pos = bodyChunks[0].pos;
                                debugDirSprite.sprite.rotation = Custom.AimFromOneVectorToAnother(bodyChunks[0].pos, moveDir);
                                debugDirSprite.sprite.scaleY = Vector2.Distance(bodyChunks[0].pos, moveDir);
                                debugDirSprite.Update(evenUpdate);
                            }
                        }
                        else if (movementMode == MovementMode.Crawl)
                        {

                        }
                        else
                        {

                        }
                    }
                }
            }
        }
    }
}
