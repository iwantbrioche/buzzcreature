
using Smoke;

namespace BuzzCreature.Objects.Buzz
{
    public class Buzz : Creature
    {
        public class BuzzJets
        {
            public class BuzzExhaust : PositionedSmokeEmitter
            {
                public class BuzzSmokeSegment : MeshSmoke.HyrbidSmokeSegment
                {
                    public int age;
                    public float power;

                    public override void Reset(SmokeSystem newOwner, Vector2 pos, Vector2 vel, float lifeTime)
                    {
                        base.Reset(newOwner, pos, vel, lifeTime);
                        age = 0;
                    }
                    public override void Update(bool eu)
                    {
                        base.Update(eu);
                        age++;
                        vel *= 0.9f;
                    }
                    public override Color MyColor(float timeStacker)
                    {
                        return JetSmokeColor(Mathf.InverseLerp(6f, 1f, (float)age + timeStacker));
                    }
                    public Color JetSmokeColor(float lrp)
                    {
                        HSLColor baseColor = HSLColor.Lerp(new HSLColor(0.65f, 1f, 0.5f), new HSLColor(0.5f, 0.5f, 0.6f), lrp); //blue color
                        //HSLColor baseColor = HSLColor.Lerp(new HSLColor(0.25f, 1f, 0.3f), new HSLColor(0.15f, 0.7f, 0.6f), lrp); //green color
                        //HSLColor baseColor = HSLColor.Lerp(new HSLColor(0.1f, 1f, 0.5f), new HSLColor(0.13f, 1f, 0.65f), lrp); //yellow color

                        return baseColor.rgb;
                    }
                    public override float MyOpactiy(float timeStacker)
                    {
                        return Mathf.Lerp(1f, 0.85f, Mathf.InverseLerp(2f, 6f, (float)age + timeStacker));
                    }
                    public override float MyRad(float timeStacker)
                    {
                        return Mathf.Pow(Mathf.Sin(Mathf.Lerp(lastLife, life, timeStacker) * Mathf.PI / 2f + 0.75f), 2f) * 3f * Mathf.Pow(power / 5f, 0.3f);
                    }
                    public override float ConDist(float timeStacker)
                    {
                        return Mathf.Lerp(0.1f, 0f, Mathf.InverseLerp(0.5f, 1f, Mathf.Lerp(lastLife, life, timeStacker))) * power;
                    }

                    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
                    {
                        base.InitiateSprites(sLeaser, rCam);
                        sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["SmokeTrail"];
                        sLeaser.sprites[1].shader = rCam.room.game.rainWorld.Shaders["FireSmoke"];
                        sLeaser.sprites[2].shader = rCam.room.game.rainWorld.Shaders["FireSmoke"];
                    }

                    public override void HybridDraw(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 Apos, Vector2 Bpos, Color Acol, Color Bcol, float Arad, float Brad)
                    {
                        base.HybridDraw(sLeaser, rCam, timeStacker, camPos, Apos, Bpos, Acol, Bcol, Arad, Brad);
                        sLeaser.sprites[1].alpha = Mathf.Pow(Acol.a, 0.75f);
                        Acol.a = 1f;
                        sLeaser.sprites[1].color = Acol;
                        sLeaser.sprites[1].scale = Arad / 1.5f;
                        sLeaser.sprites[1].scaleX *= power / 4.5f;
                        sLeaser.sprites[1].rotation = Custom.AimFromOneVectorToAnother(pos, pos + vel);
                        sLeaser.sprites[2].alpha = Mathf.Pow(Bcol.a, 1.1f);
                        Bcol.a = 1f;
                        sLeaser.sprites[2].color = Color.Lerp(Bcol, Color.white, Mathf.InverseLerp(0f, 0.8f, Mathf.Lerp(lastLife, life, timeStacker)) * 0.85f);
                        sLeaser.sprites[2].scale = Brad / 3f;
                        sLeaser.sprites[2].scaleY *= 1.5f;
                        sLeaser.sprites[2].rotation = Custom.AimFromOneVectorToAnother(pos, pos + vel);
                    }
                }
                public BuzzExhaust(Room room) : base(BuzzEnums.BuzzSmoke, room, default, 3, 0f, autoSpawn: false, 2f, -1)
                {
                }
                public override SmokeSystemParticle CreateParticle()
                {
                    return new BuzzSmokeSegment();
                }

                public void EmitParticles(Vector2 vel, float power)
                {
                    if (AddParticle(pos, vel * power, 3f) is BuzzSmokeSegment smokeSegment)
                    {
                        smokeSegment.power = power;
                    }
                }

                public void MoveToInternalContainer(int container, BuzzGraphics g)
                {
                    for (int i = 0; i < particles.Count; i++)
                    {
                        g.AddObjectToInternalContainer(particles[i], container);
                    }
                }
            }

