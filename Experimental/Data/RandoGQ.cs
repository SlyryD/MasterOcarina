using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using mzxrules.Helper;
using System.IO;
using mzxrules.OcaLib.SceneRoom;
using mzxrules.OcaLib.SceneRoom.Commands;
using mzxrules.OcaLib;
using mzxrules.OcaLib.Maps;
using System.Runtime.Serialization.Json;

namespace Experimental.Data
{
    static partial class Get
    {
        public static void GQRandoCompareHeaders(IExperimentFace face, List<string> filePath)
            => GQJson.GQRandoCompareHeaders(face, filePath);

        public static void GQCompareCollision(IExperimentFace face, List<string> filePath)
            => GQJson.GQCompareCollision(face, filePath); 

        public static void OutputGQJson(IExperimentFace face, List<string> filePath)
            => GQJson.OutputGQJson(face, filePath);

        public static void GQJsonImportAndPatch(IExperimentFace face, List<string> filePath)
            => GQJson.GQJsonImportAndPatch(face, filePath);

        public static void GQImportMapData(IExperimentFace face, List<string> filePath)
            => GQJson.GQImportMapData(face, filePath);

    }
    class GQJson
    {
        [DataContract(Name = "File")]
        class File_GQJson
        {
            [DataMember(Order = 1)]
            public string Name { get; set; }

            [DataMember(Order = 2)]
            public string Start { get; set; }

            [DataMember(Order = 3)]
            public string End { get; set; }

            [DataMember(Order = 4)]
            public string RemapStart { get; set; }
        }


        [DataContract(Name = "Scene")]
        class Scene_GQJson
        {
            [DataMember(Order = 1)]
            public File_GQJson File { get; set; }

            [DataMember(Order = 2)]
            public int Id { get; set; }

            [DataMember(Order = 3)]
            List<string> TActors { get; set; } = new List<string>();

            [DataMember(Order = 4)]
            List<Path_GQJson> Paths { get; set; } = new List<Path_GQJson>();

            public int RoomsCount = 0;

            public SegmentAddress RoomsAddress = 0;

            [DataMember(Order = 5)]
            public List<Room_GQJson> Rooms { get; set; } = new List<Room_GQJson>();

            [DataMember(Order = 6)]
            public Col_GQJson ColDelta { get; set; }

            [DataMember(Order = 7)]
            public List<DungeonFloor> Floormaps { get; set; } = new List<DungeonFloor>();

            [DataMember(Order = 8)]
            public List<DungeonMinimap> Minimaps { get; set; } = new List<DungeonMinimap>();


            public Scene_GQJson(BinaryReader br, int id, int start, int end)
            {
                File = new File_GQJson()
                {
                    Name = $"Scene {id}",
                    Start = start.ToString("X8"),
                    End = end.ToString("X8"),
                };

                Id = id;

                Console.Out.WriteLine(File.Name);
                SceneWord cmd = new SceneWord();
                do
                {
                    Console.Out.Write((start + br.BaseStream.Position).ToString("X2") + " ");
                    br.Read(cmd, 0, 8);

                    var seekback = br.BaseStream.Position;
                    HeaderCommands code = (HeaderCommands)cmd.Code;
                    Console.Out.Write(Enum.GetName(typeof(HeaderCommands), cmd.Code) + " ");
                    Console.Out.Write(cmd.Data1.ToString("X2") + " ");
                    Console.Out.WriteLine(cmd.Data2.ToString("X8") + " ");

                    if (code == HeaderCommands.PathList)
                    {
                        SegmentAddress offset = cmd.Data2;
                        br.BaseStream.Position = offset.Offset;

                        InitPaths(br);
                    }
                    else if (code == HeaderCommands.TransitionActorList)
                    {
                        SegmentAddress actorOff = cmd.Data2;
                        br.BaseStream.Position = actorOff.Offset;

                        for (int i = 0; i < cmd.Data1; i++)
                        {
                            TActors.Add(Actor_GQJson.Read(br));
                        }
                    }
                    if (code == HeaderCommands.RoomList)
                    {
                        RoomsCount = (int)cmd.Data1;
                        RoomsAddress = cmd.Data2;
                    }

                    br.BaseStream.Position = seekback;
                }
                while ((HeaderCommands)cmd.Code != HeaderCommands.End);
            }

