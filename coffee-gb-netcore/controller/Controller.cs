namespace eu.rekawek.coffeegb.controller
{
    public interface Controller
    {
        void setButtonListener(ButtonListener listener);
    }

    public class NullController : Controller
    {
        public void setButtonListener(ButtonListener listener)
        {
        }
    }

}