using System;

namespace XiaoGDesktop
{
    // 使用原画的侧脸和俯仰姿势补帧；固定身体，避免用整块平移冒充转头。
    internal sealed class HeadMotion
    {
        private const int W = 192, H = 208, MotionHeight = 144;
        private readonly byte[] neutral, reference;
        private readonly byte[][] poses = new byte[16][];
        private readonly MotionPair[] around = new MotionPair[16];

        internal HeadMotion(byte[] rest, byte[][] originals, byte[] highRest, byte[][] highOriginals)
        {
            reference = rest; neutral = highRest;
            byte[][] meshes = new byte[16][];
            for (int i = 0; i < 16; i++)
                meshes[i] = Attach(originals[i], highOriginals[i], out poses[i]);
            for (int i = 0; i < 16; i++)
                around[i] = new MotionPair(meshes[i], meshes[(i + 1) % 16], reference);
        }

        internal byte[] Render(int direction, int strength)
        {
            return RenderAmount(direction, strength / (double)FrameComposer.StrengthSteps);
        }

        // 有限动作按连续强度提前绘制，避免点头和摇头只能逐级切换。
        internal byte[] RenderAmount(int direction, double strength)
        {
            int divisions = FrameComposer.DirectionCount / 16;
            int first = direction / divisions, second = (first + 1) % 16;
            double between = (direction % divisions) / (double)divisions;
            byte[] result = around[first].Morph(neutral, poses[first], poses[second], between, Math.Max(0, Math.Min(1, strength)));
            // 过渡使用曲线边界，肩膀和翅膀处始终取待机图，不在脖子处横切一刀。
            for (int y = 110 * SourceArtwork.Scale; y < MotionHeight * SourceArtwork.Scale; y++)
                for (int x = 0; x < SourceArtwork.Width; x++)
                {
                    double weight = HeadWeight(x / (double)SourceArtwork.Scale, y / (double)SourceArtwork.Scale);
                    int p = (y * SourceArtwork.Width + x) * 4;
                    for (int c = 0; c < 4; c++) result[p + c] = Blend(neutral[p + c], result[p + c], weight);
                }
            return result;
        }

        private byte[] Attach(byte[] original, byte[] highOriginal, out byte[] high)
        {
            byte[] neutral = this.reference;
            // 用衣领中央的小区域找位置，保留头部相对脖子的朝向和下巴变化。
            int offsetX = 0, offsetY = 0;
            double best = double.MaxValue;
            for (int dy = -10; dy <= 10; dy++)
                for (int dx = -5; dx <= 5; dx++)
                {
                    double error = (dx * dx + dy * dy) * 180;
                    for (int y = 124; y < 138; y++)
                        for (int x = 85; x < 102; x++)
                        {
                            int p = (y * W + x) * 4, q = ((y + dy) * W + x + dx) * 4;
                            for (int c = 0; c < 3; c++)
                            {
                                double difference = original[q + c] - neutral[p + c];
                                error += difference * difference;
                            }
                        }
                    if (error < best) { best = error; offsetX = dx; offsetY = dy; }
                }
            double[] reference = HairColor(neutral), source = HairColor(original);
            byte[] result = (byte[])neutral.Clone();
            for (int y = 0; y < MotionHeight; y++)
                for (int x = 0; x < W; x++)
                {
                    double weight = HeadWeight(x, y);
                    int p = (y * W + x) * 4;
                    double alpha = Sample(original, x + offsetX, y + offsetY, 3);
                    for (int c = 0; c < 4; c++)
                    {
                        double value = c == 3 ? alpha : Sample(original, x + offsetX, y + offsetY, c);
                        if (c < 3) value = Math.Max(0, Math.Min(alpha, value + (reference[c] - source[c]) * alpha / 255.0));
                        result[p + c] = Blend(neutral[p + c], value, weight);
                    }
                }
            high = (byte[])this.neutral.Clone();
            for (int y = 0; y < MotionHeight * SourceArtwork.Scale; y++)
                for (int x = 0; x < SourceArtwork.Width; x++)
                {
                    double weight = HeadWeight(x / (double)SourceArtwork.Scale, y / (double)SourceArtwork.Scale);
                    int p = (y * SourceArtwork.Width + x) * 4;
                    double sx = x + offsetX * SourceArtwork.Scale, sy = y + offsetY * SourceArtwork.Scale;
                    double alpha = SampleHigh(highOriginal, sx, sy, 3);
                    for (int c = 0; c < 4; c++)
                    {
                        double value = c == 3 ? alpha : SampleHigh(highOriginal, sx, sy, c);
                        if (c < 3) value = Math.Max(0, Math.Min(alpha, value + (reference[c] - source[c]) * alpha / 255.0));
                        high[p + c] = Blend(this.neutral[p + c], value, weight);
                    }
                }
            return result;
        }

