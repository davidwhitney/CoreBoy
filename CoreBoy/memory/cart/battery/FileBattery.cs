using System.IO;
using Newtonsoft.Json;

namespace CoreBoy.memory.cart.battery
{
    public class FileBattery : IBattery
    {
        private readonly FileInfo _saveFile;

        public FileBattery(string romName)
        {
            _saveFile = new FileInfo($"{romName}.sav.json");
        }
        
        public void LoadRam(int[] ram)
        {
            if (!_saveFile.Exists)
            {
                return;
            }

            var loaded = JsonConvert.DeserializeObject<SaveState>(File.ReadAllText(_saveFile.FullName));
            loaded.RAM.CopyTo(ram, 0);
        }

        public void LoadRamWithClock(int[] ram, long[] clockData)
        {
            if (!_saveFile.Exists)
            {
                return;
            }

            var loaded = JsonConvert.DeserializeObject<SaveState>(File.ReadAllText(_saveFile.FullName));
            loaded.RAM.CopyTo(ram, 0);
            loaded.ClockData.CopyTo(clockData, 0);
        }

        public void SaveRam(int[] ram)
        {
            SaveRamWithClock(ram, null);
        }

        public void SaveRamWithClock(int[] ram, long[] clockData)
        {
            var dto = new SaveState { RAM = ram, ClockData = clockData };
            var asText = JsonConvert.SerializeObject(dto);
            File.WriteAllText(_saveFile.FullName, asText);
        }

        public class SaveState
        {
            public int[] RAM { get; set; }
            public long[] ClockData { get; set; }
        }
    }
}