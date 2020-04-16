namespace CoreBoy.memory.cart.battery
{
    public class NullBattery : IBattery
    {
        public void LoadRam(int[] ram)
        {
        }

        public void SaveRam(int[] ram)
        {
        }

        public void LoadRamWithClock(int[] ram, long[] clockData)
        {
        }

        public void SaveRamWithClock(int[] ram, long[] clockData)
        {
        }
    }
}