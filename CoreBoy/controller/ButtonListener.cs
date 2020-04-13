namespace CoreBoy.controller
{
    public class Button
    {
        public static Button RIGHT = new Button(0x01, 0x10);
        public static Button LEFT = new Button(0x02, 0x10);
        public static Button UP = new Button(0x04, 0x10);
        public static Button DOWN = new Button(0x08, 0x10);
        public static Button A = new Button(0x01, 0x20);
        public static Button B = new Button(0x02, 0x20);
        public static Button SELECT = new Button(0x04, 0x20);
        public static Button START = new Button(0x08, 0x20);

        private int mask;

        private int line;

        public Button(int mask, int line)
        {
            this.mask = mask;
            this.line = line;
        }

        public int getMask()
        {
            return mask;
        }

        public int getLine()
        {
            return line;
        }
    }

    public interface ButtonListener
    {
        void onButtonPress(Button button);

        void onButtonRelease(Button button);
    }
}