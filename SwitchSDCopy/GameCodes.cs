using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SwitchSDCopy
{
    public class GameCodes
    {

        public static void LoadGameCodes()
        {
            string codeFileName = "GameCodes.txt";
            if (!File.Exists(codeFileName))
            {
                var codeSave = presetCodes.Select<GameCode, string>((gc) => gc.code + " " + gc.title);
                File.WriteAllLines(codeFileName, codeSave);

                gCodes = presetCodes.ToList();
            }
            else
            {
                List<GameCode> readCodes = new List<GameCode>();
                try
                {
                    foreach (var line in File.ReadLines(codeFileName))
                    {
                        if (line.Count() == 0) continue;
                        if (line[0] == '#') continue;

                        string code = line.Substring(0, 32);
                        string title = line.Remove(0, 33);

                        readCodes.Add(new GameCode(title, code));
                    }
                    gCodes = readCodes;
                }
                catch (Exception)
                {
                    gCodes = presetCodes.ToList();
                }
            }

            try
            {
                Codes = gCodes.ToDictionary(x => x.code, x => x.title);
            }
            catch (Exception) {
                AppUtil.ShowMessageWarning("Game codes could not be obtained.","Error reading GameCodes.txt");
                Codes = presetCodes.ToDictionary(x => x.code, x => x.title);
            }
        }


        public struct GameCode
        {
            public string title;
            public string code;

            public GameCode(string title, string code)
            {
                this.title = title;
                this.code = code;
            }
        }

        private static readonly GameCode[] presetCodes = new GameCode[]{
            new GameCode("Minecraft BE", "11B64E28AD7A49CA9EC8AC007BE858C6"),
            new GameCode("Super Mario Odyssey", "8AEDFF741E2D23FBED39474178692DAF"),
            new GameCode("Splatoon 2", "397A963DA4660090D65D330174AC6B04"),
            new GameCode("Octopath Traveler", "93C1C73A3BAF9123A15B9B24886B634B"),
            new GameCode("Pokémon Quest", "E4B364C957D95017CA1171810D655865"),
            new GameCode("Stardew Valley", "C2B49A475DF5A340494292A1BD398579"),
            new GameCode("Arena of Valor", "A67942211CD1A968913B304D86B5486A"),
            new GameCode("Xenoblade Chronicles 2", "659B13F48903294AE2B3FA4F12DA9898"),
            new GameCode("Super Smash Brothers Ultimate", "0E7DF678130F4F0FA2C88AE72B47AFDF"),
            new GameCode("New Super Mario Bros U Deluxe", "33B8CC310F76D76B17C6DB0011A75C8A"),
            new GameCode("Pokémon Let's Go Eevee", "5F25EBBAB5987964E56ADA5BBDDE9DF2"),
            new GameCode("Smite", "52C46D01F00CD095B1AC33891F8D879C"),
            new GameCode("Diablo 3", "3D69A7ED02A1FF371048829E22A49194"),
            new GameCode("Fortnite", "F489C99A244DF57DCBDC4BFD2DB926F1"),
            new GameCode("Legend of Zelda BOTW", "F1C11A22FAEE3B82F21B330E1B786A39"),
            new GameCode("NES Switch Online", "8F655652CF5441D7471D936F3F07324D"),
            new GameCode("Skyrim", "74EA5D8C57EB2F39A242F585A490F51B"),
            new GameCode("Super Smash Brothers Ultimate - Replays", "C6D726972790F87F6521C61FBA400A1D"),
            new GameCode("Starlink - Battle for Atlas","AF92B7A16C36C3E5DE1C76E5B20E0421"),
            new GameCode("Fallout Shelter","40E06CD7167A6EF5742F08F9E8D2B84E"),
            new GameCode("Switch Menu","57B4628D2267231D57E0FC1078C0596D"),
            new GameCode("Switch Menu","1E95E5926F1CB99A87326D927F27B47E")
        };

        private static List<GameCode> gCodes = null;

        public static Dictionary<string, string> Codes = null;
    }
}
