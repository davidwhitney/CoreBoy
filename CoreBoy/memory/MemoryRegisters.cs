using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreBoy.memory
{
    public class MemoryRegisters : AddressSpace
    {
        private readonly Dictionary<int, IRegister> _registers;
        private readonly Dictionary<int, int> _values = new Dictionary<int, int>();

        public MemoryRegisters(params IRegister[] registers)
        {
            var map = new Dictionary<int, IRegister>();
            foreach (var r in registers)
            {
                if (map.ContainsKey(r.GetAddress()))
                {
                    throw new ArgumentException("Two registers with the same address: " + r.GetAddress());
                }

                map.Add(r.GetAddress(), r);
                _values.Add(r.GetAddress(), 0);
            }

            _registers = map;
        }

        private MemoryRegisters(MemoryRegisters original)
        {
            _registers = original._registers;
            _values = new Dictionary<int, int>(original._values);
        }

        public int Get(IRegister reg)
        {
            return _registers.ContainsKey(reg.GetAddress())
                ? _values[reg.GetAddress()]
                : throw new ArgumentException("Not valid register: " + reg);
        }

        public void Put(IRegister reg, int value)
        {
            _values[reg.GetAddress()] = _registers.ContainsKey(reg.GetAddress())
                ? value
                : throw new ArgumentException("Not valid register: " + reg);
        }

        public MemoryRegisters Freeze() => new MemoryRegisters(this);

        public int PreIncrement(IRegister reg)
        {
            if (!_registers.ContainsKey(reg.GetAddress()))
            {
                throw new ArgumentException("Not valid register: " + reg);
            }

            var value = _values[reg.GetAddress()] + 1;
            _values[reg.GetAddress()] = value;
            return value;
        }

        public bool accepts(int address) => _registers.ContainsKey(address);

        public void setByte(int address, int value)
        {
            var allowsWrite = new[] { RegisterType.W, RegisterType.RW };
            var regType = _registers[address].GetRegisterType();
            if (allowsWrite.Contains(regType))
            {
                _values[address] = value;
            }
        }

        public int getByte(int address)
        {
            var allowsRead = new[] { RegisterType.R, RegisterType.RW };
            var regType = _registers[address].GetRegisterType(); 
            return allowsRead.Contains(regType) ? _values[address] : 0xff;
        }
    }
}

