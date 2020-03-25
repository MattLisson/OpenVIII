﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Assimp;
using OpenVIII;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Drawing;

namespace OpenVIII.AssimpExport {
    
    public class MeshBuilder {
        
        private Mesh mesh;
        private Dictionary<(int, Vector2), int> vertexUVIndexMap = new Dictionary<(int, Vector2), int>();
        private List<Vector3D> originalVertices;
        private Dictionary<int, int> boneIdByVertId;
        private Dictionary<int, Matrix4x4> boneMatrixByBoneId;
        private Dictionary<int, Bone> boneByBoneId = new Dictionary<int, Bone>();
        private Vector2 texSize;
        public MeshBuilder(
            int meshIndex,
            int materialIndex,
            Vector2 texSize,
            List<Vector3D> originalVertices,
            Dictionary<int, int> boneIdByVertId,
            Dictionary<int, Matrix4x4> boneMatrixByBoneId)
        {
            this.originalVertices = originalVertices;
            this.boneIdByVertId = boneIdByVertId;
            this.boneMatrixByBoneId = boneMatrixByBoneId;
            this.texSize = texSize;

            mesh = new Mesh($"mesh-{meshIndex}-{materialIndex}");
            mesh.MaterialIndex = materialIndex;
        }

        public void AddTriangle(Debug_battleDat.Triangle tri)
        {
            Face face = new Face();
            int a = MaybeAddVertex(tri.A1, tri.vtb);
            int b = MaybeAddVertex(tri.B1, tri.vtc);
            int c = MaybeAddVertex(tri.C1, tri.vta);

            face.Indices.Add(c);
            face.Indices.Add(b);
            face.Indices.Add(a);

            mesh.Faces.Add(face);
        }

        public void AddQuad(Debug_battleDat.Quad quad)
        {
            Face face1 = new Face();
            Face face2 = new Face();
            int a = MaybeAddVertex(quad.A1, quad.vta);
            int b = MaybeAddVertex(quad.B1, quad.vtb);
            int c = MaybeAddVertex(quad.C1, quad.vtc);
            int d = MaybeAddVertex(quad.D1, quad.vtd);

            face1.Indices.Add(d);
            face1.Indices.Add(b);
            face1.Indices.Add(a);

            face2.Indices.Add(c);
            face2.Indices.Add(d);
            face2.Indices.Add(a);

            mesh.Faces.Add(face1);
            mesh.Faces.Add(face2);
        }

        public int MaybeAddVertex(int oldIndex, Debug_battleDat.UV uv)
        {
            Vector2 xnaUV = uv.ToVector2(texSize.X, texSize.Y);
            var vertexUV = (oldIndex, xnaUV);
            int newIndex;
            if (vertexUVIndexMap.ContainsKey(vertexUV))
            {
                newIndex = vertexUVIndexMap[vertexUV];
            }
            else
            {
                mesh.Vertices.Add(originalVertices[oldIndex]);
                mesh.TextureCoordinateChannels[0].Add(new Vector3D(xnaUV.X, xnaUV.Y, 0.0f));
                newIndex = mesh.Vertices.Count - 1;
                vertexUVIndexMap[vertexUV] = newIndex;

                AddVertToBone(oldIndex, newIndex);
            }
            return newIndex;
        }

        public void AddVertToBone(int oldIndex, int newIndex)
        {
            int boneId = boneIdByVertId[oldIndex];
            Bone bone;
            if (boneByBoneId.ContainsKey(boneId))
            {
                bone = boneByBoneId[boneId];
            }
            else
            {
                bone = new Bone();
                boneByBoneId[boneId] = bone;
                bone.Name = ModelReader.NodeNameForBoneId(boneId);
                bone.OffsetMatrix = Matrix4x4.Identity;
                //bone.OffsetMatrix = boneMatrixByBoneId[boneId];
                mesh.Bones.AddRange(boneByBoneId.Values);
            }
            bone.VertexWeights.Add(new VertexWeight(newIndex, 1.0f));
        }

        public Mesh Build()
        {
            return mesh;
        }
    }

    public class ModelReader {
        public Scene Scene { get; }
        Dictionary<int, int> boneIdByVertId = new Dictionary<int, int>();
        Dictionary<int, Node> nodesByBoneId = new Dictionary<int, Node>();
        Dictionary<int, Matrix4x4> boneMatrixByBoneId = new Dictionary<int, Matrix4x4>();
        Dictionary<int, Vector2> textureSizeById = new Dictionary<int, Vector2>();
        private string saveFolder;
        string modelName;

        public ModelReader(string saveFolder, string modelType, Debug_battleDat monsterData)
        {
            this.saveFolder = saveFolder;
            this.modelName = $"{modelType}-{monsterData.id}";
            Scene = new Scene();
            Scene.RootNode = new Node("root");
            AddModel(monsterData);
        }

