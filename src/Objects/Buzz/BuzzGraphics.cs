
using Smoke;

namespace BuzzCreature.Objects.Buzz
{
    /*  ivar ideas
     * eye variation
     * abdomen size
     * palette swap (dark to light)
     * old colors
     * longer antennae
     * floppier antennae
     */ 
    public class BuzzGraphics : GraphicsModule
    {
        private class BuzzAntennae
        {
            private BuzzGraphics graphics;
            private Buzz buzz => graphics.buzz;
            private int firstSprite;
            public int totalSprites;
            public GenericBodyPart[,] antennae;
            public int segments;
            private float length;

            public BuzzAntennae(BuzzGraphics ow, int firstSprite)
            {
                graphics = ow;
                this.firstSprite = firstSprite;
                length = 14;
                segments = Mathf.FloorToInt(length / 3f);
                antennae = new GenericBodyPart[2, segments];
                for (int i = 0; i < segments; i++)
                {
                    antennae[0, i] = new GenericBodyPart(graphics, 0.2f, 0.4f, 0.6f, buzz.bodyChunks[0]);
                    antennae[1, i] = new GenericBodyPart(graphics, 0.2f, 0.4f, 0.6f, buzz.bodyChunks[0]);
                }

                totalSprites = segments * 2;
            }

            private int AntennaSprite(int side, int part)
            {
                return firstSprite + part * 2 + side;
            }
            private Vector2 AntennaDir(int side)
            {
                return Custom.RotateAroundOrigo(new Vector2(side == 0 ? -1f : 1f, -1f).normalized, 180f);
            }
            private Vector2 AnchorPoint(int side)
            {
                return graphics.head.pos + AntennaDir(side) * 2.5f;
            }

