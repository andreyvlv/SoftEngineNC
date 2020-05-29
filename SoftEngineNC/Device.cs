using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SoftEngineNC
{
    public class Device
    {
        private byte[] backBuffer;
        private readonly float[] depthBuffer;
        private WriteableBitmap bmp;
        private readonly int renderWidth;
        private readonly int renderHeight;

        private object[] lockBuffer;

        public Device(WriteableBitmap bmp)
        {
            this.bmp = bmp;

            renderWidth = bmp.PixelWidth;
            renderHeight = bmp.PixelHeight;
 
            backBuffer = new byte[renderWidth * renderHeight * 4];
            depthBuffer = new float[renderWidth * renderHeight];
            lockBuffer = new object[renderWidth * renderHeight];
            for (var i = 0; i < lockBuffer.Length; i++)
            {
                lockBuffer[i] = new object();
            }
        }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (var index = 0; index < backBuffer.Length; index += 4)
            {
                backBuffer[index] = b;
                backBuffer[index + 1] = g;
                backBuffer[index + 2] = r;
                backBuffer[index + 3] = a;
            }

            for (var index = 0; index < depthBuffer.Length; index++)
            {
                depthBuffer[index] = float.MaxValue;
            }
        }

        public void Present()
        {
            int widthInBytes = 4 * bmp.PixelWidth;
            bmp.WritePixels(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight), backBuffer, widthInBytes, 0);
        }

        public void PutPixel(int x, int y, float z, Color color)
        {
            var index = (x + y * renderWidth);
            var index4 = index * 4;

            lock (lockBuffer[index])
            {
                if (depthBuffer[index] < z)
                {
                    return; // Discard
                }

                depthBuffer[index] = z;

                backBuffer[index4] = color.B;
                backBuffer[index4 + 1] = color.G;
                backBuffer[index4 + 2] = color.R;
                backBuffer[index4 + 3] = color.A;
            }
        }

        public Vertex Project(Vertex vertex, Matrix4x4 transMat, Matrix4x4 world)
        {
            var point2d = Utils.TransformCoordinate(vertex.Coordinates, transMat);

            var point3dWorld = Utils.TransformCoordinate(vertex.Coordinates, world);
            var normal3dWorld = Utils.TransformCoordinate(vertex.Normal, world);

            var x = point2d.X * renderWidth + renderWidth / 2.0f;
            var y = -point2d.Y * renderHeight + renderHeight / 2.0f;

            return new Vertex
            {
                Coordinates = new Vector3(x, y, point2d.Z),
                Normal = normal3dWorld,
                WorldCoordinates = point3dWorld
            };
        }

        public void DrawPoint(Vector3 point, Color color)
        {
            if (point.X >= 0 && point.Y >= 0 && point.X < renderWidth && point.Y < renderHeight)
            {
                PutPixel((int)point.X, (int)point.Y, point.Z, color);
            }
        }              

        void ProcessScanLine(ScanLineData data, Vertex va, Vertex vb, Vertex vc, Vertex vd, Color color)
        {
            Vector3 pa = va.Coordinates;
            Vector3 pb = vb.Coordinates;
            Vector3 pc = vc.Coordinates;
            Vector3 pd = vd.Coordinates;

            var gradient1 = pa.Y != pb.Y ? (data.currentY - pa.Y) / (pb.Y - pa.Y) : 1;
            var gradient2 = pc.Y != pd.Y ? (data.currentY - pc.Y) / (pd.Y - pc.Y) : 1;

            int sx = (int)Utils.Interpolate(pa.X, pb.X, gradient1);
            int ex = (int)Utils.Interpolate(pc.X, pd.X, gradient2);

            float z1 = Utils.Interpolate(pa.Z, pb.Z, gradient1);
            float z2 = Utils.Interpolate(pc.Z, pd.Z, gradient2);

            for (var x = sx; x < ex; x++)
            {
                float gradient = (x - sx) / (float)(ex - sx);

                var z = Utils.Interpolate(z1, z2, gradient);
                var ndotl = data.ndotla;

                var newColor = Color.FromArgb((int)(color.R * ndotl), (int)(color.G * ndotl), (int)(color.B * ndotl));

                DrawPoint(new Vector3(x, data.currentY, z), newColor);
            }
        }

        public void DrawTriangle(Vertex v1, Vertex v2, Vertex v3, Color color)
        {
            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            if (v2.Coordinates.Y > v3.Coordinates.Y)
            {
                var temp = v2;
                v2 = v3;
                v3 = temp;
            }

            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            Vector3 p1 = v1.Coordinates;
            Vector3 p2 = v2.Coordinates;
            Vector3 p3 = v3.Coordinates;

            Vector3 vnFace = (v1.Normal + v2.Normal + v3.Normal) / 3;

            Vector3 centerPoint = (v1.WorldCoordinates + v2.WorldCoordinates + v3.WorldCoordinates) / 3;
            
            Vector3 lightPos = new Vector3(-2, 0, 0);
            
            float ndotl = Utils.ComputeNDotL(centerPoint, vnFace, lightPos);

            var data = new ScanLineData { ndotla = ndotl };

            float dP1P2, dP1P3;

            if (p2.Y - p1.Y > 0)
                dP1P2 = (p2.X - p1.X) / (p2.Y - p1.Y);
            else
                dP1P2 = 0;

            if (p3.Y - p1.Y > 0)
                dP1P3 = (p3.X - p1.X) / (p3.Y - p1.Y);
            else
                dP1P3 = 0;

            if (dP1P2 > dP1P3)
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    data.currentY = y;

                    if (y < p2.Y)
                    {
                        ProcessScanLine(data, v1, v3, v1, v2, color);
                    }
                    else
                    {
                        ProcessScanLine(data, v1, v3, v2, v3, color);
                    }
                }
            }
            else
            {
                for (var y = (int)p1.Y; y <= (int)p3.Y; y++)
                {
                    data.currentY = y;

                    if (y < p2.Y)
                    {
                        ProcessScanLine(data, v1, v2, v1, v3, color);
                    }
                    else
                    {
                        ProcessScanLine(data, v2, v3, v1, v3, color);
                    }
                }
            }
        }

        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix4x4.CreateLookAt(camera.Position, camera.Target, Vector3.UnitY);

            var projectionMatrix =
                Matrix4x4.CreatePerspectiveFieldOfView(0.78f, (float)renderWidth / renderHeight, 0.01f, 1.0f);

            foreach (var mesh in meshes)
            {
                var worldMatrix = Matrix4x4.CreateFromYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix4x4.CreateTranslation(mesh.Position);

                var worldView = worldMatrix * viewMatrix;
                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                Parallel.For(0, mesh.Faces.Length, faceIndex =>
                {
                    var face = mesh.Faces[faceIndex];

                    var transformedNormal = Vector3.TransformNormal(face.Normal, worldView);

                    // face culling
                    if (transformedNormal.Z < 0)
                    {
                        var vertexA = mesh.Vertices[face.A];
                        var vertexB = mesh.Vertices[face.B];
                        var vertexC = mesh.Vertices[face.C];

                        var pixelA = Project(vertexA, transformMatrix, worldMatrix);
                        var pixelB = Project(vertexB, transformMatrix, worldMatrix);
                        var pixelC = Project(vertexC, transformMatrix, worldMatrix);

                        DrawTriangle(pixelA, pixelB, pixelC, Color.White);
                    }
                });
            }
        }             
    }
}