        private static double[] HairColor(byte[] image)
        {
            double[] mean = new double[3];
            int count = 0;
            for (int y = 42; y < 112; y++)
                for (int x = 48; x < 144; x++)
                {
                    int p = (y * W + x) * 4;
                    int b = image[p], g = image[p + 1], r = image[p + 2];
                    if (image[p + 3] < 245 || Math.Min(b, Math.Min(g, r)) < 145 || Math.Max(b, Math.Max(g, r)) - Math.Min(b, Math.Min(g, r)) > 35) continue;
                    for (int c = 0; c < 3; c++) mean[c] += image[p + c];
                    count++;
                }
            for (int c = 0; c < 3; c++) mean[c] = count > 0 ? mean[c] / count : 205;
            return mean;
        }

        private static double HeadWeight(double x, double y)
        {
            if (y <= 110) return 1;
            double left = (x - 68) / 13.0, right = (x - 124) / 13.0, neck = (x - 95) / 20.0;
            double bottom = 116 + 16 * Math.Exp(-left * left) + 16 * Math.Exp(-right * right) + 12 * Math.Exp(-neck * neck);
            double value = Math.Max(0, Math.Min(1, (bottom - y) / 8));
            return value * value * (3 - 2 * value);
        }

        private static byte Blend(double a, double b, double amount)
        {
            return (byte)Math.Max(0, Math.Min(255, Math.Round(a + (b - a) * amount)));
        }

        private static double Sample(byte[] image, double x, double y, int channel)
        {
            if (x < 0 || y < 0 || x >= W - 1 || y >= H - 1) return 0;
            int ix = (int)x, iy = (int)y, p = (iy * W + ix) * 4 + channel;
            double tx = x - ix, ty = y - iy;
            return (image[p] * (1 - tx) + image[p + 4] * tx) * (1 - ty) + (image[p + W * 4] * (1 - tx) + image[p + W * 4 + 4] * tx) * ty;
        }

        private static double SampleHigh(byte[] image, double x, double y, int channel)
        {
            int width = SourceArtwork.Width, height = SourceArtwork.Height;
            if (x < 0 || y < 0 || x >= width - 1 || y >= height - 1) return 0;
            int ix = (int)x, iy = (int)y, p = (iy * width + ix) * 4 + channel;
            double tx = x - ix, ty = y - iy;
            return (image[p] * (1 - tx) + image[p + 4] * tx) * (1 - ty) + (image[p + width * 4] * (1 - tx) + image[p + width * 4 + 4] * tx) * ty;
        }

        private sealed class MotionPair
        {
            private readonly Point[] first, second, rest;
            private readonly int[][] triangles;

            internal MotionPair(byte[] a, byte[] b, byte[] neutral)
            {
                first = Mesh(a); second = Mesh(b); rest = Mesh(neutral);
                var entries = new System.Collections.Generic.List<int[]>();
                int[][] face = {
                    new int[] {0,1,3}, new int[] {1,4,3}, new int[] {1,2,4}, new int[] {2,5,4},
                    new int[] {3,4,6}, new int[] {4,7,6}, new int[] {4,5,7}, new int[] {5,8,7},
                    new int[] {6,7,9}, new int[] {7,8,9}
                };
                entries.AddRange(face);
                int[] rim = {0,1,2,5,8,9,6,3};
                for (int i = 0; i < 8; i++)
                {
                    int next = (i + 1) % 8;
                    entries.Add(new int[] {rim[i],rim[next],10+next});
                    entries.Add(new int[] {rim[i],10+next,10+i});
                    entries.Add(new int[] {10+i,10+next,18+next});
                    entries.Add(new int[] {10+i,18+next,18+i});
                }
                triangles = entries.ToArray();
            }

