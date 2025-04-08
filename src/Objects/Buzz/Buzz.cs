
namespace BuzzCreature.Objects.Buzz
{
    public class Buzz : Creature
    {
        public BuzzAI AI;
        public Vector2 moveDir;
        public Vector2 lookDirection;
        public Vector2 bodyRotation;
        public float sinCounter;
        public bool flying;
        public float flyBoost = 1f;
        public Buzz(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
        {
            bodyChunks = new BodyChunk[2];
            bodyChunkConnections = new BodyChunkConnection[1];

            bodyChunks[0] = new(this, 0, default, 10f, 1f);
            bodyChunks[1] = new(this, 1, default, 6f, 1f);
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
            flying = true;
        }

        public override void InitiateGraphicsModule()
        {
            graphicsModule ??= new BuzzGraphics(this);
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
                //Stun(12);
            }

            bodyRotation = Custom.DirVec(bodyChunks[0].pos, bodyChunks[1].pos);
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

            // Bob up and down
            sinCounter += 1f / Mathf.Lerp(25f, 85f, Random.value);
            if (sinCounter > 1f) sinCounter -= 1f;

            bodyChunks[0].vel.y += Mathf.Sin(sinCounter * Mathf.PI) * 0.1f;
            bodyChunks[1].vel.y += Mathf.Sin(sinCounter * Mathf.PI) * 0.1f;

            bodyChunks[0].vel.y += 0.8f * flyBoost;

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

                    for (int i = 0; i < 3; i++)
                    {
                        // If the pathConnection destination isn't near terrain, use the pathConnection destination, otherwise use movementConnection destination
                        // For smoother flying
                        pathConnection = pather.FollowPath(pathConnection.destinationCoord, false);

                        if (pathConnection == default) break;
                        moveDir += room.MiddleOfTile(pathConnection.destinationCoord);
                        pathNum++;
                    }
                    moveDir /= pathNum;

                    if (flying)
                    {
                        Fly(movementConnection);
                    }
                    else
                    {
                        Crawl();
                    }
                }

            }
        }

        public void Crawl()
        {

            if (room.GetTile(mainBodyChunk.pos).AnyBeam)
            {
                bodyChunks[0].vel *= 0.9f;
                bodyChunks[1].vel *= 0.8f;
                bodyChunks[0].vel.y += gravity;
                bodyChunks[1].vel.y += gravity;
            }

            bodyChunks[0].vel += Vector2.ClampMagnitude(moveDir - bodyChunks[0].pos, 20f) / 20f * 1.25f;
        }
        public void Fly(MovementConnection movementConnection)
        {

            float accel = room.aimap.getTerrainProximity(mainBodyChunk.pos) / Mathf.Max(room.aimap.getTerrainProximity(mainBodyChunk.pos + Custom.DirVec(mainBodyChunk.pos, moveDir) * Mathf.Clamp(mainBodyChunk.vel.magnitude * 5f, 5f, 15f)), 1f);
            accel = Mathf.Min(accel, 1f);
            accel = Mathf.Pow(accel, 3f);
            if (Custom.DistLess(room.MiddleOfTile(AI.pathFinder.GetDestination.Tile), mainBodyChunk.pos, 50f) && AI.VisualContact(room.MiddleOfTile(AI.pathFinder.GetDestination.Tile), 0f))
            {
                accel *= Mathf.Lerp(0.2f, 1f, Mathf.InverseLerp(0f, 75, Vector2.Distance(room.MiddleOfTile(AI.pathFinder.GetDestination.Tile), mainBodyChunk.pos)));
            }

            flyBoost = Mathf.Lerp(flyBoost, 1f, 0.1f);

            // Slow down passively and float
            bodyChunks[0].vel *= 0.9f;
            bodyChunks[1].vel *= 0.8f;
            bodyChunks[0].vel.y += gravity * 0.9f;
            bodyChunks[1].vel.y += gravity * 0.7f;


            // Go towards destPos
            bodyChunks[0].vel += Vector2.ClampMagnitude(moveDir - bodyChunks[0].pos, 20f) / 20f * 1.5f * accel * flyBoost;

            // Rotate butt away from lookDir
            bodyChunks[1].vel -= lookDirection * 0.1f;
        }




    }
}