            public Buzz buzz;
            public BuzzExhaust smoke;

            public BuzzJets(Buzz buzz)
            {
                this.buzz = buzz;
            }

            public void Update(bool eu)
            {

                if (buzz.room.BeingViewed)
                {
                    if (smoke == null)
                    {
                        StartSmoke();
                    }
                }

                if (smoke != null)
                {
                    if (smoke.slatedForDeletetion || buzz.room != smoke.room)
                    {
                        smoke = null;
                    }
                }
            }

            public void StartSmoke()
            {
                smoke = new(buzz.room);
                buzz.room.AddObject(smoke);
            }
        }

        public BuzzAI AI;
        public BuzzJets[] jets;
        public Vector2 moveDestination;
        public Vector2 thrustVel;
        public Vector2 lookDirection;
        public Vector2 bodyRotation;
        public Vector2 lastBodyRotation;
        public float sinCounter;
        public float jetPower = 0f;
        public bool flying;

        private bool debugVis = true;
        private bool visualizePath = false;
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
            flying = true;

            jets = new BuzzJets[4];
            for (int i = 0; i < 4; i++)
            {
                jets[i] = new(this);
            }

                
        }

        public override void InitiateGraphicsModule()
        {
            graphicsModule ??= new BuzzGraphics(this);
        }