            internal byte[] Morph(byte[] neutral, byte[] a, byte[] b, double amount, double strength)
            {
                Point[] target = new Point[first.Length];
                for (int i = 0; i < first.Length; i++)
                    target[i] = Interpolate(rest[i], Interpolate(first[i], second[i], amount), strength);
                // 位置继续补帧，纹理只取较近的一张原画；混合不同画法会叠出龙角、发饰残影。
                byte[] texture = strength < 0.5 ? neutral : amount < 0.5 ? a : b;
                Point[] textureMesh = strength < 0.5 ? rest : amount < 0.5 ? first : second;
                byte[] result = (byte[])neutral.Clone();
                int width = SourceArtwork.Width, scale = SourceArtwork.Scale;
                foreach (int[] triangle in triangles)
                {
                    int ia = triangle[0], ib = triangle[1], ic = triangle[2];
                    Point pa = target[ia], pb = target[ib], pc = target[ic];
                    double divisor = (pb.Y-pc.Y)*(pa.X-pc.X)+(pc.X-pb.X)*(pa.Y-pc.Y);
                    if (Math.Abs(divisor) < 0.0001) continue;
                    int minX = Math.Max(0,(int)Math.Floor(Math.Min(pa.X,Math.Min(pb.X,pc.X))*scale));
                    int maxX = Math.Min(width-1,(int)Math.Ceiling(Math.Max(pa.X,Math.Max(pb.X,pc.X))*scale));
                    int minY = Math.Max(0,(int)Math.Floor(Math.Min(pa.Y,Math.Min(pb.Y,pc.Y))*scale));
                    int maxY = Math.Min(MotionHeight*scale-1,(int)Math.Ceiling(Math.Max(pa.Y,Math.Max(pb.Y,pc.Y))*scale));
                    for (int y = minY; y <= maxY; y++)
                        for (int x = minX; x <= maxX; x++)
                        {
                            double px = (x + 0.5) / scale - 0.5, py = (y + 0.5) / scale - 0.5;
                            double u = ((pb.Y-pc.Y)*(px-pc.X)+(pc.X-pb.X)*(py-pc.Y))/divisor;
                            double v = ((pc.Y-pa.Y)*(px-pc.X)+(pa.X-pc.X)*(py-pc.Y))/divisor;
                            double w = 1-u-v;
                            if (u < -0.000001 || v < -0.000001 || w < -0.000001) continue;
                            double sx = textureMesh[ia].X*u+textureMesh[ib].X*v+textureMesh[ic].X*w;
                            double sy = textureMesh[ia].Y*u+textureMesh[ib].Y*v+textureMesh[ic].Y*w;
                            SourceArtwork.Sample(texture, width, SourceArtwork.Height, (sx + 0.5) * scale - 0.5, (sy + 0.5) * scale - 0.5, result, (y*width+x)*4);
                        }
                }
                return result;
            }

            private static Point Interpolate(Point a, Point b, double amount)
            {
                return new Point(a.X+(b.X-a.X)*amount,a.Y+(b.Y-a.Y)*amount);
            }

            private static Point[] Mesh(byte[] image)
            {
                Point[] eyes = Eyes(image);
                Point left = eyes[0], right = eyes[1];
                double eyeY = (left.Y+right.Y)/2, centerX = (left.X+right.X)/2;
                Point chin = new Point(centerX,118);
                for (int y = 100; y < 128; y++)
                {
                    int count = 0, totalX = 0;
                    for (int x = 58; x < 134; x++)
                        if (Skin(image,(y*W+x)*4) && Math.Abs(x-centerX) < 32) {count++; totalX+=x;}
                    if (count >= 16) chin = new Point(totalX/(double)count,y+1);
                }
                double sum = 0, sumX = 0, sumY = 0;
                for (int y = (int)eyeY+3; y < (int)chin.Y-1; y++)
                    for (int x = (int)centerX-10; x <= (int)centerX+10; x++)
                    {
                        int p = (y*W+x)*4;
                        double weight = image[p+1] < 155 && image[p+2] < 210 ? Math.Max(0,image[p+2]-image[p+1]-20)*Math.Max(0,image[p+2]-image[p]-20) : 0;
                        sum+=weight; sumX+=x*weight; sumY+=y*weight;
                    }
                Point mouth = sum > 0 ? new Point(sumX/sum,sumY/sum) : new Point(centerX,eyeY+(chin.Y-eyeY)*0.65);
                mouth.Y = Math.Max(eyeY+3,Math.Min(chin.Y-2,mouth.Y));
                double cheekLeft = left.X-8, cheekRight = right.X+8;
                int row = (int)Math.Round(mouth.Y);
                for (int x = 58; x < 134; x++)
                    if (Skin(image,(row*W+x)*4) && Math.Abs(x-centerX) < 32)
                    {cheekLeft=Math.Min(cheekLeft,x); cheekRight=Math.Max(cheekRight,x);}
                double forehead = eyeY-24;
                Point leftSide = Side(image, true), rightSide = Side(image, false);
                return new Point[] {
                    new Point(cheekLeft-2,forehead),new Point(centerX,forehead),new Point(cheekRight+2,forehead),
                    left,new Point(centerX,eyeY),right,
                    new Point(cheekLeft,mouth.Y),mouth,new Point(cheekRight,mouth.Y),chin,
                    Tip(image,46,76),Tip(image,80,116),Tip(image,116,146),rightSide,
                    new Point(124,133),new Point(95,133),new Point(66,133),leftSide,
                    new Point(0,0),new Point(95,0),new Point(191,0),new Point(191,90),
                    new Point(191,143),new Point(95,143),new Point(0,143),new Point(0,90)
                };
            }

