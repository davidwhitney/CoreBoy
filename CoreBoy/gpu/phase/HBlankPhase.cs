namespace CoreBoy.gpu.phase
{
    public class HBlankPhase : GpuPhase
    {

        private int ticks;

        public HBlankPhase start(int ticksInLine)
        {
            ticks = ticksInLine;
            return this;
        }

        public bool tick()
        {
            ticks++;
            return ticks < 456;
        }

    }
}