            private void InitPaths(BinaryReader br)
            {
                int loop = 0;
                do
                {
                    sbyte nodes = br.ReadSByte();
                    br.BaseStream.Position += 3;
                    SegmentAddress address = br.ReadBigInt32();

                    if (nodes > 0
                        && loop < 20
                        && (address.Offset < 0x2F_FFFF) //assuming an address range of 0200 0000 to 022F FFFF, quite larger than expected
                        && address.Segment == 0x02)
                    {
                        var seekback = br.BaseStream.Position;
                        Path_GQJson path = new Path_GQJson();
                        br.BaseStream.Position = address.Offset;
                        for (int i = 0; i < nodes; i++)
                        {
                            var point = new short[] { br.ReadBigInt16(), br.ReadBigInt16(), br.ReadBigInt16() };
                            path.Points.Add(point);
                        }
                        Paths.Add(path);
                        br.BaseStream.Position = seekback;

                        loop++;
                    }
                    else
                    {
                        break;
                    }
                }
                while (true);
            }
        }

        [DataContract(Name = "Path")]
        class Path_GQJson
        {
            [DataMember]
            public List<short[]> Points { get; set; } = new List<short[]>();
        }

        [DataContract(Name = "Room")]
        class Room_GQJson
        {
            [DataMember(Order = 1)]
            public File_GQJson File { get; set; }

            [DataMember(Order = 2)]
            public int Id { get; set; }

            [DataMember(Order = 3)]
            List<string> Objects { get; set; } = new List<string>();

            [DataMember(Order = 4)]
            List<string> Actors { get; set; } = new List<string>();

            public Room_GQJson() { }

            public Room_GQJson(BinaryReader br, int sceneId, int roomId, int start, int end)
            {
                File = new File_GQJson()
                {
                    Name = $"Scene {sceneId}, Room {roomId}",
                    Start = start.ToString("X8"),
                    End = end.ToString("X8"),
                };

                Id = roomId;

                SceneWord cmd = new SceneWord();
                do
                {
                    br.Read(cmd, 0, 8);

                    var seekback = br.BaseStream.Position;

                    if ((HeaderCommands)cmd.Code == HeaderCommands.ObjectList)
                    {
                        SegmentAddress offset = cmd.Data2;
                        br.BaseStream.Position = offset.Offset;

                        for (int i = 0; i < cmd.Data1; i++)
                        {
                            Objects.Add(br.ReadBigInt16().ToString("X4"));
                        }
                    }
                    else if ((HeaderCommands)cmd.Code == HeaderCommands.ActorList)
                    {
                        SegmentAddress actorOff = cmd.Data2;
                        br.BaseStream.Position = actorOff.Offset;

                        for (int i = 0; i < cmd.Data1; i++)
                        {
                            Actors.Add(Actor_GQJson.Read(br));
                        }
                    }

                    br.BaseStream.Position = seekback;
                }
                while ((HeaderCommands)cmd.Code != HeaderCommands.End);
            }

        }

        [DataContract(Name = "CollisionDelta")]
        class Col_GQJson
        {
            [DataMember(Order = 1)]
            public bool IsLarger { get; set; }

            [DataMember(Order = 2)]
            public ColVertex_GQJson MinVertex { get; set; }

            [DataMember(Order = 3)]
            public ColVertex_GQJson MaxVertex { get; set; }

            [DataMember(Order = 4)]
            public int NumVertices { get; set; }

            [DataMember(Order = 5)]
            public List<ColVertex_GQJson> Vertices = new List<ColVertex_GQJson>();

            [DataMember(Order = 6)]
            public int NumPolys { get; set; }

            [DataMember(Order = 7)]
            public List<ColPoly_GQJson> Polys = new List<ColPoly_GQJson>();

            [DataMember(Order = 8)]
            public int NumPolyTypes { get; set; }

            [DataMember(Order = 9)]
            public List<ColMat_GQJson> PolyTypes = new List<ColMat_GQJson>();

            [DataMember(Order = 10)]
            public int NumCams { get; set; }

