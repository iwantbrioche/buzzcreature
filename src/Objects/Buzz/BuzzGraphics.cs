
namespace BuzzCreature.Objects.Buzz
{
    public class BuzzGraphics : GraphicsModule
    {
        public class BuzzAntennae
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
                            antennae[i, j].vel.x -= buzz.bodyRotation.x * -6f;
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
                        antennae[i, j].vel += buzz.lookDirection * 0.1f * a;
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
                        sLeaser.sprites[AntennaSprite(i, j)].MoveBehindOtherNode(sLeaser.sprites[graphics.EyeSprite]);
                    }
                }
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {

            }
        }
        public class BuzzHand : Limb
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
                lastConnectionPos = connectionPos;
                connectionPos = connection.pos;
                if (!small)
                {
                    connectionPos.x += side == 0 ? -8f : 8f;
                }
                else
                {
                    connectionPos.y -= 6f;
                    connectionPos.x += side == 0 ? -3f : 3f;
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

                float flip = -1f + side * 2f;

                float ikX = small ? 3f : 16f;
                float ikY = small ? 4f : 7f;
                Vector2 elbowIK = Custom.InverseKinematic(connection, handPos, ikY, ikX, -flip);
                ikX = 7f;
                ikY = 9f;
                Vector2 wristIK = Custom.InverseKinematic(elbowIK, handPos, ikY, ikX, flip);

                (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(0, connection - Custom.PerpendicularVector(elbowIK - connection) * flip - camPos);
                (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(1, connection + Custom.PerpendicularVector(elbowIK - connection) * flip * 2f - camPos);
                if (!small)
                {
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(2, elbowIK - (connection - elbowIK).normalized * flip - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(3, elbowIK + Custom.PerpendicularVector(connection - elbowIK) * flip * 3f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(4, elbowIK + Custom.PerpendicularVector(elbowIK - wristIK) * flip - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(5, elbowIK - Custom.PerpendicularVector(elbowIK - wristIK) * flip * 1.5f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(6, wristIK - Custom.PerpendicularVector(wristIK - elbowIK) * flip * 1.5f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(7, wristIK + Custom.PerpendicularVector(wristIK - elbowIK) * flip * 1.5f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(8, wristIK + Custom.PerpendicularVector(elbowIK - handPos) * flip * 1.5f - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(9, wristIK - Custom.PerpendicularVector(elbowIK - handPos) * flip - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(10, handPos - camPos);
                }
                else
                {
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(2, elbowIK - Custom.PerpendicularVector(connection - elbowIK) * flip - camPos);
                    (sLeaser.sprites[firstSprite] as TriangleMesh).MoveVertice(3, elbowIK + Custom.PerpendicularVector(connection - elbowIK) * flip * 1.5f - camPos);
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

            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                (sLeaser.sprites[firstSprite] as TriangleMesh).color = palette.blackColor;
            }
        }

        private Buzz buzz;
        private BuzzAntennae antennae;
        private BuzzHand[,] hands;
        public GenericBodyPart head;

        public Vector2[,] drawPositions;

        public int totalSprites;
        public int BodySprite;
        public int HeadSprite;
        public int EyeSprite;

        public int buttSprites;
        public Vector2[] lastButtPosition;
        public Vector2[] buttPositions;
        public float[] buttScales;
        public float[] lastButtScales;

        public BuzzGraphics(PhysicalObject ow) : base(ow, internalContainers: false)
        {
            buzz = ow as Buzz;
            buttSprites = 8;
            buttPositions = new Vector2[buttSprites];
            lastButtPosition = new Vector2[buttSprites];
            buttScales = new float[buttSprites];
            lastButtScales = new float[buttSprites];

            totalSprites = buttSprites;
            BodySprite = totalSprites++;
            HeadSprite = totalSprites++;
            EyeSprite = totalSprites++;

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
                    hands[i, j].side = i;
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

            this.bodyParts = [ .. bodyParts];
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[totalSprites];
            for (int i = 0; i < buttSprites; i++)
            {
                sLeaser.sprites[i] = new("Futile_White");
                sLeaser.sprites[i].shader = Custom.rainWorld.Shaders["JaggedCircle"];
                sLeaser.sprites[i].alpha = 0.7f;
            }

            sLeaser.sprites[BodySprite] = new("buzzBody0");

            sLeaser.sprites[HeadSprite] = new("Circle20");
            sLeaser.sprites[HeadSprite].scaleX = 0.55f;
            sLeaser.sprites[HeadSprite].scaleY = 0.5f;

            sLeaser.sprites[EyeSprite] = new("buzz0Eye0");

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
            Vector2 headPos = Vector2.Lerp(head.lastPos, head.pos, timeStacker);
            Vector2 bodyPos = Vector2.Lerp(drawPositions[0, 1], drawPositions[0, 0], timeStacker);

            int rotationIndex = Mathf.RoundToInt(Mathf.Clamp(Mathf.Abs(buzz.bodyRotation.x * 4f), 0f, 4f));

            sLeaser.sprites[BodySprite].SetElementByName("buzzBody" + rotationIndex);
            sLeaser.sprites[BodySprite].scaleY = 1.4f + -Mathf.Clamp01(-buzz.bodyRotation.y) * 0.1f;
            sLeaser.sprites[BodySprite].scaleX = buzz.bodyRotation.x > 0f ? -1.4f : 1.4f;
            sLeaser.sprites[BodySprite].rotation = -buzz.bodyRotation.x * 10f;

            sLeaser.sprites[EyeSprite].SetElementByName("buzz" + Mathf.RoundToInt(-Mathf.Clamp01(-buzz.bodyRotation.y) + 1) + "Eye" + rotationIndex);
            sLeaser.sprites[EyeSprite].scaleX = buzz.bodyRotation.x > 0f ? -1f : 1f;

            for (int i = 0; i < buttSprites; i++)
            {
                sLeaser.sprites[i].SetPosition(Vector2.Lerp(lastButtPosition[i], buttPositions[i], timeStacker) - camPos);
                sLeaser.sprites[i].scale = Mathf.Lerp(buttScales[i], lastButtScales[i], timeStacker);
            }
            sLeaser.sprites[BodySprite].SetPosition(bodyPos - camPos);
            sLeaser.sprites[HeadSprite].SetPosition(headPos - camPos);
            sLeaser.sprites[EyeSprite].SetPosition(headPos - camPos + buzz.lookDirection);

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
            Color color = Color.white;
            Color blackColor = palette.blackColor;
            for (int i = 0; i < buttSprites; i++)
            {
                sLeaser.sprites[i].color = Color.Lerp(color, blackColor, (float)i / (float)buttSprites * 0.5f);
            }
            sLeaser.sprites[BodySprite].color = Color.gray;
            sLeaser.sprites[EyeSprite].color = blackColor;

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

            for (int i = 0; i < buzz.bodyChunks.Length; i++)
            {
                drawPositions[i, 1] = drawPositions[i, 0];
                drawPositions[i, 0] = buzz.bodyChunks[i].pos;
            }

            drawPositions[0, 0] += buzz.lookDirection * 0.5f;
            drawPositions[0, 0].y += Mathf.Clamp01(buzz.bodyRotation.y) + Mathf.Abs(buzz.bodyRotation.x * 4f);

            head.vel += buzz.lookDirection * 2f;

            head.Update();
            head.ConnectToPoint(Vector2.Lerp(drawPositions[0, 0], drawPositions[0, 1], 0.2f), 2f, push: true, 0.6f, buzz.bodyChunks[0].vel, 0.7f, 0.2f);

            head.pos.x += buzz.bodyRotation.x * -6f;
            head.pos.y += -buzz.bodyRotation.y + Mathf.Abs(buzz.bodyRotation.x * 4f);

            for (int i = 0; i < buttPositions.Length; i++)
            {
                float pos = (float)i / (float)buttPositions.Length;
                lastButtPosition[i] = buttPositions[i];
                buttPositions[i] = (Vector2)Vector3.Slerp(buzz.bodyChunks[0].pos, buzz.bodyChunks[1].pos, pos);
                buttPositions[i].y -= Mathf.Sin(pos * Mathf.PI * (-Mathf.Abs(buzz.bodyRotation.x) + buzz.bodyRotation.y)) * 3f;
            }
            for (int i = 0; i < buttPositions.Length; i++)
            {
                float pos = (float)i / (float)buttPositions.Length;
                lastButtScales[i] = buttScales[i];
                buttScales[i] = 0.7f + Mathf.Sin(pos * Mathf.PI - 0.4f);
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
