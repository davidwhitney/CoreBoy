namespace CoreBoy.controller
{
    public class Button
    {
        public static Button Right = new Button(0x01, 0x10);
        public static Button Left = new Button(0x02, 0x10);
        public static Button Up = new Button(0x04, 0x10);
        public static Button Down = new Button(0x08, 0x10);
        public static Button A = new Button(0x01, 0x20);
        public static Button B = new Button(0x02, 0x20);
        public static Button Select = new Button(0x04, 0x20);
        public static Button Start = new Button(0x08, 0x20);

        public int Mask { get; }
        public int Line { get; }

        public Button(int mask, int line)
        {
            Mask = mask;
            Line = line;
        }
    }
}