            [DataMember(Order = 11)]
            public List<ColCam_GQJson> Cams = new List<ColCam_GQJson>();

            [DataMember(Order = 12)]
            public int NumWaterBoxes { get; set; }

            [DataMember(Order = 13)]
            public List<ColWaterBox_GQJson> WaterBoxes = new List<ColWaterBox_GQJson>();

            public Col_GQJson(CollisionMesh n0, CollisionMesh gq)
            {
                IsLarger = (n0.GetFileSize() < gq.GetFileSize());

                MinVertex = new ColVertex_GQJson(-1, gq.BoundsMin.x, gq.BoundsMin.y, gq.BoundsMin.z);
                MaxVertex = new ColVertex_GQJson(-1, gq.BoundsMax.x, gq.BoundsMax.y, gq.BoundsMax.z);

                NumVertices = gq.Vertices;

                for (int i = 0; i < gq.VertexList.Count; i++)
                {
                    var gqVertex = gq.VertexList[i];
                    if (i >= n0.VertexList.Count || !n0.VertexList[i].Equals(gqVertex))
                    {
                        Vertices.Add(new ColVertex_GQJson(i, gqVertex.x, gqVertex.y, gqVertex.z));
                    }
                }

                NumPolys = gq.Polys;

                for (int i = 0; i < gq.PolyList.Count; i++)
                {
                    var n0poly = n0.PolyList[i];
                    var gqpoly = gq.PolyList[i];

                    if (!n0poly.Equals(gqpoly))
                    {
                        Polys.Add(new ColPoly_GQJson(i, gqpoly.Type, gqpoly.VertexFlagsA));
                    }
                }

                NumPolyTypes = gq.PolyTypes;

                for (int i = 0; i < gq.PolyTypeList.Count; i++)
                {
                    var gqpolytype = gq.PolyTypeList[i];

                    if (!(i < n0.PolyTypeList.Count)
                        || !n0.PolyTypeList[i].Equals(gqpolytype))
                    {
                        PolyTypes.Add(new ColMat_GQJson(i, gqpolytype.HighWord, gqpolytype.LowWord));
                    }
                }

                NumCams = gq.CameraDatas;

                for (int i = 0; i < gq.CameraDataList.Count; i++)
                {
                    var gqcam = gq.CameraDataList[i];
                    ColCam_GQJson cam = null;
                    if (gqcam.PositionAddress == 0)
                    {
                        cam = new ColCam_GQJson(gqcam.CameraS, gqcam.NumCameras, -1);
                    }
                    else
                    {
                        for (int j = 0; j < n0.CameraDataList.Count; j++)
                        {
                            var n0cam = n0.CameraDataList[j];
                            if (n0cam.IsPositionListIdentical(gqcam))
                            {
                                cam = new ColCam_GQJson(gqcam.CameraS, gqcam.NumCameras, j);
                                break;
                            }
                        }
                    }
                    if (cam == null)
                    {
                        throw new Exception("BLERG");
                    }
                    Cams.Add(cam);
                }

                NumWaterBoxes = gq.WaterBoxes;

                for (int i = 0; i < gq.WaterBoxList.Count; i++)
                {
                    var gqWaterBox = gq.WaterBoxList[i];
                    if (i >= n0.WaterBoxList.Count || !n0.WaterBoxList[i].AreDataIdentical(gqWaterBox))
                    {
                        WaterBoxes.Add(new ColWaterBox_GQJson(i, gqWaterBox.Data));
                    }
                }
            }

        }

        [DataContract(Name = "Vertex")]
        class ColVertex_GQJson
        {
            [DataMember(Order = 1)]
            public int Id { get; set; }

            [DataMember(Order = 2)]
            public short X { get; set; }

            [DataMember(Order = 3)]
            public short Y { get; set; }

            [DataMember(Order = 4)]
            public short Z { get; set; }

            public ColVertex_GQJson(int id, short x, short y, short z)
            {
                Id = id;
                X = x;
                Y = y;
                Z = z;
            }

        }

        [DataContract(Name = "Poly")]
        class ColPoly_GQJson
        {
            [DataMember(Order = 1)]
            public int Id { get; set; }

