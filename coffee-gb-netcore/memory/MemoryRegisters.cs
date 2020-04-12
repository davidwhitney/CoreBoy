using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.rekawek.coffeegb.memory
{
    public enum RegisterType
    {
        R,
        W,
        RW
    }

    public interface Register
    {
        int getAddress();
        RegisterType getType();
    }

    public class MemoryRegisters : AddressSpace
    {

        private Dictionary<int, Register> registers;

        private Dictionary<int, int> values = new Dictionary<int, int>();

        public MemoryRegisters(params Register[] registers)
        {
            var map = new Dictionary<int, Register>();
            foreach (var r in registers)
            {
                if (map.ContainsKey(r.getAddress()))
                {
                    throw new ArgumentException("Two registers with the same address: " + r.getAddress());
                }

                map.Add(r.getAddress(), r);
                values.Add(r.getAddress(), 0);
            }

            this.registers = map;
        }

        private MemoryRegisters(MemoryRegisters original)
        {
            this.registers = original.registers;
            this.values = new Dictionary<int, int>(original.values);
        }

        public int get(Register reg)
        {
            if (registers.ContainsKey(reg.getAddress()))
            {
                return values[reg.getAddress()];
            }
            else
            {
                throw new ArgumentException("Not valid register: " + reg);
            }
        }

        public void put(Register reg, int value)
        {
            if (registers.ContainsKey(reg.getAddress()))
            {
                values[reg.getAddress()] = value;
            }
            else
            {
                throw new ArgumentException("Not valid register: " + reg);
            }
        }

        public MemoryRegisters freeze()
        {
            return new MemoryRegisters(this);
        }

        public int preIncrement(Register reg)
        {
            if (registers.ContainsKey(reg.getAddress()))
            {
                int value = values[reg.getAddress()] + 1;
                values[reg.getAddress()] = value;
                return value;
            }
            else
            {
                throw new ArgumentException("Not valid register: " + reg);
            }
        }

        public bool accepts(int address)
        {
            return registers.ContainsKey(address);
        }

        public void setByte(int address, int value)
        {
            var allowsWrite = new[] { RegisterType.W, RegisterType.RW };
            var regType = registers[address].getType();
            if (allowsWrite.Contains(regType))
            {
                values[address] = value;
            }
        }

        public int getByte(int address)
        {
            var allowsRead = new[] { RegisterType.R, RegisterType.RW };
            var regType = registers[address].getType(); 
            return allowsRead.Contains(regType) ? values[address] : 0xff;
        }
    }
}

