namespace eu.rekawek.coffeegb.memory.cart.battery
{
	public interface Battery
    {
        void loadRam(int[] ram);

        void saveRam(int[] ram);

        void loadRamWithClock(int[] ram, long[] clockData);

        void saveRamWithClock(int[] ram, long[] clockData);

    }

    public class NullBattery : Battery
    {
        public void loadRam(int[] ram)
        {
        }

        public void saveRam(int[] ram)
        {
        }

        public void loadRamWithClock(int[] ram, long[] clockData)
        {
        }

        public void saveRamWithClock(int[] ram, long[] clockData)
        {
        }
    }
}