            [DataMember(Order = 2)]
            public int Type { get; set; }

            [DataMember(Order = 3)]
            public int Flags { get; set; }

            public ColPoly_GQJson(int id, int type, int ex)
            {
                Id = id;
                Type = type;
                Flags = ex;
            }
        }

        [DataContract(Name = "PolyType")]
        class ColMat_GQJson
        {
            [DataMember(Order = 1)]
            public int Id { get; set; }

            [DataMember(Order = 2)]
            public int High { get; set; }

            [DataMember(Order = 3)]
            public int Low { get; set; }

            public ColMat_GQJson(int id, int hi, int lo)
            {
                Id = id;
                High = hi;
                Low = lo;
            }
        }

        [DataContract(Name = "Cam")]
        class ColCam_GQJson
        {
            [DataMember(Order = 1)]
            public int Data { get; set; }

            [DataMember(Order = 2)]
            public int PositionIndex { get; set; }

            public ColCam_GQJson(short cam, short num, int positionIndex)
            {
                Data = (cam << 16) + num;
                PositionIndex = positionIndex;
            }
        }


        [DataContract(Name = "WaterBox")]
        class ColWaterBox_GQJson
        {
            [DataMember(Order = 1)]
            public int Id { get; set; }

            [DataMember(Order = 2)]
            public short[] Data { get; set; }

            public ColWaterBox_GQJson(int id, short[] waterBox)
            {
                Id = id;
                Data = waterBox;
            }
        }


        static class Actor_GQJson
        {
            public static string Read(BinaryReader read)
            {
                List<string> result = new List<string>();
                for (int i = 0; i < 8; i++)
                {
                    string v = read.ReadBigInt16().ToString("X4");
                    result.Add(v);
                }

                return string.Join(" ", result);
            }
        }


        static readonly int[] GQRandoScenes = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

        public static void GQRandoCompareHeaders(IExperimentFace face, List<string> filePath)
        {
            Rom n0 = new ORom(filePath[0], ORom.Build.N0);
            Rom gq = new ORom(filePath[1], ORom.Build.GQU);

            StringWriter sw = new StringWriter();

            foreach (Rom rom in new Rom[] { n0, gq })
            {
                for (int i = 0; i < 16; i++)
                {
                    var scn = rom.Files.GetSceneFile(i);
                    BinaryReader br = new BinaryReader(scn);

                    SceneWord cmd;
                    SceneWord roomCmd = new SceneWord();
                    bool hasRoom = false;
                    do
                    {
                        cmd = new SceneWord();
                        br.Read(cmd, 0, 8);
                        //sw.WriteLine($"{i} -1 {cmd}");

                        if ((HeaderCommands)cmd.Code == HeaderCommands.RoomList)
                        {
                            hasRoom = true;
                            roomCmd = cmd;
                        }
                    }
                    while ((HeaderCommands)cmd.Code != HeaderCommands.End);

                    if (hasRoom)
                    {
                        var seekback = br.BaseStream.Position;

                        br.BaseStream.Position = ((SegmentAddress)roomCmd.Data2).Offset;
                        for (int j = 0; j < roomCmd.Data1; j++)
                        {
                            var roomAddr = br.ReadBigInt32();
                            br.ReadBigInt32();

                            var roomFile = rom.Files.GetFile(roomAddr);
                            BinaryReader brRoom = new BinaryReader(roomFile);
                            do
                            {
                                cmd = new SceneWord();
                                brRoom.Read(cmd, 0, 8);
                                //sw.WriteLine($"{i} {j} {cmd}");

                                if ((HeaderCommands)cmd.Code == HeaderCommands.ActorList)
                                {
                                    var va = roomFile.Record.VRom;
                                    sw.WriteLine($"{i} {j} {va.Start:X8} {va.End:X8} {(cmd.Data1 * 0x10):X8}");
                                }
                            }
                            while ((HeaderCommands)cmd.Code != HeaderCommands.End);
                        }
                        br.BaseStream.Position = seekback;
                    }
                }
                sw.WriteLine();
            }
            face.OutputText(sw.ToString());
        }