        private void AddModel(Debug_battleDat monsterData)
        {
            List<Material> materials = ReadAndSaveTextures(monsterData.textures);
            Scene.Materials.AddRange(materials);
            if (monsterData.animHeader.cAnimations > 0)
            {
                AddSkeleton(monsterData.skeleton, monsterData.animHeader.animations[0].animationFrames[0]);
            }
            
            for (int meshIndex = 0; meshIndex < monsterData.geometry.cObjects; meshIndex++)
            {
                Debug_battleDat.Object oldMesh = monsterData.geometry.objects[meshIndex];
                List<Vector3D> vertexPositions = ReadVertexPositions(oldMesh, monsterData.animHeader.animations[0].animationFrames[0]);
                Dictionary<int, MeshBuilder> meshBuildersByMaterial = new Dictionary<int, MeshBuilder>();
                
                for (int triIndex = 0; triIndex < oldMesh.cTriangles; triIndex++)
                {
                    Debug_battleDat.Triangle tri = oldMesh.triangles[triIndex];
                    int textureIndex = tri.textureIndex;
                    if (!meshBuildersByMaterial.ContainsKey(textureIndex)) {
                        meshBuildersByMaterial[textureIndex] =
                            new MeshBuilder(
                                meshIndex,
                                textureIndex,
                                this.textureSizeById[textureIndex],
                                vertexPositions,
                                boneIdByVertId,
                                boneMatrixByBoneId);
                    }
                    MeshBuilder builder = meshBuildersByMaterial[textureIndex];
                    builder.AddTriangle(tri);
                }
                for (int quadIndex = 0; quadIndex < oldMesh.cQuads; quadIndex++)
                {
                    Debug_battleDat.Quad quad = oldMesh.quads[quadIndex];
                    int textureIndex = quad.textureIndex;
                    if (!meshBuildersByMaterial.ContainsKey(textureIndex))
                    {
                        meshBuildersByMaterial[textureIndex] =
                            new MeshBuilder(
                                meshIndex,
                                textureIndex,
                                this.textureSizeById[textureIndex],
                                vertexPositions,
                                boneIdByVertId,
                                boneMatrixByBoneId);
                    }
                    MeshBuilder builder = meshBuildersByMaterial[textureIndex];
                    builder.AddQuad(quad);
                }

                foreach (MeshBuilder builder in meshBuildersByMaterial.Values) {
                    Mesh mesh = builder.Build();

                    Scene.Meshes.Add(mesh);
                    int sceneMeshIndex = Scene.Meshes.Count - 1;
                    //Scene.RootNode.MeshIndices.Add(sceneMeshIndex);
                    Scene.RootNode.Children[0].MeshIndices.Add(sceneMeshIndex);
                }
            }
        }
        
        public static string NodeNameForBoneId(int boneId)
        {
            return $"bone_{boneId}";
        }

        public Node GetOrCreateNode(int boneId, Debug_battleDat.Bone[] bones, Debug_battleDat.AnimationFrame defaultPose)
        {
            if (boneId < 0 || boneId >= bones.Length)
            {
                return Scene.RootNode;
            }
            if (nodesByBoneId.ContainsKey(boneId))
            {
                return nodesByBoneId[boneId];
            }

            string nodeName = NodeNameForBoneId(boneId);
            Debug_battleDat.Bone bone = bones[boneId];
            Node parent = GetOrCreateNode(bone.parentId, bones, defaultPose);

            Node node = new Node(nodeName, parent);
            parent.Children.Add(node);
            nodesByBoneId[boneId] = node;

            Matrix xnaBoneMat = defaultPose.boneMatrix[boneId];
            Matrix4x4 absoluteTransform = ConvertMatrix(xnaBoneMat);
            boneMatrixByBoneId[boneId] = absoluteTransform;
            Matrix4x4 parentAbsoluteTransform = boneMatrixByBoneId.ContainsKey(bone.parentId)
                ? boneMatrixByBoneId[bone.parentId]
                : Matrix4x4.Identity;
            parentAbsoluteTransform.Inverse();

            node.Transform = absoluteTransform * parentAbsoluteTransform;

            //node.Transform = Matrix4x4.Identity;
            return node;
        }

        public void AddSkeleton(Debug_battleDat.Skeleton skeleton, Debug_battleDat.AnimationFrame defaultPose)
        {
            for(int boneIndex = 0; boneIndex < skeleton.cBones; boneIndex++)
            {
                GetOrCreateNode(boneIndex, skeleton.bones, defaultPose);
            }
        }

        public static Matrix4x4 ConvertMatrix(Matrix boneMat)
        {// This transposes the 4th Row/Col, because I think OpenVIII calculates it wrong.
            return new Matrix4x4(
                boneMat.M11, boneMat.M12, boneMat.M13, boneMat.M41,
                boneMat.M21, boneMat.M22, boneMat.M23, boneMat.M42,
                boneMat.M31, boneMat.M32, boneMat.M33, boneMat.M43,
                boneMat.M14, boneMat.M24, boneMat.M34, boneMat.M44
                );
        }

