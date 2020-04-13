namespace eu.rekawek.coffeegb.sound
{
    public interface SoundOutput
    {

        void start();

        void stop();

        void play(int left, int right);

    }

    public class NullSoundOutput : SoundOutput
    {
        public void start()
        {
        }

        public void stop()
        {
        }

        public void play(int left, int right)
        {
        }
    }
}