        public static void OutputGQJson(IExperimentFace face, List<string> filePath)
        {
            Rom n0 = new ORom(filePath[0], ORom.Build.N0);
            Rom gq = new ORom(filePath[1], ORom.Build.GQU);


            var rom_scenes = new List<List<Scene_GQJson>>();
            List<Scene_GQJson> scenes;

            foreach (Rom rom in new Rom[] { n0, gq })
            {
                scenes = new List<Scene_GQJson>();
                foreach (int sceneId in GQRandoScenes)
                {
                    var sceneFile = rom.Files.GetSceneFile(sceneId);
                    using (BinaryReader br = new BinaryReader(sceneFile))
                    {
                        var va = sceneFile.Record.VRom;
                        var scene = new Scene_GQJson(br, sceneId, va.Start, va.End);

                        br.BaseStream.Position = scene.RoomsAddress.Offset;

                        for (int roomId = 0; roomId < scene.RoomsCount; roomId++)
                        {
                            int start = br.ReadBigInt32();
                            int end = br.ReadBigInt32();

                            var roomFile = rom.Files.GetFile(start);

                            var room = new Room_GQJson(new BinaryReader(roomFile), sceneId, roomId, start, end);
                            scene.Rooms.Add(room);
                        }
                        scenes.Add(scene);
                    }
                }
                rom_scenes.Add(scenes);
            }

            var n0_scenes = rom_scenes[0];
            scenes = rom_scenes[1];
            SetN0Addresses(scenes, n0_scenes);

            MemoryStream ms = SerializeScenes(scenes);
            StreamReader sr = new StreamReader(ms);
            string json = sr.ReadToEnd();

            face.OutputText(json);

        }

