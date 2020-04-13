using System;
using eu.rekawek.coffeegb.gpu;

namespace eu.rekawek.coffeegb.cpu.op
{
    public abstract class Op
    {
        public virtual bool readsMemory()
        {
            return false;
        }

        public virtual bool writesMemory()
        {
            return false;
        }

        public virtual int operandLength()
        {
            return 0;
        }

        public virtual int execute(Registers registers, AddressSpace addressSpace, int[] args, int context)
        {
            return context;
        }

        public virtual void switchInterrupts(InterruptManager interruptManager)
        {
        }

        public virtual bool proceed(Registers registers)
        {
            return true;
        }

        public virtual bool forceFinishCycle()
        {
            return false;
        }

        public virtual SpriteBug.CorruptionType? causesOemBug(Registers registers, int context)
        {
            return null;
        }
    }
}