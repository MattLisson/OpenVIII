using Assimp;
using OpenVIII;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenVIII.AssimpExport {
    static class Program {
        static void Main(string[] args)
        {
            Assimp.Unmanaged.AssimpLibrary.Instance.EnableVerboseLogging(true);
            ConsoleLogStream logStream = new ConsoleLogStream();
            logStream.Attach();

            Memory.Init(null, null, null, args);

            string testDir = "..\\..\\..\\..\\..\\Output\\Test";
            TestExport tests = new TestExport();
            tests.TestExportToFile(testDir, "obj", "obj");
            tests.TestExportToFile(testDir, "collada", "dae");
          //  tests.TestExportToFile(testDir, "fbx", "fbx");
            string outputDir = "..\\..\\..\\..\\..\\Output";
            Directory.CreateDirectory(outputDir);


            for (int monsterId = 71; monsterId < 72; monsterId++)
            {
                Debug_battleDat monsterData = Debug_battleDat.Load(monsterId,
                    Debug_battleDat.EntityType.Monster);
                ModelReader reader = new ModelReader(outputDir, "monster", monsterData);
                reader.Save();
            }

            for (int characterId = 0; characterId < 1; characterId++)
            {
                Debug_battleDat monsterData = Debug_battleDat.Load(characterId,
                    Debug_battleDat.EntityType.Character);
                ModelReader reader = new ModelReader(outputDir, "character", monsterData);
                reader.Save();
            }
        }
    }
}