        public static void SaveTexture(TIM2 texture, string filename)
        {
            TIM2.NativeColor[] colors = texture.ReadColorData();
            Bitmap bitmap = new Bitmap(texture.GetWidth, texture.GetHeight);
            for(int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var color = colors[x + (y * bitmap.Width)];
                    bitmap.SetPixel(x, y,
                        System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
                }
            }
            bitmap.Save(filename);
        }

        public List<Material> ReadAndSaveTextures(Debug_battleDat.Textures textures)
        {
            List<Material> materials = new List<Material>();
            for (int texIndex = 0; texIndex < textures.cTims; texIndex++) {
                TIM2 oldTexture = textures.tims[texIndex];
                textureSizeById[texIndex] = new Vector2(oldTexture.GetWidth, oldTexture.GetHeight);
                string filename = $"{modelName}_{texIndex}.png";
                string filepath = $"{saveFolder}\\{filename}";
                FileInfo existingFile = new FileInfo(filepath);
                if (existingFile.Exists)
                {
                    existingFile.Delete();
                }
                SaveTexture(oldTexture, filepath);
                TextureSlot diffuse = new TextureSlot()
                {
                    FilePath = filename,
                    UVIndex = 0,
                    TextureType = TextureType.Diffuse,
                };
                Material material = new Material
                {
                    TextureDiffuse = diffuse
                };
                materials.Add(material);
            }
            return materials;
        }

        private Vector3 TransformPos(Vector3 pos, Matrix matrix, Vector3 scale)
        {
            // OpenVIII custom Matrix Mult:
            // Z is -Y, Y is Z, to account for FF8 Coordinate System.
            // M41,M42,M43 are used instead of M14,M24,M34 because thats how they made it...
            Vector3 r = new Vector3(
                matrix.M11 * pos.X + matrix.M41 + matrix.M12 * pos.Z + matrix.M13 * -pos.Y,
                matrix.M21 * pos.X + matrix.M42 + matrix.M22 * pos.Z + matrix.M23 * -pos.Y,
                matrix.M31 * pos.X + matrix.M43 + matrix.M32 * pos.Z + matrix.M33 * -pos.Y);
            r = Vector3.Transform(r, Matrix.CreateScale(scale));
            return r;
        }

        public List<Vector3D> ReadVertexPositions(Debug_battleDat.Object mesh, Debug_battleDat.AnimationFrame frame)
        {
            List<Vector3D> vertices = new List<Vector3D>();

            for (int vertDataIndex = 0; vertDataIndex < mesh.cVertices; vertDataIndex++)
            {
                Debug_battleDat.VerticeData vertData = mesh.vertexData[vertDataIndex];
                int boneId = vertData.boneId;
                Matrix4x4 transform = ConvertMatrix(frame.boneMatrix[vertData.boneId]);
                //transform.Transpose();
                for (int vertIndex = 0; vertIndex < vertData.cVertices; vertIndex++)
                {
                    
                    Vector3 vert = vertData.vertices[vertIndex];
                    // FF8 Space is other-handed with Z up/down.
                    Vector3D pos = new Vector3D(vert.X, vert.Z, -vert.Y);
                    Vector3D myWay = transform * pos;
                    vertices.Add(pos);
                    int vertId = vertices.Count - 1;
                    boneIdByVertId[vertId] = boneId;
                }
            }
            return vertices;
        }

        public static List<Face> ReadFaces(Debug_battleDat.Object mesh)
        {
            List<Face> faces = new List<Face>();
            for (int triIndex = 0; triIndex < mesh.cTriangles; triIndex++)
            {
                Debug_battleDat.Triangle tri = mesh.triangles[triIndex];
                Face face = new Face();
                face.Indices.Add(tri.GetIndex(0));
                face.Indices.Add(tri.GetIndex(1));
                face.Indices.Add(tri.GetIndex(2));

                faces.Add(face);
            }
            for (int quadIndex = 0; quadIndex < mesh.cQuads; quadIndex++)
            {
                Debug_battleDat.Quad quad = mesh.quads[quadIndex];
                Face face = new Face();
                face.Indices.Add(quad.GetIndex(0));
                face.Indices.Add(quad.GetIndex(1));
                face.Indices.Add(quad.GetIndex(2));
                face.Indices.Add(quad.GetIndex(3));

                faces.Add(face);
            }
            return faces;
        }

        public void Save()
        {
            string filename = $"{saveFolder}\\{modelName}.dae";
            AssimpContext assimp = new AssimpContext();
            FileInfo existingFile = new FileInfo(filename);
            if (existingFile.Exists)
            {
                existingFile.Delete();
            }
            if (!assimp.ExportFile(Scene, filename, "collada"))
            {
                Debug.Print(Assimp.Unmanaged.AssimpLibrary.Instance.GetErrorString());
                throw new ApplicationException($"Couldn't save {filename}");
            }
        }
    }
}
