namespace CoreBoy.memory
{
    public interface IRegister
    {
        int GetAddress();
        RegisterType GetRegisterType();
    }
}