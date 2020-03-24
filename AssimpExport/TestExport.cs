using Assimp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenVIII.AssimpExport {
    public class TestExport {

        public void TestExportToFile(string outputDir, string formatId, string extension)
        {
            Directory.CreateDirectory(outputDir);
            String path = Path.Combine(outputDir, $"ExportedTriangle.{extension}");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            //Create a very simple scene a single node with a mesh that has a single face, a triangle and a default material
            Scene scene = new Scene();
            scene.RootNode = new Node("Root");

            Mesh triangle = new Mesh("", PrimitiveType.Triangle);
            triangle.Vertices.Add(new Vector3D(1, 0, 0));
            triangle.Vertices.Add(new Vector3D(5, 5, 0));
            triangle.Vertices.Add(new Vector3D(10, 0, 0));
            triangle.Faces.Add(new Face(new int[] { 0, 1, 2 }));
            triangle.MaterialIndex = 0;

            scene.Meshes.Add(triangle);
            scene.RootNode.MeshIndices.Add(0);

            Material mat = new Material();
            mat.Name = "MyMaterial";
            scene.Materials.Add(mat);

            //Export the scene then read it in and compare!

            AssimpContext context = new AssimpContext();
            Debug.Assert(context.ExportFile(scene, path, formatId));
            /*
            Scene importedScene = context.ImportFile(path);
            Debug.Assert(importedScene.MeshCount == scene.MeshCount);
            Debug.Assert(importedScene.MaterialCount == 2); //Always has the default material, should also have our material

            //Compare the meshes
            Mesh importedTriangle = importedScene.Meshes[0];

            Debug.Assert(importedTriangle.VertexCount == triangle.VertexCount);
            for (int i = 0; i < importedTriangle.VertexCount; i++)
            {
                Debug.Assert(importedTriangle.Vertices[i].Equals(triangle.Vertices[i]));
            }

            Debug.Assert(importedTriangle.FaceCount == triangle.FaceCount);
            for (int i = 0; i < importedTriangle.FaceCount; i++)
            {
                Face importedFace = importedTriangle.Faces[i];
                Face face = triangle.Faces[i];

                for (int j = 0; j < importedFace.IndexCount; j++)
                {
                    Debug.Assert(importedFace.Indices[j] == face.Indices[j]);
                }
            }*/
        }
    }
}