        public override void NewRoom(Room newRoom)
        {
            base.NewRoom(newRoom);

            if (visualizePath)
            {
                AI.followPathVisualizer = new(AI.pathFinder);
                newRoom.AddObject(AI.followPathVisualizer);
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

            //Vector2 middleOfRoom = room.MiddleOfTile(room.TileWidth / 2, room.TileHeight / 2);
            //bodyChunks[0].HardSetPosition(middleOfRoom);
            //bodyChunks[1].HardSetPosition(middleOfRoom + Custom.DirVec(bodyChunks[0].pos, (Vector2)Futile.mousePosition + room.game.cameras[0].pos) * 15f);

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

            for (int i = 0; i < 4; i++)
            {
                jets[i].Update(eu);
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
                // Grab the first MovementConnection in the path starting from the Buzzes' mainBodyChunk
                //  The actuallyFollowingThisPath parameter is specifically for leaving the room and stuff relating to savedPathConnections
                MovementConnection startingConnection = pather.FollowPath(room.GetWorldCoordinate(mainBodyChunk.pos), actuallyFollowingThisPath: true);

                // DebugDrawing of the MovementConnection, lists it's type, startCoord tile coords, and destinationCoord tile coords
                if (debugVis) DebugDrawing.DrawText(room, startingConnection.ToString(), mainBodyChunk.pos + new Vector2(15f, 15f), Color.white);

                // FollowPath returns the default of MovementConnection if it can't find a path
                if (startingConnection != default)
                {
                    // Shortcuts and NPCTransport
                    GoThroughFloors = startingConnection.destinationCoord.y < startingConnection.startCoord.y;
                    if (startingConnection.type is MovementConnection.MovementType.ShortCut or MovementConnection.MovementType.NPCTransportation)
                    {
                        enteringShortCut = startingConnection.StartTile;
                        if (startingConnection.type is MovementConnection.MovementType.NPCTransportation)
                        {
                            NPCTransportationDestination = startingConnection.destinationCoord;
                        }
                    }
                    else
                    {
                        // The moveDestination variable is where the buzz will move towards in the room
                        moveDestination = room.MiddleOfTile(startingConnection.destinationCoord);

                        // In order to make the Buzzes' flight less linear, we will use a for loop to call FollowPath multiple times,
                        //  we then average the positions of those MovementConnections to smooth out the moveDestination

                        // We create a new MovementConnection from the old connection and a counter for averaging
                        MovementConnection pathConnection = startingConnection;
                        int pathNum = 1;

                        for (int i = 0; i < 3; i++)
                        {
                            // This prevents the buzz from getting stuck in narrow spaces
                            if (room.aimap.getAItile(pathConnection.destinationCoord).narrowSpace) break;

                            // Set pathConnection to a new MovementCoordinate by using it's previous destinationCoord as the starting WorldCoordinate
                            //  This way it continues the path towards the destination from the previous MovementConnection found by FollowPath
                            //  actuallyFollowingThisPath is false since the Buzz is not going to be moving directly towards this tile
                            pathConnection = pather.FollowPath(pathConnection.destinationCoord, actuallyFollowingThisPath: false);

                            // If the pathConnection returns default, then it cannot find the next MovementConnection in the path, so we break
                            if (pathConnection == default) break;

                            // Buzz specific since they kept slamming into corners lol, just breaks the loop if the Buzz can't directly see the pathConnection's destinationCoord,
                            //  The destinationCoord being the tile being moved towards 
                            if (!AI.VisualContact(room.MiddleOfTile(pathConnection.destinationCoord), 0f)) break;

                            // Another DebugDrawing, this draws the current pathNum on the pathConnection's startCoord, the startCoord being the tile it started pathing from
                            if (debugVis) DebugDrawing.DrawText(room, $"{pathNum}", room.MiddleOfTile(pathConnection.startCoord), Color.green);

                            // Add the position of pathConnection.startCoord to moveDestination and increment pathNum
                            moveDestination += room.MiddleOfTile(pathConnection.startCoord);
                            pathNum++;
                        }
                        // Divide moveDestination pathNum to get the average of all the MovementConnections found during the loop
                        moveDestination /= pathNum;

                        // If entering a shortcut force the buzz to move directly into it
                        if (enteringShortCut.HasValue)
                        {
                            moveDestination = room.MiddleOfTile(enteringShortCut.Value);
                        }

                        // Handle movement
                        if (flying)
                        {
                            Fly();
                        }
                        else
                        {
                            Crawl();
                        }
                    }
                }
            }
        }
        public void Fly()
        {
            // Bob up and down
            sinCounter += 1f / Mathf.Lerp(25f, 85f, Random.value);
            if (sinCounter > 1f) sinCounter -= 1f;

            bodyChunks[0].vel.y += Mathf.Sin(sinCounter * Mathf.PI * Mathf.Sin(sinCounter * Mathf.PI) * 2f) * 0.2f;
            bodyChunks[1].vel.y += Mathf.Sin(sinCounter * Mathf.PI * Mathf.Sin(sinCounter * Mathf.PI) * 2f) * 0.2f;

            bodyChunks[0].vel.y += 0.8f;


            float accel = 1f;
            if (AI.pathFinder.GetDestination.room == room.abstractRoom.index && Custom.DistLess(room.MiddleOfTile(AI.pathFinder.destination.Tile), mainBodyChunk.pos, 30f) && AI.VisualContact(room.MiddleOfTile(AI.pathFinder.destination.Tile), 0f))
            {
                // Slow down when near destination
                accel *= Mathf.Lerp(0.2f, 1f, Mathf.InverseLerp(0f, 40f, Vector2.Distance(room.MiddleOfTile(AI.pathFinder.destination.Tile), mainBodyChunk.pos)));
            }

            // DebugDrawing of a line pointing from the Buzzes' mainBodyChunk to the moveDestination
            if (debugVis) DebugDrawing.DrawLine(room, mainBodyChunk.pos, moveDestination, Color.red, 2f);
            thrustVel = Vector2.ClampMagnitude(moveDestination - mainBodyChunk.pos, 20f) / 15f * 1.75f * accel;


            // Slow down passively and float
            bodyChunks[0].vel *= 0.95f;
            bodyChunks[0].vel.y += gravity * Mathf.Lerp(0.9f, -1.3f, Mathf.InverseLerp(-0.4f, -2f, thrustVel.y) / (1f + Mathf.InverseLerp(0.3f, 1f, Mathf.Abs(thrustVel.x)) / 2f));
            bodyChunks[1].vel *= 0.85f;
            bodyChunks[1].vel.y += gravity * Mathf.Lerp(0.6f, -0.9f, Mathf.InverseLerp(-0.4f, -2f, thrustVel.y) / (1f + Mathf.InverseLerp(0.3f, 1f, Mathf.Abs(thrustVel.x)) / 2f));

            // Go towards destination
            bodyChunks[0].vel += thrustVel * jetPower;
            if (enteringShortCut.HasValue)
            {
                bodyChunks[1].vel += thrustVel * jetPower;
            }

            // Rotate butt away from lookDir
            bodyChunks[1].vel -= lookDirection * 0.1f;

            jetPower = Mathf.Lerp(jetPower, 1f, 0.5f);

            if (Custom.DistLess(bodyChunks[1].pos, bodyChunks[0].pos, 10f))
            {
                bodyChunks[1].vel -= Custom.DirVec(bodyChunks[1].pos, bodyChunks[0].pos) + new Vector2(Random.value, 0f);
                bodyChunks[0].vel += Custom.DirVec(bodyChunks[1].pos, bodyChunks[0].pos) + new Vector2(Random.value, 0f);
            }
        }
        public void Crawl()
        {
            jetPower = Mathf.Lerp(jetPower, 0f, 0.5f);

            Vector2 moveDir = Custom.DirVec(mainBodyChunk.pos, moveDestination);

            bodyChunks[0].vel += moveDir * 2f;
        }


    }
}
