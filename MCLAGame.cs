using CodeX.Core.Engine;
using CodeX.Games.MCLA.RPF3;

namespace CodeX.Games.MCLA
{
    public class MCLAGame : Game
    {
        public override string Name => "Midnight Club: Los Angeles";
        public override string ShortName => "MCLA";
        public override string GameFolder { get => GameFolderSetting.GetString(); set => GameFolderSetting.Set(value); }
        public override string GamePathPrefix => "MCLA\\";
        public override bool GameFolderOk => Directory.Exists(GameFolder);
        public override bool RequiresGameFolder => true;
        public override bool Enabled { get => GameEnabledSetting.GetBool(); set => GameEnabledSetting.Set(value); }
        public override bool EnableMapView => true;
        public override FileTypeIcon Icon => FileTypeIcon.Hotdog;
        public override string HashAlgorithm => "Jenkins";

        public static Setting GameFolderSetting = Settings.Register("MCLA.GameFolder", SettingType.String, "C:\\XboxGames\\MCLA");
        public static Setting GameEnabledSetting = Settings.Register("MCLA.Enabled", SettingType.Bool, true);

        public override bool CheckGameFolder(string folder)
        {
            return Directory.Exists(folder);
        }

        public override bool AutoDetectGameFolder(out string source)
        {
            source = null;
            return false;
        }

        public override FileManager CreateFileManager()
        {
            return new Rpf3FileManager(this);
        }

        public override Level GetMapLevel()
        {
            return null;
        }

        public override Setting[] GetMapSettings()
        {
            return null;
        }
    }
}