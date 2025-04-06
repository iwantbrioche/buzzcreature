

using System;

namespace BuzzCreature.Objects.Buzz
{
    public class BuzzGraphics : GraphicsModule
    {
        public class BuzzAntennae
        {
            BuzzGraphics bGraphics;
            public GenericBodyPart[,] antennae;
            private int startSprite;
            public int totalSprites;
            private int segments;
            private float length;

            public BuzzAntennae(BuzzGraphics ow, int startSprite)
            {
                bGraphics = ow;
                this.startSprite = startSprite;
                length = 14;
                segments = Mathf.FloorToInt(length / 3f);
                antennae = new GenericBodyPart[2, segments];
                for (int i = 0; i < segments; i++)
                {
                    antennae[0, i] = new GenericBodyPart(bGraphics, 0.2f, 0.4f, 0.6f, bGraphics.buzz.bodyChunks[0]);
                    antennae[1, i] = new GenericBodyPart(bGraphics, 0.2f, 0.4f, 0.6f, bGraphics.buzz.bodyChunks[0]);
                }

                totalSprites = segments * 2;
            }

            private int AntennaSprite(int side, int part)
            {
                return startSprite + part * 2 + side;
            }
            private Vector2 AntennaDir(int side, float timeStacker)
            {
                return Custom.RotateAroundOrigo(new Vector2(side == 0 ? -1f : 1f, -1f).normalized, 180f);
            }
            private Vector2 AnchorPoint(int side, float timeStacker)
            {
                return Vector2.Lerp(bGraphics.buzz.bodyChunks[0].lastPos, bGraphics.buzz.bodyChunks[0].pos, timeStacker) + AntennaDir(side, timeStacker) * 2f;
            }

            public void Update()
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < segments; j++)
                    {
                        float a = (float)j / (float)(segments - 1);
                        a = Mathf.Lerp(a, Mathf.InverseLerp(0f, 5f, j), 0.2f);
                        antennae[i, j].vel += AntennaDir(i, 1f) * a;
                        antennae[i, j].vel.y += 0.4f * a;
                        antennae[i, j].Update();

                        Vector2 pos = bGraphics.buzz.bodyChunks[0].pos;
                        if (j == 0)
                        {
                            antennae[i, j].vel += AntennaDir(i, 1f) * 5f;
                            antennae[i, j].ConnectToPoint(AnchorPoint(i, 1f), 5f, push: true, 0f, bGraphics.buzz.mainBodyChunk.vel, 0f, 0f);
                        }
                        else
                        {
                            pos = j > 0 ? antennae[i, j - 1].pos : AnchorPoint(i, 1f);
                            Vector2 dir = Custom.DirVec(pos, antennae[i, j - 1].pos);
                            float dist = Vector2.Distance(pos, antennae[i, j - 1].pos);
                            antennae[i, j].ConnectToPoint(antennae[i, j - 1].pos + dir * dist, 6f, true, 0f, bGraphics.buzz.mainBodyChunk.vel, 0f, 0f);
                        }
                        antennae[i, j].vel += Custom.DirVec(pos, antennae[i, j].pos) * 3f * Mathf.Pow(1f - a, 0.3f);

                        if (!Custom.DistLess(bGraphics.buzz.mainBodyChunk.pos, antennae[i, j].pos, 200f))
                        {
                            antennae[i, j].pos = bGraphics.buzz.mainBodyChunk.pos;
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
                Vector2 bodyRotation = Vector2.Lerp(bGraphics.buzz.oldBodyRotation, bGraphics.buzz.bodyRotation, timeStacker);

                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < segments; j++)
                    {
                        Vector2 antennaPos = Vector2.Lerp(antennae[i, j].lastPos, antennae[i, j].pos, timeStacker);

                        sLeaser.sprites[AntennaSprite(i, j)].SetPosition(antennaPos - camPos + new Vector2(bodyRotation.x * -6f, bodyRotation.y));
                        sLeaser.sprites[AntennaSprite(i, j)].rotation = Custom.AimFromOneVectorToAnother(j == 0 ? antennae[i, j + 1].pos : antennae[i, j - 1].pos, antennae[i, j].pos);
                    }
                }
            }
        }

        private Buzz buzz;
        private BuzzAntennae antennae;

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
            BodySprite = buttSprites;
            HeadSprite = BodySprite + 1;
            EyeSprite = HeadSprite + 1;
            antennae = new(this, EyeSprite + 1);
            totalSprites = buttSprites + 3 + antennae.totalSprites;
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
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 headChunkPos = Vector2.Lerp(buzz.bodyChunks[0].lastPos, buzz.bodyChunks[0].pos, timeStacker);
            Vector2 buttChunkPos = Vector2.Lerp(buzz.bodyChunks[1].lastPos, buzz.bodyChunks[1].pos, timeStacker);
            Vector2 lookDir = Vector2.Lerp(buzz.oldLookDir, buzz.lookDir, timeStacker);
            Vector2 bodyRotation = Vector2.Lerp(buzz.oldBodyRotation, buzz.bodyRotation, timeStacker);

            int rotationIndex = Mathf.RoundToInt(Mathf.Clamp(Mathf.Abs(bodyRotation.x * 4f), 0f, 4f));

            sLeaser.sprites[BodySprite].SetElementByName("buzzBody" + rotationIndex);
            sLeaser.sprites[BodySprite].scaleY = 1.4f + -Mathf.Clamp01(-bodyRotation.y) * 0.1f;
            sLeaser.sprites[BodySprite].scaleX = bodyRotation.x > 0f ? -1.4f : 1.4f;
            sLeaser.sprites[BodySprite].rotation = -bodyRotation.x * 10f;

            sLeaser.sprites[EyeSprite].SetElementByName("buzz" + Mathf.RoundToInt(-Mathf.Clamp01(-bodyRotation.y) + 1) + "Eye" + rotationIndex);
            sLeaser.sprites[EyeSprite].scaleX = bodyRotation.x > 0f ? -1f : 1f;

            for (int i = 0; i < buttSprites; i++)
            {
                sLeaser.sprites[i].SetPosition(Vector2.Lerp(lastButtPosition[i], buttPositions[i], timeStacker) - camPos);
                sLeaser.sprites[i].scale = Mathf.Lerp(buttScales[i], lastButtScales[i], timeStacker);
            }
            sLeaser.sprites[BodySprite].SetPosition(headChunkPos - camPos + lookDir * 0.5f);
            sLeaser.sprites[HeadSprite].SetPosition(headChunkPos - camPos + lookDir * 2f + new Vector2(bodyRotation.x * -6f, bodyRotation.y));
            sLeaser.sprites[EyeSprite].SetPosition(headChunkPos - camPos + lookDir * 2.5f + new Vector2(bodyRotation.x * -6f, -bodyRotation.y - 1f));

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
            base.ApplyPalette(sLeaser, rCam, palette);
        }

        public override void Update()
        {
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

            antennae.Update();
            base.Update();
        }
    }
}