            public void Update()
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < segments; j++)
                    {
                        float a = (float)j / (float)(segments - 1);
                        a = Mathf.Lerp(a, Mathf.InverseLerp(0f, 5f, j), 0.2f);
                        antennae[i, j].vel += AntennaDir(i) * a;
                        antennae[i, j].vel.y += 0.4f * a;
                        antennae[i, j].Update();

                        Vector2 pos = buzz.bodyChunks[0].pos;
                        if (j == 0)
                        {
                            antennae[i, j].vel += AntennaDir(i) * 5f;
                            antennae[i, j].vel.x -= graphics.bodyRotation.x * -6f;
                            antennae[i, j].ConnectToPoint(AnchorPoint(i), 4f, push: true, 0f, buzz.bodyChunks[0].vel, 0f, 0f);
                        }
                        else
                        {
                            pos = j > 0 ? antennae[i, j - 1].pos : AnchorPoint(i);
                            Vector2 dir = Custom.DirVec(pos, antennae[i, j - 1].pos);
                            float dist = Vector2.Distance(pos, antennae[i, j - 1].pos);
                            antennae[i, j].ConnectToPoint(antennae[i, j - 1].pos + dir * dist, 6f, true, 0f, buzz.bodyChunks[0].vel, 0f, 0f);
                        }
                        antennae[i, j].vel += Custom.DirVec(pos, antennae[i, j].pos) * 3f * Mathf.Pow(1f - a, 0.3f);
                        antennae[i, j].vel += graphics.lookDirection * 0.1f * a;
                        antennae[i, j].vel.y += Mathf.Abs(buzz.bodyChunks[0].vel.y);

                        if (!Custom.DistLess(buzz.mainBodyChunk.pos, antennae[i, j].pos, 200f))
                        {
                            antennae[i, j].pos = buzz.mainBodyChunk.pos;
                        }
                    }
                }
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < segments; j++)
                    {
                        sLeaser.sprites[AntennaSprite(i, j)] = new FSprite("Circle20");
                        sLeaser.sprites[AntennaSprite(i, j)].scaleX = 0.15f;
                        sLeaser.sprites[AntennaSprite(i, j)].scaleY = 0.3f;
                    }
                }
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < segments; j++)
                    {
                        Vector2 antennaPos = Vector2.Lerp(antennae[i, j].lastPos, antennae[i, j].pos, timeStacker);

                        sLeaser.sprites[AntennaSprite(i, j)].SetPosition(antennaPos - camPos);
                        sLeaser.sprites[AntennaSprite(i, j)].rotation = Custom.AimFromOneVectorToAnother(j == 0 ? antennae[i, j + 1].pos : antennae[i, j - 1].pos, antennae[i, j].pos);
                    }
                }
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < segments; j++)
                    {
                        sLeaser.sprites[AntennaSprite(i, j)].MoveBehindOtherNode(sLeaser.sprites[graphics.EyeSprites]);
                    }
                }
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                Color blackColor = Color.Lerp(palette.blackColor, graphics.darkColor, 0.5f);
                Color antennaeColor = Color.Lerp(blackColor, graphics.lightColor, 0.1f);
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < segments; j++)
                    {
                        sLeaser.sprites[AntennaSprite(i, j)].color = Color.Lerp(blackColor, antennaeColor, Mathf.Pow((float)j / (float)segments, 0.5f));
                    }
                }
            }
        }
        private class BuzzHand : Limb
        {
            public BuzzGraphics graphics => owner as BuzzGraphics;
            public Buzz buzz => graphics.buzz;
            public Vector2 connectionPos;
            public Vector2 lastConnectionPos;
            public float armLength;
            public int firstSprite;
            public int totalSprites;
            public int side;
            public bool small;
            private bool debugVis = false;
            public BuzzHand(BuzzGraphics owner, int limbNum, int firstSprite) : base(owner, owner.buzz.bodyChunks[0], limbNum, 1f, 0.3f, 0.6f, 12f, 0.8f)
            {
                this.firstSprite = firstSprite;
                small = limbNumber % 2 == 0;
                totalSprites += 1;
                armLength = small ? 10f : 20f;
                if (debugVis)
                {
                    totalSprites += 3;
                    if (!small) totalSprites += 1;
                }
                mode = Mode.Dangle;
            }

            public override void Update()
            {
                base.Update();

                if (buzz.movementMode == Buzz.MovementMode.Flying)
                {
                    mode = Mode.HuntAbsolutePosition;
                    absoluteHuntPos = new Vector2(buzz.bodyChunks[1].pos.x, Mathf.Lerp(buzz.bodyChunks[0].pos.y - 30f, buzz.bodyChunks[1].pos.y, Mathf.Pow(Mathf.InverseLerp(0.4f, 1f, Mathf.Abs(buzz.bodyRotation.x)), 0.8f)));
                    if (!small)
                    {
                        absoluteHuntPos.x += 5f * side;
                        absoluteHuntPos.y -= Mathf.Abs(graphics.bodyRotation.x) * 6f;

                        if (side == -Mathf.Sign(graphics.bodyRotation.x))
                        {
                            absoluteHuntPos.x -= Mathf.Sin(graphics.bodyRotation.x * Mathf.PI) * 10f;
                        }
                    }
                    else
                    {
                        absoluteHuntPos.x -= graphics.bodyRotation.x * 3f;
                        absoluteHuntPos.y -= Mathf.Abs(graphics.bodyRotation.x) * 15f;
                    }
                }

                lastConnectionPos = connectionPos;
                connectionPos = connection.pos;
                if (!small)
                {
                    connectionPos.x += 8f * side;
                    connectionPos.x -= graphics.bodyRotation.x * 2f;
                    connectionPos.y -= Mathf.Abs(graphics.bodyRotation.x) * 2f;
                    if (side == Mathf.Sign(graphics.bodyRotation.x))
                    {
                        connectionPos.x -= graphics.bodyRotation.x * 5f;
                        connectionPos.y += Mathf.Abs(graphics.bodyRotation.x * 3f);
                    }
                    else
                    {
                        connectionPos.x += graphics.bodyRotation.x * 2f;
                        connectionPos.y += Mathf.Abs(graphics.bodyRotation.x * 3f);
                    }
                }
                else
                {
                    connectionPos.y -= 4f;
                    connectionPos.y -= 1f + Mathf.Clamp01(-graphics.bodyRotation.y);
                    connectionPos.x += 3f * side;
                    connectionPos.x -= graphics.bodyRotation.x * 7f;
                }

                vel.y -= graphics.buzz.gravity;
                if (mode == Mode.Dangle)
                {
                    ConnectToPoint(connectionPos, armLength, push: false, 0f, Vector2.zero, 0.4f, 0.1f);
                }
                else
                {
                    ConnectToPoint(connectionPos, armLength, push: false, 0f, connection.vel, 0.4f, 0.1f);
                }
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                TriangleMesh.Triangle[] tris =
                [
                    new(0,1,2),
                    new(2,1,3),
                    new(3,2,4),
                    new(4,2,5),
                    new(5,4,6),
                    new(6,4,7),
                    new(7,6,8),
                    new(8,6,9),
                    new(9,8,10)
                ];

                if (small)
                {
                    tris =
                    [
                        new(0,1,2),
                        new(2,1,3),
                        new(3,2,4)
                    ];

                }

                if (debugVis)
                {
                    if (!small)
                    {
                        sLeaser.sprites[firstSprite + 1] = new("pixel")
                        {
                            scale = 2f,
                            color = new(1f, 0f, 0f)
                        };
                        sLeaser.sprites[firstSprite + 2] = new("pixel")
                        {
                            scale = 1f,
                            color = new(0f, 1f, 0f)
                        };
                        sLeaser.sprites[firstSprite + 3] = new("pixel")
                        {
                            scale = 1f,
                            color = new(0f, 1f, 1f)
                        };
                        sLeaser.sprites[firstSprite + 4] = new("pixel")
                        {
                            scale = 1f,
                            color = new(0f, 0f, 1f)
                        };
                    }
                    else
                    {
                        sLeaser.sprites[firstSprite + 1] = new("pixel")
                        {
                            scale = 2f,
                            color = new(0.7f, 0f, 0f)
                        };
                        sLeaser.sprites[firstSprite + 2] = new("pixel")
                        {
                            scale = 1f,
                            color = new(0f, 0.7f, 0f)
                        };
                        sLeaser.sprites[firstSprite + 3] = new("pixel")
                        {
                            scale = 1f,
                            color = new(0f, 0f, 0.7f)
                        };
                    }
                }
                sLeaser.sprites[firstSprite] = new TriangleMesh("Futile_White", tris, customColor: true);


            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                Vector2 connection = Vector2.Lerp(lastConnectionPos, connectionPos, timeStacker);
                Vector2 handPos = Vector2.Lerp(lastPos, pos, timeStacker);

                float flip = side;
                if (small)
                {
                    flip = Mathf.Lerp(flip, -Mathf.Sign(graphics.bodyRotation.x), Mathf.Abs(graphics.bodyRotation.x));
                }
                float ikX = small ? 3f : 16f;
                float ikY = small ? 5f : 7f;
                Vector2 elbowIK = Custom.InverseKinematic(connection, handPos, ikY, ikX, -flip);
                Vector2 wristIK = Custom.InverseKinematic(elbowIK, handPos, 9f, 5f, flip);

                (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(0, connection + -side * Custom.PerpendicularVector(elbowIK - connection) - camPos);
                (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(1, connection + side * Custom.PerpendicularVector(elbowIK - connection) * 2f - camPos);
                if (!small)
                {
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(2, elbowIK - (connection - elbowIK).normalized * flip - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(3, elbowIK + Custom.PerpendicularVector(connection - elbowIK) * flip * 3f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(4, elbowIK + Custom.PerpendicularVector(elbowIK - wristIK) * flip - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(5, elbowIK - Custom.PerpendicularVector(elbowIK - wristIK) * flip * 1.5f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(6, wristIK - Custom.PerpendicularVector(wristIK - elbowIK) * flip * 1.5f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(7, wristIK + Custom.PerpendicularVector(wristIK - elbowIK) * flip * 1.5f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(8, wristIK + Custom.PerpendicularVector(elbowIK - handPos) * flip * 2f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(9, wristIK - Custom.PerpendicularVector(elbowIK - handPos) * flip * 1.5f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(10, handPos - Custom.PerpendicularVector(elbowIK - handPos) * flip * 1.5f - camPos);
                }
                else
                {
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(2, elbowIK + -side * Custom.PerpendicularVector(connection - elbowIK) - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(3, elbowIK + side * Custom.PerpendicularVector(connection - elbowIK) * 1.5f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(4, handPos - camPos);
                }

                if (debugVis)
                {
                    if (small)
                    {
                        sLeaser.sprites[firstSprite + 1].SetPosition(connection - camPos);
                        sLeaser.sprites[firstSprite + 2].SetPosition(elbowIK - camPos);
                        sLeaser.sprites[firstSprite + 3].SetPosition(handPos - camPos);
                    }
                    else
                    {
                        sLeaser.sprites[firstSprite + 1].SetPosition(connection - camPos);
                        sLeaser.sprites[firstSprite + 2].SetPosition(elbowIK - camPos);
                        sLeaser.sprites[firstSprite + 3].SetPosition(wristIK - camPos);
                        sLeaser.sprites[firstSprite + 4].SetPosition(handPos - camPos);
                    }
                }

                if (graphics.bodyRotation.y < -0.7f)
                {
                    sLeaser.sprites[firstSprite].MoveBehindOtherNode(sLeaser.sprites[graphics.BodySprite]);
                }
                else
                {
                    sLeaser.sprites[firstSprite].MoveInFrontOfOtherNode(sLeaser.sprites[graphics.BodySprite]);
                }
                if (!small)
                {
                    if (side == -1)
                    {
                        if (graphics.bodyRotation.x < 0.5f)
                        {
                            sLeaser.sprites[firstSprite].MoveInFrontOfOtherNode(sLeaser.sprites[graphics.BodySprite]);
                        }
                        else if (graphics.bodyRotation.x < 0.8f)
                        {
                            sLeaser.sprites[firstSprite].MoveBehindOtherNode(sLeaser.sprites[graphics.BodySprite]);
                        }
                        else
                        {
                            sLeaser.sprites[firstSprite].MoveBehindOtherNode(sLeaser.sprites[graphics.buttSprites - 1]);
                        }
                    }
                    else
                    {
                        if (graphics.bodyRotation.x > -0.5f)
                        {
                            sLeaser.sprites[firstSprite].MoveInFrontOfOtherNode(sLeaser.sprites[graphics.BodySprite]);
                        }
                        else if (graphics.bodyRotation.x > -0.8f)
                        {
                            sLeaser.sprites[firstSprite].MoveBehindOtherNode(sLeaser.sprites[graphics.BodySprite]);
                        }
                        else
                        {
                            sLeaser.sprites[firstSprite].MoveBehindOtherNode(sLeaser.sprites[graphics.buttSprites - 1]);
                        }
                    }
                }


            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                Color blackColor = Color.Lerp(palette.blackColor, graphics.darkColor, 0.5f);
                for (int i = 0; i < (sLeaser.sprites[firstSprite] as TriangleMesh).verticeColors.Length; i++)
                {
                    float t = (float)i / (float)((sLeaser.sprites[firstSprite] as TriangleMesh).verticeColors.Length - 1);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).verticeColors[i] = Color.Lerp(blackColor, Color.Lerp(graphics.bodyColor, Color.white, 0.7f), Mathf.Pow(t, 1.5f));
                }
            }
        }
        private class BuzzJets : PositionedSmokeEmitter
        {
            private BuzzGraphics graphics;
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
            public BuzzJets(Room room, BuzzGraphics g) : base(BuzzEnums.BuzzSmoke, room, default, 3, 0f, autoSpawn: false, 2f, -1)
            {
                graphics = g;
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

            public void MoveToInternalContainer(int container)
            {
                for (int i = 0; i < particles.Count; i++)
                {
                    graphics.AddObjectToInternalContainer(particles[i], container);
                }
            }
        }

        private struct JetData
        {
            public Vector2 pos;
            public Vector2 lastPos;
            public Vector2 scale;
            public Vector2 lastScale;
            public float rotation;
            public float lastRotation;
            public float flamePower;
            public float lastFlamePower;
            public BuzzJets smoke;
        }

        private Buzz buzz;
        private BuzzAntennae antennae;
        private BuzzHand[,] hands;
        public GenericBodyPart head;

        public Vector2[,] drawPositions;
        public Vector2 bodyRotation;
        public Vector2 lookDirection;

        public int totalSprites;
        private int BodySprite;
        private int HeadSprite;
        private int EyeSprites;
        private int JetSprites;

        private int buttSprites;
        private Vector2[] abdomenPositions;
        private Vector2[] lastAbdomenPositions;
        private float[] abdomenScales;
        private float[] lastAbdomenScales;

        private JetData[] jetData;

        public Color bodyColor;
        public Color lightColor;
        public Color darkColor;


        public BuzzGraphics(PhysicalObject ow) : base(ow, internalContainers: true)
        {
            buzz = ow as Buzz;
            buttSprites = 16;
            abdomenPositions = new Vector2[buttSprites];
            lastAbdomenPositions = new Vector2[buttSprites];
            abdomenScales = new float[buttSprites];
            lastAbdomenScales = new float[buttSprites];
            jetData = new JetData[4];

            totalSprites = buttSprites;
            BodySprite = totalSprites++;
            HeadSprite = totalSprites++;
            EyeSprites = totalSprites;
            totalSprites += 2;
            JetSprites = totalSprites;
            totalSprites += 4;

            antennae = new(this, totalSprites);
            totalSprites += antennae.totalSprites;

            hands = new BuzzHand[2, 2];
            int limbNum = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    hands[i, j] = new(this, limbNum++, totalSprites);
                    totalSprites += hands[i, j].totalSprites;
                    hands[i, j].side = -1 + i * 2;
                }
            }

            drawPositions = new Vector2[buzz.bodyChunks.Length, 2];
            for (int i = 0; i < buzz.bodyChunks.Length; i++)
            {
                drawPositions[i, 0] = buzz.bodyChunks[i].pos;
                drawPositions[i, 0] = buzz.bodyChunks[i].lastPos;
            }

            head = new(this, 2f, 0.8f, 0.6f, buzz.bodyChunks[0]);

            List<BodyPart> bodyParts = [];

            bodyParts.Add(head);

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < antennae.segments; j++)
                {
                    bodyParts.Add(antennae.antennae[i, j]);
                }
                for (int b = 0; b < 2; b++)
                {
                    bodyParts.Add(hands[i, b]);
                }
            }

            this.bodyParts = [.. bodyParts];

            for (int i = 0; i < 4; i++)
            {
                jetData[i].smoke = new(buzz.room, this);
                buzz.room.AddObject(jetData[i].smoke);
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.containers = new FContainer[4];
            for (int i = 0; i < sLeaser.containers.Length; i++)
            {
                sLeaser.containers[i] = new FContainer();
            }
            sLeaser.sprites = new FSprite[totalSprites];
            for (int i = 0; i < buttSprites; i++)
            {
                sLeaser.sprites[i] = new("Futile_White");
                sLeaser.sprites[i].shader = Custom.rainWorld.Shaders["JaggedCircle"];
                sLeaser.sprites[i].alpha = 0.7f;
            }

            sLeaser.sprites[BodySprite] = new("buzzBody0");

            sLeaser.sprites[HeadSprite] = new("Circle20");
            sLeaser.sprites[HeadSprite].scaleX = 0.65f;
            sLeaser.sprites[HeadSprite].scaleY = 0.6f;

            sLeaser.sprites[EyeSprites] = new("Circle20");
            sLeaser.sprites[EyeSprites].scale = 0.19f;
            sLeaser.sprites[EyeSprites + 1] = new("Circle20");
            sLeaser.sprites[EyeSprites + 1].scale = 0.19f;

            for (int i = 0; i < 4; i++)
            {
                sLeaser.sprites[JetSprites + i] = new("Futile_White");
            }

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    hands[i, j].InitiateSprites(sLeaser, rCam);
                }
            }

            antennae.InitiateSprites(sLeaser, rCam);
            AddToContainer(sLeaser, rCam, null);

            base.InitiateSprites(sLeaser, rCam);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.RemoveAllSpritesFromContainer();

            newContatiner ??= rCam.ReturnFContainer("Midground");

            for (int i = 0; i < sLeaser.containers.Length; i++)
            {
                newContatiner.AddChild(sLeaser.containers[i]);
            }
            for (int i = buttSprites - 1; i >= 0; i--)
            {
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
            for (int i = buttSprites; i < totalSprites; i++)
            {
                newContatiner.AddChild(sLeaser.sprites[i]);
            }

            antennae.AddToContainer(sLeaser, rCam, newContatiner);

        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            bodyRotation = Vector2.Lerp(buzz.lastBodyRotation, buzz.bodyRotation, timeStacker);

            Vector2 headPos = Vector2.Lerp(head.lastPos, head.pos, timeStacker);
            Vector2 bodyPos = Vector2.Lerp(drawPositions[0, 1], drawPositions[0, 0], timeStacker);
            Vector2 lowerBodyPos = Vector2.Lerp(drawPositions[1, 1], drawPositions[1, 0], timeStacker);

            int rotationIndex = Mathf.RoundToInt(Mathf.Clamp(Mathf.Abs(bodyRotation.x * 4f), 0f, 4f));

            sLeaser.sprites[BodySprite].SetElementByName("buzzBody" + rotationIndex);
            sLeaser.sprites[BodySprite].scaleY = 1.4f + -Mathf.Clamp01(bodyRotation.y) * 0.1f;
            sLeaser.sprites[BodySprite].scaleX = bodyRotation.x > 0f ? -1.4f : 1.4f;
            sLeaser.sprites[BodySprite].rotation = -bodyRotation.x * 10f;


            for (int i = 0; i < buttSprites; i++)
            {
                sLeaser.sprites[i].SetPosition(Vector2.Lerp(lastAbdomenPositions[i], abdomenPositions[i], timeStacker) - camPos);
                sLeaser.sprites[i].scale = Mathf.Lerp(lastAbdomenScales[i], abdomenScales[i], timeStacker);
            }
            sLeaser.sprites[BodySprite].SetPosition(bodyPos - camPos);
            sLeaser.sprites[HeadSprite].SetPosition(headPos - camPos);
            sLeaser.sprites[HeadSprite].scaleX = Mathf.Lerp(0.55f, 0.5f, Mathf.InverseLerp(0.2f, 0.6f, Mathf.Abs(bodyRotation.x)));

            sLeaser.sprites[EyeSprites].SetPosition(headPos + new Vector2(3.5f, -1f) - camPos + lookDirection);
            sLeaser.sprites[EyeSprites].x -= bodyRotation.x * 2f + Mathf.Clamp01(bodyRotation.x);
            sLeaser.sprites[EyeSprites].y -= Mathf.Clamp01(-bodyRotation.y);
            sLeaser.sprites[EyeSprites].scaleX = Mathf.Lerp(0.19f, 0f, Mathf.InverseLerp(-0.2f, -0.6f, bodyRotation.x));
            sLeaser.sprites[EyeSprites + 1].SetPosition(headPos + new Vector2(-3.5f, -1f) - camPos + lookDirection);
            sLeaser.sprites[EyeSprites + 1].x -= bodyRotation.x * 2f - Mathf.Clamp01(-bodyRotation.x);
            sLeaser.sprites[EyeSprites + 1].y -= Mathf.Clamp01(-bodyRotation.y);
            sLeaser.sprites[EyeSprites + 1].scaleX = Mathf.Lerp(0.19f, 0f, Mathf.InverseLerp(0.2f, 0.6f, bodyRotation.x));

            Vector2 jetLowerPosition = Vector2.Lerp(lastAbdomenPositions[1], abdomenPositions[1], timeStacker);

            for (int i = 0; i < 4; i++)
            {
                jetData[i].lastPos = jetData[i].pos;
                jetData[i].lastRotation = jetData[i].rotation;
            }

            jetData[0].pos = bodyPos + new Vector2(6f, 11f);
            jetData[0].scale = new Vector2(0.6f, 0.4f);
            jetData[0].rotation = 20f;

            jetData[1].pos = bodyPos + new Vector2(-6f, 11f);
            jetData[1].scale = new Vector2(0.6f, 0.4f);
            jetData[1].rotation = -20f;

            jetData[2].pos = jetLowerPosition + new Vector2(11f, -5f);
            jetData[2].scale = new Vector2(0.8f, 0.6f);
            jetData[2].rotation = 44f;

            jetData[3].pos = jetLowerPosition + new Vector2(-11f, -5f);
            jetData[3].scale = new Vector2(0.8f, 0.6f);
            jetData[3].rotation = -44f;

            // X Rotation of Upper Jets
            //  Right Upper Jets
            jetData[0].pos.x += Mathf.Sin(bodyRotation.x * Mathf.PI / 2f) * 5f;
            jetData[0].pos.y -= Mathf.Lerp(0f, 3f, Mathf.InverseLerp(0f, 1f, Mathf.Abs(bodyRotation.x)));
            jetData[0].rotation = Mathf.Lerp(20f, 85f, Mathf.InverseLerp(0f, 1f, bodyRotation.x));

            //  Left Upper Jets
            jetData[1].pos.x += Mathf.Sin(bodyRotation.x * Mathf.PI / 2f) * 5f;
            jetData[1].pos.y -= Mathf.Lerp(0f, 3f, Mathf.InverseLerp(0f, 1f, Mathf.Abs(bodyRotation.x)));
            jetData[1].rotation = Mathf.Lerp(-20f, -85f, Mathf.InverseLerp(0f, -1f, bodyRotation.x));

            // X Rotation of Lower Jets
            //  Right Lower Jets
            jetData[2].pos.x += Mathf.Sin(Mathf.Clamp01(bodyRotation.x) * Mathf.PI) * 4f;
            jetData[2].pos.y += Mathf.Sin(Mathf.Abs(bodyRotation.x) * Mathf.PI - Mathf.PI / 2f - Mathf.Cos(Mathf.Abs(bodyRotation.x) * Mathf.PI - Mathf.PI / 2f + Mathf.PI)) * 3f + 3f;
            jetData[2].scale.x = Mathf.Lerp(0.8f, 0.9f, Mathf.InverseLerp(0f, 0.5f, Mathf.Pow(Mathf.Clamp01(bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.5f, 1f) - 0.5f) * 2f, 2f)));
            jetData[2].scale.y = Mathf.Lerp(0.6f, 0.7f, Mathf.InverseLerp(0f, 0.7f, Mathf.Pow(Mathf.Clamp01(bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.5f, 1f) - 0.5f) * 2f, 2f)));
            jetData[2].scale.y -= Mathf.Pow(Mathf.Sin((Mathf.Clamp01(bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.2f, 1f) - 0.2f) * 1.25f) * Mathf.PI) / 2f, 3f);
            jetData[2].rotation = Mathf.Lerp(-Custom.AimFromOneVectorToAnother(lowerBodyPos, jetData[2].pos) / 2f + 15f, 44f, Mathf.InverseLerp(0.4f, 0f, Mathf.Pow(Mathf.Clamp01(bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.5f, 1f) - 0.5f) * 2f, 2f)));
            jetData[2].rotation -= Mathf.Sin((Mathf.Clamp01(bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.5f, 1f) - 0.5f) * 2f) * Mathf.PI) * 4f;

            //  Left Lower Jets
            jetData[3].pos.x += Mathf.Sin(-Mathf.Clamp01(-bodyRotation.x) * Mathf.PI) * 4f;
            jetData[3].pos.y += Mathf.Sin(Mathf.Abs(bodyRotation.x) * Mathf.PI - Mathf.PI / 2f - Mathf.Cos(Mathf.Abs(bodyRotation.x) * Mathf.PI - Mathf.PI / 2f + Mathf.PI)) * 3f + 3f;
            jetData[3].scale.x = Mathf.Lerp(0.8f, 0.9f, Mathf.InverseLerp(0f, -0.5f, -Mathf.Pow(Mathf.Clamp01(-bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.5f, 1f) - 0.5f) * 2f, 2f)));
            jetData[3].scale.y = Mathf.Lerp(0.6f, 0.7f, Mathf.InverseLerp(0f, -0.7f, -Mathf.Pow(Mathf.Clamp01(-bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.5f, 1f) - 0.5f) * 2f, 2f)));
            jetData[3].scale.y -= Mathf.Pow(Mathf.Sin((Mathf.Clamp01(-bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.2f, 1f) - 0.2f) * 1.25f) * Mathf.PI) / 2f, 3f);
            jetData[3].rotation = Mathf.Lerp(-Custom.AimFromOneVectorToAnother(lowerBodyPos, jetData[3].pos) / 2f - 15f, -44f, Mathf.InverseLerp(-0.4f, 0f, -Mathf.Pow(Mathf.Clamp01(-bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.5f, 1f) - 0.5f) * 2f, 2f)));
            jetData[3].rotation += Mathf.Sin((Mathf.Clamp01(-bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0.5f, 1f) - 0.5f) * 2f) * Mathf.PI) * 4f;

            if (bodyRotation.x < -0.1f && bodyRotation.y > 0f)
            {
                jetData[2].pos.x -= Mathf.Lerp(0f, 15f, Mathf.Lerp(0.1f, 0.5f, Mathf.Clamp01(-bodyRotation.x)));
                jetData[2].rotation += Mathf.Lerp(0f, 140f, Mathf.InverseLerp(0.1f, 0.7f, Mathf.Pow(Mathf.Clamp01(-bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0f, 0.5f)), 2f) * 2f));
            }
            else if (bodyRotation.x > 0.1f && bodyRotation.y > 0f)
            {
                jetData[3].pos.x += Mathf.Lerp(0f, 15f, Mathf.Lerp(0.1f, 0.5f, Mathf.Clamp01(bodyRotation.x)));
                jetData[3].rotation -= Mathf.Lerp(0f, 140f, Mathf.InverseLerp(0.1f, 0.7f, Mathf.Pow(Mathf.Clamp01(bodyRotation.x) - (Mathf.Clamp(-bodyRotation.y, 0f, 0.5f)) * 2f, 2f)));
            }

            // Y Rotation of Lower Jets
            //  Right Lower Jets
            jetData[2].pos.x -= Mathf.Lerp(0f, 4f, Mathf.InverseLerp(0f, -1f, bodyRotation.y));
            jetData[2].pos.x -= Mathf.Sin(Mathf.Clamp01(-bodyRotation.y)* Mathf.PI) * 2f;
            jetData[2].pos.y += Mathf.Lerp(0f, 14f, Mathf.InverseLerp(0.2f, -0.8f, bodyRotation.y));
            jetData[2].scale.x -= Mathf.Pow(Mathf.Sin((Mathf.Clamp(bodyRotation.y, -0.8f, -0.4f) + 0.4f) * 2.5f * Mathf.PI), 2f) / 2.5f;
            jetData[2].scale.y -= Mathf.Lerp(0f, 0.1f, Mathf.InverseLerp(0f, -1f, bodyRotation.y));
            jetData[2].rotation *= Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0f, -0.5f, bodyRotation.y));
            jetData[2].rotation -= Mathf.Lerp(0f, 75f, Mathf.InverseLerp(-0.3f, -0.8f, bodyRotation.y));

            //  Left Lower Jets
            jetData[3].pos.x += Mathf.Lerp(0f, 4f, Mathf.InverseLerp(0f, -1f, bodyRotation.y));
            jetData[3].pos.x += Mathf.Sin(Mathf.Clamp01(-bodyRotation.y) * Mathf.PI) * 2f;
            jetData[3].pos.y += Mathf.Lerp(0f, 14f, Mathf.InverseLerp(0.2f, -0.8f, bodyRotation.y));
            jetData[3].scale.x -= Mathf.Pow(Mathf.Sin((Mathf.Clamp(bodyRotation.y, -0.8f, -0.4f) + 0.4f) * 2.5f * Mathf.PI), 2f) / 2.5f;
            jetData[3].scale.y -= Mathf.Lerp(0f, 0.1f, Mathf.InverseLerp(0f, -1f, bodyRotation.y));
            jetData[3].rotation *= Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0f, -0.5f, bodyRotation.y));
            jetData[3].rotation += Mathf.Lerp(0f, 75f, Mathf.InverseLerp(-0.3f, -0.8f, bodyRotation.y));

            for (int i = 0; i < 4; i++)
            {
                sLeaser.sprites[JetSprites + i].SetPosition(Vector2.Lerp(jetData[i].lastPos, jetData[i].pos, timeStacker) - camPos);
                sLeaser.sprites[JetSprites + i].scaleX = jetData[i].scale.x;
                sLeaser.sprites[JetSprites + i].scaleY = jetData[i].scale.y;
                sLeaser.sprites[JetSprites + i].rotation = Mathf.Lerp(jetData[i].lastRotation, jetData[i].rotation, timeStacker);
            }

            // Sprite Layering
            if (bodyRotation.x > 0.5f)
            {
                sLeaser.sprites[JetSprites + 2].MoveBehindOtherNode(sLeaser.sprites[BodySprite]);
                sLeaser.sprites[JetSprites].MoveInFrontOfOtherNode(sLeaser.sprites[JetSprites + 2]);
            }
            else if (bodyRotation.x < -0.5f)
            {
                sLeaser.sprites[JetSprites + 3].MoveBehindOtherNode(sLeaser.sprites[BodySprite]);
                sLeaser.sprites[JetSprites + 1].MoveInFrontOfOtherNode(sLeaser.sprites[JetSprites + 3]);
            }
            else if (bodyRotation.y < 0f)
            {
                sLeaser.sprites[JetSprites].MoveBehindOtherNode(sLeaser.sprites[BodySprite]);
                sLeaser.sprites[JetSprites + 1].MoveBehindOtherNode(sLeaser.sprites[BodySprite]);
                if (bodyRotation.y < -0.6f)
                {
                    sLeaser.sprites[JetSprites + 2].MoveBehindOtherNode(sLeaser.sprites[JetSprites]);
                    sLeaser.sprites[JetSprites + 3].MoveBehindOtherNode(sLeaser.sprites[JetSprites + 1]);
                }

            }
            else
            {
                sLeaser.sprites[JetSprites].MoveBehindOtherNode(sLeaser.sprites[BodySprite]);
                sLeaser.sprites[JetSprites + 1].MoveBehindOtherNode(sLeaser.sprites[BodySprite]);
                sLeaser.sprites[JetSprites + 2].MoveBehindOtherNode(sLeaser.sprites[buttSprites - 1]);
                sLeaser.sprites[JetSprites + 3].MoveBehindOtherNode(sLeaser.sprites[buttSprites - 1]);
            }
            sLeaser.containers[0].MoveBehindOtherNode(sLeaser.sprites[JetSprites]);
            sLeaser.containers[1].MoveBehindOtherNode(sLeaser.sprites[JetSprites + 1]);
            sLeaser.containers[2].MoveBehindOtherNode(sLeaser.sprites[JetSprites + 2]);
            sLeaser.containers[3].MoveBehindOtherNode(sLeaser.sprites[JetSprites + 3]);

            // Jet Flames
            jetData[0].smoke.MoveToInternalContainer(0);
            jetData[0].smoke.MoveTo(Vector2.Lerp(jetData[0].lastPos, jetData[0].pos, timeStacker) + Custom.DegToVec(jetData[0].rotation), buzz.evenUpdate);
            jetData[0].smoke.EmitParticles(Custom.DegToVec(jetData[0].rotation) * Mathf.Lerp(jetData[0].lastFlamePower, jetData[0].flamePower, timeStacker) / 3f, Mathf.Lerp(jetData[0].lastFlamePower, jetData[0].flamePower, timeStacker));

            jetData[1].smoke.MoveToInternalContainer(1);
            jetData[1].smoke.MoveTo(Vector2.Lerp(jetData[1].lastPos, jetData[1].pos, timeStacker) + Custom.DegToVec(jetData[1].rotation), buzz.evenUpdate);
            jetData[1].smoke.EmitParticles(Custom.DegToVec(jetData[1].rotation) * Mathf.Lerp(jetData[1].lastFlamePower, jetData[1].flamePower, timeStacker) / 3f, Mathf.Lerp(jetData[1].lastFlamePower, jetData[1].flamePower, timeStacker));

            jetData[2].smoke.MoveToInternalContainer(2);
            jetData[2].smoke.MoveTo(Vector2.Lerp(jetData[2].lastPos, jetData[2].pos, timeStacker) + -Custom.PerpendicularVector(Custom.DegToVec(jetData[2].rotation)), buzz.evenUpdate);
            jetData[2].smoke.EmitParticles(-Custom.PerpendicularVector(Custom.DegToVec(jetData[2].rotation)) * Mathf.Lerp(jetData[2].lastFlamePower, jetData[2].flamePower, timeStacker) / 3f, Mathf.Lerp(jetData[2].lastFlamePower, jetData[2].flamePower, timeStacker));

            jetData[3].smoke.MoveToInternalContainer(3);
            jetData[3].smoke.EmitParticles(Custom.PerpendicularVector(Custom.DegToVec(jetData[3].rotation)) * Mathf.Lerp(jetData[3].lastFlamePower, jetData[3].flamePower, timeStacker) / 3f, Mathf.Lerp(jetData[3].lastFlamePower, jetData[3].flamePower, timeStacker));
            jetData[3].smoke.MoveTo(Vector2.Lerp(jetData[3].lastPos, jetData[3].pos, timeStacker) + Custom.PerpendicularVector(Custom.DegToVec(jetData[3].rotation)), buzz.evenUpdate);


            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    hands[i, j].DrawSprites(sLeaser, rCam, timeStacker, camPos);
                }
            }

            antennae.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            bodyColor = new Color(0.25f, 0.4f, 0.6f);
            lightColor = new Color(0f, 0.75f, 0.7f);
            darkColor = new Color(0.05f, 0f, 0.1f);
            Color blackColor = Color.Lerp(palette.blackColor, darkColor, 0.5f);

            for (int i = 0; i < buttSprites; i++)
            {
                Color abdomenColor = blackColor;
                if (i % 4 == 0) abdomenColor = lightColor;
                //if (i % 4 == 1) abdomenColor = lightColor;
                sLeaser.sprites[i].color = abdomenColor;
            }
            sLeaser.sprites[BodySprite].color = Color.Lerp(bodyColor, blackColor, 0.6f);
            sLeaser.sprites[HeadSprite].color = blackColor;
            sLeaser.sprites[EyeSprites].color = Color.white;
            sLeaser.sprites[EyeSprites + 1].color = Color.white;

            sLeaser.sprites[JetSprites].color = Color.Lerp(bodyColor, blackColor, 0.8f);
            sLeaser.sprites[JetSprites + 1].color = Color.Lerp(bodyColor, blackColor, 0.8f);
            sLeaser.sprites[JetSprites + 2].color = Color.Lerp(bodyColor, blackColor, 0.8f);
            sLeaser.sprites[JetSprites + 3].color = Color.Lerp(bodyColor, blackColor, 0.8f);

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    hands[i, j].ApplyPalette(sLeaser, rCam, palette);
                }
            }

            antennae.ApplyPalette(sLeaser, rCam, palette);
            base.ApplyPalette(sLeaser, rCam, palette);
        }

        public override void Update()
        {
            lookDirection = buzz.lookDirection;

            for (int i = 0; i < buzz.bodyChunks.Length; i++)
            {
                drawPositions[i, 1] = drawPositions[i, 0];
                drawPositions[i, 0] = buzz.bodyChunks[i].pos;
            }

            drawPositions[0, 0] += lookDirection * 0.5f;
            drawPositions[0, 0].y += -Mathf.Clamp01(-bodyRotation.y) + Mathf.Abs(bodyRotation.x * 4f);

            head.vel += lookDirection * 2f;

            head.Update();
            head.ConnectToPoint(Vector2.Lerp(drawPositions[0, 0], drawPositions[0, 1], 0.2f), 2f, push: true, 0.6f, buzz.bodyChunks[0].vel, 0.7f, 0.2f);

            head.pos.x += bodyRotation.x * -6f;
            head.pos.y += bodyRotation.y + Mathf.Abs(bodyRotation.x * 4f);

            // Abdomen position and scale
            for (int i = 0; i < abdomenPositions.Length; i++)
            {
                float pos = (float)i / (float)abdomenPositions.Length;
                lastAbdomenPositions[i] = abdomenPositions[i];
                abdomenPositions[i] = (Vector2)Vector3.Slerp(buzz.bodyChunks[0].pos, buzz.bodyChunks[1].pos, pos);
                abdomenPositions[i].y -= Mathf.Sin(pos * Mathf.PI * (-Mathf.Abs(bodyRotation.x) - bodyRotation.y)) * 3f;
            }
            for (int i = 0; i < abdomenPositions.Length; i++)
            {
                float pos = (float)i / (float)abdomenPositions.Length;
                lastAbdomenScales[i] = abdomenScales[i];
                abdomenScales[i] = 0.7f + Mathf.Sin(pos * Mathf.PI - 0.4f);
            }

            // Setting power levels for jets
            if (buzz.Consious)
            {
                float upBoost = 1f;
                float downBoost = 1f;
                float leftBoost = 1f;
                float rightBoost = 1f;
                if (buzz.mainBodyChunk.vel.y < -8f)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        jetData[i].flamePower = Mathf.Lerp(jetData[i].flamePower, 0f, 0.8f);
                    }
                    downBoost += 0.05f;
                    downBoost = Mathf.Clamp(downBoost, 1f, 2f);
                }
                else if (buzz.thrustVel.y > 2f && buzz.mainBodyChunk.vel.y > -1f)
                {
                    jetData[0].flamePower = Mathf.Lerp(jetData[0].flamePower, 0f, 0.75f);
                    jetData[1].flamePower = Mathf.Lerp(jetData[1].flamePower, 0f, 0.75f);
                    upBoost += 0.05f;
                    upBoost = Mathf.Clamp(upBoost, 1f, 1.4f);
                }
                else if (Mathf.Abs(buzz.thrustVel.x) > 1f)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        jetData[i].flamePower = Mathf.Lerp(jetData[i].flamePower, 3f, 0.2f);
                    }
                    if (buzz.thrustVel.x > 0f)
                    {
                        rightBoost += 0.05f;
                        rightBoost = Mathf.Clamp(rightBoost, 1f, 1.5f);
                    }
                    else
                    {
                        leftBoost += 0.05f;
                        leftBoost = Mathf.Clamp(leftBoost, 1f, 1.5f);
                    }
                }
                else
                {
                    jetData[0].flamePower = 1f * upBoost * leftBoost * buzz.jetPower;
                    jetData[1].flamePower = 1f * upBoost * rightBoost * buzz.jetPower;
                    jetData[2].flamePower = 4.5f * downBoost * leftBoost * buzz.jetPower;
                    jetData[3].flamePower = 4.5f * downBoost * rightBoost * buzz.jetPower;

                    downBoost = Mathf.Lerp(downBoost, 1f, 0.05f);
                    upBoost = Mathf.Lerp(upBoost, 1f, 0.05f);
                    leftBoost = Mathf.Lerp(leftBoost, 1f, 0.05f);
                    rightBoost = Mathf.Lerp(rightBoost, 1f, 0.05f);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    jetData[i].flamePower = Mathf.Lerp(jetData[i].flamePower, 0f, 0.9f);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                jetData[i].lastFlamePower = jetData[i].flamePower;
            }

                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        hands[i, j].Update();
                    }
                }

            antennae.Update();
            base.Update();
        }
    }
}