            private static Point Tip(byte[] image,int start,int end)
            {
                for (int y = 20; y < 76; y++)
                {
                    int sum = 0, count = 0;
                    for (int x = start; x < end; x++)
                        if (image[(y*W+x)*4+3]>175) {sum+=x;count++;}
                    if (count>0) return new Point(sum/(double)count,y);
                }
                return new Point((start+end)/2.0,48);
            }

            private static Point Side(byte[] image,bool left)
            {
                int edge = left ? 75 : 116;
                for (int y = 90; y < 102; y++)
                    for (int x = left ? 36 : 116; x < (left ? 76 : 155); x++)
                        if (image[(y*W+x)*4+3]>175) edge = left ? Math.Min(edge,x) : Math.Max(edge,x);
                return new Point(edge,96);
            }

            private static bool Skin(byte[] image, int p)
            {
                return image[p+3]>230 && image[p+2]-image[p+1]>12 && image[p+2]-image[p]>15 && image[p+1]>140;
            }

            private static Point[] Eyes(byte[] image)
            {
                bool[] mask = new bool[W*128];
                double skinX = 0, skinY = 0, skinCount = 0;
                for (int y = 72; y < 122; y++)
                    for (int x = 58; x < 134; x++)
                    {
                        int p = (y*W+x)*4;
                        mask[y*W+x] = image[p+3]>220 && image[p]>110 && image[p+1]>60 && image[p]-image[p+2]>20 && image[p]-image[p+1]>28;
                        if (Skin(image,p)) {skinX+=x; skinY+=y; skinCount++;}
                    }
                skinX = skinCount>0 ? skinX/skinCount : 94;
                skinY = skinCount>0 ? skinY/skinCount : 110;
                var candidates = new System.Collections.Generic.List<EyeRegion>();
                int[] queue = new int[mask.Length];
                for (int y = 72; y < 122; y++)
                    for (int x = 58; x < 134; x++)
                    {
                        int start = y*W+x;
                        if (!mask[start]) continue;
                        int read = 0, count = 1, totalX = 0, totalY = 0, minX = x, maxX = x, minY = y, maxY = y;
                        queue[0] = start; mask[start] = false;
                        while (read < count)
                        {
                            int p = queue[read++], px = p%W, py = p/W;
                            totalX+=px; totalY+=py;
                            minX=Math.Min(minX,px); maxX=Math.Max(maxX,px); minY=Math.Min(minY,py); maxY=Math.Max(maxY,py);
                            int[] neighbors = {p-1,p+1,p-W,p+W};
                            foreach (int next in neighbors)
                                if (next>=0 && next<mask.Length && mask[next]) {mask[next]=false; queue[count++]=next;}
                        }
                        if (count>=3 && count<200 && maxX-minX<22 && maxY-minY<24)
                            candidates.Add(new EyeRegion(new Point(totalX/(double)count,totalY/(double)count),count));
                    }
                double best = -1;
                Point left = new Point(80,103), right = new Point(108,103);
                for (int i = 0; i < candidates.Count; i++)
                    for (int j = i+1; j < candidates.Count; j++)
                    {
                        EyeRegion a = candidates[i], b = candidates[j];
                        double dx = Math.Abs(a.Center.X-b.Center.X), dy = a.Center.Y-b.Center.Y;
                        if (dx<12 || dx>42 || Math.Abs(dy)>9) continue;
                        double cx = (a.Center.X+b.Center.X)/2-skinX;
                        double cy = (a.Center.Y+b.Center.Y)/2-skinY+8;
                        double score = (a.Area+b.Area)*Math.Exp(-dy*dy/64-cx*cx/160-cy*cy/500);
                        if (score>best)
                        {
                            best=score;
                            left=a.Center.X<b.Center.X?a.Center:b.Center;
                            right=a.Center.X<b.Center.X?b.Center:a.Center;
                        }
                    }
                return new Point[] {left,right};
            }

            private struct EyeRegion
            {
                internal Point Center;
                internal int Area;
                internal EyeRegion(Point center,int area) {Center=center;Area=area;}
            }

            private struct Point
            {
                internal double X,Y;
                internal Point(double x,double y) {X=x;Y=y;}
            }
        }
    }
}