        private static MemoryStream SerializeScenes(List<Scene_GQJson> scenes)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<Scene_GQJson>));
            MemoryStream ms = new MemoryStream();

            ser.WriteObject(ms, scenes);
            ms.Position = 0;
            return ms;
        }

        public static void GQJsonImportAndPatch(IExperimentFace face, List<string> filePath)
        {
            Rom n0 = new ORom(filePath[0], ORom.Build.N0);
            Rom gq = new ORom(filePath[1], ORom.Build.GQU);

            List<Scene_GQJson> scenes = Load_Scene_GQJson();

            foreach (var scene in scenes)
            {
                if (!GQRandoScenes.Contains(scene.Id))
                    continue;

                var n0_scene = SceneRoomReader.InitializeScene(n0, scene.Id);
                var gq_scene = SceneRoomReader.InitializeScene(gq, scene.Id);

                CollisionMesh n0_mesh = ((CollisionCommand)n0_scene.Header[HeaderCommands.Collision]).Mesh;
                CollisionMesh gq_mesh = ((CollisionCommand)gq_scene.Header[HeaderCommands.Collision]).Mesh;

                Console.Out.WriteLine();
                Console.Out.WriteLine("n0");
                Console.Out.WriteLine(n0_mesh);
                Console.Out.WriteLine("gq");
                Console.Out.WriteLine(gq_mesh);

                var delta = new Col_GQJson(n0_mesh, gq_mesh);
                scene.ColDelta = delta;
            }
            MemoryStream s = SerializeScenes(scenes);
            StreamReader sr = new StreamReader(s);
            face.OutputText(sr.ReadToEnd());

        }

        private static List<Scene_GQJson> Load_Scene_GQJson()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<Scene_GQJson>));

            List<Scene_GQJson> scenes;
            using (FileStream fs = new FileStream("gqu.json", FileMode.Open))
            {
                scenes = (List<Scene_GQJson>)ser.ReadObject(fs);
            }

            return scenes;
        }

        static void PrintList<T>(StringBuilder sb, List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                sb.AppendLine($"{i:X4} {item.ToString()}");
            }
        }

        public static void GQCompareCollision(IExperimentFace face, List<string> filePath)
        {
            Rom n0 = new ORom(filePath[0], ORom.Build.N0);
            Rom gq = new ORom(filePath[1], ORom.Build.GQU);

            StringBuilder sb_n0 = new StringBuilder();
            StringBuilder sb_gq = new StringBuilder();
            StringBuilder sb_result = new StringBuilder();

            foreach (int id in GQRandoScenes)
            {
                var n0_scene = SceneRoomReader.InitializeScene(n0, id);
                var gq_scene = SceneRoomReader.InitializeScene(gq, id);

                CollisionMesh n0_mesh = ((CollisionCommand)n0_scene.Header[HeaderCommands.Collision]).Mesh;
                CollisionMesh gq_mesh = ((CollisionCommand)gq_scene.Header[HeaderCommands.Collision]).Mesh;

                sb_n0.AppendLine($"Scene {id}");
                sb_gq.AppendLine($"Scene {id}");
                sb_result.AppendLine($"Scene {id}");

                sb_result.AppendLine($"N0");
                foreach (var command in n0_scene.Header.Commands())
                {
                    sb_n0.AppendLine(command.OffsetFromFile.ToString("X8") + " " + command.Code.ToString("X2") + " " + command.ToString());
                }
                sb_n0.AppendLine(n0_mesh.Print());
                sb_n0.AppendLine();
                sb_result.AppendLine(n0_mesh.Print());
                sb_result.AppendLine();

                sb_result.AppendLine($"GQ");
                foreach (var command in gq_scene.Header.Commands())
                {
                    sb_gq.AppendLine(command.OffsetFromFile.ToString("X8") + " " + command.Code.ToString("X2") + " " + command.ToString());
                }
                sb_gq.AppendLine(gq_mesh.Print());
                sb_gq.AppendLine();
                sb_result.AppendLine(gq_mesh.Print());
                sb_result.AppendLine();

                PrintList(sb_n0, n0_mesh.CameraDataList);
                PrintList(sb_gq, gq_mesh.CameraDataList);

                PrintList(sb_n0, n0_mesh.WaterBoxList);
                PrintList(sb_gq, gq_mesh.WaterBoxList);


                for (int i = 0; i < n0_mesh.VertexList.Count; i++)
                {
                    var vertN0 = n0_mesh.VertexList[i];
                    var vertGQ = gq_mesh.VertexList[i];

                    if (vertN0 != vertGQ)
                    {
                        sb_n0.AppendLine($"Vertex {i:X4}: {vertN0}");
                        sb_gq.AppendLine($"Vertex {i:X4}: {vertGQ}");
                    }
                }

                for (int i = 0; i < n0_mesh.Polys; i++)
                {
                    var n0poly = n0_mesh.PolyList[i];
                    var gqpoly = gq_mesh.PolyList[i];
                    if (!n0poly.Equals(gqpoly))
                    {
                        if (n0poly.VertexFlagsC != gqpoly.VertexFlagsC
                            || n0poly.VertexFlagsB != gqpoly.VertexFlagsB)
                        {
                            sb_n0.AppendLine($"Poly {i:X4}: {n0poly}");
                            sb_gq.AppendLine($"Poly {i:X4}: {gqpoly}");
                        }
                    }
                }

                sb_gq.AppendLine();
            }
            string result = sb_result.ToString();
            face.OutputText(result);
            face.OutputText(sb_n0 + "\n~~~~~~~~~~~~~~~~~\n" + sb_gq);
        }

        private static void SetN0Addresses(List<Scene_GQJson> scenes, List<Scene_GQJson> n0_scenes)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                var sA = scenes[i];
                var sB = n0_scenes[i];

                sA.File.Start = sB.File.Start;
                sA.File.End = sB.File.End;

                for (int j = 0; j < sA.Rooms.Count; j++)
                {
                    var rA = sA.Rooms[j];
                    var rB = sB.Rooms[j];

                    rA.File.Start = rB.File.Start;
                    rA.File.End = rB.File.End;
                }
            }
        }



        public static void GQImportMapData(IExperimentFace face, List<string> files)
        {
            Rom n0 = new ORom(files[0], ORom.Build.N0);
            Rom gq = new ORom(files[1], ORom.Build.GQU);

            var scenes = Load_Scene_GQJson();

            const int dung_mark_vrom_gq = 0xBFABBC;
            N64Ptr dung_mark_vram_gq = 0x8085D2DC;

            var Minimaps = GetDungeonMinimaps(gq, dung_mark_vrom_gq, dung_mark_vram_gq);

            const int floor_index_gq = 0xB6C934;
            const int floor_data_gq = 0xBC7E00;

            var FloorMap = GetDungeonFloorData(gq, floor_index_gq, floor_data_gq);

            for (int i = 0; i < 10; i++)
            {
                scenes[i].Floormaps = FloorMap[i];
                scenes[i].Minimaps = Minimaps[i];
            }

            var ms = SerializeScenes(scenes);

            StreamReader sr = new StreamReader(ms);
            
            face.OutputText(sr.ReadToEnd());
        }

        private static List<List<DungeonMinimap>> GetDungeonMinimaps(Rom rom, int dung_mark_vrom, N64Ptr dung_mark_vram)
        {
            //uint map_mark_vram_n0 = 0x808567F0;
            //uint map_mark_rom_n0 = 0xBF40D0;
            //uint map_mark_table_n0 = 0xBFABBC;
            //uint minimaps_per_dungeon_n0 = 0xB6C794;
            List<List<DungeonMinimap>> result = new List<List<DungeonMinimap>>();

            var map_mark_record = rom.Files.GetFileStart(dung_mark_vrom).VRom;
            var map_mark_file = rom.Files.GetFile(map_mark_record);
            var dung_mark_off = map_mark_file.Record.GetRelativeAddress(dung_mark_vrom);
            N64Ptr map_mark_vram_start = (dung_mark_vram - dung_mark_off);

            List<N64Ptr> dung_ptr_data = new List<N64Ptr>();

            using (BinaryReader br = new BinaryReader(map_mark_file))
            {
                br.BaseStream.Position = dung_mark_off;
                for (int i = 0; i < 10; i++)
                {
                    dung_ptr_data.Add(br.ReadBigInt32());
                }

                dung_ptr_data.Add(dung_mark_vram);

                for (int i = 0; i < 10; i++)
                {
                    List<DungeonMinimap> m = new List<DungeonMinimap>();
                    var minimaps = (dung_ptr_data[i + 1] - dung_ptr_data[i]) / 0x72;

                    br.BaseStream.Position = dung_ptr_data[i] - map_mark_vram_start;

                    for (int j = 0; j < minimaps; j++)
                    {
                        m.Add(new DungeonMinimap(br));
                    }
                    result.Add(m);
                }
            }
            return result;
        }

        private static List<List<DungeonFloor>> GetDungeonFloorData(Rom rom, int floor_index_vrom, int floor_data_vrom)
        {
            const int max_floors = (0x42 / 2) + 1;
            List<short> floorIndices = new List<short>();

            List<List<DungeonFloor>> result = new List<List<DungeonFloor>>();

            var code_file = rom.Files.GetFile(ORom.FileList.code);
            using (BinaryReader br = new BinaryReader(code_file))
            {
                br.BaseStream.Position = code_file.Record.GetRelativeAddress(floor_index_vrom);
                for (int i = 0; i < 10; i++)
                {
                    short index = (short)(br.ReadBigInt16() / 2);
                    floorIndices.Add(index);
                }
            }

            floorIndices.Add(max_floors);

            var kaliedo_addr = rom.Files.GetPlayPauseAddress(0);
            var kaliedo_file = rom.Files.GetFile(kaliedo_addr);

            List<DungeonFloor> floors = new List<DungeonFloor>();

            //read dungeons
            using (BinaryReader br = new BinaryReader(kaliedo_file))
            {
                br.BaseStream.Position = kaliedo_file.Record.GetRelativeAddress(floor_data_vrom);
                for (int i = 0; i < max_floors; i++)
                {
                    floors.Add(new DungeonFloor(br));
                }
            }

            //build dungeon list
            for (int sceneId = 0; sceneId < floorIndices.Count-1; sceneId++)
            {
                int index = floorIndices[sceneId];
                int end = floorIndices[sceneId + 1];

                List<DungeonFloor> dung = new List<DungeonFloor>();

                for (int i = index; i < end; i++)
                {
                    dung.Add(floors[i]);
                }
                result.Add(dung);
            }

            return result;
        }



        public static void GetActors(IExperimentFace face, List<string> files)
        {

        }
    }
}
