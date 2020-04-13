namespace eu.rekawek.coffeegb.gpu.phase
{
    public class VBlankPhase : GpuPhase
    {
        private int ticks;

        public VBlankPhase start()
        {
            ticks = 0;
            return this;
        }

        public bool tick()
        {
            return ++ticks < 456;
        }
    }
}