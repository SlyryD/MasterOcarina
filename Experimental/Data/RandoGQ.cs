using System.Collections.Generic;
using mzxrules.OcaLib;

namespace Experimental.Data
{
    static partial class Get
    {
        public static void GQRandoCompareHeaders(IExperimentFace face, List<string> filePath)
        {
            MQUtils.CompareHeaders(face, GQGetInput(filePath));
        }

        public static void GQCompareCollision(IExperimentFace face, List<string> filePath)
        {
            MQUtils.CompareCollision(face, GQGetInput(filePath));
        }

        public static void OutputGQJson(IExperimentFace face, List<string> filePath)
        {
            MQUtils.OutputJson(face, GQGetInput(filePath));
        }

        public static void GQJsonImportAndPatch(IExperimentFace face, List<string> filePath)
        {
            MQUtils.ImportAndPatchJson(face, GQGetInput(filePath));
        }

        public static void GQImportMapData(IExperimentFace face, List<string> filePath)
        {
            MQUtils.ImportMapData(face, GQGetInput(filePath));
        }

        public static void GQOverworldRandoCompareHeaders(IExperimentFace face, List<string> filePath)
        {
            MQUtils.CompareHeaders(face, GQOverworldGetInput(filePath));
        }

        public static void GQOverworldCompareCollision(IExperimentFace face, List<string> filePath)
        {
            MQUtils.CompareCollision(face, GQOverworldGetInput(filePath));
        }

        public static void OutputGQOverworldJson(IExperimentFace face, List<string> filePath)
        {
            MQUtils.OutputJson(face, GQOverworldGetInput(filePath));
        }

        public static void GQOverworldJsonImportAndPatch(IExperimentFace face, List<string> filePath)
        {
            MQUtils.ImportAndPatchJson(face, GQOverworldGetInput(filePath));
        }

        public static void GQOverworldImportMapData(IExperimentFace face, List<string> filePath)
        {
            MQUtils.ImportMapData(face, GQOverworldGetInput(filePath));
        }

        private static MQUtilsInput GQGetInput(List<string> filePath) {
            return new MQUtilsInput()
            {
                N0 = new ORom(filePath[0], ORom.Build.N0),
                MQ = new ORom(filePath[1], ORom.Build.GQU),
                MQJson = "gqu.json",
                SceneNumbers = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 13 },
                DungMarkVrom = 0xBFABBC, // n0 0xBFABBC
                DungMarkVram = 0x8085D2DC, // n0 0x8085D2DC
                FloorIndex = 0xB6C934, // n0 0xB6C934
                FloorData = 0xBC7E00 // n0 0xBC7E00
            };
        }

        private static MQUtilsInput GQOverworldGetInput(List<string> filePath)
        {
            return new MQUtilsInput()
            {
                N0 = new ORom(filePath[0], ORom.Build.N0),
                MQ = new ORom(filePath[1], ORom.Build.GQU),
                MQJson = "gqu_overworld.json",
                SceneNumbers = new int[] { 12, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99 },
                DungMarkVrom = 0xBFABBC, // n0 0xBFABBC
                DungMarkVram = 0x8085D2DC, // n0 0x8085D2DC
                FloorIndex = 0xB6C934, // n0 0xB6C934
                FloorData = 0xBC7E00 // n0 0xBC7E00
            };
        }
    }
}
