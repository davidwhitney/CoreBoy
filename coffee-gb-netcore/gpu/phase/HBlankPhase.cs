namespace eu.rekawek.coffeegb.gpu.phase
{
    public class HBlankPhase : GpuPhase
    {

        private int ticks;

        public HBlankPhase start(int ticksInLine)
        {
            this.ticks = ticksInLine;
            return this;
        }

        public bool tick()
        {
            ticks++;
            return ticks < 456;
        }

    }
}